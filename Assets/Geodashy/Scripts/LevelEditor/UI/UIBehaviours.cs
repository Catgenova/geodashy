using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Geodashy.Editing.UI
{
    /// <summary>Shows a tooltip after a short hover. Attach with UIFactory.Tip.</summary>
    public class Tooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
    {
        public const string Pref = "geodashy.tooltips";
        public static bool Enabled
        {
            get => PlayerPrefs.GetInt(Pref, 1) == 1;
            set => PlayerPrefs.SetInt(Pref, value ? 1 : 0);
        }

        public string text;
        float hoverT = -1f;
        bool shown;

        public void OnPointerEnter(PointerEventData e)
        {
            hoverT = 0f;
        }

        public void OnPointerExit(PointerEventData e)
        {
            hoverT = -1f;
            if (shown)
            {
                shown = false;
                if (EditorUI.Instance != null) EditorUI.Instance.HideTooltip(this);
            }
        }

        public void OnPointerDown(PointerEventData e) => OnPointerExit(e);

        void Update()
        {
            if (hoverT < 0f || shown) return;
            hoverT += Time.unscaledDeltaTime;
            if (hoverT > 0.45f && Enabled && EditorUI.Instance != null && !string.IsNullOrEmpty(text))
            {
                shown = true;
                EditorUI.Instance.ShowTooltip(this, text, transform as RectTransform);
            }
        }

        void OnDisable()
        {
            if (shown && EditorUI.Instance != null) EditorUI.Instance.HideTooltip(this);
            shown = false;
            hoverT = -1f;
        }
    }

    /// <summary>Accent outline that appears while the pointer is over a button.</summary>
    public class HoverGlow : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        Image ring;

        public static void Attach(RectTransform target)
        {
            if (target.GetComponent<HoverGlow>() != null) return;
            target.gameObject.AddComponent<HoverGlow>();
        }

        void EnsureRing()
        {
            if (ring != null) return;
            var rt = UIFactory.Rect(transform, "Glow");
            ring = rt.gameObject.AddComponent<Image>();
            ring.sprite = Geodashy.Rendering.PlaceholderSpriteFactory.Outline();
            ring.type = Image.Type.Sliced;
            ring.color = new Color(UIFactory.Accent.r, UIFactory.Accent.g, UIFactory.Accent.b, 0.85f);
            ring.raycastTarget = false;
            UIFactory.Stretch(rt, -1, -1, -1, -1);
            rt.SetAsLastSibling();
            ring.enabled = false;
        }

        public void OnPointerEnter(PointerEventData e)
        {
            EnsureRing();
            ring.enabled = true;
        }

        public void OnPointerExit(PointerEventData e)
        {
            if (ring != null) ring.enabled = false;
        }

        void OnDisable()
        {
            if (ring != null) ring.enabled = false;
        }
    }

    /// <summary>Drag left or right on a field's label to scrub its value; Shift for fine steps.</summary>
    public class ScrubHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public Action<float> onDelta;
        public Action onBegin, onEnd;
        float carry;

        public void OnBeginDrag(PointerEventData e)
        {
            carry = 0f;
            onBegin?.Invoke();
        }

        public void OnDrag(PointerEventData e)
        {
            bool fine = UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.shiftKey.isPressed;
            carry += e.delta.x / (fine ? 40f : 8f);
            int steps = (int)carry;
            if (steps != 0)
            {
                carry -= steps;
                onDelta?.Invoke(steps);
            }
        }

        public void OnEndDrag(PointerEventData e) => onEnd?.Invoke();
    }

    /// <summary>Shows a tile's caption only while hovered (desktop palette).</summary>
    public class HoverCaption : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public GameObject caption;
        public void OnPointerEnter(PointerEventData e) { if (caption != null) caption.SetActive(true); }
        public void OnPointerExit(PointerEventData e) { if (caption != null) caption.SetActive(false); }
        void OnDisable() { if (caption != null) caption.SetActive(false); }
    }
}
