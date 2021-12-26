using ColossalFramework.Globalization;
using ColossalFramework.UI;
using System.Globalization;
using UnityEngine;

namespace PropAnarchy.PLT {
    internal sealed class OptionPanel : UIPanel {
        private const float OPTIONPANEL_WIDTH = 374f;
        private const float OPTIONPANEL_HEIGHT = 420f;
        private const float TITLEBAR_HEIGHT = 42f;
        private const float TABSTRIP_HEIGHT = 32f;
        private const float PADDING_TABSTRIP_SIDES = 7f;
        private const float PADDING_PANEL = 10f;
        private const int TAB_PADDING_HORIZONTAL = 10;
        private const int TAB_PADDING_VERTICAL = 8;
        internal delegate void EventOptionPanelHandler(PropLineTool.ItemType mode);
#pragma warning disable IDE1006
        internal static event EventOptionPanelHandler eventOnOptionClose;
        internal static event EventOptionPanelHandler eventOnOptionOpen;
        internal static event EventOptionPanelHandler eventOnAnglePanelToggle;
#pragma warning restore IDE1006

        public override void Awake() {
            base.Awake();
            atlas = PAUtils.GetAtlas(@"Ingame");
            backgroundSprite = @"MenuPanel2";
            size = new Vector2(OPTIONPANEL_WIDTH, OPTIONPANEL_HEIGHT);
            UIDragHandle titleBar = AddUIComponent<UIDragHandle>();
            titleBar.width = width;
            titleBar.height = TITLEBAR_HEIGHT;
            titleBar.target = this;
            titleBar.relativePosition = new Vector3(0f, 0f);
            UITabstrip tabstrip = titleBar.AddUIComponent<UITabstrip>();
            tabstrip.width = width - (2f * PADDING_TABSTRIP_SIDES);
            tabstrip.height = TABSTRIP_HEIGHT;
            tabstrip.padding = new RectOffset(0, 0, 0, 0);
            tabstrip.relativePosition = new Vector3(PADDING_TABSTRIP_SIDES, PADDING_PANEL);
            UITabContainer tabContainer = AddUIComponent<UITabContainer>();
            tabContainer.size = new Vector2(OPTIONPANEL_WIDTH, OPTIONPANEL_HEIGHT - TITLEBAR_HEIGHT);
            tabContainer.relativePosition = new Vector3(0f, TITLEBAR_HEIGHT);
            tabstrip.tabPages = tabContainer;
            UIPanel paramsPanel = AddTab(this, tabstrip, PALocale.GetLocale(@"PLTParameters"), 0);
            PopulateParams(paramsPanel);
            UIPanel optionsPanel = AddTab(this, tabstrip, PALocale.GetLocale(@"PLTOptions"), 1);
            PopulateOptions(optionsPanel);
            titleBar.relativePosition = new Vector3(0f, 0f);
            tabstrip.relativePosition = new Vector3(PADDING_TABSTRIP_SIDES, PADDING_PANEL);
            tabstrip.tabPages.relativePosition = new Vector3(0f, TITLEBAR_HEIGHT);
            UILabel mainTitleBar = CreateLabel(this, PALocale.GetLocale(@"PLTTitleBar"), 0.85f, UIHorizontalAlignment.Right, new RectOffset(2, 7, 2, 5), new Vector2(180f, 30f), new Vector3(187f, 7f));
            mainTitleBar.textColor = new Color32(164, 164, 164, 255);
            mainTitleBar.disabledTextColor = new Color32(82, 82, 82, 255);
            mainTitleBar.relativePosition = new Vector3(187f, 7f);
            paramsPanel.isVisible = true;
            titleBar.BringToFront();
            eventOnOptionClose += (mode) => {
                Hide();
                tabstrip.selectedIndex = 0;
            };
            eventOnOptionOpen += (mode) => Show();
        }

        public override void Start() {
            base.Start();
            absolutePosition = new Vector3(Mathf.Floor(GetUIView().GetScreenResolution().x - width - 50f), Mathf.Floor(GetUIView().GetScreenResolution().y - height - 300f));
            Hide();
        }

        public static void Open(PropLineTool.ItemType mode) => eventOnOptionOpen?.Invoke(mode);

        public static void Close() => eventOnOptionClose?.Invoke(PropLineTool.ItemType.Undefined);

        public static void ToggleAnglePanel(PropLineTool.ItemType mode) => eventOnAnglePanelToggle?.Invoke(mode);

