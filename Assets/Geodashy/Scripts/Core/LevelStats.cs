using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Geodashy.Core
{
    /// <summary>Per-level play statistics: where the rider died and the best progress reached. Stored next to the levels.</summary>
    [Serializable]
    public class LevelStats
    {
        public const int MaxDeaths = 400;

        public string levelId = "";
        /// <summary>Best progress (0..1) reached from the level start.</summary>
        public float bestProgress;
        public int attempts;
        public int completions;
        /// <summary>Progress fractions (0..1) of recent deaths from full runs, oldest first.</summary>
        public List<float> deaths = new List<float>();

        public void RecordDeath(float progress)
        {
            deaths.Add(Mathf.Clamp01(progress));
            if (deaths.Count > MaxDeaths) deaths.RemoveRange(0, deaths.Count - MaxDeaths);
        }
    }

    public static class LevelStatsStorage
    {
        static string Directory_
        {
            get
            {
                var dir = Path.Combine(Application.persistentDataPath, "geodashy", "stats");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                return dir;
            }
        }

        static string PathFor(string levelId) => Path.Combine(Directory_, LevelStorage.SafeFileName(levelId) + ".json");

        public static LevelStats Load(string levelId)
        {
            try
            {
                var path = PathFor(levelId);
                if (File.Exists(path))
                {
                    var s = JsonUtility.FromJson<LevelStats>(File.ReadAllText(path));
                    if (s != null)
                    {
                        if (s.deaths == null) s.deaths = new List<float>();
                        s.levelId = levelId;
                        return s;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("Could not read level stats: " + e.Message);
            }
            return new LevelStats { levelId = levelId };
        }

        public static void Save(LevelStats stats)
        {
            try
            {
                File.WriteAllText(PathFor(stats.levelId), JsonUtility.ToJson(stats));
            }
            catch (Exception e)
            {
                Debug.LogWarning("Could not save level stats: " + e.Message);
            }
        }

        public static void Reset(string levelId)
        {
            var path = PathFor(levelId);
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
