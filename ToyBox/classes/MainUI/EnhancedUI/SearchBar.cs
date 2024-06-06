using Kingmaker;
using ModKit;
using Owlcat.Runtime.UI.Controls.Button;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
namespace ToyBox {
    public class SearchBar {
        public TMP_Dropdown Dropdown;
        public OwlcatButton DropdownButton;
        public GameObject DropdownIconObject;
        public OwlcatButton InputButton;
        public TMP_InputField InputField;
        public TextMeshProUGUI PlaceholderText;
        public GameObject GameObject;

        public SearchBar(Transform parent, string placeholder, bool withDropdown = true, string name = "EnhancedInventory_SearchBar") {
            var prefab_transform = UIHelpers.SearchViewPrototype;
            //Game.Instance.UI.MainCanvas.transform.Find("ChargenPCView/ContentWrapper/DetailedViewZone/ChargenFeaturesDetailedPCView/FeatureSelectorPlace/FeatureSelectorView/FeatureSearchView");

            if (prefab_transform == null) {
                var err = "Error: Unable to locate search bar prefab, it's likely a patch has changed the UI setup, or you are in an unexpected situation. Please report this bug!";
                Mod.Error(err);
                throw new UnityException(err);
            }

            GameObject = Object.Instantiate(prefab_transform, parent, false).gameObject;
            GameObject.name = name;

            InputButton = GameObject.transform.Find("FieldPlace/SearchField/SearchBackImage/Placeholder").GetComponent<OwlcatButton>();
            if (withDropdown) {
                Dropdown = GameObject.transform.Find("FieldPlace/SearchField/SearchBackImage/Dropdown").GetComponent<TMP_Dropdown>();
                DropdownButton = GameObject.transform.Find("FieldPlace/SearchField/SearchBackImage/Dropdown/GenerateButtonPlace").GetComponent<OwlcatButton>();
                DropdownIconObject = GameObject.transform.Find("FieldPlace/SearchField/SearchBackImage/Dropdown/GenerateButtonPlace/GenerateButton/Icon").gameObject;
            } else
                Object.Destroy(GameObject.transform.Find("FieldPlace/SearchField/SearchBackImage/Dropdown").gameObject);
            PlaceholderText = GameObject.transform.Find("FieldPlace/SearchField/SearchBackImage/Placeholder/Label").GetComponent<TextMeshProUGUI>();
            InputField = GameObject.transform.Find("FieldPlace/SearchField/SearchBackImage/InputField").GetComponent<TMP_InputField>();

            InputField.onValueChanged.AddListener(delegate { OnInputFieldEdit(); });
            InputField.onEndEdit.AddListener(delegate { OnInputFieldEditEnd(); });
            InputButton.OnLeftClick.AddListener(delegate { OnInputClick(); });
            if (withDropdown) {
                Dropdown.onValueChanged.AddListener(delegate { OnDropdownSelected(); });
                DropdownButton.OnLeftClick.AddListener(delegate { OnDropdownButton(); });
            }
            InputField.transform.Find("Text Area/Placeholder").GetComponent<TextMeshProUGUI>().SetText(placeholder);

            if (withDropdown) {
                Dropdown.ClearOptions();
                Object.Destroy(Dropdown.template.Find("Viewport/TopBorderImage").gameObject);
                var border = Dropdown.template.Find("Viewport/Content/Item/BottomBorderImage");
                var rect = border.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.0f, 0.0f);
                rect.anchorMax = new Vector2(1.0f, 0.0f);
                rect.offsetMin = new Vector2(0.0f, 0.0f);
                rect.offsetMax = new Vector2(0.0f, 2.0f);
            }
        }

        public void FocusSearchBar() => OnInputClick();

        public void UpdatePlaceholder() => PlaceholderText.text = string.IsNullOrEmpty(InputField.text) ? "Search..." : InputField.text;

        private void OnDropdownButton() => Dropdown.Show();

        private void OnDropdownSelected() => UpdatePlaceholder();

        private void OnInputClick() {
            InputButton.gameObject.SetActive(false);
            InputField.gameObject.SetActive(true);
            InputField.Select();
            InputField.ActivateInputField();
        }

        private void OnInputFieldEdit() => UpdatePlaceholder();

        private void OnInputFieldEditEnd() {
            InputField.gameObject.SetActive(false);
            InputButton.gameObject.SetActive(true);

            if (!EventSystem.current.alreadySelecting) // could be, in same click, ending edit and starting dropdown
                EventSystem.current.SetSelectedGameObject(GameObject); // return focus to regular UI
        }
    }
}