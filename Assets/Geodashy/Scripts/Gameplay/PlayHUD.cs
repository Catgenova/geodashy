using System;
using Geodashy.Editing.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Geodashy.Gameplay
{
    /// <summary>Progress bar, attempt counter, mount hint, pause and completion panels.</summary>
    public class PlayHUD : MonoBehaviour
    {
        Image progressFill;
        Text attemptText, coinText, hintText, progressText;
        RectTransform pausePanel, completePanel;
        Text completeStats;
        float hintTimer;

        public static PlayHUD Create(Transform parent, Action onResume, Action onRestart, Action onExit)
        {
            var go = new GameObject("Play HUD");
            go.transform.SetParent(parent, false);
            var hud = go.AddComponent<PlayHUD>();
            hud.Build(onResume, onRestart, onExit);
            return hud;
        }

        void Build(Action onResume, Action onRestart, Action onExit)
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();

            var root = UIFactory.Rect(transform, "Root");
            UIFactory.Stretch(root);

            // progress bar
            var barBg = UIFactory.Panel(root, "ProgressBg", new Color(0, 0, 0, 0.5f));
            UIFactory.Anchor(barBg, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-300, -34), new Vector2(300, -14));
            barBg.GetComponent<Image>().raycastTarget = false;
            var fillRt = UIFactory.Rect(barBg, "Fill");
            progressFill = fillRt.gameObject.AddComponent<Image>();
            progressFill.color = UIFactory.Accent;
            progressFill.raycastTarget = false;
            UIFactory.Anchor(fillRt, new Vector2(0, 0), new Vector2(0, 1), new Vector2(2, 2), new Vector2(2, -2));
            progressText = UIFactory.Label(root, "0%", 14, TextAnchor.MiddleCenter, UIFactory.TextColor);
            UIFactory.Anchor(progressText.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-60, -60), new Vector2(60, -36));

            attemptText = UIFactory.Label(root, "Attempt 1", 22, TextAnchor.MiddleLeft, UIFactory.TextColor, -1, -1, true);
            UIFactory.Anchor(attemptText.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -60), new Vector2(400, -14));
            coinText = UIFactory.Label(root, "", 18, TextAnchor.MiddleRight, UIFactory.Accent);
            UIFactory.Anchor(coinText.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-400, -60), new Vector2(-20, -14));
            hintText = UIFactory.Label(root, "", 18, TextAnchor.MiddleLeft, UIFactory.TextColor);
            UIFactory.Anchor(hintText.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(20, 16), new Vector2(900, 50));
            var esc = UIFactory.Label(root, "Esc — pause / back to editor", 13, TextAnchor.MiddleRight, UIFactory.TextDim);
            UIFactory.Anchor(esc.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-400, 16), new Vector2(-20, 40));

            // pause panel
            pausePanel = UIFactory.Panel(root, "Pause", new Color(0, 0, 0, 0.6f));
            var pw = UIFactory.Panel(pausePanel, "Window", UIFactory.PanelBg2);
            UIFactory.Anchor(pw, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-200, -130), new Vector2(200, 130));
            UIFactory.VLayout(pw, 10, 20);
            UIFactory.Label(pw, "PAUSED", 26, TextAnchor.MiddleCenter, UIFactory.Accent, -1, 40, true);
            UIFactory.Button(pw, "Resume", () => onResume(), -1, 40, UIFactory.Good, 16);
            UIFactory.Button(pw, "Restart from start", () => onRestart(), -1, 40, null, 16);
            UIFactory.Button(pw, "Back to editor", () => onExit(), -1, 40, UIFactory.Danger, 16);
            pausePanel.gameObject.SetActive(false);

            // complete panel
            completePanel = UIFactory.Panel(root, "Complete", new Color(0, 0, 0, 0.6f));
            var cw = UIFactory.Panel(completePanel, "Window", UIFactory.PanelBg2);
            UIFactory.Anchor(cw, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-240, -160), new Vector2(240, 160));
            UIFactory.VLayout(cw, 10, 20);
            UIFactory.Label(cw, "QUEST COMPLETE", 28, TextAnchor.MiddleCenter, UIFactory.Accent, -1, 44, true);
            completeStats = UIFactory.Label(cw, "", 16, TextAnchor.MiddleCenter, UIFactory.TextColor, -1, 70);
            UIFactory.Button(cw, "Play again", () => onRestart(), -1, 40, UIFactory.Good, 16);
            UIFactory.Button(cw, "Back to editor", () => onExit(), -1, 40, null, 16);
            completePanel.gameObject.SetActive(false);
        }

        public void SetProgress(float t)
        {
            t = Mathf.Clamp01(t);
            var rt = progressFill.rectTransform;
            rt.anchorMax = new Vector2(t, 1);
            rt.offsetMax = new Vector2(-2, -2);
            progressText.text = Mathf.RoundToInt(t * 100f) + "%";
        }

        public void SetAttempt(int n) => attemptText.text = "Attempt " + n;
        public void SetCoins(int n, int total) => coinText.text = total > 0 ? "Loot " + n + " / " + total : "";

        public void ShowHint(string text, float seconds = 4f)
        {
            hintText.text = text;
            hintText.color = UIFactory.TextColor;
            hintTimer = seconds;
        }

        public void ShowPause(bool on) => pausePanel.gameObject.SetActive(on);

        public void ShowComplete(int attempts, float seconds, int jumps, int coins, int totalCoins)
        {
            completeStats.text = string.Format("Attempts: {0}\nTime: {1:0.0}s   Jumps: {2}\nLoot: {3} / {4}", attempts, seconds, jumps, coins, totalCoins);
            completePanel.gameObject.SetActive(true);
        }

        public void HideComplete() => completePanel.gameObject.SetActive(false);

        void Update()
        {
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