        private static void PopulateParams(UIPanel parent) {
            const float SPACING_OFFSETY = 83f;
            const float ANGLE_OFFSETY = 230f;
            const float BUTTON_WIDTH = 80f;
            const float BUTTON_HEIGHT = 30f;
            const float TABSTRIP_WIDTH = 175f;
            const float TABSTRIP_HEIGHT = 30f;
            const float TAB_HORIZONTAL_SPACING = 5f;
            const float VERTICAL_PADDING = 15f;
            const int TAB_PADDING_LEFT = 0;
            const int TAB_PADDING_RIGHT = 5;
            parent.autoLayout = false;
            UITabstrip controlMode = parent.AddUIComponent<UITabstrip>();
            controlMode.relativePosition = new Vector2(62f, 7f);
            controlMode.size = new Vector2(TABSTRIP_WIDTH, TABSTRIP_HEIGHT);
            controlMode.padding.left = TAB_PADDING_LEFT;
            controlMode.padding.right = TAB_PADDING_RIGHT;
            UIButton itemwiseBtn = controlMode.AddTab();
            itemwiseBtn.size = new Vector2(BUTTON_WIDTH, BUTTON_HEIGHT);
            itemwiseBtn.relativePosition = Vector2.zero;
            SetButtonSprite(itemwiseBtn, @"", @"PLT_ItemwiseZero");
            UIButton spacingBtn = controlMode.AddTab("Spacing", itemwiseBtn, false);
            spacingBtn.size = new Vector2(BUTTON_WIDTH, BUTTON_HEIGHT);
            spacingBtn.relativePosition = new Vector2(BUTTON_WIDTH + TAB_HORIZONTAL_SPACING, BUTTON_HEIGHT);
            SetButtonSprite(spacingBtn, @"", @"PLT_SpacingwiseZero");
            itemwiseBtn.focusedBgSprite = @"PLT_ItemwiseOneFocused";
            itemwiseBtn.tooltip = PALocale.GetLocale(@"PLTItemWiseTooltip");
            spacingBtn.focusedBgSprite = @"PLT_SpacingwiseOneFocused";
            spacingBtn.tooltip = PALocale.GetLocale(@"PLTSpacingTooltip");
            controlMode.selectedIndex = controlMode.startSelectedIndex = (int)Settings.ControlMode;
            controlMode.eventSelectedIndexChanged += (c, index) => {
                Settings.ControlMode = (PropLineTool.ControlMode)index;
            };
            CreateBlueBtn(parent, PALocale.GetLocale(@"PLTDecouplePrevSegment"), 0.8f, new Vector2(250f, 24f), new Vector3(62f, 45f)).eventClick += (c, p) => {
                SegmentState.ResetLastContinueParameters();
            };
            _ = CreateDivider(parent, new Vector3(12f, SPACING_OFFSETY - 8f));
            _ = CreateDivider(parent, new Vector3(12f, ANGLE_OFFSETY - 8f));
            UILabel spacingLabel = CreateLabel(parent, PALocale.GetLocale(@"PLTSpacingTitle"), 1.25f, UIHorizontalAlignment.Left, new RectOffset(5, 5, 5, 5), Vector2.zero, new Vector3(12f, SPACING_OFFSETY + 2f));
            UIPLTCheckbox autoDefaultCB = parent.AddUIComponent<UIPLTCheckbox>();
            autoDefaultCB.text = PALocale.GetLocale(@"PLTAutoDefaultSpacing");
            autoDefaultCB.tooltip = PALocale.GetLocale(@"PLTAutoDefaultSpacingTooltip");
            autoDefaultCB.isChecked = Settings.AutoDefaultSpacing;
            autoDefaultCB.relativePosition = new Vector3(parent.width - autoDefaultCB.width - 20f, (spacingLabel.height - autoDefaultCB.height) / 2f + SPACING_OFFSETY + 2f);
            autoDefaultCB.m_checkbox.eventActiveStateIndexChanged += (c, index) => {
                if (index != 0) {
                    Settings.AutoDefaultSpacing = true;
                    PropLineTool.ItemInfo.SetDefaultSpacing();
                } else {
                    Settings.AutoDefaultSpacing = false;
                }
                DrawMode.CurActiveMode.UpdatePlacement();
            };
            PropLineTool.SetAutoSpacing = (state) => autoDefaultCB.isChecked = state;
            UINumEditbox spacingField = CreateNumboxField(parent, PALocale.GetLocale(@"PLTSpacingTitle"), @"m", out _);
            spacingField.parent.relativePosition = new Vector2(parent.width - spacingField.parent.width - 20f, SPACING_OFFSETY + spacingLabel.height + 5f);
            spacingField.Value = 1f;// PropLineTool.ItemInfo.Spacing;
            spacingField.eventValueChanged += (c, value) => SegmentState.m_pendingPlacementUpdate = true;
            PropLineTool.SetSpacingValue = (value) => spacingField.Value = value;
            PropLineTool.GetSpacingValue = () => spacingField.Value;
            UIBasicSpacingCalculator spacingCalculator = parent.AddUIComponent<UIBasicSpacingCalculator>();
            spacingCalculator.relativePosition = new Vector3(26f, SPACING_OFFSETY + spacingLabel.height + spacingField.height + VERTICAL_PADDING);
            UIPanel anglePanel = parent.AddUIComponent<UIPanel>();
            anglePanel.size = new Vector2(parent.width, parent.height - ANGLE_OFFSETY);
            anglePanel.relativePosition = new Vector2(0f, ANGLE_OFFSETY);
            UILabel angleLabel = CreateLabel(anglePanel, PALocale.GetLocale(@"PLTAngleTitle"), 1.25f, UIHorizontalAlignment.Left, new RectOffset(5, 5, 5, 5), Vector2.zero, new Vector3(12f, 2f));
            UITabstrip angleMode = anglePanel.AddUIComponent<UITabstrip>();
            UIButton angleDynamicBtn = angleMode.AddTab();
            RectOffset btnPadding = new RectOffset(8, 5, 5, 3);
            angleDynamicBtn.autoSize = true;
            angleDynamicBtn.textPadding = btnPadding;
            angleDynamicBtn.textVerticalAlignment = UIVerticalAlignment.Middle;
            angleDynamicBtn.textHorizontalAlignment = UIHorizontalAlignment.Center;
            angleDynamicBtn.textScale = 0.875f;
            angleDynamicBtn.text = PALocale.GetLocale(@"PLTAngleDynamic");
            angleDynamicBtn.normalBgSprite = @"SubBarButtonBase";
            angleDynamicBtn.disabledBgSprite = @"SubBarButtonBaseDisabled";
            angleDynamicBtn.focusedBgSprite = @"SubBarButtonBaseFocused";
            angleDynamicBtn.hoveredBgSprite = @"SubBarButtonBaseHovered";
            angleDynamicBtn.pressedBgSprite = @"SubBarButtonBasePressed";
            UIButton angleSingleBtn = angleMode.AddTab(PALocale.GetLocale(@"PLTAngleSingle"), angleDynamicBtn, false);
            angleSingleBtn.autoSize = false;
            angleSingleBtn.textPadding = btnPadding;
            angleSingleBtn.textVerticalAlignment = UIVerticalAlignment.Middle;
            angleSingleBtn.textHorizontalAlignment = UIHorizontalAlignment.Center;
            angleSingleBtn.textScale = 0.875f;
            angleSingleBtn.text = PALocale.GetLocale(@"PLTAngleSingle");
            angleSingleBtn.size = angleDynamicBtn.size;
            angleSingleBtn.normalBgSprite = @"SubBarButtonBase";
            angleSingleBtn.disabledBgSprite = @"SubBarButtonBaseDisabled";
            angleSingleBtn.focusedBgSprite = @"SubBarButtonBaseFocused";
            angleSingleBtn.hoveredBgSprite = @"SubBarButtonBaseHovered";
            angleSingleBtn.pressedBgSprite = @"SubBarButtonBasePressed";
            angleMode.startSelectedIndex = (int)Settings.AngleMode;
            angleMode.selectedIndex = (int)Settings.AngleMode;
            angleMode.size = new Vector2(angleDynamicBtn.width + angleSingleBtn.width, angleDynamicBtn.height);
            angleMode.relativePosition = new Vector2(anglePanel.width - angleMode.width - 20f, angleLabel.relativePosition.y + (angleLabel.height - angleMode.height) / 2f);
            PropLineTool.SetAngleModeState = (state) => {
                if (state) angleMode.Enable();
                else angleMode.Disable();
            };
            angleDynamicBtn.relativePosition = Vector2.zero;
            angleSingleBtn.relativePosition = new Vector2(angleDynamicBtn.width, 0f);
            UIPLTCheckbox flip180CB = anglePanel.AddUIComponent<UIPLTCheckbox>();
            flip180CB.text = PALocale.GetLocale(@"PLTFlip180");
            flip180CB.tooltip = PALocale.GetLocale(@"PLTFlip180Tooltip");
            flip180CB.isChecked = Settings.AngleFlip180;
            flip180CB.relativePosition = new Vector2(angleLabel.relativePosition.x + 5f, angleLabel.height + 10f);
            flip180CB.m_checkbox.eventActiveStateIndexChanged += (c, index) => {
                Settings.AngleFlip180 = index != 0;
                DrawMode.CurActiveMode.UpdatePlacement();
            };
            UINumEditbox angleField = CreateNumboxField(anglePanel, PALocale.GetLocale(@"PLTRelativeAngle"), angleMode.selectedIndex == 0 ? @"Δ°" : @"°", out UILabel angleUnit);
            angleField.parent.relativePosition = new Vector2(anglePanel.width - angleField.parent.width - 20f, angleLabel.height + 5f);
            angleField.Value = 0f;
            angleField.eventValueChanged += (c, value) => SegmentState.m_pendingPlacementUpdate = true;
            PropLineTool.SetAngleMode = (val) => angleMode.selectedIndex = val;
            PropLineTool.GetAngleValue = () => angleField.Value;
            PropLineTool.SetAngleValue = (val) => angleField.Value = val;
            angleMode.eventSelectedIndexChanged += (c, index) => {
                Settings.AngleMode = (PropLineTool.AngleMode)index;
                angleUnit.text = index == 0 ? @"Δ°" : @"°";
                SegmentState.m_pendingPlacementUpdate = true;
            };
            UIBasicAngleCalculator angleCalculator = anglePanel.AddUIComponent<UIBasicAngleCalculator>();
            angleCalculator.relativePosition = new Vector2(26f, angleLabel.height + angleField.height + VERTICAL_PADDING);
            eventOnAnglePanelToggle += (mode) => {
                switch (mode) {
                case PropLineTool.ItemType.Prop: anglePanel.isVisible = true; break;
                case PropLineTool.ItemType.Tree: anglePanel.isVisible = false; break;
                }
            };
            eventOnOptionOpen += eventOnAnglePanelToggle;
        }

