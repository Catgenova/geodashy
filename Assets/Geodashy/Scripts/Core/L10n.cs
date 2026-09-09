using System;
using System.Collections.Generic;
using UnityEngine;

namespace Geodashy.Core
{
    /// <summary>
    /// Minimal string table. Call L10n.T("English text") anywhere; the English text is the key and the fallback,
    /// so untranslated strings still read correctly. Tables live in Resources/Strings/&lt;code&gt;.json as
    /// {"name":"Español","entries":[{"key":"Play","value":"Jugar"}, …]}. Drop in a new file to add a language.
    /// </summary>
    public static class L10n
    {
        [Serializable]
        class Entry
        {
            public string key;
            public string value;
        }

        [Serializable]
        class Table
        {
            public string name;
            public List<Entry> entries = new List<Entry>();
        }

        const string Pref = "geodashy.language";
        static Dictionary<string, string> current = new Dictionary<string, string>();
        static string loaded = null;
        static string[] codes, names;

        public static event Action LanguageChanged;

        public static string Language
        {
            get => PlayerPrefs.GetString(Pref, "en");
            set
            {
                PlayerPrefs.SetString(Pref, value);
                PlayerPrefs.Save();
                loaded = null;
                Ensure();
                LanguageChanged?.Invoke();
            }
        }

        /// <summary>Codes of every table in Resources/Strings, "en" first.</summary>
        public static string[] Codes
        {
            get
            {
                Scan();
                return codes;
            }
        }

        public static string[] Names
        {
            get
            {
                Scan();
                return names;
            }
        }

        static void Scan()
        {
            if (codes != null) return;
            var c = new List<string> { "en" };
            var n = new List<string> { "English" };
            foreach (var ta in Resources.LoadAll<TextAsset>("Strings"))
            {
                if (ta.name == "en") continue;
                string display = ta.name;
                try
                {
                    var t = JsonUtility.FromJson<Table>(ta.text);
                    if (t != null && !string.IsNullOrEmpty(t.name)) display = t.name;
                }
                catch (Exception)
                {
                }
                c.Add(ta.name);
                n.Add(display);
            }
            codes = c.ToArray();
            names = n.ToArray();
        }

        static void Ensure()
        {
            var lang = Language;
            if (loaded == lang) return;
            loaded = lang;
            current = new Dictionary<string, string>();
            if (lang == "en") return;
            var ta = Resources.Load<TextAsset>("Strings/" + lang);
            if (ta == null) return;
            try
            {
                var t = JsonUtility.FromJson<Table>(ta.text);
                if (t?.entries == null) return;
                foreach (var e in t.entries) if (!string.IsNullOrEmpty(e.key) && e.value != null) current[e.key] = e.value;
            }
            catch (Exception e)
            {
                Debug.LogWarning("Could not read string table " + lang + ": " + e.Message);
            }
        }

        /// <summary>Translation of an English string, or the string itself.</summary>
        public static string T(string english)
        {
            if (string.IsNullOrEmpty(english)) return english;
            Ensure();
            return current.TryGetValue(english, out var v) ? v : english;
        }

        public static void CycleLanguage()
        {
            var c = Codes;
            int i = Array.IndexOf(c, Language);
            Language = c[(i + 1) % c.Length];
        }

        public static string LanguageName
        {
            get
            {
                int i = Array.IndexOf(Codes, Language);
                return i < 0 ? Language : Names[i];
            }
        }
    }
}
