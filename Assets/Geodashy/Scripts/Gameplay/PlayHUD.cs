using System;
using System.Collections.Generic;
using Geodashy.Core;
using Geodashy.Editing.UI;
using Geodashy.Rendering;
using UnityEngine;
using UnityEngine.UI;

namespace Geodashy.Gameplay
{
    /// <summary>Everything the end-of-run report card shows.</summary>
    public class ReportCard
    {
        public int attempts, jumps, coins, totalCoins, gems, totalGems, totalAttempts, completions, nearMisses;
        public float seconds, par;
        public string difficultyName = "";
        public bool medalTime, medalLoot, medalDeathless;
        /// <summary>Rider height over the run, 0..1 of the room, sampled evenly.</summary>
        public List<float> profile = new List<float>();
        /// <summary>Where this session's deaths happened (0..1 progress).</summary>
        public List<float> deaths = new List<float>();
    }

    /// <summary>Progress bar, attempt counter, mount hint, pause and completion panels.</summary>
    public class PlayHUD : MonoBehaviour
    {
        public Action onScreenshot, onClip;
        Text perfText;
        RectTransform medalRow, profileChart;
        Text completeTitle;
        Button clipButton;
        Text comboText;
        float comboTimer;
        Image ripple;
        float rippleT = 1f;
        Image progressFill;
        RectTransform barRoot, markerLayer;
        Image bestMarker;
        Text bestLabel;
        readonly List<Image> tickPool = new List<Image>();
        Text attemptText, coinText, hintText, progressText;

        public static readonly Color AllTimeDeathColor = new Color(1f, 0.25f, 0.25f, 1f);
        public static readonly Color SessionDeathColor = new Color(1f, 0.6f, 0.15f, 1f);
        public static readonly Color BestColor = new Color(1f, 0.85f, 0.3f, 1f);
        RectTransform pausePanel, completePanel;
        Text completeStats;
        float hintTimer;

        Text practiceText;
        Button practiceToggle;
        RectTransform deathPanel;
        Text deathText;
        Button pauseExitButton, completeExitButton;
        Text escHint;
        Image flash;
        float flashTimer, flashDuration;
        Color flashColor;
        Image vignette;
        float vignetteStrength;
        RectTransform lootBanner;
        Text lootText;
        float lootTimer;
        Image crestIcon, sealCrest;
        RectTransform introPanel;
        Text introTitle, introBody, introMount;
        Image introMountIcon;

        static readonly Color Parchment = new Color(0.93f, 0.86f, 0.68f, 0.98f);
        static readonly Color Ink = new Color(0.28f, 0.17f, 0.08f, 1f);
        RectTransform checkpointButtons;

        public static PlayHUD Create(Transform parent, Action onResume, Action onRestart, Action onExit, Action onTogglePractice, Action onCheckpoint, Action onRemoveCheckpoint)
        {
            var go = new GameObject("Play HUD");
            go.transform.SetParent(parent, false);
            var hud = go.AddComponent<PlayHUD>();
            hud.Build(onResume, onRestart, onExit, onTogglePractice, onCheckpoint, onRemoveCheckpoint);
            return hud;
        }

        void Build(Action onResume, Action onRestart, Action onExit, Action onTogglePractice, Action onCheckpoint, Action onRemoveCheckpoint)
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
            canvas.pixelPerfect = true;
            EditorUI.ConfigureScaler(gameObject.AddComponent<CanvasScaler>());
            gameObject.AddComponent<GraphicRaycaster>();

            bool phone = EditorUI.PhoneLayout;
            var root = UIFactory.Rect(transform, "Root");
            UIFactory.Stretch(root);
            if (phone) EditorUI.ApplySafeArea(root);
            float bw = phone ? 180f : 300f;   // progress bar half-width