        private static void PopulateOptions(UIPanel parent) {
            const float OFFSETX = 24f;
            const float STARTY = 15f;
            const float PADDINGY = 10f;
            const float DIVIDERHEIGHT = 5f;
            parent.autoLayout = false;
            UIPLTCheckbox showUndoPreviewCB = parent.AddUIComponent<UIPLTCheckbox>();
            showUndoPreviewCB.text = PALocale.GetLocale(@"PLTShowUndoPreview");
            showUndoPreviewCB.tooltip = PALocale.GetLocale(@"PLTShowUndoPreviewTooltip");
            showUndoPreviewCB.isChecked = Settings.ShowUndoPreviews;
            showUndoPreviewCB.relativePosition = new Vector3(OFFSETX, STARTY);
            showUndoPreviewCB.m_checkbox.eventActiveStateIndexChanged += (c, index) => Settings.ShowUndoPreviews = index != 0;
            _ = CreateDivider(parent, new Vector3(12f, showUndoPreviewCB.relativePosition.y + showUndoPreviewCB.height + PADDINGY));
            UIPLTCheckbox meshCenterCorrectionCB = parent.AddUIComponent<UIPLTCheckbox>();
            meshCenterCorrectionCB.text = PALocale.GetLocale(@"PLTMeshCenterCorrection");
            meshCenterCorrectionCB.tooltip = PALocale.GetLocale(@"PLTMeshCenterCorrectionTooltip");
            meshCenterCorrectionCB.isChecked = Settings.UseMeshCenterCorrection;
            meshCenterCorrectionCB.relativePosition = new Vector3(OFFSETX, showUndoPreviewCB.relativePosition.y + showUndoPreviewCB.height + DIVIDERHEIGHT * 2f + PADDINGY);
            meshCenterCorrectionCB.m_checkbox.eventActiveStateIndexChanged += (c, index) => Settings.UseMeshCenterCorrection = index != 0;
            UIPLTCheckbox perfectCircleCB = parent.AddUIComponent<UIPLTCheckbox>();
            perfectCircleCB.text = PALocale.GetLocale(@"PLTPerfectCircle");
            perfectCircleCB.tooltip = PALocale.GetLocale(@"PLTPerfectCircleTooltip");
            perfectCircleCB.isChecked = Settings.PerfectCircles;
            perfectCircleCB.relativePosition = new Vector3(OFFSETX, meshCenterCorrectionCB.relativePosition.y + meshCenterCorrectionCB.height + PADDINGY);
            perfectCircleCB.m_checkbox.eventActiveStateIndexChanged += (c, index) => Settings.PerfectCircles = index != 0;
            UIPLTCheckbox linearFenceFillCB = parent.AddUIComponent<UIPLTCheckbox>();
            linearFenceFillCB.text = PALocale.GetLocale(@"PLTLinearFence");
            linearFenceFillCB.tooltip = PALocale.GetLocale(@"PLTLinearFenceTooltip");
            linearFenceFillCB.isChecked = Settings.LinearFenceFill;
            linearFenceFillCB.relativePosition = new Vector3(OFFSETX, perfectCircleCB.relativePosition.y + perfectCircleCB.height + PADDINGY);
            linearFenceFillCB.m_checkbox.eventActiveStateIndexChanged += (c, index) => Settings.LinearFenceFill = index != 0;
        }

