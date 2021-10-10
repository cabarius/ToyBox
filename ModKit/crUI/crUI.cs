#if BUILD_CRUI
#warning crUI conditionally included

using HarmonyLib;
using Kingmaker.AI.Blueprints;
using Kingmaker.AI;
using Kingmaker.Assets.UI.Common;
using Kingmaker.Assets.UI;
using Kingmaker.Blueprints;
using Kingmaker.Cheats;
using Kingmaker.Controllers;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem;
using Kingmaker.Items.Slots;
using Kingmaker.Items;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem;
using Kingmaker.UI.Common;
using Kingmaker.UI.MVVM;
using Kingmaker.UI;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic;
using Kingmaker.Utility;
using Kingmaker.View;
using Kingmaker;
using Owlcat.Runtime.UI.Controls.Button;
using Owlcat.Runtime.UI.Controls.Selectable;
using Owlcat.Runtime.UI.Controls.SelectableState;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine;
using UnityModManagerNet;

namespace ModKit {

public static class crUI {
	public static void Demo() {
		if(crUI.obj) {
			obj.SetActive(true);
			return;
		}

		crUI.Init();

		RectTransform root = crUI.TopLevelWindow(crUI.CommonPCView.transform.Find("FadeCanvas"));
		RectTransform frame = crUI.Frame(root);
		RectTransform brand = crUI.Text(root,
			"<color=#000000FF><size=32><font=\"Saber_Dist32\"><color=#672B31>T</color></font>oyBox</size></color> " +
			$"<color=#888888FF><size=16><smallcaps>v{ToyBox.Main.modEntry.Version}</smallcaps></size></color>"
		);
		RectTransform tagline = crUI.Text(root, "<color=#333333FF><size=16><align=\"center\">a box of toys</align></size></color>");
		RectTransform space = crUI.Space(root, minSize: new[] {-1.0f, 32.0f}, flexibleSize: new[] {1.0f, -1.0f});

		string[] options = {
			"Allow Achievements While Using Mods",
            "Object Highlight Toggle Mode",
            "Highlight Copyable Scrolls",
            "Spiders begone (experimental)",
            "Vescavors begone (experimental)",
            "Make Tutorials Not Appear If Disabled In Settings",
            "Refill consumables in belt slots if in inventory",
            "Auto Load Last Save On Launch",
            "Allow Shift Click To Use Items In Inventory",
            "Allow Shift Click To Transfer Entire Stack",
		};

		RectTransform vgroup = crUI.VerticalGroup(root);
		foreach(string option in options) {
			RectTransform hgroup = crUI.HorizontalGroup(vgroup);
			RectTransform checkbox = crUI.Checkbox(hgroup);
			RectTransform spacing = crUI.Space(hgroup, minSize: new[] {8.0f, -1.0f}, flexibleSize: new[] {-1.0f, 1.0f});
			RectTransform text = crUI.Text(hgroup, $"<color=#000000FF><align=\"left\">{option}</align></color>");
		}

		RectTransform hgroup_buttons = crUI.HorizontalGroup(root);
		RectTransform button0 = crUI.Button(hgroup_buttons, "click me");
		RectTransform button1 = crUI.Button(hgroup_buttons, "click me too");
	}

	public static GameObject obj;
	public static MonoBehaviour CommonPCView;
	public static Image FrameBackgroundImage;
	public static Image CheckboxBackgroundImage;
	public static Image CheckboxCheckmarkImage;
	public static List<OwlcatSelectableLayerPart> CheckboxLayerParts;
	public static Image ButtonBackgroundImage;
	public static SpriteState ButtonBackgroundSpriteState;
	public static float DefaultSpacing = 0.0f;
	public static RectOffset DefaultPadding = new RectOffset(0, 0, 0, 0);

