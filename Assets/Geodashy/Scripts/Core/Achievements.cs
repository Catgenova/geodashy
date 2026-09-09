using System;
using System.Collections.Generic;
using UnityEngine;

namespace Geodashy.Core
{
    /// <summary>Medieval-flavoured achievements, unlocked by events across play and the editor; stored in PlayerPrefs as JSON.</summary>
    public static class Achievements
    {
        public class Def
        {
            public string id, name, description;
            public Def(string id, string name, string description) { this.id = id; this.name = name; this.description = description; }
        }

        public static readonly Def[] All =
        {
            new Def("first_clear", "Squire", "Clear any quest."),
            new Def("champion_clear", "Knighted", "Clear a quest on Champion."),
            new Def("deathless", "Unbroken", "Clear a quest on the first attempt."),
            new Def("all_loot", "Magpie", "Gather every coin and gem in a quest."),
            new Def("near_miss_10", "Close Shave", "Brush past ten hazards in one run and live."),
            new Def("under_par", "Swift Rider", "Finish a quest under par time."),
            new Def("jumps_100", "Bounding Hare", "Jump a hundred times in one run."),
            new Def("daily_1", "Pilgrim", "Complete a Daily Quest."),
            new Def("daily_10", "Devout Pilgrim", "Complete ten Daily Quests."),
            new Def("campaign_complete", "Realm's Champion", "Clear every campaign quest."),
            new Def("all_mounts", "Stablemaster", "Ride all seven mounts."),
            new Def("coins_1000", "Treasurer", "Collect a thousand coins in total."),
            new Def("levels_10", "Wanderer", "Play ten different quests."),
            new Def("deaths_100", "Stubborn", "Die a hundred times. Keep going."),
            new Def("first_save", "Architect", "Save a quest of your own."),
            new Def("builder_500", "Master Mason", "Save a quest with 500 objects."),
            new Def("share_code", "Herald", "Copy a share code."),
            new Def("stamp_saved", "Sealmaker", "Save a stamp."),
            new Def("volley_used", "Siege Engineer", "Save a quest with a Volley trigger."),
            new Def("song_matched", "Bard", "Save a quest whose finish sits within ten blocks of the song's end."),
        };

        [Serializable]
        class State
        {
            public List<string> unlocked = new List<string>();
            public List<string> counterKeys = new List<string>();
            public List<int> counterValues = new List<int>();
            public List<string> mountsRidden = new List<string>();
            public List<string> levelsPlayed = new List<string>();
        }

        const string Pref = "geodashy.achievements";
        static State state;
        public static event Action<Def> Unlocked;
        /// <summary>Set by the HUD or menu to show unlock toasts.</summary>
        public static Action<string> Announce;

        static State S
        {
            get
            {
                if (state != null) return state;
                try { state = JsonUtility.FromJson<State>(PlayerPrefs.GetString(Pref, "")) ?? new State(); }
                catch (Exception) { state = new State(); }
                return state;
            }
        }

        static void Save()
        {
            PlayerPrefs.SetString(Pref, JsonUtility.ToJson(S));
            PlayerPrefs.Save();
        }

        public static bool IsUnlocked(string id) => S.unlocked.Contains(id);
        public static int UnlockedCount => S.unlocked.Count;

        public static Def Get(string id)
        {
            foreach (var d in All) if (d.id == id) return d;
            return null;
        }

        public static void Unlock(string id)
        {
            if (S.unlocked.Contains(id)) return;
            var def = Get(id);
            if (def == null) return;
            S.unlocked.Add(id);
            Save();
            Announce?.Invoke("Achievement: " + def.name + " — " + def.description);
            Unlocked?.Invoke(def);
        }

        public static int Counter(string key)
        {
            int i = S.counterKeys.IndexOf(key);
            return i < 0 ? 0 : S.counterValues[i];
        }

        public static int AddCounter(string key, int amount)
        {
            int i = S.counterKeys.IndexOf(key);
            if (i < 0) { S.counterKeys.Add(key); S.counterValues.Add(amount); i = S.counterValues.Count - 1; }
            else S.counterValues[i] += amount;
            Save();
            return S.counterValues[i];
        }

        public static void MountRidden(string id)
        {
            if (S.mountsRidden.Contains(id)) return;
            S.mountsRidden.Add(id);
            Save();
            if (S.mountsRidden.Count >= 7) Unlock("all_mounts");
        }

        public static void LevelPlayed(string id)
        {
            if (S.levelsPlayed.Contains(id)) return;
            S.levelsPlayed.Add(id);
            Save();
            if (S.levelsPlayed.Count >= 10) Unlock("levels_10");
        }
    }
}