            // full-screen flash (drawn first so everything else sits above it)
            var flashRt = UIFactory.Rect(root, "Flash");
            UIFactory.Stretch(flashRt);
            flash = flashRt.gameObject.AddComponent<Image>();
            flash.color = Color.clear;
            flash.raycastTarget = false;
            // beat vignette: darkens the edges for a moment on each beat
            var vigRt = UIFactory.Rect(root, "Vignette");
            UIFactory.Stretch(vigRt);
            vignette = vigRt.gameObject.AddComponent<Image>();
            vignette.sprite = PlaceholderSpriteFactory.Vignette();
            vignette.color = Color.clear;
            vignette.raycastTarget = false;

            // progress bar
            var barBg = UIFactory.Panel(root, "ProgressBg", new Color(0, 0, 0, 0.5f));
            UIFactory.Anchor(barBg, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-bw, -34), new Vector2(bw, -14));
            barBg.GetComponent<Image>().raycastTarget = false;
            barRoot = barBg;
            var fillRt = UIFactory.Rect(barBg, "Fill");
            progressFill = fillRt.gameObject.AddComponent<Image>();
            progressFill.color = UIFactory.Accent;
            progressFill.raycastTarget = false;
            UIFactory.Anchor(fillRt, new Vector2(0, 0), new Vector2(0, 1), new Vector2(2, 2), new Vector2(2, -2));