	public static void Init() {
		// there's definitely a better way to get sprites and images
		// but don't know how
		crUI.CommonPCView = AccessTools
			.DeclaredField(typeof(RootUIContext), "m_CommonView")?
				.GetValue(Game.Instance?.RootUiContext) as MonoBehaviour;

		crUI.FrameBackgroundImage = crUI.CommonPCView.transform
			.Find("FadeCanvas/InfoWindow/Window/Background")?
				.gameObject?.GetComponent<Image>();

		Transform owlcat_checkbox = null;
		foreach(GameObject obj in Resources.FindObjectsOfTypeAll<GameObject>()) {
			if(obj.name == "ShowHelmContainer") {
				owlcat_checkbox = obj.transform.Find("ToggleOwlcatMultiSelectable");
				if(owlcat_checkbox != null) {
					break;
				}
			}
		}

		crUI.CheckboxBackgroundImage = owlcat_checkbox?.Find("Background")?
			.GetComponent<Image>();
	
		crUI.CheckboxCheckmarkImage = owlcat_checkbox?.Find("Background/Checkmark")?
			.GetComponent<Image>();

		crUI.CheckboxLayerParts = AccessTools.DeclaredField(typeof(OwlcatSelectable), "m_CommonLayer")
			.GetValue(owlcat_checkbox?.GetComponent<OwlcatMultiButton>()) as List<OwlcatSelectableLayerPart>;

		Transform owlcat_save_button = crUI.CommonPCView.gameObject.transform
			.Find("FadeCanvas/EscMenuView/Window/ButtonBlock/SaveButton");

		crUI.ButtonBackgroundImage = owlcat_save_button?.GetComponent<Image>();

		List<OwlcatSelectableLayerPart> button_layer_parts = AccessTools.DeclaredField(typeof(OwlcatSelectable), "m_CommonLayer")
			.GetValue(owlcat_save_button?.GetComponent<OwlcatButton>()) as List<OwlcatSelectableLayerPart>;

		crUI.ButtonBackgroundSpriteState = button_layer_parts[0].SpriteState;
	}

	public static RectTransform TopLevelWindow(Transform parent) {
		GameObject obj = new GameObject("ToyBox.TopLevelWindow");
		ToyBox.Main.Objects.Add(obj);
		crUI.obj = obj;

		RectTransform transform = obj.AddComponent<RectTransform>();
		transform.SetParent(parent, false);
		transform.anchorMin = new Vector2(0.0f, 0.0f);
		transform.anchorMax = new Vector2(1.0f, 1.0f);
		transform.pivot = new Vector2(0.5f, 0.5f);
		transform.offsetMin = new Vector2(0.0f, 0.0f);
		transform.offsetMax = new Vector2(0.0f, 0.0f);

		VerticalLayoutGroupWorkaround group = obj.AddComponent<VerticalLayoutGroupWorkaround>();
		group.SetLayoutVertical();
		group.childControlWidth = true;
		group.childControlHeight = true;
		group.childForceExpandWidth = false;
		group.childForceExpandHeight = false;
		group.childScaleWidth = false;
		group.childScaleHeight = false;
		group.childAlignment = TextAnchor.UpperLeft;
		group.spacing = DefaultSpacing;
		group.padding = DefaultPadding;

		LayoutElement layout = obj.AddComponent<LayoutElement>();
		layout.minWidth = -1.0f;
		layout.minHeight = -1.0f;
		layout.preferredWidth = -1.0f;
		layout.preferredHeight = -1.0f;
		layout.flexibleWidth = 1.0f;
		layout.flexibleHeight = 1.0f;

		CanvasGroup canvas = obj.AddComponent<CanvasGroup>();

		ContentSizeFitterExtended fitter = obj.AddComponent<ContentSizeFitterExtended>();
		AccessTools
			.DeclaredField(typeof(ContentSizeFitterExtended), "m_HorizontalFit")
				.SetValue(fitter, ContentSizeFitterExtended.FitMode.PreferredSize);
		AccessTools
			.DeclaredField(typeof(ContentSizeFitterExtended), "m_VerticalFit")
				.SetValue(fitter, ContentSizeFitterExtended.FitMode.PreferredSize);
		fitter.SetLayoutVertical();

		return transform;
	}

