using ColossalFramework;
using ColossalFramework.UI;
using HarmonyLib;
using System.Threading;
using UnityEngine;

namespace PropAnarchy.PLT {
    public class ToolBar : UIPanel {
        private const string PLTHarmonyID = @"PropAnarchyPLT";
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
        private UIMultiStateButton m_fenceModeToggleBtn;
        public UIMultiStateButton m_controlPanelToggleBtn;
        private static UIPanel m_brushPanel;

        public override void Awake() {
            base.Awake();
            PALocale locale = SingletonLite<PALocale>.instance;
            size = new Vector2(PLT_TOOLBAR_WIDTH, PLT_TOOLBAR_HEIGHT);
            atlas = PropLineTool.m_sharedTextures;
            m_fenceModeToggleBtn = AddToggleBtn(this, @"PLTToggleFenceMode", PropLineTool.m_sharedTextures, @"PLT_MultiStateZero", @"PLT_MultiStateOne", @"PLT_FenceModeZero", @"PLT_FenceModeOne");
            m_fenceModeToggleBtn.relativePosition = new Vector3(0, 0);
            m_fenceModeToggleBtn.tooltip = locale.GetLocale(@"PLTToggleFenceMode");
            m_fenceModeToggleBtn.eventActiveStateIndexChanged += (c, index) => {
                PropLineTool.m_fenceMode = index != 0;
            };
            UITabstrip tabStrip = AddUIComponent<UITabstrip>();
            tabStrip.relativePosition = new Vector3(PLT_TOOLBAR_BTNSIZE, 0f);
            tabStrip.width = 180f;
            tabStrip.height = PLT_TOOLBAR_HEIGHT;
            tabStrip.padding.right = 0;
            UIButton buttonTemplate = GameObject.Find(@"ToolMode").GetComponent<UITabstrip>().GetComponentInChildren<UIButton>();
            UIButton singleDefaultBtn = AddButton(tabStrip, buttonTemplate, PLT_SINGLEDEFAULT_NAME, @"•", 1.5f);
            singleDefaultBtn.textPadding.left = 0;
            singleDefaultBtn.textPadding.right = 1;
            singleDefaultBtn.textPadding.top = 4;
            singleDefaultBtn.textPadding.bottom = 0;
            _ = AddButton(tabStrip, buttonTemplate, PLT_STRAIGHT_NAME);
            _ = AddButton(tabStrip, buttonTemplate, PLT_CURVED_NAME);
            _ = AddButton(tabStrip, buttonTemplate, PLT_FREEFORM_NAME);
            UIButton circleBtn = AddButton(tabStrip, buttonTemplate, PLT_CIRCLE_NAME, @"○", 3.0f);
            circleBtn.textPadding.left = -2;
            circleBtn.textPadding.right = 1;
            circleBtn.textPadding.top = -13;
            circleBtn.textPadding.bottom = 0;

            tabStrip.selectedIndex = (int)PropLineTool.m_drawMode;
            tabStrip.eventSelectedIndexChanged += (c, index) => {
                PropLineTool.m_drawMode = (PropLineTool.PLTDrawMode)index;
                switch ((PropLineTool.PLTDrawMode)index) {
                case PropLineTool.PLTDrawMode.Single:
                    m_controlPanelToggleBtn.activeStateIndex = 0;
                    if (!m_brushPanel.isVisible) m_brushPanel.Show();
                    m_fenceModeToggleBtn.isVisible = false;
                    m_controlPanelToggleBtn.isVisible = false;
                    break;
                default:
                    if (m_brushPanel.isVisible) m_brushPanel.Hide();
                    m_fenceModeToggleBtn.isVisible = true;
                    m_controlPanelToggleBtn.isVisible = true;
                    break;
                }
            };
            m_fenceModeToggleBtn.isVisible = false;
            tabStrip.startSelectedIndex = 0;

            m_controlPanelToggleBtn = AddToggleBtn(this, @"PLTToggleControlPanel", atlas, @"PLT_ToggleCPZero", @"PLT_ToggleCPOne", @"", @"");
            m_controlPanelToggleBtn.relativePosition = new Vector3(PLT_TOOLBAR_BTNSIZE * 6f, 0f);
            m_controlPanelToggleBtn.tooltip = locale.GetLocale(@"PLTToggleControlPanel");
            m_controlPanelToggleBtn.isVisible = false;
            m_controlPanelToggleBtn.eventActiveStateIndexChanged += (c, index) => {
                if (index != 0) OptionPanel.Open(PropLineTool.m_objectMode);
                else OptionPanel.Close();
            };
            LandscapingGroupPanel landscapingPanel = UIView.GetAView().GetComponentInChildren<LandscapingGroupPanel>();
            UITabstrip m_landscapingStrip = AccessTools.Field(typeof(LandscapingGroupPanel), @"m_Strip").GetValue(landscapingPanel) as UITabstrip;
            m_landscapingStrip.eventSelectedIndexChanged += (c, index) => {
                switch (index) {
                case 3: /* TreeTool */
                    PropLineTool.m_objectMode = PropLineTool.PLTObjectMode.Trees;
                    OptionPanel.ToggleAnglePanel(PropLineTool.m_objectMode);
                    break;
                case 4: /* PropTool */
                    PropLineTool.m_objectMode = PropLineTool.PLTObjectMode.Props;
                    OptionPanel.ToggleAnglePanel(PropLineTool.m_objectMode);
                    break;
                default:
                    PropLineTool.m_objectMode = PropLineTool.PLTObjectMode.Undefined;
                    break;
                }
            };
            Harmony harmony = new Harmony(PLTHarmonyID);
            harmony.Patch(AccessTools.Method(typeof(ToolController), @"SetTool"),
                postfix: new HarmonyMethod(AccessTools.Method(typeof(ToolBar), nameof(SetToolPostfix))));
            harmony.Patch(AccessTools.Method(typeof(BrushOptionPanel), nameof(BrushOptionPanel.Show)),
                prefix: new HarmonyMethod(AccessTools.Method(typeof(ToolBar), nameof(ShowBrushPrefix))));
        }