        private static void SetButtonSprite(UIButton button, string fgSpriteName, string bgSpriteName) {
            button.autoSize = false;
            button.playAudioEvents = true;
            button.atlas = ToolBar.m_sharedTextures;
            button.normalBgSprite = bgSpriteName;
            button.focusedBgSprite = bgSpriteName + @"Focused";
            button.hoveredBgSprite = bgSpriteName + @"Hovered";
            button.pressedBgSprite = bgSpriteName + @"Pressed";
            button.disabledBgSprite = bgSpriteName + @"Disabled";
            button.normalFgSprite = fgSpriteName;
            button.focusedFgSprite = fgSpriteName + @"Focused";
            button.hoveredFgSprite = fgSpriteName + @"Hovered";
            button.pressedFgSprite = fgSpriteName + @"Pressed";
            button.disabledFgSprite = fgSpriteName + @"Disabled";
        }

        private static UIPanel AddTab(UIPanel parent, UITabstrip tabStrip, string tabName, int tabIndex) {
            UIButton tabButton = tabStrip.AddTab(tabName);
            tabButton.normalBgSprite = @"SubBarButtonBase";
            tabButton.disabledBgSprite = @"SubBarButtonBaseDisabled";
            tabButton.focusedBgSprite = @"SubBarButtonBaseFocused";
            tabButton.hoveredBgSprite = @"SubBarButtonBaseHovered";
            tabButton.pressedBgSprite = @"SubBarButtonBasePressed";
            tabButton.tooltip = tabName;
            tabButton.textPadding = new RectOffset(TAB_PADDING_HORIZONTAL, TAB_PADDING_HORIZONTAL, TAB_PADDING_VERTICAL, TAB_PADDING_VERTICAL);
            tabButton.autoSize = true;
            tabButton.textScale = 0.85f;
            tabButton.playAudioEvents = true;
            tabButton.pressedTextColor = new Color32(255, 255, 255, 255);
            tabButton.focusedTextColor = new Color32(230, 230, 230, 255);
            tabButton.focusedColor = new Color32(205, 220, 255, 255);
            tabButton.disabledTextColor = new Color32(230, 230, 230, 140);
            tabStrip.selectedIndex = tabIndex;
            UIPanel rootPanel = tabStrip.tabContainer.components[tabIndex] as UIPanel;
            rootPanel.atlas = parent.atlas;
            rootPanel.width = parent.width;
            rootPanel.autoLayout = true;
            rootPanel.autoLayoutDirection = LayoutDirection.Vertical;
            rootPanel.autoLayoutPadding = new RectOffset(0, 0, 0, 0);
            rootPanel.isVisible = false;
            return rootPanel;
        }