	public static RectTransform Frame(RectTransform parent) {
		GameObject obj = new GameObject("ToyBox.Frame");

		RectTransform transform = obj.AddComponent<RectTransform>();
		transform.SetParent(parent, false);
		transform.anchorMin = new Vector2(0.0f, 0.0f);
		transform.anchorMax = new Vector2(1.0f, 1.0f);
		transform.pivot = new Vector2(0.5f, 0.5f);
		transform.offsetMin = new Vector2(-16.0f, -16.0f);
		transform.offsetMax = new Vector2(16.0f, 16.0f);

		HorizontalLayoutGroupWorkaround group = obj.AddComponent<HorizontalLayoutGroupWorkaround>();
		group.SetLayoutHorizontal();
		group.childControlWidth = true;
		group.childControlHeight = true;
		group.childForceExpandWidth = false;
		group.childForceExpandHeight = false;
		group.childScaleWidth = false;
		group.childScaleHeight = false;
		group.childAlignment = TextAnchor.UpperRight;
		group.spacing = DefaultSpacing;
		group.padding = new RectOffset(left: 0, bottom: 0, right: 16, top: 8);

		LayoutElement layout = obj.AddComponent<LayoutElement>();
		layout.ignoreLayout = true;
		layout.minWidth = -1.0f;
		layout.minHeight = -1.0f;
		layout.preferredWidth = -1.0f;
		layout.preferredHeight = -1.0f;
		layout.flexibleWidth = 1.0f;
		layout.flexibleHeight = 1.0f;

		CanvasRenderer renderer = obj.AddComponent<CanvasRenderer>();

		Image image = obj.AddComponent<Image>();
		image.sprite = crUI.FrameBackgroundImage?.sprite;

		DraggbleWindow drag = obj.AddComponent<DraggbleWindow>();
		AccessTools
			.DeclaredField(typeof(DraggbleWindow), "m_OwnRectTransform")
				.SetValue(drag, parent);
		AccessTools
			.DeclaredField(typeof(DraggbleWindow), "m_ParentRectTransform")
				.SetValue(drag, parent.parent);

		RectTransform close = crUI.Button(transform, "Close");

		LayoutElement close_layout = close.GetComponent<LayoutElement>();
		close_layout.minWidth = 64.0f;
		close_layout.minHeight = 32.0f;
		close_layout.preferredWidth = close_layout.minWidth;
		close_layout.preferredHeight = close_layout.minHeight;
		close_layout.flexibleWidth = 0.0f;
		close_layout.flexibleHeight = 0.0f;

		OwlcatButton button = close.GetComponent<OwlcatButton>();
		Button.ButtonClickedEvent evt = AccessTools
			.DeclaredField(typeof(OwlcatButton), "m_OnLeftClick").GetValue(button) as Button.ButtonClickedEvent;

		evt.AddListener(() => {
			parent.gameObject.SetActive(false);
		});

		return transform;
	}

	public static RectTransform Text(RectTransform parent, string text) {
		GameObject obj = new GameObject("ToyBox.Text");

		RectTransform transform = obj.AddComponent<RectTransform>();
		transform.SetParent(parent, false);
		transform.anchorMin = new Vector2(0.0f, 0.0f);
		transform.anchorMax = new Vector2(1.0f, 1.0f);
		transform.pivot = new Vector2(0.5f, 0.5f);
		transform.offsetMin = new Vector2(0.0f, 0.0f);
		transform.offsetMax = new Vector2(0.0f, 0.0f);

		LayoutElement layout = obj.AddComponent<LayoutElement>();
		layout.minWidth = -1.0f;
		layout.minHeight = -1.0f;
		layout.preferredWidth = -1.0f;
		layout.preferredHeight = -1.0f;
		layout.flexibleWidth = 4.0f;
		layout.flexibleHeight = 4.0f;

		CanvasRenderer renderer = obj.AddComponent<CanvasRenderer>();

		TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
		tmp.alignment = TextAlignmentOptions.Left;
		tmp.autoSizeTextContainer = true;
		tmp.enableWordWrapping = false;
		tmp.fontSize = 18.0f;
		tmp.richText = true;
		tmp.text = text;

		return transform;
	}

