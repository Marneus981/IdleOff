using UnityEngine;
using UnityEngine.UI;

namespace IdleOff.Game
{
    public sealed class BootFlowUIView : MonoBehaviour
    {
        [Header("Loading")]
        [SerializeField] private GameObject loadingPanel;
        [SerializeField] private Image loadingBarFill;

        [Header("Title")]
        [SerializeField] private GameObject titlePanel;
        [SerializeField] private InputField profileNameInput;
        [SerializeField] private Button createProfileButton;
        [SerializeField] private Button loadProfileButton;
        [SerializeField] private Button deleteProfileButton;
        [SerializeField] private Button titleExitButton;

        [Header("Profile Popup")]
        [SerializeField] private GameObject profilePopup;
        [SerializeField] private Text profilePopupTitle;
        [SerializeField] private Button profilePopupCloseButton;
        [SerializeField] private RectTransform profileListContainer;

        [Header("Character Select")]
        [SerializeField] private GameObject characterSelectPanel;
        [SerializeField] private Text characterSelectProfileName;
        [SerializeField] private RectTransform characterSlotContainer;
        [SerializeField] private Button characterSelectExitButton;

        [Header("Character Create")]
        [SerializeField] private GameObject characterCreatePopup;
        [SerializeField] private InputField characterNameInput;
        [SerializeField] private Button characterCreateCloseButton;
        [SerializeField] private Button classPreviousButton;
        [SerializeField] private Text classValueText;
        [SerializeField] private Button classNextButton;
        [SerializeField] private Button genderPreviousButton;
        [SerializeField] private Text genderValueText;
        [SerializeField] private Button genderNextButton;
        [SerializeField] private Button hatPreviousButton;
        [SerializeField] private Text hatValueText;
        [SerializeField] private Button hatNextButton;
        [SerializeField] private Button starSignPreviousButton;
        [SerializeField] private Text starSignValueText;
        [SerializeField] private Button starSignNextButton;
        [SerializeField] private Button createCharacterButton;

        [Header("Hub Placeholder")]
        [SerializeField] private GameObject hubPlaceholderPanel;
        [SerializeField] private Text hubCharacterSummaryText;

        public InputField ProfileNameInput => profileNameInput;
        public Button CreateProfileButton => createProfileButton;
        public Button LoadProfileButton => loadProfileButton;
        public Button DeleteProfileButton => deleteProfileButton;
        public Button TitleExitButton => titleExitButton;
        public Button ProfilePopupCloseButton => profilePopupCloseButton;
        public RectTransform ProfileListContainer => profileListContainer;
        public RectTransform CharacterSlotContainer => characterSlotContainer;
        public Button CharacterSelectExitButton => characterSelectExitButton;
        public InputField CharacterNameInput => characterNameInput;
        public Button CharacterCreateCloseButton => characterCreateCloseButton;
        public Button ClassPreviousButton => classPreviousButton;
        public Button ClassNextButton => classNextButton;
        public Button GenderPreviousButton => genderPreviousButton;
        public Button GenderNextButton => genderNextButton;
        public Button HatPreviousButton => hatPreviousButton;
        public Button HatNextButton => hatNextButton;
        public Button StarSignPreviousButton => starSignPreviousButton;
        public Button StarSignNextButton => starSignNextButton;
        public Button CreateCharacterButton => createCharacterButton;

        public bool IsConfigured => loadingPanel != null
            && titlePanel != null
            && characterSelectPanel != null
            && characterCreatePopup != null
            && profilePopup != null
            && hubPlaceholderPanel != null;

        public void ShowLoading()
        {
            ShowOnly(loadingPanel);
            SetLoadingProgress(0f);
        }

        public void SetLoadingProgress(float progress)
        {
            if (loadingBarFill != null)
            {
                loadingBarFill.fillAmount = Mathf.Clamp01(progress);
            }
        }

        public void ShowTitle()
        {
            ShowOnly(titlePanel);
        }

        public void ShowProfilePopup(string title)
        {
            if (profilePopupTitle != null)
            {
                profilePopupTitle.text = title;
            }

            SetActive(profilePopup, true);
        }

        public void HideProfilePopup()
        {
            SetActive(profilePopup, false);
        }

        public void ShowCharacterSelect(string profileName)
        {
            ShowOnly(characterSelectPanel);
            if (characterSelectProfileName != null)
            {
                characterSelectProfileName.text = profileName;
            }
        }

        public void ShowCharacterCreate()
        {
            SetActive(characterCreatePopup, true);
        }

        public void HideCharacterCreate()
        {
            SetActive(characterCreatePopup, false);
        }

        public void SetCharacterCreateValues(string className, string gender, string hatName, string starSignName)
        {
            if (classValueText != null)
            {
                classValueText.text = className;
            }

            if (genderValueText != null)
            {
                genderValueText.text = gender;
            }

            if (hatValueText != null)
            {
                hatValueText.text = hatName;
            }

            if (starSignValueText != null)
            {
                starSignValueText.text = starSignName;
            }
        }

        public void ShowHubPlaceholder(string characterSummary)
        {
            ShowOnly(hubPlaceholderPanel);
            if (hubCharacterSummaryText != null)
            {
                hubCharacterSummaryText.text = characterSummary;
            }
        }

        public void ClearProfileRows()
        {
            ClearChildren(profileListContainer);
        }

        public void ClearCharacterRows()
        {
            ClearChildren(characterSlotContainer);
        }

        private void ShowOnly(GameObject panel)
        {
            SetActive(loadingPanel, panel == loadingPanel);
            SetActive(titlePanel, panel == titlePanel);
            SetActive(profilePopup, false);
            SetActive(characterSelectPanel, panel == characterSelectPanel);
            SetActive(characterCreatePopup, false);
            SetActive(hubPlaceholderPanel, panel == hubPlaceholderPanel);
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null)
            {
                target.SetActive(active);
            }
        }

        private static void ClearChildren(RectTransform container)
        {
            if (container == null)
            {
                return;
            }

            for (var i = container.childCount - 1; i >= 0; i--)
            {
                DestroyUiObject(container.GetChild(i).gameObject);
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
    }
}
