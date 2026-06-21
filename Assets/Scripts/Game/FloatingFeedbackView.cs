using UnityEngine;
using UnityEngine.UI;

namespace IdleOff.Game
{
    public sealed class FloatingFeedbackView : MonoBehaviour
    {
        private const float DownwardDriftPixels = 40f;

        private RectTransform rect;
        private Text text;
        private Outline outline;
        private Color textColor;
        private Color outlineColor;
        private Vector2 startPosition;
        private Vector2 stackOriginPosition;
        private Vector2 stackOffset;
        private Transform followTarget;
        private Vector3 followWorldOffset;
        private float lifetime;
        private float elapsed;

        public bool IsActive { get; private set; }

        public void Build()
        {
            rect = gameObject.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(260f, 48f);
            text = gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;

            outline = gameObject.AddComponent<Outline>();
            outline.effectDistance = new Vector2(2f, -2f);
            outline.useGraphicAlpha = true;
            gameObject.SetActive(false);
        }

        public void Show(
            string value,
            Vector2 screenPosition,
            int fontSize,
            Color foreground,
            Color outlineTint,
            float seconds,
            Vector2 pileOffset,
            Transform follow = null,
            Vector3 followOffset = default)
        {
            if (rect == null)
            {
                Build();
            }

            text.text = value;
            text.fontSize = Mathf.Max(8, fontSize);
            textColor = foreground;
            outlineColor = outlineTint;
            text.color = textColor;
            outline.effectColor = outlineColor;
            lifetime = Mathf.Max(0.05f, seconds);
            elapsed = 0f;
            startPosition = screenPosition;
            stackOriginPosition = screenPosition;
            stackOffset = pileOffset;
            followTarget = follow;
            followWorldOffset = followOffset;
            rect.position = screenPosition + stackOffset;
            IsActive = true;
            gameObject.SetActive(true);
        }

        public void Tick(float deltaTime)
        {
            if (!IsActive)
            {
                return;
            }

            elapsed += Mathf.Max(0f, deltaTime);
            var t = Mathf.Clamp01(elapsed / lifetime);
            var anchorPosition = ResolveAnchorPosition();
            rect.position = anchorPosition + stackOffset + Vector2.down * (DownwardDriftPixels * t);
            var alpha = 1f - t;
            text.color = new Color(textColor.r, textColor.g, textColor.b, textColor.a * alpha);
            outline.effectColor = new Color(outlineColor.r, outlineColor.g, outlineColor.b, outlineColor.a * alpha);

            if (elapsed >= lifetime)
            {
                Hide();
            }
        }

        public void Hide()
        {
            IsActive = false;
            followTarget = null;
            gameObject.SetActive(false);
        }

        public bool IsStackedNear(Vector2 screenPosition, float distance)
        {
            return IsActive && Vector2.Distance(stackOriginPosition, screenPosition) <= distance;
        }

        private Vector2 ResolveAnchorPosition()
        {
            if (followTarget == null)
            {
                return startPosition;
            }

            var camera = Camera.main;
            if (camera == null)
            {
                return startPosition;
            }

            return camera.WorldToScreenPoint(followTarget.position + followWorldOffset);
        }
    }
}