	public static RectTransform Button(RectTransform parent, string text) {
		GameObject obj = new GameObject("ToyBox.Button");

		RectTransform transform = obj.AddComponent<RectTransform>();
		transform.SetParent(parent, false);
		transform.anchorMin = new Vector2(0.0f, 0.0f);
		transform.anchorMax = new Vector2(1.0f, 1.0f);
		transform.pivot = new Vector2(0.5f, 0.5f);
		transform.offsetMin = new Vector2(0.0f, 0.0f);
		transform.offsetMax = new Vector2(0.0f, 0.0f);

		HorizontalLayoutGroupWorkaround group = obj.AddComponent<HorizontalLayoutGroupWorkaround>();
		group.SetLayoutHorizontal();
		group.childControlWidth = true;
		group.childControlHeight = true;
		group.childForceExpandWidth = false;
		group.childForceExpandHeight = false;
		group.childScaleWidth = false;
		group.childScaleHeight = false;
		group.childAlignment = TextAnchor.MiddleCenter;
		group.spacing = DefaultSpacing;
		group.padding = DefaultPadding;

		LayoutElement layout = obj.AddComponent<LayoutElement>();
		layout.minWidth = -1.0f;
		layout.minHeight = -1.0f;
		layout.preferredWidth = -1.0f;
		layout.preferredHeight = -1.0f;
		layout.flexibleWidth = 1.0f;
		layout.flexibleHeight = 1.0f;

		CanvasRenderer renderer = obj.AddComponent<CanvasRenderer>();

		RectTransform inner_text = crUI.Text(transform, text);
		inner_text.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

		Image image = obj.AddComponent<Image>();
		image.sprite = crUI.ButtonBackgroundImage?.sprite;

		SpriteState sprite_state = crUI.ButtonBackgroundSpriteState;

		OwlcatButton button = obj.AddComponent<OwlcatButton>();
		button.ClickSoundType = -1;
		button.HoverSoundType = 0;

		OwlcatSelectableLayerPart part0 = new OwlcatSelectableLayerPart();
		part0.Image = image;
		part0.Transition = OwlcatTransition.SpriteSwap;
		part0.SpriteState = sprite_state;

		OwlcatSelectableLayerPart part1 = new OwlcatSelectableLayerPart();
		ColorBlock colors = new ColorBlock();
		colors.normalColor = new Color(0.7843f, 0.7686f, 0.749f, 1.0f);
		colors.highlightedColor = new Color(0.9176f, 0.8824f, 0.8235f, 1.0f);
		colors.pressedColor = new Color(0.7647f, 0.749f, 0.7294f, 1.0f);
		colors.selectedColor = new Color(0.9176f, 0.8824f, 0.8235f, 1.0f);
		colors.disabledColor = new Color(0.6941f, 0.6941f, 0.6902f, 0.1922f);
		colors.colorMultiplier = 1.0f;
		colors.fadeDuration = 0.1f;
		part1.Colors = colors;
		part1.TargetGraphic = inner_text.GetComponent<TextMeshProUGUI>();

		button.AddLayerToMainPart(part0);
		button.AddLayerToMainPart(part1);

		return transform;
	}

	public static RectTransform Space(RectTransform parent, float[] minSize, float[] flexibleSize) {
		GameObject obj = new GameObject("ToyBox.Space");

		RectTransform transform = obj.AddComponent<RectTransform>();
		transform.SetParent(parent, false);
		transform.anchorMin = new Vector2(0.0f, 0.0f);
		transform.anchorMax = new Vector2(1.0f, 1.0f);
		transform.pivot = new Vector2(0.5f, 0.5f);
		transform.offsetMin = new Vector2(0.0f, 0.0f);
		transform.offsetMax = new Vector2(0.0f, 0.0f);

		LayoutElement layout = obj.AddComponent<LayoutElement>();
		layout.minWidth = minSize[0];
		layout.minHeight = minSize[1];
		layout.preferredWidth = layout.minWidth;
		layout.preferredHeight = layout.minHeight;
		layout.flexibleWidth = flexibleSize[0];
		layout.flexibleHeight = flexibleSize[1];

		return transform;
	}