            // overlay for death ticks and the personal-best marker
            markerLayer = UIFactory.Rect(barBg, "Markers");
            UIFactory.Stretch(markerLayer, 2, 0, 2, 0);
            var bestRt = UIFactory.Rect(barBg, "Best");
            bestMarker = bestRt.gameObject.AddComponent<Image>();
            bestMarker.color = BestColor;
            bestMarker.raycastTarget = false;
            UIFactory.Anchor(bestRt, new Vector2(0, 0), new Vector2(0, 1), new Vector2(-2, -4), new Vector2(2, 4));
            bestLabel = UIFactory.Label(bestRt, "", 12, TextAnchor.LowerCenter, BestColor, -1, -1, true);
            bestLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
            UIFactory.Anchor(bestLabel.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-60, 2), new Vector2(60, 18));
            bestRt.gameObject.SetActive(false);
            var legend = UIFactory.Label(root, "", 11, TextAnchor.MiddleCenter, UIFactory.TextDim);
            legend.text = "red = past deaths   orange = this session   gold = personal best";
            UIFactory.Anchor(legend.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-bw, -76), new Vector2(bw, -60));
            if (phone) legend.gameObject.SetActive(false);
            progressText = UIFactory.Label(root, "0%", 14, TextAnchor.MiddleCenter, UIFactory.TextColor);
            UIFactory.Anchor(progressText.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-60, -60), new Vector2(60, -36));

            crestIcon = UIFactory.Icon(root, PlaceholderSpriteFactory.Crest(PlayerProfile.Crest, PlayerProfile.Primary, PlayerProfile.Secondary), 44);
            UIFactory.Anchor(crestIcon.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(16, -60), new Vector2(60, -16));
            attemptText = UIFactory.Label(root, "Attempt 1", 22, TextAnchor.MiddleLeft, UIFactory.TextColor, -1, -1, true);
            UIFactory.Anchor(attemptText.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(68, -60), new Vector2(phone ? 300 : 440, -14));
            coinText = UIFactory.Label(root, "", phone ? 15 : 18, TextAnchor.MiddleRight, UIFactory.Accent);
            UIFactory.Anchor(coinText.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(phone ? -330 : -400, -60), new Vector2(phone ? -76 : -20, -14));
            hintText = UIFactory.Label(root, "", phone ? 15 : 18, TextAnchor.MiddleLeft, UIFactory.TextColor);
            UIFactory.Anchor(hintText.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(20, 16), new Vector2(phone ? 560 : 900, 50));
            escHint = UIFactory.Label(root, "Esc — pause / back to editor", 13, TextAnchor.MiddleRight, UIFactory.TextDim);
            UIFactory.Anchor(escHint.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-400, 16), new Vector2(-20, 40));
            if (phone)
            {
                // no Esc key on a phone: an always-visible pause button in the top-right corner
                escHint.gameObject.SetActive(false);
                var pause = UIFactory.Button(root, "❚❚", () => onResume(), 52, 44, UIFactory.PanelBg3, 16);
                UIFactory.Anchor(pause.GetComponent<RectTransform>(), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-64, -60), new Vector2(-12, -16));
            }

            // practice mode status + on-screen checkpoint buttons
            practiceText = UIFactory.Label(root, "", phone ? 13 : 16, TextAnchor.MiddleRight, new Color(0.4f, 1f, 0.5f, 1f), -1, -1, true);
            UIFactory.Anchor(practiceText.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-420, -92), new Vector2(-20, -64));
            checkpointButtons = UIFactory.Rect(root, "CheckpointButtons");
            UIFactory.Anchor(checkpointButtons, new Vector2(1, 0), new Vector2(1, 0), new Vector2(phone ? -300 : -260, phone ? 40 : 50), new Vector2(-20, phone ? 88 : 92));
            UIFactory.HLayout(checkpointButtons, 8, 0, true);
            UIFactory.Button(checkpointButtons, phone ? "+ Waystone" : "+ Waystone (Z)", () => onCheckpoint(), -1, phone ? 48 : 40, UIFactory.Good, 14);
            UIFactory.Button(checkpointButtons, phone ? "− Remove" : "− Remove (X)", () => onRemoveCheckpoint(), -1, phone ? 48 : 40, UIFactory.Danger, 14);
            checkpointButtons.gameObject.SetActive(false);

            // loot banner: crest + text, slides in under the progress bar when every piece is gathered
            lootBanner = UIFactory.Panel(root, "LootBanner", new Color(0.12f, 0.08f, 0.02f, 0.85f));
            UIFactory.Anchor(lootBanner, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-220, -140), new Vector2(220, -84));
            lootBanner.GetComponent<Image>().raycastTarget = false;
            UIFactory.HLayout(lootBanner, 10, 8, false, TextAnchor.MiddleCenter);
            var lootCrest = UIFactory.Icon(lootBanner, PlaceholderSpriteFactory.Crest(PlayerProfile.Crest, PlayerProfile.Primary, PlayerProfile.Secondary), 40);
            lootText = UIFactory.Label(lootBanner, "", 18, TextAnchor.MiddleLeft, BestColor, -1, 40, true);
            UIFactory.Layout(lootText.gameObject, -1, 40, 1);
            lootBanner.gameObject.SetActive(false);

            // death panel (bottom centre so the crash site stays visible)
            deathPanel = UIFactory.Panel(root, "Death", new Color(0.35f, 0.05f, 0.05f, 0.85f));
            UIFactory.Anchor(deathPanel, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-330, 60), new Vector2(330, 128));
            deathPanel.GetComponent<Image>().raycastTarget = false;
            deathText = UIFactory.Label(deathPanel, "", 17, TextAnchor.MiddleCenter, UIFactory.TextColor);
            UIFactory.Stretch(deathText.rectTransform, 10, 4, 10, 4);
            deathPanel.gameObject.SetActive(false);

            // pause panel
            pausePanel = UIFactory.Panel(root, "Pause", new Color(0, 0, 0, 0.6f));
            var pw = UIFactory.Panel(pausePanel, "Window", UIFactory.PanelBg2);
            UIFactory.Anchor(pw, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-200, -155), new Vector2(200, 155));
            UIFactory.VLayout(pw, 10, 20);
            UIFactory.Label(pw, "PAUSED", 26, TextAnchor.MiddleCenter, UIFactory.Accent, -1, 40, true);
            UIFactory.Button(pw, "Resume", () => onResume(), -1, 40, UIFactory.Good, 16);
            practiceToggle = UIFactory.Button(pw, "Switch to Training (C)", () => onTogglePractice(), -1, 40, null, 16);
            UIFactory.Button(pw, "Restart from start", () => onRestart(), -1, 40, null, 16);
            var capRow = UIFactory.Row(pw, 36, 6);
            UIFactory.Button(capRow, "📷 Screenshot", () => onScreenshot?.Invoke(), -1, 34, null, 13);
            clipButton = UIFactory.Button(capRow, "🎞 Save 5 s clip", () => onClip?.Invoke(), -1, 34, null, 13);
            pauseExitButton = UIFactory.Button(pw, "Back to editor", () => onExit(), -1, 40, UIFactory.Danger, 16);
            UIFactory.Anchor(pw, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-200, -180), new Vector2(200, 180));
            pausePanel.gameObject.SetActive(false);

            // rune combo counter and the gravity ripple ring
            comboText = UIFactory.Label(root, "", 30, TextAnchor.MiddleCenter, BestColor, -1, -1, true);
            UIFactory.Anchor(comboText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-200, 60), new Vector2(200, 120));
            comboText.raycastTarget = false;
            comboText.gameObject.SetActive(false);
            var rippleRt = UIFactory.Rect(root, "Ripple");
            ripple = rippleRt.gameObject.AddComponent<Image>();
            ripple.sprite = PlaceholderSpriteFactory.Outline();
            ripple.type = Image.Type.Sliced;
            ripple.color = Color.clear;
            ripple.raycastTarget = false;
            UIFactory.Anchor(rippleRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-40, -40), new Vector2(40, 40));

            // performance readout (Options ▸ Performance overlay)
            perfText = UIFactory.Label(root, "", 12, TextAnchor.UpperLeft, new Color(0.6f, 1f, 0.7f, 0.9f));
            UIFactory.Anchor(perfText.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(16, -96), new Vector2(400, -64));
            perfText.gameObject.SetActive(false);

            // quest scroll (intro card)
            introPanel = UIFactory.Panel(root, "Intro", new Color(0, 0, 0, 0.55f));
            var scrollCard = UIFactory.Panel(introPanel, "Scroll", Parchment);
            UIFactory.Anchor(scrollCard, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-330, -210), new Vector2(330, 210));
            var scrollEdgeTop = UIFactory.Panel(scrollCard, "EdgeTop", new Color(0.55f, 0.38f, 0.2f, 1f));
            UIFactory.Anchor(scrollEdgeTop, new Vector2(0, 1), new Vector2(1, 1), new Vector2(-12, -14), new Vector2(12, 0));
            var scrollEdgeBottom = UIFactory.Panel(scrollCard, "EdgeBottom", new Color(0.55f, 0.38f, 0.2f, 1f));
            UIFactory.Anchor(scrollEdgeBottom, new Vector2(0, 0), new Vector2(1, 0), new Vector2(-12, 0), new Vector2(12, 14));
            var scrollBody = UIFactory.Rect(scrollCard, "Body");
            UIFactory.Stretch(scrollBody, 28, 24, 28, 24);
            UIFactory.VLayout(scrollBody, 8, 0, true, true, TextAnchor.UpperCenter);
            UIFactory.Label(scrollBody, "A QUEST IS OFFERED", 13, TextAnchor.MiddleCenter, new Color(0.5f, 0.32f, 0.12f), -1, 20, true);
            introTitle = UIFactory.Label(scrollBody, "", 30, TextAnchor.MiddleCenter, Ink, -1, 44, true);
            introBody = UIFactory.Label(scrollBody, "", 15, TextAnchor.UpperCenter, Ink, -1, 74);
            introBody.horizontalOverflow = HorizontalWrapMode.Wrap;
            var mountRow = UIFactory.Row(scrollBody, 70, 12, TextAnchor.MiddleCenter);
            introMountIcon = UIFactory.Icon(mountRow, null, 70);
            introMount = UIFactory.Label(mountRow, "", 13, TextAnchor.MiddleLeft, Ink, 420, 70);
            UIFactory.Label(scrollBody, phone ? "Tap to ride out" : "Click or press Space to ride out", 14, TextAnchor.MiddleCenter, new Color(0.5f, 0.32f, 0.12f), -1, 26, true);
            introPanel.gameObject.SetActive(false);

            // complete panel
            completePanel = UIFactory.Panel(root, "Complete", new Color(0, 0, 0, 0.6f));
            var cw = UIFactory.Panel(completePanel, "Window", Parchment);
            UIFactory.Anchor(cw, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-280, -240), new Vector2(280, 240));
            UIFactory.VLayout(cw, 8, 16, true, true, TextAnchor.UpperCenter);
            var sealRow = UIFactory.Row(cw, 64, 0, TextAnchor.MiddleCenter);
            var seal = UIFactory.Icon(sealRow, PlaceholderSpriteFactory.Circle(), 64, new Color(0.62f, 0.12f, 0.1f, 1f));
            var sealInner = UIFactory.Icon(seal.transform, PlaceholderSpriteFactory.Circle(), 54, new Color(0.72f, 0.16f, 0.13f, 1f));
            UIFactory.Anchor(sealInner.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-27, -27), new Vector2(27, 27));
            sealCrest = UIFactory.Icon(seal.transform, PlaceholderSpriteFactory.Crest(PlayerProfile.Crest, PlayerProfile.Primary, PlayerProfile.Secondary), 36);
            UIFactory.Anchor(sealCrest.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-18, -18), new Vector2(18, 18));
            sealCrest.color = new Color(1f, 0.85f, 0.7f, 0.9f);
            completeTitle = UIFactory.Label(cw, "QUEST COMPLETE", 26, TextAnchor.MiddleCenter, Ink, -1, 36, true);
            completeStats = UIFactory.Label(cw, "", 14, TextAnchor.MiddleCenter, Ink, -1, 78);
            medalRow = UIFactory.Row(cw, 40, 8, TextAnchor.MiddleCenter);
            profileChart = UIFactory.Rect(cw, "Profile");
            UIFactory.Layout(profileChart.gameObject, -1, 60);
            profileChart.gameObject.AddComponent<Image>().color = new Color(0.2f, 0.12f, 0.05f, 0.12f);
            var btnRow = UIFactory.Row(cw, 36, 8);
            UIFactory.Button(btnRow, "Play again", () => onRestart(), -1, 36, UIFactory.Good, 15);
            completeExitButton = UIFactory.Button(btnRow, "Back to editor", () => onExit(), -1, 36, null, 15);
            completePanel.gameObject.SetActive(false);
        }

        /// <summary>Updates the bar. Precise mode shows thousandths of a percent (used on completion and death).</summary>
        public void SetProgress(float t, bool precise = false)
        {
            t = Mathf.Clamp01(t);
            var rt = progressFill.rectTransform;
            rt.anchorMax = new Vector2(t, 1);
            rt.offsetMax = new Vector2(-2, -2);
            progressText.text = precise ? (t * 100f).ToString("0.000") + "%" : Mathf.RoundToInt(t * 100f) + "%";
        }

        /// <summary>Redraws the death ticks and personal-best marker. Clustered deaths render brighter.</summary>
        public void SetMarkers(List<float> allTimeDeaths, List<float> sessionDeaths, float bestProgress)
        {
            int needed = (allTimeDeaths?.Count ?? 0) + (sessionDeaths?.Count ?? 0);
            while (tickPool.Count < needed)
            {
                var rt = UIFactory.Rect(markerLayer, "Tick");
                var img = rt.gameObject.AddComponent<Image>();
                img.raycastTarget = false;
                UIFactory.Anchor(rt, new Vector2(0, 0), new Vector2(0, 1), new Vector2(-1, 3), new Vector2(1, -3));
                tickPool.Add(img);
            }
            int used = 0;
            PlaceTicks(allTimeDeaths, AllTimeDeathColor, 0.35f, 3f, ref used);
            PlaceTicks(sessionDeaths, SessionDeathColor, 0.9f, 1f, ref used);
            for (int i = used; i < tickPool.Count; i++) tickPool[i].gameObject.SetActive(false);

            bool showBest = bestProgress > 0.001f;
            bestMarker.gameObject.SetActive(showBest);
            if (showBest)
            {
                var rt = bestMarker.rectTransform;
                rt.anchorMin = new Vector2(Mathf.Clamp01(bestProgress), 0);
                rt.anchorMax = new Vector2(Mathf.Clamp01(bestProgress), 1);
                rt.offsetMin = new Vector2(-2, -4);
                rt.offsetMax = new Vector2(2, 4);
                bestLabel.text = bestProgress >= 0.9995f ? "PB 100%" : "PB " + (bestProgress * 100f).ToString("0.0") + "%";
            }
        }

        void PlaceTicks(List<float> deaths, Color color, float baseAlpha, float boostPer, ref int used)
        {
            if (deaths == null) return;
            // alpha grows with how many other deaths sit within 1% of this one
            for (int i = 0; i < deaths.Count; i++)
            {
                float d = deaths[i];
                int neighbours = 0;
                for (int j = 0; j < deaths.Count; j++) if (j != i && Mathf.Abs(deaths[j] - d) < 0.01f) neighbours++;
                var img = tickPool[used++];
                img.gameObject.SetActive(true);
                var rt = img.rectTransform;
                rt.anchorMin = new Vector2(Mathf.Clamp01(d), 0);
                rt.anchorMax = new Vector2(Mathf.Clamp01(d), 1);
                rt.offsetMin = new Vector2(-1, 3);
                rt.offsetMax = new Vector2(1, -3);
                var c = color;
                c.a = Mathf.Clamp01(baseAlpha + neighbours * 0.08f * boostPer);
                img.color = c;
            }
        }

        public void SetMode(string title, string detail, bool training, bool auto)
        {
            practiceText.text = title + "  ·  " + detail + (training && auto ? "  ·  auto" : "") + (training && !EditorUI.PhoneLayout ? "  ·  ← → scrub" : "");
            checkpointButtons.gameObject.SetActive(training);
            UIFactory.SetButtonLabel(practiceToggle, training ? "Training mode: on (C)" : "Switch to Training (C)");
            UIFactory.SetButtonActive(practiceToggle, training);
        }

        /// <summary>Names the place Exit returns to ("editor" or "menu").</summary>
        public void SetExitTarget(string target)
        {
            UIFactory.SetButtonLabel(pauseExitButton, "Back to " + target);
            UIFactory.SetButtonLabel(completeExitButton, "Back to " + target);
            escHint.text = "Esc — pause / back to " + target;
        }

        public void ShowIntro(string title, string description, Sprite mountSprite, string mountName, string control, float facing = 1f)
        {
            introTitle.text = title;
            introBody.text = string.IsNullOrEmpty(description) ? "Ride from the west gate to the finish. Do not touch anything red." : description;
            introMountIcon.sprite = mountSprite;
            introMountIcon.rectTransform.localScale = new Vector3(facing, 1f, 1f);
            introMount.text = "You ride the " + mountName + ". " + control;
            introPanel.gameObject.SetActive(true);
        }

        public void HideIntro() => introPanel.gameObject.SetActive(false);

        public void ShowDeath(string cause, float progress)
        {
            deathText.text = EditorUI.PhoneLayout
                ? string.Format("{0} at {1:0.000}%\nRed = the edge that killed you · dot = contact point\nTap anywhere to retry", cause, progress * 100f)
                : string.Format("{0} at {1:0.000}%\nRed = the edge that killed you · dot = contact point · yellow = your hurt box\nClick, Space or Enter to retry · R restarts", cause, progress * 100f);
            deathPanel.gameObject.SetActive(true);
        }

        public void HideDeath() => deathPanel.gameObject.SetActive(false);

        public void SetAttempt(int n) => attemptText.text = "Attempt " + n;
        public void SetCoins(int n, int total) => coinText.text = total > 0 ? "Loot " + n + " / " + total : "";

        public void SetLoot(int coins, int totalCoins, int gems, int totalGems, int keys)
        {
            var parts = new List<string>();
            if (totalCoins > 0) parts.Add("Gold " + coins + "/" + totalCoins);
            if (totalGems > 0) parts.Add("Gems " + gems + "/" + totalGems);
            if (keys > 0) parts.Add("Keys " + keys);
            coinText.text = string.Join("   ", parts);
        }

        public void ShowHint(string text, float seconds = 4f)
        {
            hintText.text = text;
            hintText.color = UIFactory.TextColor;
            hintTimer = seconds;
        }

        public void ShowPause(bool on) => pausePanel.gameObject.SetActive(on);

        /// <summary>Momentary edge darkening; strength 0..1 decays over about a third of a second.</summary>
        public void PulseVignette(float strength)
        {
            if (Accessibility.ReduceFlash) return;
            vignetteStrength = Mathf.Max(vignetteStrength, Mathf.Clamp01(strength));
        }

        public void ShowLootBanner(string text, float seconds = 3f)
        {
            lootText.text = text;
            lootBanner.gameObject.SetActive(true);
            lootBanner.localScale = Vector3.one * 0.6f;
            lootTimer = seconds;
        }

        public void HideLootBanner()
        {
            lootTimer = 0f;
            if (lootBanner != null) lootBanner.gameObject.SetActive(false);
        }

        public void ShowComplete(ReportCard c)
        {
            string loot = "Gold " + c.coins + "/" + c.totalCoins + (c.totalGems > 0 ? "   Gems " + c.gems + "/" + c.totalGems : "");
            completeStats.text = string.Format("{0}   ·   Progress 100.000%   ·   {1:0.0}s (par {2:0.0}s)\nAttempts {3}   ·   Jumps {4}   ·   Near misses {5}\n{6}   ·   champion runs {7}, cleared {8}×",
                c.difficultyName, c.seconds, c.par, c.attempts, c.jumps, c.nearMisses, loot, c.totalAttempts, c.completions);
            foreach (Transform child in medalRow) Destroy(child.gameObject);
            void Medal(string name, bool earned, Color color)
            {
                var chip = UIFactory.Panel(medalRow, "Medal", earned ? color : new Color(0.3f, 0.25f, 0.2f, 0.25f));
                UIFactory.Layout(chip.gameObject, 150, 36);
                var t = UIFactory.Label(chip, (earned ? "★ " : "☆ ") + name, 13, TextAnchor.MiddleCenter, earned ? Color.white : new Color(0.4f, 0.3f, 0.2f, 0.8f), -1, -1, earned);
                UIFactory.Stretch(t.rectTransform, 4, 2, 4, 2);
            }
            Medal("Swift (under par)", c.medalTime, new Color(0.2f, 0.55f, 0.9f));
            Medal("All loot", c.medalLoot, new Color(0.85f, 0.65f, 0.15f));
            Medal("Deathless", c.medalDeathless, new Color(0.62f, 0.12f, 0.1f));
            // run profile: rider height over the level with this session's deaths ticked underneath
            foreach (Transform child in profileChart) Destroy(child.gameObject);
            int n = c.profile.Count;
            for (int i = 0; i < n; i++)
            {
                var bar = UIFactory.Rect(profileChart, "P");
                var img = bar.gameObject.AddComponent<Image>();
                img.color = new Color(0.45f, 0.28f, 0.1f, 0.85f);
                img.raycastTarget = false;
                float x0 = i / (float)n, x1 = (i + 1) / (float)n;
                float h = Mathf.Clamp01(c.profile[i]);
                UIFactory.Anchor(bar, new Vector2(x0, 0), new Vector2(x1, 0), new Vector2(0.5f, 8), new Vector2(-0.5f, 8 + 44f * h));
            }
            foreach (var d in c.deaths)
            {
                var tick = UIFactory.Rect(profileChart, "D");
                var img = tick.gameObject.AddComponent<Image>();
                img.color = AllTimeDeathColor;
                img.raycastTarget = false;
                UIFactory.Anchor(tick, new Vector2(Mathf.Clamp01(d), 0), new Vector2(Mathf.Clamp01(d), 0), new Vector2(-1.5f, 0), new Vector2(1.5f, 7));
            }
            var lbl = UIFactory.Label(profileChart, "run profile · red = this session's deaths", 9, TextAnchor.UpperRight, new Color(0.4f, 0.3f, 0.2f, 0.9f));
            lbl.raycastTarget = false;
            UIFactory.Anchor(lbl.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(4, -14), new Vector2(-4, 0));
            completePanel.gameObject.SetActive(true);
        }

        /// <summary>"x3" style counter for chained runes; hides itself after a moment.</summary>
        public void ShowCombo(int n)
        {
            if (n < 2)
            {
                comboText.gameObject.SetActive(false);
                return;
            }
            comboText.text = "x" + n + (n >= 5 ? "  FLOW" : "");
            comboText.gameObject.SetActive(true);
            comboText.rectTransform.localScale = Vector3.one * 1.4f;
            comboTimer = 1.2f;
        }

        /// <summary>Expanding ring from the screen centre (gravity runes and gates).</summary>
        public void Ripple(Color color)
        {
            if (Accessibility.ReduceFlash) return;
            rippleT = 0f;
            ripple.color = color;
        }

        public void SetPerf(string text)
        {
            bool on = !string.IsNullOrEmpty(text);
            if (perfText.gameObject.activeSelf != on) perfText.gameObject.SetActive(on);
            perfText.text = text;
        }

        public void SetClipAvailable(bool on)
        {
            if (clipButton != null) clipButton.gameObject.SetActive(on);
        }

        public void HideComplete() => completePanel.gameObject.SetActive(false);

        public void Flash(Color color, float duration)
        {
            if (Accessibility.ReduceFlash) color.a *= 0.25f;
            flashColor = color;
            flashDuration = Mathf.Max(0.01f, duration);
            flashTimer = flashDuration;
        }

        void Update()
        {
            if (flashTimer > 0f)
            {
                flashTimer -= Time.unscaledDeltaTime;
                var c = flashColor;
                c.a *= Mathf.Clamp01(flashTimer / flashDuration);
                flash.color = c;
            }
            if (vignetteStrength > 0f)
            {
                vignette.color = new Color(0.05f, 0.02f, 0.08f, vignetteStrength * 0.55f);
                vignetteStrength = Mathf.Max(0f, vignetteStrength - Time.unscaledDeltaTime * 3f);
                if (vignetteStrength <= 0f) vignette.color = Color.clear;
            }
            if (comboTimer > 0f)
            {
                comboTimer -= Time.unscaledDeltaTime;
                comboText.rectTransform.localScale = Vector3.one * Mathf.Lerp(comboText.rectTransform.localScale.x, 1f, 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime));
                if (comboTimer <= 0f) comboText.gameObject.SetActive(false);
            }
            if (rippleT < 1f)
            {
                rippleT = Mathf.Min(1f, rippleT + Time.unscaledDeltaTime * 2.2f);
                float s = Mathf.Lerp(1f, 22f, rippleT);
                ripple.rectTransform.localScale = Vector3.one * s;
                var c = ripple.color;
                c.a = (1f - rippleT) * 0.6f;
                ripple.color = c;
            }
            if (lootTimer > 0f)
            {
                lootTimer -= Time.unscaledDeltaTime;
                float s = Mathf.Lerp(lootBanner.localScale.x, 1f, 1f - Mathf.Exp(-12f * Time.unscaledDeltaTime));
                lootBanner.localScale = Vector3.one * s;
                if (lootTimer <= 0f) lootBanner.gameObject.SetActive(false);
            }
            if (hintTimer > 0f)
            {
                hintTimer -= Time.unscaledDeltaTime;
                if (hintTimer < 1f)
                {
                    var c = hintText.color;
                    c.a = Mathf.Clamp01(hintTimer);
                    hintText.color = c;
                }
            }
        }
    }
}
