using ColossalFramework;
using ColossalFramework.UI;
using EManagersLib;
using System.Threading;
using UnityEngine;

namespace PropAnarchy.PLT {
    public class ToolBar : UIPanel {
        private const float PLT_TOOLBAR_WIDTH = 256f;
        private const float PLT_TOOLBAR_HEIGHT = 36f;
        private const float SIZE_INEDITOR_WIDTH = 266f;
        private const float SIZE_INEDITOR_HEIGHT = 46f;
        private const float HOLDERPANEL_OFFSET = 5f;
        private const float PLT_TOOLBAR_BTNSIZE = PLT_TOOLBAR_HEIGHT;
        private const string PLT_SINGLEDEFAULT_NAME = @"Single/Default";
        private const string PLT_STRAIGHT_NAME = @"Straight";
        private const string PLT_CURVED_NAME = @"Curved";
        private const string PLT_FREEFORM_NAME = @"Freeform";
        private const string PLT_CIRCLE_NAME = @"Circle";
        private static UIPanel m_brushPanel;
        internal static UITextureAtlas m_sharedTextures;

        public override void Awake() {
            string[] m_spriteNamesPLT = {
                "PLT_MultiStateZero", "PLT_MultiStateZeroFocused", "PLT_MultiStateZeroHovered", "PLT_MultiStateZeroPressed", "PLT_MultiStateZeroDisabled",
                "PLT_MultiStateOne", "PLT_MultiStateOneFocused", "PLT_MultiStateOneHovered", "PLT_MultiStateOnePressed", "PLT_MultiStateOneDisabled",
                "PLT_MultiStateTwo", "PLT_MultiStateTwoFocused", "PLT_MultiStateTwoHovered", "PLT_MultiStateTwoPressed", "PLT_MultiStateTwoDisabled",
                "PLT_ToggleCPZero", "PLT_ToggleCPZeroFocused", "PLT_ToggleCPZeroHovered", "PLT_ToggleCPZeroPressed", "PLT_ToggleCPZeroDisabled",
                "PLT_ToggleCPOne", "PLT_ToggleCPOneFocused", "PLT_ToggleCPOneHovered", "PLT_ToggleCPOnePressed", "PLT_ToggleCPOneDisabled",
                "PLT_FenceModeZero", "PLT_FenceModeZeroFocused", "PLT_FenceModeZeroHovered", "PLT_FenceModeZeroPressed", "PLT_FenceModeZeroDisabled",
                "PLT_FenceModeOne", "PLT_FenceModeOneFocused", "PLT_FenceModeOneHovered", "PLT_FenceModeOnePressed", "PLT_FenceModeOneDisabled",
                "PLT_FenceModeTwo", "PLT_FenceModeTwoFocused", "PLT_FenceModeTwoHovered", "PLT_FenceModeTwoPressed", "PLT_FenceModeTwoDisabled",
                "PLT_ItemwiseZero", "PLT_ItemwiseZeroFocused", "PLT_ItemwiseZeroHovered", "PLT_ItemwiseZeroPressed", "PLT_ItemwiseZeroDisabled",
                "PLT_ItemwiseOne", "PLT_ItemwiseOneFocused", "PLT_ItemwiseOneHovered", "PLT_ItemwiseOnePressed", "PLT_ItemwiseOneDisabled",
                "PLT_SpacingwiseZero", "PLT_SpacingwiseZeroFocused", "PLT_SpacingwiseZeroHovered", "PLT_SpacingwiseZeroPressed", "PLT_SpacingwiseZeroDisabled",
                "PLT_SpacingwiseOne", "PLT_SpacingwiseOneFocused", "PLT_SpacingwiseOneHovered", "PLT_SpacingwiseOnePressed", "PLT_SpacingwiseOneDisabled",
                "PLT_BasicDividerTile02x02"
            };

            base.Awake();
            atlas = m_sharedTextures = PAUtils.CreateTextureAtlas(@"PLTAtlas", @"PropAnarchy.PLT.Icons.", m_spriteNamesPLT, 1024);
            size = new Vector2(PLT_TOOLBAR_WIDTH, PLT_TOOLBAR_HEIGHT);
            UIMultiStateButton fenceModeToggleBtn = AddToggleBtn(this, @"PLTToggleFenceMode", atlas, @"PLT_MultiStateZero", @"PLT_MultiStateOne", @"PLT_FenceModeZero", @"PLT_FenceModeOne");
            fenceModeToggleBtn.relativePosition = new Vector3(0, 0);
            fenceModeToggleBtn.tooltip = PALocale.GetLocale(@"PLTToggleFenceMode");
            fenceModeToggleBtn.isVisible = false;
            PropLineTool.GetFenceMode = () => fenceModeToggleBtn.activeStateIndex != 0;
            UITabstrip controlTabStrip = AddUIComponent<UITabstrip>();
            controlTabStrip.relativePosition = new Vector3(PLT_TOOLBAR_BTNSIZE, 0f);
            controlTabStrip.width = 180f;
            controlTabStrip.height = PLT_TOOLBAR_HEIGHT;
            controlTabStrip.padding.right = 0;
            UIButton buttonTemplate = GameObject.Find(@"ToolMode").GetComponent<UITabstrip>().GetComponentInChildren<UIButton>();
            UIButton singleDefaultBtn = AddButton(controlTabStrip, buttonTemplate, PLT_SINGLEDEFAULT_NAME, @"•", 1.5f);
            singleDefaultBtn.textPadding.left = 0;
            singleDefaultBtn.textPadding.right = 1;
            singleDefaultBtn.textPadding.top = 4;
            singleDefaultBtn.textPadding.bottom = 0;
            _ = AddButton(controlTabStrip, buttonTemplate, PLT_STRAIGHT_NAME);
            _ = AddButton(controlTabStrip, buttonTemplate, PLT_CURVED_NAME);
            _ = AddButton(controlTabStrip, buttonTemplate, PLT_FREEFORM_NAME);
            UIButton circleBtn = AddButton(controlTabStrip, buttonTemplate, PLT_CIRCLE_NAME, @"○", 3.0f);
            circleBtn.textPadding.left = -2;
            circleBtn.textPadding.right = 1;
            circleBtn.textPadding.top = -13;
            circleBtn.textPadding.bottom = 0;
            controlTabStrip.selectedIndex = PropLineTool.DrawMode.SINGLE;
            controlTabStrip.startSelectedIndex = PropLineTool.DrawMode.SINGLE;
            PropLineTool.DrawMode.SetCurrentSelected = (value) => controlTabStrip.selectedIndex = value;
            UIMultiStateButton controlPanelToggleBtn = AddToggleBtn(this, @"PLTToggleControlPanel", atlas, @"PLT_ToggleCPZero", @"PLT_ToggleCPOne", @"", @"");
            controlPanelToggleBtn.relativePosition = new Vector3(PLT_TOOLBAR_BTNSIZE * 6f, 0f);
            controlPanelToggleBtn.tooltip = PALocale.GetLocale(@"PLTToggleControlPanel");
            controlPanelToggleBtn.isVisible = false;
            controlPanelToggleBtn.eventActiveStateIndexChanged += (c, index) => {
                if (index != 0) OptionPanel.Open(PropLineTool.m_itemType);
                else OptionPanel.Close();
            };
            //LandscapingGroupPanel landscapingPanel = UIView.GetAView().GetComponentInChildren<LandscapingGroupPanel>();
            //UITabstrip landscapingStrip = AccessTools.Field(typeof(LandscapingGroupPanel), @"m_Strip").GetValue(landscapingPanel) as UITabstrip;
            controlTabStrip.eventSelectedIndexChanged += (c, index) => {
                PropLineTool.DrawMode.Current = index;
                if (index == PropLineTool.DrawMode.SINGLE) {
                    ToolBase currentTool = ToolsModifierControl.toolController.CurrentTool;
                    if (currentTool is TreeTool || currentTool is PropTool) {
                        controlPanelToggleBtn.activeStateIndex = 0;
                        if (m_brushPanel && !m_brushPanel.isVisible) m_brushPanel.Show();
                        fenceModeToggleBtn.isVisible = false;
                        controlPanelToggleBtn.isVisible = false;
                    }
                } else {
                    if (m_brushPanel && m_brushPanel.isVisible) m_brushPanel.Hide();
                    fenceModeToggleBtn.isVisible = true;
                    controlPanelToggleBtn.isVisible = true;
                    ToolsModifierControl.SetTool<PropLineTool>();
                }
            };
            isVisible = false;
        }