	public static RectTransform VerticalGroup(RectTransform parent) {
		GameObject obj = new GameObject("ToyBox.VerticalGroup");

		RectTransform transform = obj.AddComponent<RectTransform>();
		transform.SetParent(parent, false);
		transform.anchorMin = new Vector2(0.0f, 0.0f);
		transform.anchorMax = new Vector2(1.0f, 1.0f);
		transform.pivot = new Vector2(0.5f, 0.5f);
		transform.offsetMin = new Vector2(0.0f, 0.0f);
		transform.offsetMax = new Vector2(0.0f, 0.0f);

		VerticalLayoutGroupWorkaround group = obj.AddComponent<VerticalLayoutGroupWorkaround>();
		group.SetLayoutVertical();
		group.childControlWidth = true;
		group.childControlHeight = true;
		group.childForceExpandWidth = false;
		group.childForceExpandHeight = false;
		group.childScaleWidth = false;
		group.childScaleHeight = false;
		group.childAlignment = TextAnchor.UpperLeft;
		group.spacing = DefaultSpacing;
		group.padding = DefaultPadding;

		return transform;
	}

	public static RectTransform HorizontalGroup(RectTransform parent) {
		GameObject obj = new GameObject("ToyBox.HorizontalGroup");

		RectTransform transform = obj.AddComponent<RectTransform>();
		transform.SetParent(parent, false);

		HorizontalLayoutGroupWorkaround group = obj.AddComponent<HorizontalLayoutGroupWorkaround>();
		group.SetLayoutHorizontal();
		group.childControlWidth = true;
		group.childControlHeight = true;
		group.childForceExpandWidth = false;
		group.childForceExpandHeight = false;
		group.childScaleWidth = false;
		group.childScaleHeight = false;
		group.childAlignment = TextAnchor.UpperLeft;
		group.spacing = DefaultSpacing;
		group.padding = DefaultPadding;

		return transform;
	}

