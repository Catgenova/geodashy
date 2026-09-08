using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Geodashy.Core
{
    /// <summary>Summary of a saved level file used by the load dialog.</summary>
    public class LevelFileInfo
    {
        public string path;
        public string id;
        public string name;
        public string author;
        public int objectCount;
        public DateTime modified;
        public bool builtIn;
        public string description;
        public string startMount;
        public float lengthSeconds;
        public string backgroundTheme;
        public int campaignOrder;
    }

    /// <summary>Display name of the game. Code, folders and save paths keep the internal "geodashy" name.</summary>
    public static class GameInfo
    {
        public const string Title = "LyreFlyer";
    }

    /// <summary>Saves and loads levels as JSON in the persistent data folder. Built-in levels live in Resources/Levels.</summary>
    public static class LevelStorage
    {
        static bool migrated;

        /// <summary>
        /// Unity derives persistentDataPath from the product name. When the title changed from "geodashy",
        /// saves made under the old name are copied across once so nothing is lost.
        /// </summary>
        static void MigrateOldSaves()
        {
            if (migrated) return;
            migrated = true;
            try
            {
                var current = Path.Combine(Application.persistentDataPath, "geodashy");
                if (Directory.Exists(current)) return;
                var parent = Directory.GetParent(Application.persistentDataPath);
                if (parent == null) return;
                foreach (var oldProduct in new[] { "geodashy", "Geodashy" })
                {
                    var old = Path.Combine(parent.FullName, oldProduct, "geodashy");
                    if (!Directory.Exists(old) || string.Equals(Path.GetFullPath(old), Path.GetFullPath(current), StringComparison.OrdinalIgnoreCase)) continue;
                    CopyDirectory(old, current);
                    Debug.Log("Copied saved levels from " + old + " to " + current);
                    return;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("Could not migrate old saves: " + e.Message);
            }
        }

        static void CopyDirectory(string from, string to)
        {
            Directory.CreateDirectory(to);
            foreach (var f in Directory.GetFiles(from)) File.Copy(f, Path.Combine(to, Path.GetFileName(f)), true);
            foreach (var d in Directory.GetDirectories(from)) CopyDirectory(d, Path.Combine(to, Path.GetFileName(d)));
        }

        public static string LevelsDirectory
        {
            get
            {
                MigrateOldSaves();
                var dir = Path.Combine(Application.persistentDataPath, "geodashy", "levels");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                return dir;
            }
        }

        public static string BackupDirectory
        {
            get
            {
                MigrateOldSaves();
                var dir = Path.Combine(Application.persistentDataPath, "geodashy", "backups");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                return dir;
            }
        }

        public static readonly string[] AudioExtensions = { ".mp3", ".ogg", ".wav" };

        /// <summary>Folder holding a level's imported songs.</summary>
        public static string AssetsDirectory(string levelId, bool create = true)
        {
            var dir = Path.Combine(Application.persistentDataPath, "geodashy", "assets", SafeFileName(levelId));
            if (create && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return dir;
        }

        public static string AssetPath(string levelId, string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return null;
            return Path.Combine(AssetsDirectory(levelId, false), fileName);
        }

        public static bool AssetExists(string levelId, string fileName)
        {
            var p = AssetPath(levelId, fileName);
            return p != null && File.Exists(p);
        }

        /// <summary>Copies a file into the level's asset folder and returns the stored file name.</summary>
        public static string ImportAsset(string levelId, string sourcePath)
        {
            if (!File.Exists(sourcePath)) throw new FileNotFoundException("File not found: " + sourcePath);
            var dir = AssetsDirectory(levelId);
            var name = SafeFileName(Path.GetFileNameWithoutExtension(sourcePath));
            var ext = Path.GetExtension(sourcePath).ToLowerInvariant();
            var target = Path.Combine(dir, name + ext);
            int n = 2;
            while (File.Exists(target) && !FilesEqual(target, sourcePath)) target = Path.Combine(dir, name + "_" + n++ + ext);
            if (!File.Exists(target)) File.Copy(sourcePath, target);
            return Path.GetFileName(target);
        }

        static bool FilesEqual(string a, string b)
        {
            try
            {
                var fa = new FileInfo(a);
                var fb = new FileInfo(b);
                return fa.Length == fb.Length && fa.LastWriteTimeUtc == fb.LastWriteTimeUtc;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>Copies every imported asset from one level id to another (Save As).</summary>
        public static void CopyAssets(string fromId, string toId)
        {
            var from = AssetsDirectory(fromId, false);
            if (!Directory.Exists(from)) return;
            var to = AssetsDirectory(toId);
            foreach (var f in Directory.GetFiles(from)) File.Copy(f, Path.Combine(to, Path.GetFileName(f)), true);
        }

        public static void DeleteAssets(string levelId)
        {
            var dir = AssetsDirectory(levelId, false);
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }

        public static string PathFor(LevelData data)
        {
            return Path.Combine(LevelsDirectory, SafeFileName(data.id) + ".json");
        }

        public static string SafeFileName(string s)
        {
            if (string.IsNullOrEmpty(s)) return "level";
            var invalid = Path.GetInvalidFileNameChars();
            var chars = s.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (Array.IndexOf(invalid, chars[i]) >= 0) chars[i] = '_';
            }
            return new string(chars);
        }

        public static void Save(LevelData data)
        {
            data.modifiedUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var json = LevelSerializer.ToJson(data, true);
            var path = PathFor(data);
            var tmp = path + ".tmp";
            File.WriteAllText(tmp, json);
            if (File.Exists(path)) File.Copy(path, Path.Combine(BackupDirectory, SafeFileName(data.id) + ".bak.json"), true);
            if (File.Exists(path)) File.Delete(path);
            File.Move(tmp, path);
        }

        public static void SaveAutosave(LevelData data)
        {
            var json = LevelSerializer.ToJson(data, false);
            File.WriteAllText(Path.Combine(BackupDirectory, "autosave.json"), json);
        }

        public static LevelData LoadAutosave()
        {
            var path = Path.Combine(BackupDirectory, "autosave.json");
            if (!File.Exists(path)) return null;
            return LevelSerializer.TryFromJson(File.ReadAllText(path), out var data, out _) ? data : null;
        }

        public static LevelData Load(string path)
        {
            if (path.StartsWith("res:"))
            {
                var ta = Resources.Load<TextAsset>("Levels/" + path.Substring(4));
                if (ta == null) throw new FileNotFoundException("Built-in level not found: " + path);
                var d = LevelSerializer.FromJson(ta.text);
                return d;
            }
            return LevelSerializer.FromJson(File.ReadAllText(path));
        }

        public static void Delete(string path)
        {
            if (File.Exists(path)) File.Delete(path);
        }

        static LevelFileInfo Describe(LevelData d, string path, DateTime modified, bool builtIn)
        {
            return new LevelFileInfo
            {
                path = path, id = d.id, name = d.name, author = d.author, objectCount = d.objects.Count, modified = modified, builtIn = builtIn,
                description = d.description, startMount = d.settings.startMount, backgroundTheme = d.settings.backgroundTheme,
                lengthSeconds = d.GetFinishX() / MountCatalog.Speed(d.settings.startSpeed), campaignOrder = d.campaignOrder
            };
        }

        public static List<LevelFileInfo> ListLevels()
        {
            var list = new List<LevelFileInfo>();
            foreach (var ta in Resources.LoadAll<TextAsset>("Levels"))
            {
                if (LevelSerializer.TryFromJson(ta.text, out var d, out _))
                {
                    list.Add(Describe(d, "res:" + ta.name, DateTimeOffset.FromUnixTimeSeconds(d.modifiedUnix).DateTime, true));
                }
            }
            foreach (var file in Directory.GetFiles(LevelsDirectory, "*.json"))
            {
                try
                {
                    var d = LevelSerializer.FromJson(File.ReadAllText(file));
                    list.Add(Describe(d, file, File.GetLastWriteTime(file), false));
                }
                catch (Exception e)
                {
                    Debug.LogWarning("Skipping unreadable level " + file + ": " + e.Message);
                }
            }
            list.Sort((a, b) =>
            {
                if (a.builtIn != b.builtIn) return a.builtIn ? -1 : 1;
                if (a.builtIn)
                {
                    int c = a.campaignOrder.CompareTo(b.campaignOrder);
                    return c != 0 ? c : string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase);
                }
                return b.modified.CompareTo(a.modified);
            });
            return list;
        }

        // ---- starter songs & campaign export ---------------------------------------

        /// <summary>Names of every clip in Resources/Songs (the ids used by songId and Song triggers).</summary>
        public static string[] StarterSongIds()
        {
            var clips = Resources.LoadAll<AudioClip>("Songs");
            var ids = new List<string>();
            foreach (var c in clips) ids.Add(c.name);
            ids.Sort(StringComparer.OrdinalIgnoreCase);
            return ids.ToArray();
        }

        /// <summary>Where campaign levels live inside the Unity project (editor only).</summary>
        public static string CampaignLevelsFolder => Path.Combine(Application.dataPath, "Geodashy", "Resources", "Levels");
        public static string CampaignSongsFolder => Path.Combine(Application.dataPath, "Geodashy", "Resources", "Songs");

        /// <summary>
        /// Writes the level as a campaign map. In the Unity editor it lands in Resources/Levels (and an imported song
        /// is copied into Resources/Songs); in a build it goes to a folder you can commit by hand. Returns the file path.
        /// </summary>
        public static string ExportToCampaign(LevelData source)
        {
            var copy = source.DeepClone();
            var fileName = SafeFileName(string.IsNullOrWhiteSpace(copy.name) ? "level" : copy.name.Trim().ToLowerInvariant().Replace(' ', '_')) + ".json";
            string folder;
            if (Application.isEditor)
            {
                folder = CampaignLevelsFolder;
                Directory.CreateDirectory(folder);
                if (!string.IsNullOrEmpty(copy.settings.songFile))
                {
                    var src = AssetPath(source.id, copy.settings.songFile);
                    if (src != null && File.Exists(src))
                    {
                        Directory.CreateDirectory(CampaignSongsFolder);
                        var songName = SafeFileName(Path.GetFileNameWithoutExtension(copy.settings.songFile).ToLowerInvariant().Replace(' ', '_'));
                        File.Copy(src, Path.Combine(CampaignSongsFolder, songName + Path.GetExtension(src).ToLowerInvariant()), true);
                        copy.settings.songId = songName;
                    }
                    copy.settings.songFile = "";
                }
            }
            else
            {
                folder = Path.Combine(Application.persistentDataPath, "geodashy", "campaign-exports");
                Directory.CreateDirectory(folder);
            }
            copy.playtestX = -1f;
            copy.playtestY = -1f;
            var path = Path.Combine(folder, fileName);
            File.WriteAllText(path, LevelSerializer.ToJson(copy, true));
#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif
            return path;
        }
    }
}
