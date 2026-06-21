using UnityEngine;

namespace IdleOff.Game
{
    public readonly struct FloatingFeedbackRequest
    {
        public FloatingFeedbackRequest(
            string text,
            Vector3 worldPosition,
            FloatingFeedbackType type,
            float lifetime = 1.5f,
            float fontSizeMultiplier = 0.25f,
            Transform sizeReference = null,
            Transform followTarget = null,
            Vector3 followWorldOffset = default)
        {
            Text = text;
            WorldPosition = worldPosition;
            Type = type;
            Lifetime = lifetime;
            FontSizeMultiplier = fontSizeMultiplier;
            SizeReference = sizeReference;
            FollowTarget = followTarget;
            FollowWorldOffset = followWorldOffset;
        }

        public string Text { get; }
        public Vector3 WorldPosition { get; }
        public FloatingFeedbackType Type { get; }
        public float Lifetime { get; }
        public float FontSizeMultiplier { get; }
        public Transform SizeReference { get; }
        public Transform FollowTarget { get; }
        public Vector3 FollowWorldOffset { get; }
    }
}
