using UnityEngine;
using UnityEngine.UI;
using IdleOff.Combat;
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
        private PlayerCombatant displayedPlayer;
        private HudValueBar hpBar;
        private HudValueBar mpBar;
        private HudValueBar xpBar;

        public static GameplayHud Instance { get; private set; }
        public RectTransform Root => root;
        public RectTransform SectionOne => sectionOne;
        public RectTransform SectionTwo => sectionTwo;
        public RectTransform SectionThree => sectionThree;
        public RectTransform SectionFour => sectionFour;

        public void SetCharacter(CharacterData character)
        {
            EnsureBuilt();

            if (displayedClass != null)
            {
                displayedClass.Changed -= RefreshDisplayedCharacter;
            }

            if (displayedCharacter != null)
            {
                displayedCharacter.StatsChanged -= RefreshResourceBars;
            }

            displayedCharacter = character;
            displayedClass = character?.CharacterClass;
            if (displayedClass != null)
            {
                displayedClass.Changed += RefreshDisplayedCharacter;
            }

            if (displayedCharacter != null)
            {
                displayedCharacter.StatsChanged += RefreshResourceBars;
            }

            RefreshDisplayedCharacter();
            RefreshResourceBars();
        }

        public void SetPlayer(PlayerCombatant player)
        {
            EnsureBuilt();

            if (displayedPlayer != null)
            {
                displayedPlayer.ResourcesChanged -= RefreshResourceBars;
            }

            displayedPlayer = player;
            if (displayedPlayer != null)
            {
                displayedPlayer.ResourcesChanged += RefreshResourceBars;
            }

            RefreshResourceBars();
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
            RefreshResourceBars();
        }

        public static GameplayHud EnsureExists()
        {
            if (Instance != null)
            {
                Instance.gameObject.SetActive(true);
                Instance.EnsureBuilt();
                return Instance;
            }

            var existing = FindFirstObjectByType<GameplayHud>(FindObjectsInactive.Include);
            if (existing != null)
            {
                existing.gameObject.SetActive(true);
                existing.EnsureBuilt();
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
                EnsureBuilt();
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

            if (displayedCharacter != null)
            {
                displayedCharacter.StatsChanged -= RefreshResourceBars;
            }

            if (displayedPlayer != null)
            {
                displayedPlayer.ResourcesChanged -= RefreshResourceBars;
            }

            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void EnsureBuilt()
        {
            if (root != null && sectionOne != null && sectionTwo != null && sectionThree != null && sectionFour != null)
            {
                return;
            }

            Build();
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
            BuildSectionTwoResourceWidget();
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
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
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

        private void BuildSectionTwoResourceWidget()
        {
            hpBar = CreateValueBar(sectionTwo, "HP Bar", "HP", new Color32(190, 54, 58, 255), 0.68f, 0.95f);
            mpBar = CreateValueBar(sectionTwo, "MP Bar", "MP", new Color32(67, 111, 210, 255), 0.365f, 0.635f);
            xpBar = CreateValueBar(sectionTwo, "XP Bar", "XP", new Color32(219, 170, 62, 255), 0.05f, 0.32f);
            RefreshResourceBars();
        }

        private HudValueBar CreateValueBar(RectTransform parent, string objectName, string label, Color32 fillColor, float anchorMinY, float anchorMaxY)
        {
            var rowObject = new GameObject(objectName);
            rowObject.transform.SetParent(parent, false);
            var row = rowObject.AddComponent<RectTransform>();
            row.anchorMin = new Vector2(0.05f, anchorMinY);
            row.anchorMax = new Vector2(0.95f, anchorMaxY);
            row.offsetMin = Vector2.zero;
            row.offsetMax = Vector2.zero;

            var labelText = CreatePaddedText(row, objectName + " Label", label, TextAnchor.MiddleLeft, 0f, 0.2f, 0.05f);
            ConfigureResourceTextFit(labelText);

            var meterObject = new GameObject(objectName + " Meter");
            meterObject.transform.SetParent(row, false);
            var meter = meterObject.AddComponent<RectTransform>();
            meter.anchorMin = new Vector2(0.2f, 0f);
            meter.anchorMax = Vector2.one;
            meter.offsetMin = Vector2.zero;
            meter.offsetMax = Vector2.zero;
            meterObject.AddComponent<Image>().color = new Color32(8, 9, 12, 255);

            var fillObject = new GameObject(objectName + " Fill");
            fillObject.transform.SetParent(meter, false);
            var fill = fillObject.AddComponent<RectTransform>();
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(0f, 1f);
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
            fillObject.AddComponent<Image>().color = fillColor;

            var valueText = CreatePaddedText(meter, objectName + " Value", string.Empty, TextAnchor.MiddleCenter, 0f, 1f, 0.05f);
            ConfigureResourceTextFit(valueText);
            valueText.raycastTarget = false;

            return new HudValueBar(fill, valueText);
        }

        private Text CreatePaddedText(RectTransform parent, string objectName, string value, TextAnchor alignment, float anchorMinX, float anchorMaxX, float paddingPercent)
        {
            var boundsObject = new GameObject(objectName + " Bounds");
            boundsObject.transform.SetParent(parent, false);
            var bounds = boundsObject.AddComponent<RectTransform>();
            bounds.anchorMin = new Vector2(anchorMinX, 0f);
            bounds.anchorMax = new Vector2(anchorMaxX, 1f);
            bounds.offsetMin = Vector2.zero;
            bounds.offsetMax = Vector2.zero;

            var textObject = new GameObject(objectName);
            textObject.transform.SetParent(bounds, false);
            var textRect = textObject.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(paddingPercent, paddingPercent);
            textRect.anchorMax = new Vector2(1f - paddingPercent, 1f - paddingPercent);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var text = textObject.AddComponent<Text>();
            text.font = hudFont;
            text.text = value;
            text.color = Color.white;
            text.alignment = alignment;
            return text;
        }

        private Text CreateText(RectTransform parent, string objectName, string value, TextAnchor alignment, float anchorMinX, float anchorMaxX)
        {
            var textObject = new GameObject(objectName);
            textObject.transform.SetParent(parent, false);
            var rect = textObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(anchorMinX, 0f);
            rect.anchorMax = new Vector2(anchorMaxX, 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var text = textObject.AddComponent<Text>();
            text.font = hudFont;
            text.text = value;
            text.color = Color.white;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private static void ConfigureResourceTextFit(Text text)
        {
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 4;
            text.resizeTextMaxSize = 80;
            text.supportRichText = false;
        }

        private void RefreshResourceBars()
        {
            if (displayedPlayer != null)
            {
                hpBar?.Set(displayedPlayer.CurrentHp, displayedPlayer.MaxHp);
                mpBar?.Set(displayedPlayer.CurrentMp, displayedPlayer.MaxMp);
            }
            else
            {
                hpBar?.Set(0f, 1f);
                mpBar?.Set(0f, 1f);
            }

            var characterClass = displayedCharacter?.CharacterClass;
            xpBar?.Set(characterClass?.GetCurrentXP() ?? 0f, characterClass?.GetMaxXP() ?? 1f);
        }

        private sealed class HudValueBar
        {
            private readonly RectTransform fill;
            private readonly Text valueText;

            public HudValueBar(RectTransform fill, Text valueText)
            {
                this.fill = fill;
                this.valueText = valueText;
            }

            public void Set(float current, float max)
            {
                var safeMax = Mathf.Max(1f, max);
                var safeCurrent = Mathf.Clamp(current, 0f, safeMax);
                var ratio = Mathf.Clamp01(safeCurrent / safeMax);
                if (fill != null)
                {
                    fill.anchorMax = new Vector2(ratio, 1f);
                }

                if (valueText != null)
                {
                    valueText.text = $"{ratio * 100f:0.0}%({safeCurrent:0.0}/{safeMax:0.0})";
                }
            }
        }
    }
}
