using UnityEngine;
using UnityEngine.UI;
using IdleOff.Profiles;

namespace IdleOff.Game
{
    [DisallowMultipleComponent]
    public sealed class GameplayHud : MonoBehaviour
    {
        public const float HeightPercent = 0.20f;

        [SerializeField] private Canvas canvas;
        [SerializeField] private RectTransform root;
        [SerializeField] private RectTransform sectionOne;
        [SerializeField] private RectTransform sectionTwo;
        [SerializeField] private RectTransform sectionThree;
        [SerializeField] private RectTransform sectionFour;
        [SerializeField] private Text characterNameText;
        [SerializeField] private Text characterClassText;
        [SerializeField] private Text characterLevelText;

        private Font hudFont;
        private CharacterData displayedCharacter;
        private CharacterClass displayedClass;

        public static GameplayHud Instance { get; private set; }
        public RectTransform Root => root;
        public RectTransform SectionOne => sectionOne;
        public RectTransform SectionTwo => sectionTwo;
        public RectTransform SectionThree => sectionThree;
        public RectTransform SectionFour => sectionFour;

        public void SetCharacter(CharacterData character)
        {
            if (displayedClass != null)
            {
                displayedClass.Changed -= RefreshDisplayedCharacter;
            }

            displayedCharacter = character;
            displayedClass = character?.CharacterClass;
            if (displayedClass != null)
            {
                displayedClass.Changed += RefreshDisplayedCharacter;
            }

            RefreshDisplayedCharacter();
        }

        private void RefreshDisplayedCharacter()
        {
            if (displayedCharacter == null)
            {
                SetSectionOneText(string.Empty, string.Empty, string.Empty);
                return;
            }

            SetSectionOneText(
                displayedCharacter.CharacterName,
                displayedCharacter.CharacterClass.GetClassName(),
                $"LVL {displayedCharacter.Level:00}");
        }

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
            if (displayedClass != null)
            {
                displayedClass.Changed -= RefreshDisplayedCharacter;
            }

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

            hudFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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

            sectionOne = CreateSection("HUD Section 1", 0.01f, 0.20f);
            sectionTwo = CreateSection("HUD Section 2", 0.21f, 0.40f);
            sectionThree = CreateSection("HUD Section 3", 0.41f, 0.695f);
            sectionFour = CreateSection("HUD Section 4", 0.705f, 0.99f);
            BuildSectionOneCharacterWidget();
        }

        private RectTransform CreateSection(string sectionName, float anchorMinX, float anchorMaxX)
        {
            var section = new GameObject(sectionName);
            section.transform.SetParent(root, false);

            var rect = section.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(anchorMinX, 0.05f);
            rect.anchorMax = new Vector2(anchorMaxX, 0.95f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = section.AddComponent<Image>();
            image.color = new Color32(28, 31, 42, 255);
            return rect;
        }

        private void BuildSectionOneCharacterWidget()
        {
            characterNameText = CreateSectionOneText("Character Name", 0.65f, 0.95f);
            characterClassText = CreateSectionOneText("Character Class", 0.35f, 0.65f);
            characterLevelText = CreateSectionOneText("Character Level", 0.05f, 0.35f);
            SetSectionOneText(string.Empty, string.Empty, string.Empty);
        }

        private Text CreateSectionOneText(string objectName, float anchorMinY, float anchorMaxY)
        {
            var textObject = new GameObject(objectName);
            textObject.transform.SetParent(sectionOne, false);

            var rect = textObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.05f, anchorMinY);
            rect.anchorMax = new Vector2(0.95f, anchorMaxY);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var text = textObject.AddComponent<Text>();
            text.font = hudFont;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 8;
            text.resizeTextMaxSize = 80;
            return text;
        }

        private void SetSectionOneText(string characterName, string className, string levelText)
        {
            if (characterNameText != null)
            {
                characterNameText.text = characterName;
            }

            if (characterClassText != null)
            {
                characterClassText.text = className;
            }

            if (characterLevelText != null)
            {
                characterLevelText.text = levelText;
            }
        }
    }
}
