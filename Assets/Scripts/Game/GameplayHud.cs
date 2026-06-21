using System.Collections;
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
        [SerializeField] private Button inventoryButton;
        [SerializeField] private Button skillsButton;
        [SerializeField] private Button menuButton;
        [SerializeField] private RectTransform menuPanel;
        [SerializeField] private RectTransform skillsPanel;
        [SerializeField] private RectTransform skillsContent;
        [SerializeField] private RectTransform skillsBottomCanvas;
        [SerializeField] private ScrollRect skillsScrollRect;
        [SerializeField] private Text baseTalentPointsText;
        [SerializeField] private Text classTalentPointsText;
        [SerializeField] private Button titleScreenButton;
        [SerializeField] private Button characterSelectionButton;

        private Font hudFont;
        private CharacterData displayedCharacter;
        private CharacterClass displayedClass;
        private PlayerCombatant displayedPlayer;
        private HudValueBar hpBar;
        private HudValueBar mpBar;
        private HudValueBar xpBar;
        private Coroutine menuAnimation;
        private Coroutine skillsAnimation;
        private bool menuOpen;
        private bool skillsOpen;

        public static GameplayHud Instance { get; private set; }
        public RectTransform Root => root;
        public RectTransform SectionOne => sectionOne;
        public RectTransform SectionTwo => sectionTwo;
        public RectTransform SectionThree => sectionThree;
        public RectTransform SectionFour => sectionFour;

        public void SetCharacter(CharacterData character)
        {
            EnsureBuilt();
            CloseExpandablePanelsImmediate();

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
            CloseExpandablePanelsImmediate();

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
            RefreshSkillsPanel();
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

        private void OnDisable()
        {
            CloseExpandablePanelsImmediate();
        }

        private void OnEnable()
        {
            CloseExpandablePanelsImmediate();
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
            BuildSectionFourMenuWidget();
            BuildExpandableMenuPanel();
            BuildExpandableSkillsPanel();
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

        private void BuildSectionFourMenuWidget()
        {
            inventoryButton = CreateHudButton(sectionFour, "Inventory Button", "Inventory", 0f, 0.30f, ShowInventory);
            skillsButton = CreateHudButton(sectionFour, "Skills Button", "Skills", 0.35f, 0.65f, ShowSkills);
            menuButton = CreateHudButton(sectionFour, "Menu Button", "Menu", 0.70f, 1f, ShowMenu);
        }

        private Button CreateHudButton(RectTransform parent, string objectName, string label, float anchorMinX, float anchorMaxX, UnityEngine.Events.UnityAction onClick)
        {
            return CreateHudButton(parent, objectName, label, anchorMinX, anchorMaxX, 0f, 1f, onClick);
        }

        private Button CreateHudButton(RectTransform parent, string objectName, string label, float anchorMinX, float anchorMaxX, float anchorMinY, float anchorMaxY, UnityEngine.Events.UnityAction onClick)
        {
            var buttonObject = new GameObject(objectName);
            buttonObject.transform.SetParent(parent, false);
            var rect = buttonObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(anchorMinX, anchorMinY);
            rect.anchorMax = new Vector2(anchorMaxX, anchorMaxY);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = buttonObject.AddComponent<Image>();
            image.color = new Color32(48, 52, 68, 255);

            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);

            var text = CreatePaddedText(rect, objectName + " Text", label, TextAnchor.MiddleCenter, 0f, 1f, 0.05f);
            ConfigureResourceTextFit(text);
            text.raycastTarget = false;

            return button;
        }

        public void ShowInventory()
        {
            Debug.Log("[HUD] Inventory button pressed.");
        }

        public void ShowSkills()
        {
            SetSkillsOpen(!skillsOpen);
        }

        public void ShowMenu()
        {
            SetMenuOpen(!menuOpen);
        }

        public void ShowTitleScreen()
        {
            if (BootFlowUI.Instance == null)
            {
                Debug.LogWarning("[HUD] Cannot return to title screen because no BootFlowUI instance exists.");
                return;
            }

            BootFlowUI.Instance.ReturnToTitleScreenFromGameplay();
        }

        public void ShowCharacterSelection()
        {
            if (BootFlowUI.Instance == null)
            {
                Debug.LogWarning("[HUD] Cannot return to character selection because no BootFlowUI instance exists.");
                return;
            }

            BootFlowUI.Instance.ReturnToCharacterSelectionFromGameplay();
        }

        private void BuildExpandableMenuPanel()
        {
            var panelObject = new GameObject("HUD Menu Panel");
            panelObject.transform.SetParent(root, false);
            menuPanel = panelObject.AddComponent<RectTransform>();
            menuPanel.anchorMin = new Vector2(0.705f, 1f);
            menuPanel.anchorMax = new Vector2(0.99f, 1f);
            menuPanel.pivot = new Vector2(0.5f, 0f);
            menuPanel.offsetMin = Vector2.zero;
            menuPanel.offsetMax = Vector2.zero;

            var image = panelObject.AddComponent<Image>();
            image.color = new Color32(24, 27, 38, 255);

            titleScreenButton = CreateHudButton(menuPanel, "Title Screen Button", "Title Screen", 0.05f, 0.95f, 0.05f, 0.475f, ShowTitleScreen);
            characterSelectionButton = CreateHudButton(menuPanel, "Character Selection Button", "Character Selection", 0.05f, 0.95f, 0.525f, 0.95f, ShowCharacterSelection);
            menuPanel.gameObject.SetActive(false);
        }

        private void BuildExpandableSkillsPanel()
        {
            var panelObject = new GameObject("HUD Skills Panel");
            panelObject.transform.SetParent(root, false);
            skillsPanel = panelObject.AddComponent<RectTransform>();
            skillsPanel.anchorMin = new Vector2(0.41f, 1f);
            skillsPanel.anchorMax = new Vector2(0.99f, 1f);
            skillsPanel.pivot = new Vector2(0.5f, 0f);
            skillsPanel.offsetMin = Vector2.zero;
            skillsPanel.offsetMax = Vector2.zero;

            var image = panelObject.AddComponent<Image>();
            image.color = new Color32(24, 27, 38, 255);

            var innerCanvas = new GameObject("HUD Skills Canvas");
            innerCanvas.transform.SetParent(skillsPanel, false);
            var innerCanvasRect = innerCanvas.AddComponent<RectTransform>();
            innerCanvasRect.anchorMin = new Vector2(0.05f, 0.05f);
            innerCanvasRect.anchorMax = new Vector2(0.95f, 0.95f);
            innerCanvasRect.offsetMin = Vector2.zero;
            innerCanvasRect.offsetMax = Vector2.zero;
            innerCanvas.AddComponent<Image>().color = new Color32(28, 31, 42, 255);

            var scrollField = new GameObject("HUD Skills Scroll Field");
            scrollField.transform.SetParent(innerCanvasRect, false);
            var scrollFieldRect = scrollField.AddComponent<RectTransform>();
            scrollFieldRect.anchorMin = new Vector2(0f, 0.20f);
            scrollFieldRect.anchorMax = Vector2.one;
            scrollFieldRect.offsetMin = Vector2.zero;
            scrollFieldRect.offsetMax = Vector2.zero;
            scrollField.AddComponent<Image>().color = new Color32(12, 14, 20, 255);
            skillsScrollRect = scrollField.AddComponent<ScrollRect>();
            skillsScrollRect.horizontal = false;
            skillsScrollRect.vertical = true;
            skillsScrollRect.movementType = ScrollRect.MovementType.Clamped;

            var viewport = new GameObject("HUD Skills Viewport");
            viewport.transform.SetParent(scrollFieldRect, false);
            var viewportRect = viewport.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            viewport.AddComponent<Image>().color = new Color32(0, 0, 0, 1);
            viewport.AddComponent<Mask>().showMaskGraphic = false;

            var content = new GameObject("HUD Skills Content");
            content.transform.SetParent(viewportRect, false);
            skillsContent = content.AddComponent<RectTransform>();
            skillsContent.anchorMin = new Vector2(0f, 1f);
            skillsContent.anchorMax = new Vector2(1f, 1f);
            skillsContent.pivot = new Vector2(0.5f, 1f);
            skillsContent.anchoredPosition = Vector2.zero;
            skillsContent.sizeDelta = new Vector2(0f, 0f);

            skillsScrollRect.viewport = viewportRect;
            skillsScrollRect.content = skillsContent;

            var bottomCanvas = new GameObject("HUD Skills Bottom Canvas");
            bottomCanvas.transform.SetParent(innerCanvasRect, false);
            skillsBottomCanvas = bottomCanvas.AddComponent<RectTransform>();
            skillsBottomCanvas.anchorMin = Vector2.zero;
            skillsBottomCanvas.anchorMax = new Vector2(1f, 0.15f);
            skillsBottomCanvas.offsetMin = Vector2.zero;
            skillsBottomCanvas.offsetMax = Vector2.zero;
            bottomCanvas.AddComponent<Image>().color = new Color32(36, 40, 54, 255);

            BuildSkillsPointSummary();
            RefreshSkillsPanel();
            skillsPanel.gameObject.SetActive(false);
        }

        private void BuildSkillsPointSummary()
        {
            CreatePaddedText(skillsBottomCanvas, "Base Skill Points Label", "Base Skill Points", TextAnchor.MiddleCenter, 0f, 0.2125f, 0.05f);
            baseTalentPointsText = CreatePaddedText(skillsBottomCanvas, "Base Skill Points Value", "0", TextAnchor.MiddleCenter, 0.2625f, 0.375f, 0.05f);
            CreatePaddedText(skillsBottomCanvas, "Class Skill Points Label", "Class Skill Points", TextAnchor.MiddleCenter, 0.425f, 0.7375f, 0.05f);
            classTalentPointsText = CreatePaddedText(skillsBottomCanvas, "Class Skill Points Value", "0", TextAnchor.MiddleCenter, 0.7875f, 1f, 0.05f);

            foreach (var text in skillsBottomCanvas.GetComponentsInChildren<Text>(true))
            {
                ConfigureResourceTextFit(text);
            }
        }

        private void RefreshSkillsPanel()
        {
            if (skillsContent == null)
            {
                return;
            }

            ClearChildren(skillsContent);
            var characterClass = displayedCharacter?.CharacterClass;
            if (characterClass == null)
            {
                SetSkillsPointSummary(0, 0);
                return;
            }

            SetSkillsPointSummary(characterClass.GetBaseTalentPoints(), characterClass.GetClassTalentPoints());
            var rowIndex = 0;
            foreach (var modifier in characterClass.GetClassModifiers())
            {
                if (modifier == null)
                {
                    continue;
                }

                CreateSkillEntry(
                    "Modifier Skill Entry " + modifier.modifierID,
                    modifier.name,
                    modifier.description,
                    modifier.level,
                    modifier.maxLevel,
                    characterClass.CanUpgradeModifier(modifier),
                    rowIndex++,
                    () =>
                    {
                        if (characterClass.TryUpgradeModifier(modifier))
                        {
                            displayedCharacter?.UpdateStats();
                            RefreshDisplayedCharacter();
                        }
                    });
            }

            foreach (var action in characterClass.GetClassActions())
            {
                if (action == null)
                {
                    continue;
                }

                CreateSkillEntry(
                    "Action Skill Entry " + action.actionID,
                    action.name,
                    action.description,
                    action.level,
                    action.maxLevel,
                    characterClass.CanUpgradeAction(action),
                    rowIndex++,
                    () =>
                    {
                        if (characterClass.TryUpgradeAction(action))
                        {
                            RefreshDisplayedCharacter();
                        }
                    });
            }

            var rowCount = Mathf.CeilToInt(rowIndex / 2f);
            skillsContent.sizeDelta = new Vector2(0f, Mathf.Max(0f, rowCount * 43f));
        }

        private static void ClearChildren(RectTransform parent)
        {
            if (parent == null)
            {
                return;
            }

            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);
                if (child != null)
                {
                    DestroyHudObject(child.gameObject);
                }
            }
        }

        private static void DestroyHudObject(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }

        private void SetSkillsPointSummary(int basePoints, int classPoints)
        {
            if (baseTalentPointsText != null)
            {
                baseTalentPointsText.text = basePoints.ToString();
            }

            if (classTalentPointsText != null)
            {
                classTalentPointsText.text = classPoints.ToString();
            }
        }

        private void CreateSkillEntry(string objectName, string entryName, string description, int level, int maxLevel, bool canUpgrade, int rowIndex, UnityEngine.Events.UnityAction onUpgrade)
        {
            var rowObject = new GameObject(objectName);
            rowObject.transform.SetParent(skillsContent, false);
            var row = rowObject.AddComponent<RectTransform>();
            var column = rowIndex % 2;
            var gridRow = rowIndex / 2;
            row.anchorMin = new Vector2(column == 0 ? 0f : 0.525f, 1f);
            row.anchorMax = new Vector2(column == 0 ? 0.475f : 1f, 1f);
            row.pivot = new Vector2(0.5f, 1f);
            row.sizeDelta = new Vector2(0f, 40f);
            row.anchoredPosition = new Vector2(0f, -gridRow * 43f);
            rowObject.AddComponent<Image>().color = new Color32(32, 36, 48, 255);

            var levelText = CreatePaddedText(row, objectName + " Level", $"LVL\n{level}\n{maxLevel}", TextAnchor.MiddleCenter, 0f, 0.15f, 0.05f);
            ConfigureResourceTextFit(levelText);

            var detailArea = new GameObject(objectName + " Details");
            detailArea.transform.SetParent(row, false);
            var detailRect = detailArea.AddComponent<RectTransform>();
            detailRect.anchorMin = new Vector2(0.20f, 0f);
            detailRect.anchorMax = new Vector2(0.75f, 1f);
            detailRect.offsetMin = Vector2.zero;
            detailRect.offsetMax = Vector2.zero;

            var nameText = CreatePaddedText(detailRect, objectName + " Name", string.IsNullOrWhiteSpace(entryName) ? "Unnamed" : entryName, TextAnchor.MiddleLeft, 0f, 1f, 0.05f);
            nameText.rectTransform.anchorMin = new Vector2(0.05f, 0.5f);
            nameText.rectTransform.anchorMax = new Vector2(0.95f, 0.95f);
            ConfigureResourceTextFit(nameText);

            var descriptionText = CreatePaddedText(detailRect, objectName + " Description", string.IsNullOrWhiteSpace(description) ? "No description." : description, TextAnchor.MiddleLeft, 0f, 1f, 0.05f);
            descriptionText.rectTransform.anchorMin = new Vector2(0.05f, 0.05f);
            descriptionText.rectTransform.anchorMax = new Vector2(0.95f, 0.5f);
            ConfigureResourceTextFit(descriptionText);

            var upgradeButton = CreateHudButton(row, objectName + " Upgrade Button", "Upgrade", 0.80f, 1f, 0.05f, 0.95f, onUpgrade);
            upgradeButton.interactable = canUpgrade;
            upgradeButton.GetComponent<Image>().color = canUpgrade
                ? new Color32(74, 86, 112, 255)
                : new Color32(70, 70, 76, 255);
        }

        private void SetMenuOpen(bool open)
        {
            EnsureBuilt();
            if (menuPanel == null)
            {
                return;
            }

            if (open)
            {
                CloseSkillsImmediate();
            }

            menuOpen = open;
            if (menuAnimation != null)
            {
                StopCoroutine(menuAnimation);
            }

            menuAnimation = StartCoroutine(AnimateMenuPanel(open));
        }

        private void SetSkillsOpen(bool open)
        {
            EnsureBuilt();
            if (skillsPanel == null)
            {
                return;
            }

            if (open)
            {
                CloseMenuImmediate();
            }

            skillsOpen = open;
            if (skillsAnimation != null)
            {
                StopCoroutine(skillsAnimation);
            }

            skillsAnimation = StartCoroutine(AnimateSkillsPanel(open));
        }

        public void CloseMenuImmediate()
        {
            menuOpen = false;
            if (menuAnimation != null)
            {
                StopCoroutine(menuAnimation);
                menuAnimation = null;
            }

            if (menuPanel == null)
            {
                return;
            }

            SetMenuPanelHeight(0f);
            menuPanel.gameObject.SetActive(false);
        }

        public void CloseExpandablePanelsImmediate()
        {
            CloseMenuImmediate();
            CloseSkillsImmediate();
        }

        public void CloseSkillsImmediate()
        {
            skillsOpen = false;
            if (skillsAnimation != null)
            {
                StopCoroutine(skillsAnimation);
                skillsAnimation = null;
            }

            if (skillsPanel == null)
            {
                return;
            }

            SetSkillsPanelHeight(0f);
            skillsPanel.gameObject.SetActive(false);
        }

        private IEnumerator AnimateMenuPanel(bool open)
        {
            const float duration = 0.15f;
            var targetHeight = GetMenuPanelTargetHeight();
            var startHeight = menuPanel.offsetMax.y;
            var endHeight = open ? targetHeight : 0f;

            menuPanel.gameObject.SetActive(true);
            for (var elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                var t = Mathf.Clamp01(elapsed / duration);
                t = t * t * (3f - 2f * t);
                SetMenuPanelHeight(Mathf.Lerp(startHeight, endHeight, t));
                yield return null;
            }

            SetMenuPanelHeight(endHeight);
            if (!open)
            {
                menuPanel.gameObject.SetActive(false);
            }

            menuAnimation = null;
        }

        private IEnumerator AnimateSkillsPanel(bool open)
        {
            const float duration = 0.15f;
            var targetHeight = GetSkillsPanelTargetHeight();
            var startHeight = skillsPanel.offsetMax.y;
            var endHeight = open ? targetHeight : 0f;

            skillsPanel.gameObject.SetActive(true);
            for (var elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                var t = Mathf.Clamp01(elapsed / duration);
                t = t * t * (3f - 2f * t);
                SetSkillsPanelHeight(Mathf.Lerp(startHeight, endHeight, t));
                yield return null;
            }

            SetSkillsPanelHeight(endHeight);
            if (!open)
            {
                skillsPanel.gameObject.SetActive(false);
            }

            skillsAnimation = null;
        }

        private float GetMenuPanelTargetHeight()
        {
            if (root != null && root.rect.height > 0f)
            {
                return root.rect.height * 1.1f;
            }

            return 120f;
        }

        private float GetSkillsPanelTargetHeight()
        {
            if (root != null && root.rect.height > 0f)
            {
                return root.rect.height * 3.6f;
            }

            return 390f;
        }

        private void SetMenuPanelHeight(float height)
        {
            menuPanel.offsetMin = Vector2.zero;
            menuPanel.offsetMax = new Vector2(0f, Mathf.Max(0f, height));
        }

        private void SetSkillsPanelHeight(float height)
        {
            skillsPanel.offsetMin = Vector2.zero;
            skillsPanel.offsetMax = new Vector2(0f, Mathf.Max(0f, height));
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
