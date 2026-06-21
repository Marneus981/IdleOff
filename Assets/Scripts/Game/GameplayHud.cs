using UnityEngine;
using UnityEngine.UI;

namespace IdleOff.Game
{
    [DisallowMultipleComponent]
    public sealed class GameplayHud : MonoBehaviour
    {
        public const float HeightPercent = 0.20f;

        [SerializeField] private Canvas canvas;
        [SerializeField] private RectTransform root;

        public static GameplayHud Instance { get; private set; }
        public RectTransform Root => root;

        public static GameplayHud EnsureExists()
        {
            if (Instance != null)
            {
                Instance.gameObject.SetActive(true);
                return Instance;
            }

            var existing = FindFirstObjectByType<GameplayHud>(FindObjectsInactive.Include);
            if (existing != null)
            {
                existing.gameObject.SetActive(true);
                return existing;
            }

            return new GameObject("Gameplay HUD").AddComponent<GameplayHud>();
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                Build();
                return;
            }

            if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Build()
        {
            canvas = gameObject.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
            }

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;

            var scaler = gameObject.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = gameObject.AddComponent<CanvasScaler>();
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

            if (gameObject.GetComponent<GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }

            var panel = new GameObject("Gameplay HUD Root");
            panel.transform.SetParent(transform, false);
            root = panel.AddComponent<RectTransform>();
            root.anchorMin = Vector2.zero;
            root.anchorMax = new Vector2(1f, HeightPercent);
            root.pivot = Vector2.zero;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            var image = panel.AddComponent<Image>();
            image.color = new Color32(18, 20, 28, 255);
        }
    }
}