        public static UILabel CreateLabel(UIComponent parent, string text, float textScale, UIHorizontalAlignment textAlignment, RectOffset padding, Vector2 size, Vector3 relativePosition) {
            UILabel label = parent.AddUIComponent<UILabel>();
            if (size == Vector2.zero) {
                label.autoSize = true;
            } else {
                label.autoSize = false;
                label.size = size;
            }
            label.textScale = textScale;
            label.textAlignment = textAlignment;
            label.verticalAlignment = UIVerticalAlignment.Bottom;
            label.textColor = new Color32(255, 255, 255, 255);
            label.disabledTextColor = new Color32(128, 128, 128, 255);
            label.text = text;
            label.padding = padding;
            label.relativePosition = relativePosition;
            return label;
        }

        private static UIButton CreateBlueBtn(UIPanel parent, string text, float textScale, Vector2 size, Vector3 relativePosition) {
            UIButton button = parent.AddUIComponent<UIButton>();
            button.normalBgSprite = @"ButtonMenu";
            button.focusedBgSprite = @"ButtonMenu";
            button.hoveredBgSprite = @"ButtonMenuHovered";
            button.pressedBgSprite = @"ButtonMenuPressed";
            button.disabledBgSprite = @"ButtonMenuDisabled";
            button.text = text;
            button.textScale = textScale;
            button.textPadding = new RectOffset(6, 6, 2, 0);
            button.textHorizontalAlignment = UIHorizontalAlignment.Center;
            button.textVerticalAlignment = UIVerticalAlignment.Middle;
            button.textColor = new Color32(255, 255, 255, 255);
            button.disabledTextColor = new Color32(255, 255, 255, 128);
            button.color = new Color32(255, 255, 255, 200);
            button.wordWrap = true;
            button.playAudioEvents = true;
            button.size = size;
            button.relativePosition = relativePosition;
            return button;
        }

        private static UITiledSprite CreateDivider(UIPanel parent, Vector3 relativePosition) {
            UITiledSprite divider = parent.AddUIComponent<UITiledSprite>();
            divider.atlas = ToolBar.m_sharedTextures;
            divider.spriteName = @"PLT_BasicDividerTile02x02";
            divider.tileScale = new Vector2(1f, 1f);
            divider.tileOffset = new Vector2(0f, 0f);
            divider.size = new Vector2(350f, 2f);
            divider.relativePosition = relativePosition;
            return divider;
        }

        private static UINumEditbox CreateNumboxField(UIComponent parent, string name, string unitName, out UILabel unitLabel) {
            const float NUMBOXWIDTH = 90f;
            const float NUMBOXHEIGHT = 30f;
            UIPanel container = parent.AddUIComponent<UIPanel>();
            container.autoLayout = false;
            UILabel nameLabel = CreateLabel(container, name, 1f, UIHorizontalAlignment.Right, new RectOffset(2, 6, 2, 4), Vector2.zero, Vector2.zero);
            UINumEditbox numbox = container.AddUIComponent<UINumEditbox>();
            numbox.size = new Vector2(NUMBOXWIDTH, NUMBOXHEIGHT);
            numbox.Value = 1f;
            numbox.DecimalPlaces = 2;
            numbox.relativePosition = new Vector3(nameLabel.width, 0);
            numbox.disabledColor = new Color32(255, 255, 255, 128);
            nameLabel.relativePosition = new Vector2(0f, NUMBOXHEIGHT - nameLabel.height);
            unitLabel = CreateLabel(container, unitName, 1f, UIHorizontalAlignment.Left, new RectOffset(4, 2, 2, 4), Vector2.zero,
                                            new Vector3(nameLabel.width + numbox.width, NUMBOXHEIGHT - nameLabel.height));
            container.size = new Vector2(nameLabel.width + NUMBOXWIDTH + unitLabel.width, NUMBOXHEIGHT);
            return numbox;
        }

