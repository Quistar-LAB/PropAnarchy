using ColossalFramework;
using ColossalFramework.UI;
using EManagersLib;
using System.Collections;
using UnityEngine;

namespace PropAnarchy.PLT {
    internal sealed class ToolBar : UIPanel {
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
        private delegate PropTool GetPropToolAPI();
        private delegate TreeTool GetTreeToolAPI();
        private delegate PropLineTool GetPropLineToolAPI();
        private delegate BulldozeTool GetBulldozeToolAPI();
        private static UIPanel m_brushPanel;
        internal static UITextureAtlas m_sharedTextures;
        internal static PropertyChangedEventHandler<int> SetCurrentMode;

        public override void Awake() {
            base.Awake();
            atlas = m_sharedTextures;
            size = new Vector2(PLT_TOOLBAR_WIDTH, PLT_TOOLBAR_HEIGHT);
            UIMultiStateButton fenceModeToggleBtn = AddToggleBtn(this, @"PLTToggleFenceMode", atlas, @"PLT_MultiStateZero", @"PLT_MultiStateOne", @"PLT_FenceModeZero", @"PLT_FenceModeOne");
            fenceModeToggleBtn.relativePosition = new Vector3(0, 0);
            fenceModeToggleBtn.tooltip = PALocale.GetLocale(@"PLTToggleFenceMode");
            fenceModeToggleBtn.isVisible = false;
            fenceModeToggleBtn.eventActiveStateIndexChanged += (c, index) => {
                PropLineTool.ItemInfo.FenceMode = index != 0;
            };
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
            singleDefaultBtn.tooltip = PALocale.GetLocale(@"PLTToggleSingleMode");
            UIButton straightBtn = AddButton(controlTabStrip, buttonTemplate, PLT_STRAIGHT_NAME);
            straightBtn.tooltip = PALocale.GetLocale(@"PLTToggleStraightMode");
            UIButton curveBtn = AddButton(controlTabStrip, buttonTemplate, PLT_CURVED_NAME);
            curveBtn.tooltip = PALocale.GetLocale(@"PLTToggleCurveMode");
            UIButton freeformBtn = AddButton(controlTabStrip, buttonTemplate, PLT_FREEFORM_NAME);
            freeformBtn.tooltip = PALocale.GetLocale(@"PLTToggleFreeformMode");
            UIButton circleBtn = AddButton(controlTabStrip, buttonTemplate, PLT_CIRCLE_NAME, @"○", 3.0f);
            circleBtn.tooltip = PALocale.GetLocale(@"PLTToggleCircleMode");
            circleBtn.textPadding.left = -2;
            circleBtn.textPadding.right = 1;
            circleBtn.textPadding.top = -13;
            circleBtn.textPadding.bottom = 0;
            controlTabStrip.selectedIndex = DrawMode.Single;
            controlTabStrip.startSelectedIndex = DrawMode.Single;
            SetCurrentMode = (c, val) => controlTabStrip.selectedIndex = val;
            UIMultiStateButton controlPanelToggleBtn = AddToggleBtn(this, @"PLTToggleControlPanel", atlas, @"PLT_ToggleCPZero", @"PLT_ToggleCPOne", @"", @"");
            controlPanelToggleBtn.relativePosition = new Vector3(PLT_TOOLBAR_BTNSIZE * 6f, 0f);
            controlPanelToggleBtn.tooltip = PALocale.GetLocale(@"PLTToggleControlPanel");
            controlPanelToggleBtn.isVisible = false;
            controlPanelToggleBtn.eventActiveStateIndexChanged += (c, index) => {
                if (index != 0) OptionPanel.Open(PropLineTool.ItemInfo.Type);
                else OptionPanel.Close();
            };
            controlTabStrip.eventSelectedIndexChanged += (c, index) => {
                DrawMode.CurrentMode = index;
                UIPanel brushPanel = m_brushPanel;
                ToolBase currentTool = ToolsModifierControl.toolController.CurrentTool;
                if (index == DrawMode.Single) {
                    if (currentTool is TreeTool || currentTool is PropTool) {
                        controlPanelToggleBtn.activeStateIndex = 0;
                        if (brushPanel && !brushPanel.isVisible) brushPanel.Show();
                        fenceModeToggleBtn.isVisible = false;
                        controlPanelToggleBtn.isVisible = false;
                    }
                } else {
                    if (brushPanel && brushPanel.isVisible) brushPanel.Hide();
                    fenceModeToggleBtn.isVisible = true;
                    controlPanelToggleBtn.isVisible = true;
                    if (currentTool is PropTool propTool) {
                        PropLineTool.ItemInfo.Prefab = propTool.m_prefab;
                        ToolsModifierControl.SetTool<PropLineTool>();
                    } else if (currentTool is TreeTool treeTool) {
                        PropLineTool.ItemInfo.Prefab = treeTool.m_prefab;
                        ToolsModifierControl.SetTool<PropLineTool>();
                    }
                }
            };
            isVisible = false;
            eventVisibilityChanged += (c, visible) => {
                ToolBase currentTool = ToolsModifierControl.toolController.CurrentTool;
                if (visible) {
                    if (currentTool is TreeTool treeTool) {
                        PropLineTool.ItemInfo.Prefab = treeTool.m_prefab;
                    } else if (currentTool is PropTool propTool) {
                        PropLineTool.ItemInfo.Prefab = propTool.m_prefab;
                    }
                } else {
                    controlPanelToggleBtn.activeStateIndex = 0;
                    PropLineTool.ResetPLT();
                }
            };
        }

