using System.Collections;
using System.Collections.Generic;
using System.Linq;
using IdleOff.Profiles;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace IdleOff.Game
{
    [DisallowMultipleComponent]
    public sealed class BootFlowUI : MonoBehaviour
    {
        [SerializeField] private BootFlowUIView view;

        private readonly ProfileManager profileManager = new();
        private readonly List<ClassOption> classOptions = new();
        private readonly List<CharacterGender> genderOptions = new();
        private readonly List<Item> hatOptions = new();
        private readonly List<StarSignModifier> starSignOptions = new();

        private Canvas canvas;
        private RectTransform root;
        private Font font;
        private InputField profileNameField;
        private GameObject darkOverlay;
        private ProfileRecord selectedProfile;
        private InputField characterNameField;
        private int classIndex;
        private int genderIndex;
        private int hatIndex;
        private int starSignIndex;

        private bool HasPrefabView => view != null && view.IsConfigured;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            LoadCatalogs();
            BuildCanvas();
        }

        private void Start()
        {
            StartCoroutine(BootSequence());
        }

        private IEnumerator BootSequence()
        {
            GameStateManager.Instance.SetState(GameState.Boot);
            ShowLoadingScreen();
            var elapsed = 0f;
            while (elapsed < 1.25f)
            {
                elapsed += Time.unscaledDeltaTime;
                UpdateLoadingBar(Mathf.Clamp01(elapsed / 1.25f));
                yield return null;
            }

            profileManager.LoadProfiles();
            ShowTitleScreen();
        }

        private void LoadCatalogs()
        {
            GlobalModifierCatalog.EnsureLoaded();
            GlobalItemCatalog.EnsureLoaded();

            classOptions.Clear();
            classOptions.Add(new ClassOption("Wandering Soul", CharacterClass.CreateWanderingSoul));
            classOptions.Add(new ClassOption("Errant Knight", CharacterClass.CreateErrantKnight));
            classOptions.Add(new ClassOption("Wild Hunter", CharacterClass.CreateWildHunter));
            classOptions.Add(new ClassOption("Void Whisperer", CharacterClass.CreateVoidWhisperer));

            genderOptions.Clear();
            genderOptions.AddRange((CharacterGender[])System.Enum.GetValues(typeof(CharacterGender)));

            hatOptions.Clear();
            hatOptions.AddRange(GlobalItemCatalog.Items.Values.Where(item => item.HasTag("hat")).OrderBy(item => item.itemID));

            starSignOptions.Clear();
            starSignOptions.AddRange(GlobalModifierCatalog.StarSignBonuses.Values.OrderBy(modifier => modifier.modifierID));
        }

        private void BuildCanvas()
        {
            view = view != null ? view : FindFirstObjectByType<BootFlowUIView>();
            var eventSystem = FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                var eventSystemObject = new GameObject("EventSystem");
                eventSystem = eventSystemObject.AddComponent<EventSystem>();
            }

            var legacyInputModule = eventSystem.GetComponent<StandaloneInputModule>();
            if (legacyInputModule != null)
            {
                Destroy(legacyInputModule);
            }

            if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
            {
                var inputModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
                inputModule.AssignDefaultActions();
            }

            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (HasPrefabView)
            {
                return;
            }

            var canvasObject = new GameObject("Boot Flow Canvas");
            canvasObject.transform.SetParent(transform);
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5000;
            canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObject.AddComponent<GraphicRaycaster>();
            root = canvasObject.GetComponent<RectTransform>();
        }

        private void ClearRoot()
        {
            if (root == null)
            {
                return;
            }

            for (var i = root.childCount - 1; i >= 0; i--)
            {
                DestroyUiObject(root.GetChild(i).gameObject);
            }
        }

        private void ShowLoadingScreen()
        {
            if (HasPrefabView)
            {
                view.ShowLoading();
                return;
            }

            ClearRoot();
            CreatePanel(root, "Loading Background", Stretch(), new Color32(14, 16, 22, 255));
            CreateText(root, "Loading...", 26, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(360f, 48f), Vector2.zero);
            var barBack = CreatePanel(root, "Loading Bar Back", BottomStretch(28f, 24f), new Color32(50, 55, 68, 255));
            var barFill = CreatePanel(barBack.GetComponent<RectTransform>(), "Loading Bar Fill", LeftStretch(), new Color32(104, 190, 255, 255));
            barFill.name = "Loading Bar Fill";
            barFill.GetComponent<RectTransform>().anchorMax = new Vector2(0f, 1f);
        }

        private void UpdateLoadingBar(float progress)
        {
            if (HasPrefabView)
            {
                view.SetLoadingProgress(progress);
                return;
            }

            var fill = GameObject.Find("Loading Bar Fill");
            if (fill != null)
            {
                fill.GetComponent<RectTransform>().anchorMax = new Vector2(progress, 1f);
            }
        }

        private void ShowTitleScreen()
        {
            GameStateManager.Instance.SetState(GameState.Title);
            if (HasPrefabView)
            {
                view.ShowTitle();
                profileNameField = view.ProfileNameInput;
                BindButton(view.CreateProfileButton, CreateProfileAndCharacter);
                BindButton(view.LoadProfileButton, () => ShowProfilePopup("Load Profile", LoadSelectedProfile));
                BindButton(view.DeleteProfileButton, () => ShowProfilePopup("Delete Profile", DeleteSelectedProfile));
                BindButton(view.TitleExitButton, ExitGame);
                return;
            }

            ClearRoot();
            CreatePanel(root, "Title Background", Stretch(), new Color32(18, 21, 30, 255));
            CreateText(root, "IdleOff", 58, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.73f), new Vector2(520f, 96f), Vector2.zero);

            var stack = CreateVerticalStack(root, "Title Stack", new Vector2(0.5f, 0.42f), new Vector2(420f, 230f), 14f);
            var createRow = CreateHorizontalStack(stack, "Create Profile Row", new Vector2(400f, 42f), 8f);
            profileNameField = CreateInput(createRow, "Profile Name", "Profile name", new Vector2(230f, 38f));
            CreateButton(createRow, "Create Profile", new Vector2(150f, 38f), CreateProfileAndCharacter);
            CreateButton(stack, "Load Profile", new Vector2(400f, 42f), () => ShowProfilePopup("Load Profile", LoadSelectedProfile));
            CreateButton(stack, "Delete Profile", new Vector2(400f, 42f), () => ShowProfilePopup("Delete Profile", DeleteSelectedProfile));
            CreateButton(stack, "Exit Game", new Vector2(400f, 42f), () => ExitGame());
        }

        private void CreateProfileAndCharacter()
        {
            selectedProfile = profileManager.CreateProfile(profileNameField.text);
            GameSession.SetActiveProfile(selectedProfile);
            ShowDarkTransition(() => ShowCharacterSelect(false));
        }

        private void LoadSelectedProfile(ProfileRecord record)
        {
            selectedProfile = record;
            GameSession.SetActiveProfile(selectedProfile);
            ShowDarkTransition(() => ShowCharacterSelect(false));
        }

        private void DeleteSelectedProfile(ProfileRecord record)
        {
            profileManager.DeleteProfile(record);
            ShowTitleScreen();
        }

        private void ShowProfilePopup(string title, System.Action<ProfileRecord> onSelected)
        {
            GameStateManager.Instance.SetState(GameState.ProfilePopup);
            if (HasPrefabView)
            {
                view.ShowProfilePopup(title);
                view.ClearProfileRows();
                BindButton(view.ProfilePopupCloseButton, () =>
                {
                    view.HideProfilePopup();
                    GameStateManager.Instance.SetState(GameState.Title);
                });

                if (profileManager.Profiles.Count == 0)
                {
                    CreateText(view.ProfileListContainer, "No profiles found.", 20, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(360f, 40f), Vector2.zero);
                    return;
                }

                foreach (var profile in profileManager.Profiles)
                {
                    CreateButton(view.ProfileListContainer, profile.ProfileName, new Vector2(360f, 38f), () => onSelected(profile));
                }

                return;
            }

            var overlay = CreatePanel(root, "Profile Popup Overlay", Stretch(), new Color32(0, 0, 0, 150));
            var popup = CreatePanel(overlay.GetComponent<RectTransform>(), "Profile Popup", Centered(460f, 420f), new Color32(32, 36, 48, 255));
            CreateText(popup.GetComponent<RectTransform>(), title, 28, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.9f), new Vector2(360f, 48f), Vector2.zero);
            CreateButton(popup.GetComponent<RectTransform>(), "X", new Vector2(38f, 38f), () => ShowTitleScreen(), new Vector2(0.93f, 0.91f));

            var list = CreateVerticalStack(popup.GetComponent<RectTransform>(), "Profile List", new Vector2(0.5f, 0.48f), new Vector2(380f, 280f), 8f);
            if (profileManager.Profiles.Count == 0)
            {
                CreateText(list, "No profiles found.", 20, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(360f, 40f), Vector2.zero);
                return;
            }

            foreach (var profile in profileManager.Profiles)
            {
                CreateButton(list, profile.ProfileName, new Vector2(360f, 38f), () => onSelected(profile));
            }
        }

        private void ShowDarkTransition(System.Action onComplete)
        {
            if (HasPrefabView)
            {
                onComplete?.Invoke();
                return;
            }

            StartCoroutine(DarkTransition(onComplete));
        }

        private IEnumerator DarkTransition(System.Action onComplete)
        {
            darkOverlay = CreatePanel(root, "Dark Transition", Stretch(), new Color32(0, 0, 0, 0));
            var image = darkOverlay.GetComponent<Image>();
            for (var t = 0f; t < 1f; t += Time.unscaledDeltaTime)
            {
                image.color = new Color(0f, 0f, 0f, Mathf.Clamp01(t / 0.25f));
                yield return null;
            }

            onComplete?.Invoke();
        }

        private void ShowCharacterSelect(bool forceCreatePopup)
        {
            GameStateManager.Instance.SetState(GameState.CharacterSelect);
            if (HasPrefabView)
            {
                view.ShowCharacterSelect(selectedProfile.ProfileName);
                view.ClearCharacterRows();
                for (var i = 0; i < CharacterProfile.MaxCharacters; i++)
                {
                    var index = i;
                    CreateButton(view.CharacterSlotContainer, GetCharacterSlotLabel(index), new Vector2(560f, 46f), () => HandleCharacterSlotClicked(index));
                }

                BindButton(view.CharacterSelectExitButton, ExitGame);
                if (forceCreatePopup)
                {
                    ShowCharacterCreatePopup(true);
                }

                return;
            }

            ClearRoot();
            CreatePanel(root, "Character Background", Stretch(), new Color32(13, 16, 23, 255));
            CreateText(root, selectedProfile.ProfileName, 34, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.88f), new Vector2(520f, 56f), Vector2.zero);

            var list = CreateScrollableVerticalStack(root, "Character Slot Scroll Frame", new Vector2(0.5f, 0.47f), new Vector2(505f, 298f), new Vector2(620f, 620f), 8f, 18f);
            for (var i = 0; i < CharacterProfile.MaxCharacters; i++)
            {
                var index = i;
                var label = GetCharacterSlotLabel(index);
                CreateButton(list, label, new Vector2(560f, 46f), () => HandleCharacterSlotClicked(index));
            }

            if (forceCreatePopup)
            {
                ShowCharacterCreatePopup();
            }

            CreateButton(root, "Exit Game", new Vector2(160f, 40f), ExitGame, new Vector2(0.9f, 0.08f));
        }

        private string GetCharacterSlotLabel(int index)
        {
            if (selectedProfile == null || index >= selectedProfile.Profile.Characters.Count)
            {
                return "New Character";
            }

            var character = selectedProfile.Profile.Characters[index];
            return $"{character.CharacterName}   |   {character.CharacterClass.GetClassName()}   |   Lv. {character.Level}";
        }

        private void HandleCharacterSlotClicked(int index)
        {
            if (index < selectedProfile.Profile.Characters.Count)
            {
                selectedProfile.Profile.SetActiveCharacterIndex(index);
                profileManager.SaveProfile(selectedProfile);
                ShowGameHubPlaceholder();
                return;
            }

            ShowCharacterCreatePopup(true);
        }

        private void ShowCharacterCreatePopup(bool resetOptions = false)
        {
            GameStateManager.Instance.SetState(GameState.CharacterCreate);
            if (resetOptions)
            {
                classIndex = 0;
                genderIndex = 0;
                hatIndex = 0;
                starSignIndex = 0;
            }

            if (HasPrefabView)
            {
                view.ShowCharacterCreate();
                characterNameField = view.CharacterNameInput;
                BindButton(view.CharacterCreateCloseButton, () =>
                {
                    view.HideCharacterCreate();
                    GameStateManager.Instance.SetState(GameState.CharacterSelect);
                });
                BindButton(view.ClassPreviousButton, () => { StepIndex(ref classIndex, classOptions.Count, -1); RefreshPrefabCharacterCreateValues(); });
                BindButton(view.ClassNextButton, () => { StepIndex(ref classIndex, classOptions.Count, 1); RefreshPrefabCharacterCreateValues(); });
                BindButton(view.GenderPreviousButton, () => { StepIndex(ref genderIndex, genderOptions.Count, -1); RefreshPrefabCharacterCreateValues(); });
                BindButton(view.GenderNextButton, () => { StepIndex(ref genderIndex, genderOptions.Count, 1); RefreshPrefabCharacterCreateValues(); });
                BindButton(view.HatPreviousButton, () => { StepIndex(ref hatIndex, hatOptions.Count, -1); RefreshPrefabCharacterCreateValues(); });
                BindButton(view.HatNextButton, () => { StepIndex(ref hatIndex, hatOptions.Count, 1); RefreshPrefabCharacterCreateValues(); });
                BindButton(view.StarSignPreviousButton, () => { StepIndex(ref starSignIndex, starSignOptions.Count, -1); RefreshPrefabCharacterCreateValues(); });
                BindButton(view.StarSignNextButton, () => { StepIndex(ref starSignIndex, starSignOptions.Count, 1); RefreshPrefabCharacterCreateValues(); });
                BindButton(view.CreateCharacterButton, CreateCharacter);
                RefreshPrefabCharacterCreateValues();
                return;
            }

            DestroyObjectsNamed("Character Create Overlay");
            var overlay = CreatePanel(root, "Character Create Overlay", Stretch(), new Color32(0, 0, 0, 155));
            var popup = CreatePanel(overlay.GetComponent<RectTransform>(), "Character Create Popup", Centered(459f, 400f), new Color32(32, 36, 48, 255));
            CreateButton(popup.GetComponent<RectTransform>(), "X", new Vector2(38f, 38f), () => ShowCharacterSelect(false), new Vector2(0.93f, 0.92f));
            characterNameField = CreateInput(popup.GetComponent<RectTransform>(), "Character Name", "Character name", new Vector2(400f, 42f), new Vector2(0.5f, 0.84f));

            var stack = CreateVerticalStack(popup.GetComponent<RectTransform>(), "Character Options", new Vector2(0.5f, 0.49f), new Vector2(460f, 270f), 12f);
            CreateOptionRow(stack, "Class", () => classOptions[classIndex].Name, () => StepIndex(ref classIndex, classOptions.Count, -1), () => StepIndex(ref classIndex, classOptions.Count, 1));
            CreateOptionRow(stack, "Gender", () => genderOptions[genderIndex].ToString(), () => StepIndex(ref genderIndex, genderOptions.Count, -1), () => StepIndex(ref genderIndex, genderOptions.Count, 1));
            CreateOptionRow(stack, "Starting Hat", () => hatOptions.Count == 0 ? "None" : hatOptions[hatIndex].name, () => StepIndex(ref hatIndex, hatOptions.Count, -1), () => StepIndex(ref hatIndex, hatOptions.Count, 1));
            CreateOptionRow(stack, "Star Sign", () => starSignOptions.Count == 0 ? "None" : starSignOptions[starSignIndex].name, () => StepIndex(ref starSignIndex, starSignOptions.Count, -1), () => StepIndex(ref starSignIndex, starSignOptions.Count, 1));

            CreateButton(popup.GetComponent<RectTransform>(), "Create", new Vector2(220f, 44f), CreateCharacter, new Vector2(0.5f, 0.08f));
        }

        private void CreateOptionRow(RectTransform parent, string label, System.Func<string> getValue, System.Action previous, System.Action next)
        {
            var row = CreateHorizontalStack(parent, label + " Row", new Vector2(430f, 48f), 8f);
            CreateText(row, label, 16, TextAnchor.MiddleLeft, new Vector2(0.5f, 0.5f), new Vector2(120f, 40f), Vector2.zero);
            CreateButton(row, "<", new Vector2(36f, 36f), () => { previous(); RefreshCharacterCreatePopup(); });
            CreateText(row, getValue(), 16, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(190f, 40f), Vector2.zero);
            CreateButton(row, ">", new Vector2(36f, 36f), () => { next(); RefreshCharacterCreatePopup(); });
        }

        private void RefreshCharacterCreatePopup()
        {
            if (HasPrefabView)
            {
                RefreshPrefabCharacterCreateValues();
                return;
            }

            var name = characterNameField != null ? characterNameField.text : string.Empty;
            ShowCharacterCreatePopup(false);
            characterNameField.text = name;
        }

        private void RefreshPrefabCharacterCreateValues()
        {
            if (!HasPrefabView)
            {
                return;
            }

            view.SetCharacterCreateValues(
                classOptions.Count == 0 ? "Wandering Soul" : classOptions[classIndex].Name,
                genderOptions.Count == 0 ? CharacterGender.Unspecified.ToString() : genderOptions[genderIndex].ToString(),
                hatOptions.Count == 0 ? "None" : hatOptions[hatIndex].name,
                starSignOptions.Count == 0 ? "None" : starSignOptions[starSignIndex].name);
        }

        private static void StepIndex(ref int index, int count, int direction)
        {
            if (count <= 0)
            {
                index = 0;
                return;
            }

            index = (index + direction + count) % count;
        }

        private static void DestroyObjectsNamed(string objectName)
        {
            foreach (var gameObject in FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (gameObject != null && gameObject.name == objectName)
                {
                    DestroyUiObject(gameObject);
                }
            }
        }

        private static void DestroyUiObject(GameObject target)
        {
            if (target == null)
            {
                return;
            }

#if UNITY_EDITOR
            UnityEditor.Selection.objects = System.Array.Empty<Object>();
#endif
            Destroy(target);
        }

        private void CreateCharacter()
        {
            if (selectedProfile == null || !selectedProfile.Profile.HasRoom)
            {
                return;
            }

            var characterName = string.IsNullOrWhiteSpace(characterNameField.text) ? "Wandering Soul" : characterNameField.text.Trim();
            var characterClass = classOptions[classIndex].Create();
            var gender = genderOptions[genderIndex];
            var hatID = hatOptions.Count == 0 ? 5001 : hatOptions[hatIndex].itemID;
            var starSignID = starSignOptions.Count == 0 ? 3002 : starSignOptions[starSignIndex].modifierID;
            var character = new CharacterData(characterName, gender, characterClass, starSignID, hatID);
            selectedProfile.Profile.TryAddCharacter(character);
            selectedProfile.Profile.SetActiveCharacterIndex(selectedProfile.Profile.Characters.Count - 1);
            profileManager.SaveProfile(selectedProfile);
            ShowGameHubPlaceholder();
        }

        private void ShowGameHubPlaceholder()
        {
            GameStateManager.Instance.SetState(GameState.GameHub);
            var character = selectedProfile.Profile.ActiveCharacter;
            var summary = character == null ? "No character selected" : $"{character.CharacterName} - {character.CharacterClass.GetClassName()} Lv. {character.Level}";
            if (HasPrefabView)
            {
                view.ShowHubPlaceholder(summary);
                return;
            }

            ClearRoot();
            CreatePanel(root, "Hub Placeholder Background", Stretch(), new Color32(12, 18, 24, 255));
            CreateText(root, "Game Hub", 46, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.62f), new Vector2(520f, 72f), Vector2.zero);
            CreateText(root, summary, 22, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(640f, 48f), Vector2.zero);
            CreateText(root, "Hub map loading will connect here in the next phase.", 18, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.42f), new Vector2(640f, 42f), Vector2.zero);
        }

        private static void BindButton(Button button, System.Action action)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => action?.Invoke());
        }

        public void ExitGame()
        {
            if (selectedProfile != null)
            {
                profileManager.SaveProfile(selectedProfile);
            }

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private static RectTransform CreateVerticalStack(RectTransform parent, string name, Vector2 anchor, Vector2 size, float spacing)
        {
            var panel = CreateRect(parent, name, size, anchor);
            var group = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            group.spacing = spacing;
            group.childAlignment = TextAnchor.MiddleCenter;
            group.childControlWidth = false;
            group.childControlHeight = false;
            group.childForceExpandWidth = false;
            group.childForceExpandHeight = false;
            return panel;
        }

        private static RectTransform CreateHorizontalStack(RectTransform parent, string name, Vector2 size, float spacing)
        {
            var panel = CreateRect(parent, name, size, new Vector2(0.5f, 0.5f));
            var group = panel.gameObject.AddComponent<HorizontalLayoutGroup>();
            group.spacing = spacing;
            group.childAlignment = TextAnchor.MiddleCenter;
            group.childControlWidth = false;
            group.childControlHeight = false;
            group.childForceExpandWidth = false;
            group.childForceExpandHeight = false;
            return panel;
        }

        private static RectTransform CreateScrollableVerticalStack(RectTransform parent, string name, Vector2 anchor, Vector2 frameSize, Vector2 contentSize, float spacing, float padding)
        {
            var frame = CreateRect(parent, name, frameSize, anchor);
            frame.gameObject.AddComponent<Image>().color = new Color32(24, 29, 40, 210);

            var scrollRect = frame.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            var viewport = CreateRect(frame, name + " Viewport", Vector2.zero, new Vector2(0.5f, 0.5f));
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(padding, padding);
            viewport.offsetMax = new Vector2(-padding, -padding);
            viewport.gameObject.AddComponent<Image>().color = new Color32(0, 0, 0, 1);
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;

            var content = CreateRect(viewport, "Character Slot List", contentSize, new Vector2(0.5f, 1f));
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, contentSize.y);

            var group = content.gameObject.AddComponent<VerticalLayoutGroup>();
            group.spacing = spacing;
            group.childAlignment = TextAnchor.UpperCenter;
            group.childControlWidth = false;
            group.childControlHeight = false;
            group.childForceExpandWidth = false;
            group.childForceExpandHeight = false;

            scrollRect.viewport = viewport;
            scrollRect.content = content;
            return content;
        }

        private Button CreateButton(Transform parent, string label, Vector2 size, System.Action onClick, Vector2? anchor = null)
        {
            var rect = CreateRect(parent as RectTransform, "Button - " + label, size, anchor ?? new Vector2(0.5f, 0.5f));
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color32(74, 86, 112, 255);
            var button = rect.gameObject.AddComponent<Button>();
            button.onClick.AddListener(() => onClick?.Invoke());
            CreateText(rect, label, 18, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), size, Vector2.zero);
            return button;
        }

        private InputField CreateInput(Transform parent, string name, string placeholder, Vector2 size, Vector2? anchor = null)
        {
            var rect = CreateRect(parent as RectTransform, "Input - " + name, size, anchor ?? new Vector2(0.5f, 0.5f));
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color32(238, 241, 247, 255);
            var input = rect.gameObject.AddComponent<InputField>();
            var text = CreateText(rect, string.Empty, 17, TextAnchor.MiddleLeft, new Vector2(0.5f, 0.5f), size - new Vector2(24f, 0f), Vector2.zero);
            text.color = Color.black;
            var placeholderText = CreateText(rect, placeholder, 17, TextAnchor.MiddleLeft, new Vector2(0.5f, 0.5f), size - new Vector2(24f, 0f), Vector2.zero);
            placeholderText.color = new Color32(120, 125, 135, 255);
            input.textComponent = text;
            input.placeholder = placeholderText;
            return input;
        }

        private Text CreateText(Transform parent, string text, int size, TextAnchor alignment, Vector2 anchor, Vector2 rectSize, Vector2 offset)
        {
            var rect = CreateRect(parent as RectTransform, "Text - " + text, rectSize, anchor);
            rect.anchoredPosition = offset;
            var textComponent = rect.gameObject.AddComponent<Text>();
            textComponent.font = font;
            textComponent.text = text;
            textComponent.fontSize = size;
            textComponent.alignment = alignment;
            textComponent.color = Color.white;
            textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
            textComponent.verticalOverflow = VerticalWrapMode.Truncate;
            return textComponent;
        }

        private static GameObject CreatePanel(RectTransform parent, string name, RectPreset preset, Color color)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            var rect = panel.AddComponent<RectTransform>();
            preset.Apply(rect);
            panel.AddComponent<Image>().color = color;
            return panel;
        }

        private static RectTransform CreateRect(RectTransform parent, string name, Vector2 size, Vector2 anchor)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            var rect = child.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            return rect;
        }

        private static RectPreset Stretch()
        {
            return new RectPreset(Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Vector2.zero);
        }

        private static RectPreset LeftStretch()
        {
            return new RectPreset(new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
        }

        private static RectPreset BottomStretch(float height, float margin)
        {
            return new RectPreset(new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(-margin * 2f, height), new Vector2(0f, margin + height * 0.5f));
        }

        private static RectPreset Centered(float width, float height)
        {
            return new RectPreset(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(width, height), Vector2.zero);
        }

        private readonly struct ClassOption
        {
            public ClassOption(string name, System.Func<CharacterClass> create)
            {
                Name = name;
                Create = create;
            }

            public string Name { get; }
            public System.Func<CharacterClass> Create { get; }
        }

        private readonly struct RectPreset
        {
            private readonly Vector2 anchorMin;
            private readonly Vector2 anchorMax;
            private readonly Vector2 pivot;
            private readonly Vector2 sizeDelta;
            private readonly Vector2 anchoredPosition;

            public RectPreset(Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 sizeDelta, Vector2 anchoredPosition)
            {
                this.anchorMin = anchorMin;
                this.anchorMax = anchorMax;
                this.pivot = pivot;
                this.sizeDelta = sizeDelta;
                this.anchoredPosition = anchoredPosition;
            }

            public void Apply(RectTransform rect)
            {
                rect.anchorMin = anchorMin;
                rect.anchorMax = anchorMax;
                rect.pivot = pivot;
                rect.sizeDelta = sizeDelta;
                rect.anchoredPosition = anchoredPosition;
            }
        }
    }
}