        public class UINumEditbox : UITextField {
            private float m_rawValue = 1f;
            private int m_decimalPlaces = 0;
#pragma warning disable IDE1006
            public event PropertyChangedEventHandler<float> eventValueChanged;
#pragma warning restore IDE1006
            public float Value {
                get => m_rawValue;
                set {
                    if (m_rawValue != value) {
                        m_rawValue = value;
                        text = value.ToString(@"F" + m_decimalPlaces, LocaleManager.cultureInfo);
                        eventValueChanged?.Invoke(this, value);
                    }
                }
            }
            public int DecimalPlaces {
                get => m_decimalPlaces;
                set {
                    m_decimalPlaces = Mathf.Clamp(value, 0, 7);
                    text = value.ToString(@"F" + m_decimalPlaces, LocaleManager.cultureInfo);
                }
            }

            public override void Awake() {
                base.Awake();
                numericalOnly = true;
                maxLength = 8;
                allowFloats = true;
                padding = new RectOffset(6, 6, 8, 6);
                builtinKeyNavigation = true;
                isInteractive = true;
                readOnly = false;
                horizontalAlignment = UIHorizontalAlignment.Center;
                selectionSprite = @"EmptySprite";
                selectionBackgroundColor = new Color32(0, 172, 234, 255);
                normalBgSprite = @"TextFieldPanelHovered";
                disabledBgSprite = @"TextFieldPanel";
                textColor = new Color32(0, 0, 0, 255);
                disabledTextColor = new Color32(0, 0, 0, 128);
                color = new Color32(255, 255, 255, 255);
                disabledColor = new Color32(180, 180, 180, 255);
                eventTextSubmitted += (c, text) => {
                    if (float.TryParse(text, NumberStyles.Number, LocaleManager.cultureInfo, out float result)) Value = result;
                };
            }
        }

        public class UIPLTCheckbox : UIComponent {
            public UIMultiStateButton m_checkbox;
            public UILabel m_label;
#pragma warning disable IDE1006 // Naming Styles
            public bool isChecked {
                get => m_checkbox.activeStateIndex != 0;
                set => m_checkbox.activeStateIndex = value ? 1 : 0;
            }
            public string text {
                get => m_label.text;
                set {
                    m_label.text = value;
                    size = new Vector2(m_checkbox.width + m_label.width, m_checkbox.height);
                }
            }
#pragma warning restore IDE1006 // Naming Styles
            public override void Awake() {
                base.Awake();
                m_checkbox = ToolBar.AddToggleBtn(this, @"", ToolBar.m_sharedTextures, @"PLT_MultiStateZero", @"PLT_MultiStateOne", @"", @"");
                m_checkbox.size = new Vector2(20f, 20f);
                m_checkbox.relativePosition = new Vector3(0f, 0f);
                m_label = CreateLabel(this, @"", 1f, UIHorizontalAlignment.Left, new RectOffset(6, 6, 6, 0), Vector2.zero, new Vector3(20f, 0f));
                m_label.verticalAlignment = UIVerticalAlignment.Top;
                m_label.padding = new RectOffset(4, 4, 5, 0);
                m_label.textScale = 0.75f;
                m_label.wordWrap = false;
            }
        }

        public abstract class UICalculator : UIComponent {
            protected const float TEXTSCALE = 0.6875f;
            protected UIPanel m_setPanel;
            protected UIPanel m_adjustPanel;
            public override void Awake() {
                base.Awake();
                m_setPanel = AddUIComponent<UIPanel>();
                m_setPanel.backgroundSprite = @"GenericPanelLight";
                m_setPanel.color = new Color32(90, 100, 105, 255);
                m_adjustPanel = AddUIComponent<UIPanel>();
                m_adjustPanel.backgroundSprite = @"GenericPanelLight";
                m_adjustPanel.color = new Color32(74, 83, 88, 255);
            }

            protected static UIButton AddButton(UIComponent parent, string text, float textScale, Vector2 size, Vector3 relativePosition) {
                UIButton button = parent.AddUIComponent<UIButton>();
                button.canFocus = false;
                button.normalBgSprite = @"ButtonMenu";
                button.focusedBgSprite = @"ButtonMenu"; // @"ButtonMenuFocused";
                button.hoveredBgSprite = @"ButtonMenuHovered";
                button.pressedBgSprite = @"ButtonMenuPressed";
                button.disabledBgSprite = @"ButtonMenuDisabled";
                button.text = text;
                button.textScale = textScale;
                button.textPadding = new RectOffset(1, 1, 3, 0);
                button.textHorizontalAlignment = UIHorizontalAlignment.Center;
                button.textVerticalAlignment = UIVerticalAlignment.Middle;
                button.textColor = new Color32(255, 255, 255, 255);
                button.disabledTextColor = new Color32(255, 255, 255, 128);
                button.wordWrap = true;
                button.playAudioEvents = true;
                button.size = size;
                button.relativePosition = relativePosition;
                return button;
            }
        }

