using System;
using UnityEngine;

namespace Geodashy.Core
{
    /// <summary>The player's heraldry: crest, banner colour and trim colour. Drives the rider's colours and the P1/P2 colour channels.</summary>
    public static class PlayerProfile
    {
        public static readonly string[] CrestNames = { "Cross", "Star", "Chevron", "Tower", "Dragon", "Lion", "Oak", "Key" };
        public static readonly string[] CrestGlyphs = { "+", "*", "V", "T", "D", "L", "O", "K" };
        public static readonly string[] Palette =
        {
            "b8302c", "2c4fb8", "2f8a3a", "e0b64a", "6a3cff", "1c1c24", "f0e9d2", "ff7fd1", "ff8a2a", "3fa8ff", "5a3a1a", "6ad4ff"
        };

        const string CrestKey = "geodashy.heraldry.crest";
        const string PrimaryKey = "geodashy.heraldry.primary";
        const string SecondaryKey = "geodashy.heraldry.secondary";

        static bool loaded;
        static int crest;
        static Color primary = ObjectCatalog.Hex("2f8a3a");
        static Color secondary = ObjectCatalog.Hex("e0b64a");

        public static event Action Changed;

        static void Load()
        {
            if (loaded) return;
            loaded = true;
            crest = Mathf.Clamp(PlayerPrefs.GetInt(CrestKey, 0), 0, CrestNames.Length - 1);
            if (ColorUtility.TryParseHtmlString("#" + PlayerPrefs.GetString(PrimaryKey, "2f8a3a"), out var p)) primary = p;
            if (ColorUtility.TryParseHtmlString("#" + PlayerPrefs.GetString(SecondaryKey, "e0b64a"), out var s)) secondary = s;
        }

        static void Save()
        {
            PlayerPrefs.SetInt(CrestKey, crest);
            PlayerPrefs.SetString(PrimaryKey, ColorUtility.ToHtmlStringRGB(primary));
            PlayerPrefs.SetString(SecondaryKey, ColorUtility.ToHtmlStringRGB(secondary));
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        public static int Crest
        {
            get
            {
                Load();
                return crest;
            }
            set
            {
                Load();
                crest = Mathf.Clamp(value, 0, CrestNames.Length - 1);
                Save();
            }
        }

        public static Color Primary
        {
            get
            {
                Load();
                return primary;
            }
            set
            {
                Load();
                primary = value;
                Save();
            }
        }

        public static Color Secondary
        {
            get
            {
                Load();
                return secondary;
            }
            set
            {
                Load();
                secondary = value;
                Save();
            }
        }

        public static string Glyph => CrestGlyphs[Crest];
        public static string CrestName => CrestNames[Crest];

        /// <summary>Cache key fragment so placeholder art regenerates when the heraldry changes.</summary>
        public static string Signature => Crest + ":" + ColorUtility.ToHtmlStringRGB(Primary) + ":" + ColorUtility.ToHtmlStringRGB(Secondary);
    }
}
