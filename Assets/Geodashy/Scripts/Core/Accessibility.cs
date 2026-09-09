using UnityEngine;

namespace Geodashy.Core
{
    /// <summary>Accessibility preferences: edge colour palettes, reduced flashing, larger overlays, hold-to-jump assist.</summary>
    public static class Accessibility
    {
        public static readonly string[] PaletteNames = { "Default (green / red)", "Colour-blind (blue / orange)", "High contrast (white / magenta)" };

        public static int Palette
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt("geodashy.a11y.palette", 0), 0, PaletteNames.Length - 1);
            set { PlayerPrefs.SetInt("geodashy.a11y.palette", value); PlayerPrefs.Save(); }
        }

        public static bool ReduceFlash
        {
            get => PlayerPrefs.GetInt("geodashy.a11y.reduceFlash", 0) == 1;
            set { PlayerPrefs.SetInt("geodashy.a11y.reduceFlash", value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static bool BigOverlays
        {
            get => PlayerPrefs.GetInt("geodashy.a11y.bigOverlays", 0) == 1;
            set { PlayerPrefs.SetInt("geodashy.a11y.bigOverlays", value ? 1 : 0); PlayerPrefs.Save(); }
        }

        /// <summary>Holding the button keeps jumping / flapping on mounts that normally need a fresh tap.</summary>
        public static bool HoldAssist
        {
            get => PlayerPrefs.GetInt("geodashy.a11y.holdAssist", 0) == 1;
            set { PlayerPrefs.SetInt("geodashy.a11y.holdAssist", value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static float OverlayThicknessMultiplier => BigOverlays ? 2.2f : 1f;

        public static Color SafeColor
        {
            get
            {
                switch (Palette)
                {
                    case 1: return new Color(0.25f, 0.55f, 1f, 0.95f);
                    case 2: return new Color(1f, 1f, 1f, 1f);
                    default: return new Color(0.3f, 1f, 0.45f, 0.95f);
                }
            }
        }

        public static Color DangerColor
        {
            get
            {
                switch (Palette)
                {
                    case 1: return new Color(1f, 0.6f, 0.1f, 0.95f);
                    case 2: return new Color(1f, 0.1f, 0.9f, 1f);
                    default: return new Color(1f, 0.22f, 0.22f, 0.95f);
                }
            }
        }

        public static Color InteractColor
        {
            get
            {
                switch (Palette)
                {
                    case 1: return new Color(0.95f, 0.9f, 0.3f, 0.95f);
                    case 2: return new Color(0.3f, 1f, 1f, 1f);
                    default: return new Color(0.4f, 0.9f, 1f, 0.9f);
                }
            }
        }
    }
}