        public class UIBasicSpacingCalculator : UICalculator {
            public override void Awake() {
                base.Awake();
                Vector2 longsize = new Vector2(50f, 14f);
                Vector2 shortSize = new Vector2(25f, 14f);
                m_setPanel.size = new Vector2(112f, 40f);
                m_setPanel.relativePosition = new Vector3(0f, 0f);
                m_adjustPanel.size = new Vector2(203f, 40f);
                m_adjustPanel.relativePosition = new Vector3(120f, 0f);

                AddButton(m_setPanel, PALocale.GetLocale(@"PLTDefault"), TEXTSCALE, new Vector2(50f, 32f), new Vector3(4f, 4f)).eventClick += (c, p) => {
                    if (p.buttons == UIMouseButton.Left) {
                        PropLineTool.ItemInfo.SetDefaultSpacing();
                    }
                };
                AddButton(m_setPanel, PALocale.GetLocale(@"PLTLength"), TEXTSCALE, longsize, new Vector3(58f, 4f)).eventClick += (c, p) => {
                    if (p.buttons == UIMouseButton.Left) {
                        PropLineTool.SetSpacingValue(PropLineTool.ItemInfo.Height);
                    }
                };
                AddButton(m_setPanel, PALocale.GetLocale(@"PLTWidth"), TEXTSCALE, longsize, new Vector3(58f, 22f)).eventClick += (c, p) => {
                    if (p.buttons == UIMouseButton.Left) {
                        PropLineTool.SetSpacingValue(PropLineTool.ItemInfo.Width);
                    }
                };
                AddButton(m_adjustPanel, "+0.1", 0.625f, shortSize, new Vector3(4f, 4f)).eventClick += (c, p) => {
                    if (p.buttons == UIMouseButton.Left) {
                        PropLineTool.ItemInfo.Spacing += 0.1f;
                    }
                };
                AddButton(m_adjustPanel, "-0.1", 0.625f, shortSize, new Vector3(4f, 22f)).eventClick += (c, p) => {
                    if (p.buttons == UIMouseButton.Left) {
                        PropLineTool.ItemInfo.Spacing -= 0.1f;
                    }
                };
                AddButton(m_adjustPanel, "+ 1", TEXTSCALE, shortSize, new Vector3(33f, 4f)).eventClick += (c, p) => {
                    if (p.buttons == UIMouseButton.Left) {
                        PropLineTool.ItemInfo.Spacing += 1f;
                    }
                };
                AddButton(m_adjustPanel, "- 1", TEXTSCALE, shortSize, new Vector3(33f, 22f)).eventClick += (c, p) => {
                    if (p.buttons == UIMouseButton.Left) {
                        PropLineTool.ItemInfo.Spacing -= 1f;
                    }
                };
                AddButton(m_adjustPanel, "+10", TEXTSCALE, shortSize, new Vector3(62f, 4f)).eventClick += (c, p) => {
                    if (p.buttons == UIMouseButton.Left) {
                        PropLineTool.ItemInfo.Spacing += 10f;
                    }
                };
                AddButton(m_adjustPanel, "-10", TEXTSCALE, shortSize, new Vector3(62f, 22f)).eventClick += (c, p) => {
                    if (p.buttons == UIMouseButton.Left) {
                        PropLineTool.ItemInfo.Spacing -= 10f;
                    }
                };
                AddButton(m_adjustPanel, "+100", TEXTSCALE, new Vector2(31f, 14f), new Vector3(91f, 4f)).eventClick += (c, p) => {
                    if (p.buttons == UIMouseButton.Left) {
                        PropLineTool.ItemInfo.Spacing += 100f;
                    }
                };
                AddButton(m_adjustPanel, "-100", TEXTSCALE, new Vector2(31f, 14f), new Vector3(91f, 22f)).eventClick += (c, p) => {
                    if (p.buttons == UIMouseButton.Left) {
                        PropLineTool.ItemInfo.Spacing -= 100f;
                    }
                };
                AddButton(m_adjustPanel, PALocale.GetLocale(@"PLTRound"), TEXTSCALE, new Vector2(73f, 32f), new Vector3(126f, 4f)).eventClick += (c, p) => {
                    if (p.buttons == UIMouseButton.Left) {
                        PropLineTool.ItemInfo.Spacing = (int)(PropLineTool.ItemInfo.Spacing);
                    }
                };
            }
        }

