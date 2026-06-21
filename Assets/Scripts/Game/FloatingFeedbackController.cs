using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace IdleOff.Game
{
    public sealed class FloatingFeedbackController : MonoBehaviour
    {
        private const int MaxActiveFeedback = 60;
        private const float StackDistancePixels = 96f;
        private const float StackStepPixels = 26f;
        private const int MaxStackSlots = 8;
        private readonly List<FloatingFeedbackView> views = new();
        private Canvas canvas;

        public static FloatingFeedbackController Instance { get; private set; }

        public static FloatingFeedbackController EnsureExists()
        {
            if (Instance != null)
            {
                return Instance;
            }

            var existing = FindFirstObjectByType<FloatingFeedbackController>(FindObjectsInactive.Include);
            if (existing != null)
            {
                Instance = existing;
                existing.gameObject.SetActive(true);
                existing.EnsureBuilt();
                return existing;
            }

            var controllerObject = new GameObject("Floating Feedback Controller");
            DontDestroyOnLoad(controllerObject);
            Instance = controllerObject.AddComponent<FloatingFeedbackController>();
            Instance.EnsureBuilt();
            return Instance;
        }

        public void Show(FloatingFeedbackRequest request)
        {
            EnsureBuilt();
            var camera = Camera.main;
            if (camera == null || string.IsNullOrWhiteSpace(request.Text))
            {
                return;
            }

            var screenPosition = camera.WorldToScreenPoint(request.WorldPosition);
            var view = GetAvailableView();
            var style = GetStyle(request.Type);
            var stackOffset = ResolveStackOffset(screenPosition);
            view.Show(
                request.Text,
                screenPosition,
                ResolveFontSize(request, camera),
                style.Foreground,
                style.Outline,
                request.Lifetime,
                stackOffset,
                request.FollowTarget,
                request.FollowWorldOffset);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            EnsureBuilt();
        }

        private void Update()
        {
            foreach (var view in views)
            {
                view.Tick(Time.deltaTime);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void EnsureBuilt()
        {
            canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
            }

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 4500;

            if (GetComponent<CanvasScaler>() == null)
            {
                gameObject.AddComponent<CanvasScaler>();
            }

            if (GetComponent<GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }
        }

        private FloatingFeedbackView GetAvailableView()
        {
            foreach (var view in views)
            {
                if (!view.IsActive)
                {
                    return view;
                }
            }

            if (views.Count >= MaxActiveFeedback)
            {
                views[0].Hide();
                return views[0];
            }

            var viewObject = new GameObject("Floating Feedback Text");
            viewObject.transform.SetParent(transform, false);
            var newView = viewObject.AddComponent<FloatingFeedbackView>();
            newView.Build();
            views.Add(newView);
            return newView;
        }

        private Vector2 ResolveStackOffset(Vector2 screenPosition)
        {
            var stackedCount = 0;
            foreach (var view in views)
            {
                if (view.IsStackedNear(screenPosition, StackDistancePixels))
                {
                    stackedCount++;
                }
            }

            var stackSlot = stackedCount % MaxStackSlots;
            return Vector2.up * (StackStepPixels * stackSlot);
        }

        private static int ResolveFontSize(FloatingFeedbackRequest request, Camera camera)
        {
            var referenceHeight = ResolveReferenceScreenHeight(request.SizeReference, camera);
            return Mathf.RoundToInt(referenceHeight * Mathf.Max(0.05f, request.FontSizeMultiplier));
        }

        private static float ResolveReferenceScreenHeight(Transform reference, Camera camera)
        {
            if (reference != null)
            {
                var collider = reference.GetComponentInChildren<Collider2D>();
                if (collider != null)
                {
                    var bottom = camera.WorldToScreenPoint(new Vector3(collider.bounds.center.x, collider.bounds.min.y, collider.bounds.center.z));
                    var top = camera.WorldToScreenPoint(new Vector3(collider.bounds.center.x, collider.bounds.max.y, collider.bounds.center.z));
                    return Mathf.Max(24f, Mathf.Abs(top.y - bottom.y));
                }
            }

            return 64f;
        }

        private static FloatingFeedbackStyle GetStyle(FloatingFeedbackType type)
        {
            return type switch
            {
                FloatingFeedbackType.DamageTaken => new FloatingFeedbackStyle(Color.white, new Color32(210, 30, 45, 255)),
                FloatingFeedbackType.DamageDealt => new FloatingFeedbackStyle(Color.white, new Color32(245, 190, 40, 255)),
                FloatingFeedbackType.Miss => new FloatingFeedbackStyle(new Color32(150, 150, 150, 255), new Color32(55, 55, 55, 255)),
                FloatingFeedbackType.LevelUp => new FloatingFeedbackStyle(Color.white, new Color32(245, 190, 40, 255)),
                FloatingFeedbackType.BaseTalentPoints => new FloatingFeedbackStyle(Color.white, new Color32(145, 65, 210, 255)),
                FloatingFeedbackType.ClassTalentPoints => new FloatingFeedbackStyle(Color.white, new Color32(145, 65, 210, 255)),
                _ => new FloatingFeedbackStyle(Color.white, Color.black)
            };
        }

        private readonly struct FloatingFeedbackStyle
        {
            public FloatingFeedbackStyle(Color foreground, Color outline)
            {
                Foreground = foreground;
                Outline = outline;
            }

            public Color Foreground { get; }
            public Color Outline { get; }
        }
    }
}