        public override void Start() {
            UIComponent optionsBar = GameObject.Find(@"OptionsBar").GetComponent<UIComponent>();
            if (optionsBar is null) {
                PAModule.PALog($"OptionBar not found");
                absolutePosition = new Vector3(261f, 542f);
            } else {
                absolutePosition = optionsBar.absolutePosition;
                float widthDifference = width - optionsBar.width;
                if (widthDifference != 0f) {
                    float absX = absolutePosition.x;
                    float absY = absolutePosition.y;
                    float newX = EMath.RoundToInt(absX - (widthDifference / 2));
                    if (newX < 0) newX = 0;
                    float newY = absY + optionsBar.height - height - 6f;
                    ItemClass.Availability mode = Singleton<ToolManager>.instance.m_properties.m_mode;
                    if (mode == ItemClass.Availability.AssetEditor || mode == ItemClass.Availability.MapEditor) {
                        size = new Vector2(SIZE_INEDITOR_WIDTH, SIZE_INEDITOR_HEIGHT);
                        backgroundSprite = @"GenericPanel";
                        color = new Color32(91, 97, 106, 255);
                        relativePosition = new Vector2(HOLDERPANEL_OFFSET, HOLDERPANEL_OFFSET);
                    }
                    absolutePosition = new Vector2(newX, newY);
                }
            }
            GameObject brushGO = GameObject.Find(@"BrushPanel");
            if (!(brushGO is null)) {
                m_brushPanel = brushGO.GetComponent<UIPanel>();
            }
        }

