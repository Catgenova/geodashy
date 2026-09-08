using System;
using System.Collections.Generic;
using Geodashy.Core;
using Geodashy.Editing.UI;
using Geodashy.Rendering;
using UnityEngine;
using UnityEngine.UI;

namespace Geodashy.Gameplay
{
    /// <summary>Progress bar, attempt counter, mount hint, pause and completion panels.</summary>
    public class PlayHUD : MonoBehaviour
    {
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

            var root = UIFactory.Rect(transform, "Root");
            UIFactory.Stretch(root);

            // full-screen flash (drawn first so everything else sits above it)
            var flashRt = UIFactory.Rect(root, "Flash");
            UIFactory.Stretch(flashRt);
            flash = flashRt.gameObject.AddComponent<Image>();
            flash.color = Color.clear;
            flash.raycastTarget = false;

            // progress bar
            var barBg = UIFactory.Panel(root, "ProgressBg", new Color(0, 0, 0, 0.5f));
            UIFactory.Anchor(barBg, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-300, -34), new Vector2(300, -14));
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
            UIFactory.Anchor(legend.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-300, -76), new Vector2(300, -60));
            progressText = UIFactory.Label(root, "0%", 14, TextAnchor.MiddleCenter, UIFactory.TextColor);
            UIFactory.Anchor(progressText.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-60, -60), new Vector2(60, -36));

            crestIcon = UIFactory.Icon(root, PlaceholderSpriteFactory.Crest(PlayerProfile.Crest, PlayerProfile.Primary, PlayerProfile.Secondary), 44);
            UIFactory.Anchor(crestIcon.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(16, -60), new Vector2(60, -16));
            attemptText = UIFactory.Label(root, "Attempt 1", 22, TextAnchor.MiddleLeft, UIFactory.TextColor, -1, -1, true);
            UIFactory.Anchor(attemptText.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(68, -60), new Vector2(440, -14));
            coinText = UIFactory.Label(root, "", 18, TextAnchor.MiddleRight, UIFactory.Accent);
            UIFactory.Anchor(coinText.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-400, -60), new Vector2(-20, -14));
            hintText = UIFactory.Label(root, "", 18, TextAnchor.MiddleLeft, UIFactory.TextColor);
            UIFactory.Anchor(hintText.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(20, 16), new Vector2(900, 50));
            escHint = UIFactory.Label(root, "Esc — pause / back to editor", 13, TextAnchor.MiddleRight, UIFactory.TextDim);
            UIFactory.Anchor(escHint.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-400, 16), new Vector2(-20, 40));

            // practice mode status + on-screen checkpoint buttons
            practiceText = UIFactory.Label(root, "", 16, TextAnchor.MiddleRight, new Color(0.4f, 1f, 0.5f, 1f), -1, -1, true);
            UIFactory.Anchor(practiceText.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-420, -92), new Vector2(-20, -64));
            checkpointButtons = UIFactory.Rect(root, "CheckpointButtons");
            UIFactory.Anchor(checkpointButtons, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-260, 50), new Vector2(-20, 92));
            UIFactory.HLayout(checkpointButtons, 8, 0, true);
            UIFactory.Button(checkpointButtons, "+ Waystone (Z)", () => onCheckpoint(), -1, 40, UIFactory.Good, 14);
            UIFactory.Button(checkpointButtons, "− Remove (X)", () => onRemoveCheckpoint(), -1, 40, UIFactory.Danger, 14);
            checkpointButtons.gameObject.SetActive(false);

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
            practiceToggle = UIFactory.Button(pw, "Squire mode (waystones): off", () => onTogglePractice(), -1, 40, null, 16);
            UIFactory.Button(pw, "Restart from start", () => onRestart(), -1, 40, null, 16);
            pauseExitButton = UIFactory.Button(pw, "Back to editor", () => onExit(), -1, 40, UIFactory.Danger, 16);
            pausePanel.gameObject.SetActive(false);

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
            introMount = UIFactory.Label(mountRow, "", 15, TextAnchor.MiddleLeft, Ink, 380, 70);
            UIFactory.Label(scrollBody, "Click or press Space to ride out", 14, TextAnchor.MiddleCenter, new Color(0.5f, 0.32f, 0.12f), -1, 26, true);
            introPanel.gameObject.SetActive(false);

            // complete panel
            completePanel = UIFactory.Panel(root, "Complete", new Color(0, 0, 0, 0.6f));
            var cw = UIFactory.Panel(completePanel, "Window", Parchment);
            UIFactory.Anchor(cw, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-260, -215), new Vector2(260, 215));
            UIFactory.VLayout(cw, 10, 20, true, true, TextAnchor.UpperCenter);
            var sealRow = UIFactory.Row(cw, 84, 0, TextAnchor.MiddleCenter);
            var seal = UIFactory.Icon(sealRow, PlaceholderSpriteFactory.Circle(), 84, new Color(0.62f, 0.12f, 0.1f, 1f));
            var sealInner = UIFactory.Icon(seal.transform, PlaceholderSpriteFactory.Circle(), 70, new Color(0.72f, 0.16f, 0.13f, 1f));
            UIFactory.Anchor(sealInner.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-35, -35), new Vector2(35, 35));
            sealCrest = UIFactory.Icon(seal.transform, PlaceholderSpriteFactory.Crest(PlayerProfile.Crest, PlayerProfile.Primary, PlayerProfile.Secondary), 46);
            UIFactory.Anchor(sealCrest.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-23, -23), new Vector2(23, 23));
            sealCrest.color = new Color(1f, 0.85f, 0.7f, 0.9f);
            UIFactory.Label(cw, "QUEST COMPLETE", 28, TextAnchor.MiddleCenter, Ink, -1, 44, true);
            completeStats = UIFactory.Label(cw, "", 16, TextAnchor.MiddleCenter, Ink, -1, 96);
            UIFactory.Button(cw, "Play again", () => onRestart(), -1, 40, UIFactory.Good, 16);
            completeExitButton = UIFactory.Button(cw, "Back to editor", () => onExit(), -1, 40, null, 16);
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

        public void SetPractice(bool on, int checkpoints, bool auto, int current = -1)
        {
            string pos = checkpoints > 0 && current >= 0 ? (current + 1) + "/" + checkpoints : checkpoints.ToString();
            practiceText.text = on ? "SQUIRE MODE  ·  waystone " + pos + (auto ? "  ·  auto" : "") + "  ·  ← → scrub" : "";
            checkpointButtons.gameObject.SetActive(on);
            UIFactory.SetButtonLabel(practiceToggle, on ? "Squire mode (waystones): on (C)" : "Squire mode (waystones): off (C)");
            UIFactory.SetButtonActive(practiceToggle, on);
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
            introMount.text = "You ride the " + mountName + ".\n" + control;
            introPanel.gameObject.SetActive(true);
        }

        public void HideIntro() => introPanel.gameObject.SetActive(false);

        public void ShowDeath(string cause, float progress)
        {
            deathText.text = string.Format("{0} at {1:0.000}%\nRed = the edge that killed you · dot = contact point · yellow = your hurt box\nClick, Space or Enter to retry · R restarts", cause, progress * 100f);
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

        public void ShowComplete(int attempts, float seconds, int jumps, int coins, int totalCoins, int totalAttempts = 0, int completions = 0, int gems = 0, int totalGems = 0)
        {
            string loot = "Gold " + coins + "/" + totalCoins + (totalGems > 0 ? "   Gems " + gems + "/" + totalGems : "");
            completeStats.text = string.Format("Progress: 100.000%\nAttempts: {0}   (all time: {4}, cleared {5}x)\nTime: {1:0.0}s   Jumps: {2}\n{3}", attempts, seconds, jumps, loot, totalAttempts, completions);
            completePanel.gameObject.SetActive(true);
        }

        public void HideComplete() => completePanel.gameObject.SetActive(false);

        public void Flash(Color color, float duration)
        {
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