        public override void Start() {
            GameObject brushGO = GameObject.Find(@"BrushPanel");
            if (brushGO) {
                UIPanel brushPanel = brushGO.GetComponent<UIPanel>();
                brushPanel.eventVisibilityChanged += (c, visible) => {
                    if (visible && isVisible && DrawMode.CurrentMode != DrawMode.Single) {
                        brushPanel.Hide();
                    }
                };
                m_brushPanel = brushPanel;
            }
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
            StartCoroutine(PollToolState(this));
        }

        public override void OnDestroy() {
            StopCoroutine(PollToolState(null));
        }

        internal static IEnumerator PollToolState(ToolBar toolBar) {
            PropTool propTool;
            TreeTool treeTool;
            GetPropToolAPI getPropTool = ToolsModifierControl.GetCurrentTool<PropTool>;
            GetTreeToolAPI getTreeTool = ToolsModifierControl.GetCurrentTool<TreeTool>;
            GetPropLineToolAPI getPropLineTool = ToolsModifierControl.GetCurrentTool<PropLineTool>;
            UIPanel brushPanel = m_brushPanel;
            WaitForSeconds wait = new WaitForSeconds(0.2f);
            //WaitForEndOfFrame wait = new WaitForEndOfFrame();
            while (true) {
                yield return wait;
                propTool = getPropTool();
                treeTool = getTreeTool();
                PropLineTool propLineTool = getPropLineTool();
                if (propTool is null && treeTool is null && propLineTool is null) {
                    if (toolBar.isVisibleSelf) {
                        toolBar.Hide();
                        OptionPanel.Close();
                    }
                } else {
                    if (!toolBar.isVisibleSelf) toolBar.Show();
                    switch (DrawMode.CurrentMode) {
                    case DrawMode.Straight:
                    case DrawMode.Circle:
                    case DrawMode.Freeform:
                    case DrawMode.Curved:
                        if (propLineTool is null && (propTool || treeTool)) {
                            if (brushPanel && brushPanel.isVisibleSelf) {
                                brushPanel.Hide();
                            }
                            if (propTool) {
                                PropLineTool.ItemInfo.Prefab = propTool.m_prefab;
                            } else if (treeTool) {
                                PropLineTool.ItemInfo.Prefab = treeTool.m_prefab;
                            }
                            ToolsModifierControl.SetTool<PropLineTool>();
                        }
                        break;
                    }
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
