using Kingmaker;
using Kingmaker.UI.MVVM._PCView.CharGen.Phases.FeatureSelector;
using Owlcat.Runtime.UI.Controls.Button;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using ModKit;

namespace ToyBox
{
    public class SearchBar
    {
        public GameObject GameObject;
        public TMP_InputField InputField;
        public OwlcatButton InputButton;
        public TMP_Dropdown Dropdown;
        public OwlcatButton DropdownButton;
        public GameObject DropdownIconObject;
        public TextMeshProUGUI PlaceholderText;

        public SearchBar(Transform parent, string placeholder, string name = "EnhancedInventory_SearchBar")
        {
            Transform prefab_transform = Game.Instance.UI.MainCanvas.transform.Find("ChargenPCView/ContentWrapper/DetailedViewZone/ChargenFeaturesDetailedPCView/FeatureSelectorPlace/FeatureSelectorView/FeatureSearchView");

            if (prefab_transform == null)
            {
                string err = "Error: Unable to locate search bar prefab, it's likely a patch has changed the UI setup, or you are in an unexpected situation. Please report this bug!";
                Mod.Error(err);
                throw new UnityException(err);
            }

            GameObject = GameObject.Instantiate(prefab_transform, parent, false).gameObject;
            GameObject.name = name;

            InputButton = GameObject.transform.Find("FieldPlace/SearchField/SearchBackImage/Placeholder").GetComponent<OwlcatButton>();
            Dropdown = GameObject.transform.Find("FieldPlace/SearchField/SearchBackImage/Dropdown").GetComponent<TMP_Dropdown>();
            DropdownButton = GameObject.transform.Find("FieldPlace/SearchField/SearchBackImage/Dropdown/GenerateButtonPlace").GetComponent<OwlcatButton>();
            DropdownIconObject = GameObject.transform.Find("FieldPlace/SearchField/SearchBackImage/Dropdown/GenerateButtonPlace/GenerateButton/Icon").gameObject;
            PlaceholderText = GameObject.transform.Find("FieldPlace/SearchField/SearchBackImage/Placeholder/Label").GetComponent<TextMeshProUGUI>();
            InputField = GameObject.transform.Find("FieldPlace/SearchField/SearchBackImage/InputField").GetComponent<TMP_InputField>();

            InputField.onValueChanged.AddListener(delegate (string _) { OnInputFieldEdit(); });
            InputField.onEndEdit.AddListener(delegate (string _) { OnInputFieldEditEnd(); });
            InputButton.OnLeftClick.AddListener(delegate { OnInputClick(); });
            Dropdown.onValueChanged.AddListener(delegate (int _) { OnDropdownSelected(); });
            DropdownButton.OnLeftClick.AddListener(delegate { OnDropdownButton(); });

            GameObject.Destroy(GameObject.GetComponent<CharGenFeatureSearchPCView>()); // controller from where we stole the search bar
            InputField.transform.Find("Text Area/Placeholder").GetComponent<TextMeshProUGUI>().SetText(placeholder);
            Dropdown.ClearOptions();

            GameObject.Destroy(Dropdown.template.Find("Viewport/TopBorderImage").gameObject);
            Transform border = Dropdown.template.Find("Viewport/Content/Item/BottomBorderImage");
            RectTransform rect = border.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.0f, 0.0f);
            rect.anchorMax = new Vector2(1.0f, 0.0f);
            rect.offsetMin = new Vector2(0.0f, 0.0f);
            rect.offsetMax = new Vector2(0.0f, 2.0f);
        }

        public void FocusSearchBar()
        {
            OnInputClick();
        }

        public void UpdatePlaceholder()
        {
            PlaceholderText.text = string.IsNullOrEmpty(InputField.text) ? Dropdown.options[Dropdown.value].text : InputField.text;
        }

        private void OnDropdownButton()
        {
            Dropdown.Show();
        }

        private void OnDropdownSelected()
        {
            UpdatePlaceholder();
        }

        private void OnInputClick()
        {
            InputButton.gameObject.SetActive(false);
            InputField.gameObject.SetActive(true);
            InputField.Select();
            InputField.ActivateInputField();
        }

        private void OnInputFieldEdit()
        {
            UpdatePlaceholder();
        }

        private void OnInputFieldEditEnd()
        {
            InputField.gameObject.SetActive(false);
            InputButton.gameObject.SetActive(true);

            if (!EventSystem.current.alreadySelecting) // could be, in same click, ending edit and starting dropdown
            {
                EventSystem.current.SetSelectedGameObject(GameObject); // return focus to regular UI
            }
        }
    }
}
