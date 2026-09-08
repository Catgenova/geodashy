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
    }

    /// <summary>Saves and loads levels as JSON in the persistent data folder. Built-in levels live in Resources/Levels.</summary>
    public static class LevelStorage
    {
        public static string LevelsDirectory
        {
            get
            {
                var dir = Path.Combine(Application.persistentDataPath, "geodashy", "levels");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                return dir;
            }
        }

        public static string BackupDirectory
        {
            get
            {
                var dir = Path.Combine(Application.persistentDataPath, "geodashy", "backups");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                return dir;
            }
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

        public static List<LevelFileInfo> ListLevels()
        {
            var list = new List<LevelFileInfo>();
            foreach (var ta in Resources.LoadAll<TextAsset>("Levels"))
            {
                if (LevelSerializer.TryFromJson(ta.text, out var d, out _))
                {
                    list.Add(new LevelFileInfo
                    {
                        path = "res:" + ta.name, id = d.id, name = d.name, author = d.author, objectCount = d.objects.Count,
                        modified = DateTimeOffset.FromUnixTimeSeconds(d.modifiedUnix).DateTime, builtIn = true
                    });
                }
            }
            foreach (var file in Directory.GetFiles(LevelsDirectory, "*.json"))
            {
                try
                {
                    var d = LevelSerializer.FromJson(File.ReadAllText(file));
                    list.Add(new LevelFileInfo
                    {
                        path = file, id = d.id, name = d.name, author = d.author, objectCount = d.objects.Count,
                        modified = File.GetLastWriteTime(file)
                    });
                }
                catch (Exception e)
                {
                    Debug.LogWarning("Skipping unreadable level " + file + ": " + e.Message);
                }
            }
            list.Sort((a, b) =>
            {
                if (a.builtIn != b.builtIn) return a.builtIn ? 1 : -1;
                return b.modified.CompareTo(a.modified);
            });
            return list;
        }
    }
}
