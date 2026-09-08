using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace Geodashy.Editing.UI
{
    /// <summary>A small in-game file picker (Unity has none at runtime). Folders first, then files matching the extension filter.</summary>
    public static class FileBrowserDialog
    {
        const string LastFolderPref = "geodashy.fileBrowser.lastFolder";

        public static void Open(EditorUI ui, string title, string[] extensions, Action<string> onPick)
        {
            var c = ui.OpenModal(title, 860, 640);
            var modal = ui.TopModal;
            string folder = PlayerPrefs.GetString(LastFolderPref, "");
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder)) folder = DefaultFolder();
            // declared up front: the quick-link buttons capture Navigate, which uses these
            ScrollRect scroll = null;
            RectTransform list = null;

            var pathRow = UIFactory.Row(c, 34, 6);
            var up = UIFactory.Button(pathRow, "↑ Up", null, 70, 32);
            var pathInput = UIFactory.Input(pathRow, "folder path", folder, null, -1, 32);
            var go = UIFactory.Button(pathRow, "Go", null, 60, 32);

            var quick = UIFactory.Row(c, 30, 4);
            UIFactory.Label(quick, "Go to:", 12, TextAnchor.MiddleLeft, UIFactory.TextDim, 44, 30);
            foreach (var q in QuickLinks())
            {
                var target = q.Value;
                UIFactory.Button(quick, q.Key, () => Navigate(target), -1, 28, null, 12);
            }

            var filterLabel = UIFactory.Label(c, "Showing " + string.Join(", ", extensions) + " files. Click a file to choose it.", 12, TextAnchor.MiddleLeft, UIFactory.TextDim, -1, 20);
            scroll = UIFactory.ScrollView(c, "Entries", out list, true, false);
            UIFactory.VLayout(list, 2, 4);
            UIFactory.Fitter(list, true, false);
            var bottom = UIFactory.Row(c, 34, 6, TextAnchor.MiddleRight);
            UIFactory.Button(bottom, "Cancel", () => ui.CloseModal(modal), 110, 32);

            void Navigate(string dir)
            {
                if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
                {
                    ui.Toast("Folder not found");
                    return;
                }
                folder = dir;
                pathInput.SetTextWithoutNotify(folder);
                PlayerPrefs.SetString(LastFolderPref, folder);
                Populate();
            }

            void Populate()
            {
                foreach (Transform child in list) UnityEngine.Object.Destroy(child.gameObject);
                string[] dirs, files;
                try
                {
                    dirs = Directory.GetDirectories(folder);
                    files = Directory.GetFiles(folder);
                }
                catch (Exception e)
                {
                    UIFactory.Label(list, "Cannot read folder: " + e.Message, 13, TextAnchor.MiddleLeft, UIFactory.Danger, -1, 28);
                    return;
                }
                Array.Sort(dirs, StringComparer.OrdinalIgnoreCase);
                Array.Sort(files, StringComparer.OrdinalIgnoreCase);
                int shown = 0;
                foreach (var d in dirs)
                {
                    var name = Path.GetFileName(d);
                    if (name.StartsWith(".")) continue;
                    var target = d;
                    var b = UIFactory.Button(list, "📁  " + name, () => Navigate(target), -1, 28, UIFactory.PanelBg3, 13);
                    b.GetComponentInChildren<Text>().alignment = TextAnchor.MiddleLeft;
                    shown++;
                }
                foreach (var f in files)
                {
                    var ext = Path.GetExtension(f).ToLowerInvariant();
                    if (Array.IndexOf(extensions, ext) < 0) continue;
                    var target = f;
                    long size = 0;
                    try
                    {
                        size = new FileInfo(f).Length;
                    }
                    catch (Exception)
                    {
                    }
                    var b = UIFactory.Button(list, "♪  " + Path.GetFileName(f) + "    (" + (size / 1024f / 1024f).ToString("0.0") + " MB)", () =>
                    {
                        PlayerPrefs.SetString(LastFolderPref, folder);
                        ui.CloseModal(modal);
                        onPick?.Invoke(target);
                    }, -1, 28, null, 13);
                    b.GetComponentInChildren<Text>().alignment = TextAnchor.MiddleLeft;
                    shown++;
                }
                if (shown == 0) UIFactory.Label(list, "Nothing here matches. Try another folder.", 13, TextAnchor.MiddleLeft, UIFactory.TextDim, -1, 28);
                scroll.verticalNormalizedPosition = 1f;
            }

            up.onClick.AddListener(() =>
            {
                try
                {
                    var parent = Directory.GetParent(folder);
                    if (parent != null) Navigate(parent.FullName);
                    else
                    {
                        // at a drive root: list drives
                        var drives = Directory.GetLogicalDrives();
                        if (drives.Length > 1) ShowDrives(drives);
                    }
                }
                catch (Exception)
                {
                }
            });
            go.onClick.AddListener(() => Navigate(pathInput.text.Trim()));
            pathInput.onEndEdit.AddListener(v => Navigate(v.Trim()));

            void ShowDrives(string[] drives)
            {
                foreach (Transform child in list) UnityEngine.Object.Destroy(child.gameObject);
                foreach (var d in drives)
                {
                    var target = d;
                    UIFactory.Button(list, "💽  " + d, () => Navigate(target), -1, 28, UIFactory.PanelBg3, 13);
                }
            }

            Populate();
        }

        static string DefaultFolder()
        {
            var music = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
            if (!string.IsNullOrEmpty(music) && Directory.Exists(music)) return music;
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrEmpty(home) && Directory.Exists(home)) return home;
            return Directory.GetCurrentDirectory();
        }

        static List<KeyValuePair<string, string>> QuickLinks()
        {
            var links = new List<KeyValuePair<string, string>>();
            void Add(string label, Environment.SpecialFolder sf)
            {
                var p = Environment.GetFolderPath(sf);
                if (!string.IsNullOrEmpty(p) && Directory.Exists(p)) links.Add(new KeyValuePair<string, string>(label, p));
            }
            Add("Home", Environment.SpecialFolder.UserProfile);
            Add("Desktop", Environment.SpecialFolder.DesktopDirectory);
            Add("Documents", Environment.SpecialFolder.MyDocuments);
            Add("Music", Environment.SpecialFolder.MyMusic);
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var downloads = string.IsNullOrEmpty(home) ? null : Path.Combine(home, "Downloads");
            if (downloads != null && Directory.Exists(downloads)) links.Add(new KeyValuePair<string, string>("Downloads", downloads));
            return links;
        }
    }
}
