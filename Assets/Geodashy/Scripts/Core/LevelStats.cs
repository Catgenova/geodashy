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
        /// <summary>Champion clears.</summary>
        public int completions;
        /// <summary>Clears on the Checkpoints difficulty.</summary>
        public int checkpointCompletions;
        /// <summary>Progress fractions (0..1) of recent deaths from full runs, oldest first.</summary>
        public List<float> deaths = new List<float>();
        /// <summary>Every piece of loot gathered in one Champion or Checkpoints clear.</summary>
        public bool fullLoot;
        /// <summary>Positions of the personal-best Champion run sampled every BestRunStep seconds.</summary>
        public List<float> bestRunX = new List<float>();
        public List<float> bestRunY = new List<float>();
        public float bestRunProgress;
        public const float BestRunStep = 0.05f;
        /// <summary>Best Champion clears, fastest first (at most five).</summary>
        public List<RunRecord> bestRuns = new List<RunRecord>();

        public void AddRun(RunRecord r)
        {
            if (bestRuns == null) bestRuns = new List<RunRecord>();
            bestRuns.Add(r);
            bestRuns.Sort((a, b) => a.seconds.CompareTo(b.seconds));
            if (bestRuns.Count > 5) bestRuns.RemoveRange(5, bestRuns.Count - 5);
        }

        /// <summary>Death causes (object names) and how often each ended a full run.</summary>
        public List<string> killerNames = new List<string>();
        public List<int> killerCounts = new List<int>();

        public void RecordDeath(float progress)
        {
            deaths.Add(Mathf.Clamp01(progress));
            if (deaths.Count > MaxDeaths) deaths.RemoveRange(0, deaths.Count - MaxDeaths);
        }

        public void RecordKiller(string name)
        {
            if (string.IsNullOrEmpty(name)) name = "the world";
            int i = killerNames.IndexOf(name);
            if (i >= 0) killerCounts[i]++;
            else
            {
                killerNames.Add(name);
                killerCounts.Add(1);
            }
        }

        /// <summary>Most frequent death cause and its count (empty when nothing recorded).</summary>
        public string TopKiller(out int count)
        {
            count = 0;
            string best = "";
            for (int i = 0; i < killerNames.Count && i < killerCounts.Count; i++) if (killerCounts[i] > count) { count = killerCounts[i]; best = killerNames[i]; }
            return best;
        }

        /// <summary>Deaths per progress bucket (buckets of 1/n) from the recorded full runs.</summary>
        public int[] DeathHistogram(int buckets)
        {
            var h = new int[Mathf.Max(1, buckets)];
            foreach (var d in deaths) h[Mathf.Clamp(Mathf.FloorToInt(d * h.Length), 0, h.Length - 1)]++;
            return h;
        }

        public float AverageDeathProgress()
        {
            if (deaths.Count == 0) return 0f;
            float sum = 0f;
            foreach (var d in deaths) sum += d;
            return sum / deaths.Count;
        }
    }

    [Serializable]
    public class RunRecord
    {
        public string rider = "";
        public float seconds;
        public long dateUnix;
        public string medals = "";
        public int attempts;
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
                        if (s.bestRunX == null) s.bestRunX = new List<float>();
                        if (s.bestRunY == null) s.bestRunY = new List<float>();
                        if (s.killerNames == null) s.killerNames = new List<string>();
                        if (s.killerCounts == null) s.killerCounts = new List<int>();
                        if (s.bestRuns == null) s.bestRuns = new List<RunRecord>();
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
