using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Geodashy.Core
{
    /// <summary>A saved brush: object type plus rotation, scale, flips, colour channel and properties.</summary>
    [Serializable]
    public class BrushPreset
    {
        public string name = "Preset";
        public string type = "";
        public float rotation;
        public float scale = 1f;
        public bool flipX, flipY;
        public int baseColor;
        public List<ObjectProp> props = new List<ObjectProp>();
    }

    /// <summary>Starred palette objects and named presets, kept in geodashy/presets.json next to the saves.</summary>
    public static class PresetStorage
    {
        [Serializable]
        class File_
        {
            public List<string> favourites = new List<string>();
            public List<BrushPreset> presets = new List<BrushPreset>();
        }

        static File_ data;
        public static event Action Changed;

        static string Path_ => Path.Combine(Application.persistentDataPath, "geodashy", "presets.json");

        static File_ Data
        {
            get
            {
                if (data != null) return data;
                try
                {
                    data = File.Exists(Path_) ? JsonUtility.FromJson<File_>(File.ReadAllText(Path_)) : null;
                }
                catch (Exception e)
                {
                    Debug.LogWarning("Could not read presets: " + e.Message);
                }
                if (data == null) data = new File_();
                if (data.favourites == null) data.favourites = new List<string>();
                if (data.presets == null) data.presets = new List<BrushPreset>();
                return data;
            }
        }

        static void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path_));
                File.WriteAllText(Path_, JsonUtility.ToJson(Data));
            }
            catch (Exception e)
            {
                Debug.LogWarning("Could not save presets: " + e.Message);
            }
            Changed?.Invoke();
        }

        public static IReadOnlyList<string> Favourites => Data.favourites;
        public static IReadOnlyList<BrushPreset> Presets => Data.presets;
        public static bool IsFavourite(string type) => Data.favourites.Contains(type);

        public static void ToggleFavourite(string type)
        {
            if (!Data.favourites.Remove(type)) Data.favourites.Add(type);
            Save();
        }

        public static void AddPreset(BrushPreset p)
        {
            Data.presets.Add(p);
            Save();
        }

        public static void RemovePreset(BrushPreset p)
        {
            Data.presets.Remove(p);
            Save();
        }
    }
}