        public override void Start() {
            UIComponent optionsBar = GameObject.Find(@"OptionsBar").GetComponent<UIComponent>();
            if (optionsBar is null) {
                absolutePosition = new Vector3(261f, 542f);
            } else {
                absolutePosition = optionsBar.absolutePosition;
                float widthDifference = width - optionsBar.width;
                if (widthDifference != 0f) {
                    float absX = absolutePosition.x;
                    float absY = absolutePosition.y;
                    float newX = Mathf.RoundToInt(absX - (widthDifference / 2));
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
            GameObject brushPanel = GameObject.Find(@"BrushPanel");
            if (!(brushPanel is null)) {
                m_brushPanel = brushPanel.GetComponent<UIPanel>();
            }
        }

        public override void OnDestroy() {
            base.OnDestroy();
            new Harmony(PLTHarmonyID).Unpatch(AccessTools.Method(typeof(ToolController), @"SetTool"), HarmonyPatchType.Postfix, PLTHarmonyID);
        }

        private static void PollBrushPanelVisibility(object _) {
            int elapsed = 0;
            UIPanel brushPanel = m_brushPanel;
            while (!brushPanel.isVisible && elapsed < 250) {
                Thread.Sleep(2);
            }
            brushPanel.Hide();
        }

        private static void SetToolPostfix(ToolBase tool) {
            if (tool is TreeTool || tool is PropTool) {
                if (PropLineTool.m_drawMode == PropLineTool.PLTDrawMode.Single) {
                    if (!m_brushPanel.isVisible) m_brushPanel.Show();
                } else {
                    ThreadPool.QueueUserWorkItem(PollBrushPanelVisibility);
                }
                PropLineTool.m_toolBar.isVisible = true;
                PropLineTool.m_objectMode = tool is TreeTool ? PropLineTool.PLTObjectMode.Trees : PropLineTool.PLTObjectMode.Props;
                return;
            }
            if (PropLineTool.m_toolBar.m_IsVisible) {
                PropLineTool.m_toolBar.isVisible = false;
                PropLineTool.m_toolBar.m_controlPanelToggleBtn.activeStateIndex = 0;
                OptionPanel.Close();

            }
        }

        private static bool ShowBrushPrefix() {
            if(PropLineTool.m_drawMode == PropLineTool.PLTDrawMode.Single) {
                return true;
            }
            return false;
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