        public class UIBasicAngleCalculator : UICalculator {
            public override void Awake() {
                Vector2 longsize = new Vector2(50f, 14f);
                Vector2 shortSize = new Vector2(25f, 14f);
                base.Awake();
                UIPanel setPanel = m_setPanel;
                setPanel.size = new Vector2(58f, 40f);
                setPanel.relativePosition = new Vector3(0f, 0f);
                UIPanel adjustPanel = m_adjustPanel;
                adjustPanel.size = new Vector2(255f, 40f);
                adjustPanel.relativePosition = new Vector3(68f, 0f);
                AddButton(setPanel, PALocale.GetLocale(@"PLTZero"), TEXTSCALE, new Vector2(50f, 32f), new Vector3(4f, 4f)).eventClick += (c, p) => {
                    if (p.buttons == UIMouseButton.Left) {
                        PropLineTool.ItemInfo.Angle = 0f;
                    }
                };
                AddButton(adjustPanel, "+0.1", 0.625f, shortSize, new Vector3(4f, 4f)).eventClick += (c, p) => {
                    if (p.buttons == UIMouseButton.Left) {
                        PropLineTool.ItemInfo.Angle += (0.1f * Mathf.Deg2Rad);
                    }
                };
                AddButton(adjustPanel, "-0.1", 0.625f, shortSize, new Vector3(4f, 22f)).eventClick += (c, p) => {
                    if (p.buttons == UIMouseButton.Left) {
                        PropLineTool.ItemInfo.Angle -= (0.1f * Mathf.Deg2Rad);
                    }
                };
                AddButton(adjustPanel, "+ 1", TEXTSCALE, shortSize, new Vector3(33f, 4f)).eventClick += (c, p) => {
                    if (p.buttons == UIMouseButton.Left) {
                        PropLineTool.ItemInfo.Angle += (1f * Mathf.Deg2Rad);
                    }
                };
                AddButton(adjustPanel, "- 1", TEXTSCALE, shortSize, new Vector3(33f, 22f)).eventClick += (c, p) => {
                    if (p.buttons == UIMouseButton.Left) {
                        PropLineTool.ItemInfo.Angle -= (1f * Mathf.Deg2Rad);
                    }
                };
                AddButton(adjustPanel, "+10", TEXTSCALE, shortSize, new Vector3(62f, 4f)).eventClick += (c, p) => {
                    if (p.buttons == UIMouseButton.Left) {
                        PropLineTool.ItemInfo.Angle += (10f * Mathf.Deg2Rad);
                    }
                };
                AddButton(adjustPanel, "-10", TEXTSCALE, shortSize, new Vector3(62f, 22f)).eventClick += (c, p) => {
                    if (p.buttons == UIMouseButton.Left) {
                        PropLineTool.ItemInfo.Angle -= (10f * Mathf.Deg2Rad);
                    }
                };
                AddButton(adjustPanel, "+30", TEXTSCALE, shortSize, new Vector3(91f, 4f)).eventClick += (c, p) => {
                    if (p.buttons == UIMouseButton.Left) {
                        PropLineTool.ItemInfo.Angle += (30f * Mathf.Deg2Rad);
                    }
                };
                AddButton(adjustPanel, "-30", TEXTSCALE, shortSize, new Vector3(91f, 22f)).eventClick += (c, p) => {
                    if (p.buttons == UIMouseButton.Left) {
                        PropLineTool.ItemInfo.Angle -= (30f * Mathf.Deg2Rad);
                    }
                };
                AddButton(adjustPanel, "+45", TEXTSCALE, shortSize, new Vector3(120f, 4f)).eventClick += (c, p) => {
                    if (p.buttons == UIMouseButton.Left) {
                        PropLineTool.ItemInfo.Angle += (45f * Mathf.Deg2Rad);
                    }
                };
                AddButton(adjustPanel, "-45", TEXTSCALE, shortSize, new Vector3(120f, 22f)).eventClick += (c, p) => {
                    if (p.buttons == UIMouseButton.Left) {
                        PropLineTool.ItemInfo.Angle -= (45f * Mathf.Deg2Rad);
                    }
                };
                AddButton(adjustPanel, "+90", TEXTSCALE, shortSize, new Vector3(149f, 4f)).eventClick += (c, p) => {
                    if (p.buttons == UIMouseButton.Left) {
                        PropLineTool.ItemInfo.Angle += (90f * Mathf.Deg2Rad);
                    }
                };
                AddButton(adjustPanel, "-90", TEXTSCALE, shortSize, new Vector3(149f, 22f)).eventClick += (c, p) => {
                    if (p.buttons == UIMouseButton.Left) {
                        PropLineTool.ItemInfo.Angle -= (90f * Mathf.Deg2Rad);
                    }
                };
                AddButton(adjustPanel, PALocale.GetLocale(@"PLTRound"), TEXTSCALE, new Vector2(73f, 32f), new Vector3(178f, 4f)).eventClick += (c, p) => {
                    if (p.buttons == UIMouseButton.Left) {
                        PropLineTool.ItemInfo.Angle = Mathf.Round(PropLineTool.ItemInfo.Angle * Mathf.Rad2Deg) * Mathf.Deg2Rad;
                    }
                };
            }
        }
    }
}