        public override void OnDestroy() { }

        public static void ActivatePLT(object _) {
            Thread.Sleep(1);
            ToolsModifierControl.SetTool<PropLineTool>();
        }

        public static bool SetToolPrefix(ToolBase tool) {
            if ((tool is TreeTool || tool is PropTool) && ToolsModifierControl.toolController.CurrentTool is PropLineTool) {
                if (PropLineTool.DrawMode.Current != PropLineTool.DrawMode.SINGLE)
                    return false;
            }
            return true;
        }

        public static void SetToolPostfix(ToolBase tool) {
            if (!(tool is null) && !(PropLineTool.m_toolBar is null) && !(m_brushPanel is null)) {
                if (tool is TreeTool || tool is PropTool) {
                    if (tool is TreeTool) {
                        PropLineTool.m_itemType = PropLineTool.ItemType.TREE;
                        OptionPanel.ToggleAnglePanel(PropLineTool.ItemType.TREE);
                    } else {
                        PropLineTool.m_itemType = PropLineTool.ItemType.PROP;
                        OptionPanel.ToggleAnglePanel(PropLineTool.ItemType.PROP);
                    }
                    if (!PropLineTool.m_toolBar.isVisible) {
                        PropLineTool.DrawMode.SetCurrentSelected?.Invoke(0);
                        PropLineTool.m_toolBar.Show();
                    }
                    if (PropLineTool.DrawMode.Current == PropLineTool.DrawMode.SINGLE) {
                        if (!m_brushPanel.isVisible) m_brushPanel.Show();
                    }
                } else if (tool is PropLineTool) {
                    PropLineTool.m_toolBar.Show();
                } else {
                    if (PropLineTool.m_toolBar.isVisible) {
                        PropLineTool.m_toolBar.Hide();
                        PropLineTool.m_optionPanel.Hide();
                    }
                    if (m_brushPanel.isVisible) m_brushPanel.Hide();
                }
            }
        }

        private static UIButton AddButton(UITabstrip tab, UIButton btnTemplate, string name, string textDisplay, float textScale) {
            UIButton btn = tab.AddTab(name, btnTemplate, false);
            btn.autoSize = false;
            btn.tooltip = @"[PLT]: " + name;
            btn.height = 36f;
            btn.width = 36f;
            btn.name = name;
            btn.normalFgSprite = @"";
            btn.focusedFgSprite = @"";
            btn.hoveredFgSprite = @"";
            btn.pressedFgSprite = @"";
            btn.disabledFgSprite = @"";
            btn.text = textDisplay;
            btn.textScale = textScale;
            btn.textColor = new Color32(119, 124, 126, 255);
            btn.hoveredTextColor = new Color32(110, 113, 114, 255);
            btn.pressedTextColor = new Color32(172, 175, 176, 255);
            btn.focusedTextColor = new Color32(187, 224, 235, 255);
            btn.disabledTextColor = new Color32(66, 69, 70, 255);
            btn.playAudioEvents = true;
            return btn;
        }