	public static RectTransform Checkbox(RectTransform parent) {
		GameObject obj = new GameObject("ToyBox.Checkbox");

		RectTransform transform = obj.AddComponent<RectTransform>();
		transform.SetParent(parent, false);
		transform.anchorMin = new Vector2(0.0f, 0.0f);
		transform.anchorMax = new Vector2(1.0f, 1.0f);
		transform.pivot = new Vector2(0.5f, 0.5f);
		transform.offsetMin = new Vector2(0.0f, 0.0f);
		transform.offsetMax = new Vector2(0.0f, 0.0f);

		HorizontalLayoutGroupWorkaround group = obj.AddComponent<HorizontalLayoutGroupWorkaround>();
		group.SetLayoutHorizontal();
		group.childControlWidth = true;
		group.childControlHeight = true;
		group.childForceExpandWidth = false;
		group.childForceExpandHeight = false;
		group.childScaleWidth = false;
		group.childScaleHeight = false;
		group.childAlignment = TextAnchor.UpperLeft;
		group.spacing = DefaultSpacing;
		group.padding = DefaultPadding;

		LayoutElement layout = obj.AddComponent<LayoutElement>();
		layout.minWidth = 32.0f;
		layout.minHeight = 32.0f;
		layout.preferredWidth = -1.0f;
		layout.preferredHeight = -1.0f;
		layout.flexibleWidth = 0.0f;
		layout.flexibleHeight = 0.0f;

		OwlcatMultiButton button = obj.AddComponent<OwlcatMultiButton>();
		button.ClickSoundType = -1;
		//button.HoverSoundType = 1;

		// background
		GameObject background_obj = new GameObject("ToyBox.Checkbox.Background");

		RectTransform background_transform = background_obj.AddComponent<RectTransform>();
		background_transform.SetParent(transform, false);
		background_transform.anchorMin = new Vector2(0.0f, 0.0f);
		background_transform.anchorMax = new Vector2(1.0f, 1.0f);
		background_transform.pivot = new Vector2(0.5f, 0.5f);
		background_transform.offsetMin = new Vector2(0.0f, 0.0f);
		background_transform.offsetMax = new Vector2(0.0f, 0.0f);

		HorizontalLayoutGroupWorkaround background_group = background_obj.AddComponent<HorizontalLayoutGroupWorkaround>();
		background_group.SetLayoutHorizontal();
		background_group.childControlWidth = true;
		background_group.childControlHeight = true;
		background_group.childForceExpandWidth = false;
		background_group.childForceExpandHeight = false;
		background_group.childScaleWidth = false;
		background_group.childScaleHeight = false;
		background_group.childAlignment = TextAnchor.UpperLeft;
		background_group.spacing = DefaultSpacing;
		background_group.padding = new RectOffset(
			(int) (layout.minWidth * 0.2f),
			(int) (layout.minHeight * 0.2f),
			(int) (layout.minWidth * 0.2f),
			(int) (layout.minHeight * 0.2f)
		);

		LayoutElement background_layout = background_obj.AddComponent<LayoutElement>();
		background_layout.minWidth = -1.0f;
		background_layout.minHeight = -1.0f;
		background_layout.preferredWidth = -1.0f;
		background_layout.preferredHeight = -1.0f;
		background_layout.flexibleWidth = 0.0f;
		background_layout.flexibleHeight = 0.0f;

		CanvasRenderer background_renderer = background_obj.AddComponent<CanvasRenderer>();

		Image background_image = background_obj.AddComponent<Image>();
		background_image.sprite = crUI.CheckboxBackgroundImage?.sprite;

		// checkmark
		GameObject checkmark_obj = new GameObject("ToyBox.Checkbox.Checkmark");

		RectTransform checkmark_transform = checkmark_obj.AddComponent<RectTransform>();
		checkmark_transform.SetParent(background_transform, false);
		checkmark_transform.anchorMin = new Vector2(0.0f, 0.0f);
		checkmark_transform.anchorMax = new Vector2(1.0f, 1.0f);
		checkmark_transform.pivot = new Vector2(0.5f, 0.5f);
		checkmark_transform.offsetMin = new Vector2(0.0f, 0.0f);
		checkmark_transform.offsetMax = new Vector2(0.0f, 0.0f);

		LayoutElement checkmark_layout = checkmark_obj.AddComponent<LayoutElement>();
		checkmark_layout.minWidth = -1.0f;
		checkmark_layout.minHeight = -1.0f;
		checkmark_layout.preferredWidth = -1.0f;
		checkmark_layout.preferredHeight = -1.0f;
		checkmark_layout.flexibleWidth = 1.0f;
		checkmark_layout.flexibleHeight = 1.0f;

		CanvasRenderer checkmark_renderer = checkmark_obj.AddComponent<CanvasRenderer>();

		Image checkmark_image = checkmark_obj.AddComponent<Image>();
		checkmark_image.sprite = crUI.CheckboxCheckmarkImage?.sprite;

		SpriteState sprite_state = crUI.CheckboxLayerParts[0].SpriteState;

		OwlcatSelectableLayerPart layer0 = new OwlcatSelectableLayerPart();
		layer0.Image = background_image;
		layer0.Transition = OwlcatTransition.SpriteSwap;
		layer0.SpriteState = sprite_state;
		button.AddLayerToMainPart(layer0);

		button.AddMultiLayer();
		button.AddMultiLayer();

		List<OwlcatMultiLayer> layers = AccessTools.DeclaredField(typeof(OwlcatMultiSelectable), "m_MultiLayers")
			.GetValue(button) as List<OwlcatMultiLayer>;

		layers[0].LayerName = "Unchecked";

		OwlcatMultiLayer layer1 = layers[1];
		layer1.LayerName = "Checked";
		layer1.AddPart();
		layer1.Parts[0].Transition = OwlcatTransition.Activate;
		AccessTools.DeclaredField(typeof(OwlcatSelectableLayerPart), "m_TargetGameObject")
			.SetValue(layer1.Parts[0], checkmark_obj);

		AccessTools.DeclaredMethod(typeof(OwlcatMultiSelectable), "DoSetLayers").Invoke(button, new object[] {});

		Button.ButtonClickedEvent evt = AccessTools
			.DeclaredField(typeof(OwlcatMultiButton), "m_OnLeftClick").GetValue(button) as Button.ButtonClickedEvent;

		evt.AddListener(() => {
			List<OwlcatMultiLayer> layers = AccessTools.DeclaredField(typeof(OwlcatMultiSelectable), "m_MultiLayers")
				.GetValue(button) as List<OwlcatMultiLayer>;
			button.SetActiveLayer((button.ActiveLayerIndex + 1) % layers.Count);
		});

		return transform;
	}
}

}

#endif