        private static UIButton AddButton(UITabstrip tab, UIButton btnTemplate, string name) {
            string spriteName = @"RoadOption" + name;
            UIButton btn = tab.AddTab(name, btnTemplate, false);
            btn.autoSize = false;
            btn.tooltip = @"[PLT]: " + name;
            btn.height = 36f;
            btn.width = 36f;
            btn.name = name;
            btn.normalFgSprite = spriteName;
            btn.focusedFgSprite = spriteName + @"Focused";
            btn.hoveredFgSprite = spriteName + @"Hovered";
            btn.pressedFgSprite = spriteName + @"Pressed";
            btn.disabledFgSprite = spriteName + @"Disabled";
            btn.playAudioEvents = true;
            return btn;
        }

        public static UIMultiStateButton AddToggleBtn(UIComponent parent, string name, UITextureAtlas atlas, string bgPrefix0, string bgPrefix1, string fgPrefix0, string fgPrefix1) {
            UIMultiStateButton toggleBtn = parent.AddUIComponent<UIMultiStateButton>();
            toggleBtn.name = name;
            toggleBtn.cachedName = name;
            toggleBtn.atlas = atlas;
            UIMultiStateButton.SpriteSetState fgSpriteSetState = toggleBtn.foregroundSprites;
            UIMultiStateButton.SpriteSetState bgSpriteSetState = toggleBtn.backgroundSprites;
            UIMultiStateButton.SpriteSet fgSpriteSet0 = fgSpriteSetState[0];
            UIMultiStateButton.SpriteSet bgSpriteSet0 = bgSpriteSetState[0];
            if (fgSpriteSet0 is null) {
                fgSpriteSetState.AddState();
                fgSpriteSet0 = fgSpriteSetState[0];
            }
            if (bgSpriteSet0 is null) {
                bgSpriteSetState.AddState();
                bgSpriteSet0 = bgSpriteSetState[0];
            }
            if (fgPrefix0 != @"") {
                fgSpriteSet0.normal = (fgPrefix0 + @"");
                fgSpriteSet0.focused = (fgPrefix0 + @"Focused");
                fgSpriteSet0.hovered = (fgPrefix0 + @"Hovered");
                fgSpriteSet0.pressed = (fgPrefix0 + @"Pressed");
                fgSpriteSet0.disabled = (fgPrefix0 + @"Disabled");
            }
            if (bgPrefix0 != @"") {
                bgSpriteSet0.normal = (bgPrefix0 + @"");
                bgSpriteSet0.focused = (bgPrefix0 + @"Focused");
                bgSpriteSet0.hovered = (bgPrefix0 + @"Hovered");
                bgSpriteSet0.pressed = (bgPrefix0 + @"Pressed");
                bgSpriteSet0.disabled = (bgPrefix0 + @"Disabled");
            }
            fgSpriteSetState.AddState();
            bgSpriteSetState.AddState();
            UIMultiStateButton.SpriteSet fgSpriteSet1 = fgSpriteSetState[1];
            UIMultiStateButton.SpriteSet bgSpriteSet1 = bgSpriteSetState[1];
            if (fgPrefix1 != @"") {
                fgSpriteSet1.normal = (fgPrefix1 + @"");
                fgSpriteSet1.focused = (fgPrefix1 + @"Focused");
                fgSpriteSet1.hovered = (fgPrefix1 + @"Hovered");
                fgSpriteSet1.pressed = (fgPrefix1 + @"Pressed");
                fgSpriteSet1.disabled = (fgPrefix1 + @"Disabled");
            }
            if (bgPrefix1 != @"") {
                bgSpriteSet1.normal = (bgPrefix1 + @"");
                bgSpriteSet1.focused = (bgPrefix1 + @"Focused");
                bgSpriteSet1.hovered = (bgPrefix1 + @"Hovered");
                bgSpriteSet1.pressed = (bgPrefix1 + @"Pressed");
                bgSpriteSet1.disabled = (bgPrefix1 + @"Disabled");
            }
            toggleBtn.height = PLT_TOOLBAR_BTNSIZE;
            toggleBtn.width = PLT_TOOLBAR_BTNSIZE;
            toggleBtn.playAudioEvents = true;
            toggleBtn.state = UIMultiStateButton.ButtonState.Normal;
            toggleBtn.activeStateIndex = 0;
            toggleBtn.foregroundSpriteMode = UIForegroundSpriteMode.Scale;
            toggleBtn.spritePadding = new RectOffset(0, 0, 0, 0);
            toggleBtn.autoSize = false;
            toggleBtn.canFocus = false;
            toggleBtn.enabled = true;
            toggleBtn.isInteractive = true;
            toggleBtn.isVisible = true;
            return toggleBtn;
        }
    }
}
