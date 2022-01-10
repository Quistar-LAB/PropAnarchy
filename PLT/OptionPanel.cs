using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.UI;
using EManagersLib;
using System;
using System.Globalization;
using UnityEngine;
using static PropAnarchy.PLT.PropLineTool;

namespace PropAnarchy.PLT {
    internal sealed class FieldInput : UITextField {
        private float m_rawValue = 1f;
        private int m_decimalPlaces = 0;
#pragma warning disable IDE1006
        public event PropertyChangedEventHandler<float> eventValueChanged;
#pragma warning restore IDE1006
        public Func<float> GetDefaultVal;

        public float Value {
            get => m_rawValue;
            set {
                if (m_rawValue != value) {
                    if (MinValue > 0) {
                        value = value >= MinValue ? value : MinValue;
                    }
                    m_rawValue = (float)Math.Round(value, 2);
                    text = m_rawValue.ToString();  // value.ToString(@"F" + m_decimalPlaces, LocaleManager.cultureInfo);
                    eventValueChanged?.Invoke(this, value);
                }
            }
        }
        public float MinValue { get; set; } = 0f;

        public int DecimalPlaces {
            get => m_decimalPlaces;
            set {
                m_decimalPlaces = Mathf.Clamp(value, 0, 7);
                text = value.ToString(@"F" + m_decimalPlaces, LocaleManager.cultureInfo);
            }
        }

        protected override void OnMouseWheel(UIMouseEventParameter p) {
            float val = m_rawValue;
            Event e = Event.current;
            if ((e.modifiers & EventModifiers.Control) == EventModifiers.Control) {
                val += p.wheelDelta * 5f;
            } else if ((e.modifiers & EventModifiers.Alt) == EventModifiers.Alt) {
                val += p.wheelDelta * 0.1f;
            } else {
                val += p.wheelDelta;
            }
            if (MinValue > 0) {
                Value = val >= MinValue ? val : MinValue;
            } else {
                Value = val;
            }
            base.OnMouseWheel(p);
        }

        protected override void OnMouseDown(UIMouseEventParameter p) {
            base.OnMouseDown(p);
            if (p.buttons == UIMouseButton.Right) {
                Value = GetDefaultVal();
            }
        }


        public override void Awake() {
            base.Awake();
            atlas = ToolBar.m_sharedTextures;
            autoSize = true;
            numericalOnly = true;
            maxLength = 8;
            allowFloats = true;
            padding = new RectOffset(23, 2, 2, 2);
            builtinKeyNavigation = true;
            isInteractive = true;
            readOnly = false;
            selectionSprite = @"EmptySprite";
            selectionBackgroundColor = new Color32(0, 172, 234, 255);
            normalBgSprite = @"TextBorder";
            disabledBgSprite = @"TextBorder";
            hoveredBgSprite = @"TextBorder";
            focusedBgSprite = @"TextBorder";
            horizontalAlignment = UIHorizontalAlignment.Left;
            verticalAlignment = UIVerticalAlignment.Middle;
            textScale = 0.90f;
            textColor = new Color32(0, 0, 0, 255);
            disabledTextColor = new Color32(0, 0, 0, 128);
            color = new Color32(255, 255, 255, 255);
            disabledColor = new Color32(180, 180, 180, 255);
            eventTextSubmitted += (c, text) => {
                if (float.TryParse(text, NumberStyles.Number, LocaleManager.cultureInfo, out float result)) Value = result;
            };
        }
    }

    internal sealed class MultiStateBtn : UIMultiStateButton {
        public override bool canFocus { get; set; }
    }

    internal sealed class OptionPanel : UIPanel {
        private const float TITLEBAR_HEIGHT = 30f;
        private const float PANEL_PADDING = 8f;
        private const float PANEL_MINWIDTH = 110f;
        private const float BTN_SIZEX = 45f;
        private const float BTN_SIZEY = 45f;
        private float m_delta = 1f;
        private delegate void OptionPanelHandler(ItemType mode);
        private static OptionPanelHandler CloseOption;
        private static OptionPanelHandler OpenOption;
        private Action<Event> UpdateUnit;
        internal static Action<ItemType> OnAnglePanelToggle;

        public static void Open(ItemType mode) => OpenOption(mode);

        public static void Close() => CloseOption(ItemType.Undefined);

        public override void Start() {
            base.Start();
            atlas = PAUtils.GetAtlas(@"Ingame");
            backgroundSprite = @"SubcategoriesPanel"; //@"GenericPanel"; //@"MenuPanel2";
            opacity = 0.65f;
            autoLayout = false;
            UIDragHandle titleBar = AddUIComponent<UIDragHandle>();
            #region titleBar
            titleBar.size = new Vector2(0f, TITLEBAR_HEIGHT);
            titleBar.target = this;
            titleBar.relativePosition = new Vector3(0f, 0f);
            #endregion titleBar
            UILabel titleLabel = AddUIComponent<UILabel>();
            #region titleLabel
            titleLabel.autoSize = true;
            titleLabel.autoHeight = false;
            titleLabel.size = new Vector2(0f, TITLEBAR_HEIGHT);
            titleLabel.relativePosition = new Vector3(0f, 0f);
            titleLabel.textScale = 0.85f;
            titleLabel.verticalAlignment = UIVerticalAlignment.Middle;
            titleLabel.textAlignment = UIHorizontalAlignment.Center;
            titleLabel.textColor = new Color32(0xe8, 0xe8, 0xe8, 0xee);
            titleLabel.disabledTextColor = new Color32(0x40, 0x40, 0x40, 0xee);
            titleLabel.padding = new RectOffset(10, 7, 5, 2);
            titleLabel.text = PALocale.GetLocale(@"PLTTitleBar");
            titleLabel.relativePosition = new Vector3(0f, 0f);
            #endregion titleLabel
            UISlicedSprite spacingContainerBG = AddUIComponent<UISlicedSprite>();
            #region spacingContainerBG
            spacingContainerBG.autoSize = false;
            spacingContainerBG.opacity = 0.43f;
            spacingContainerBG.spriteName = @"GenericPanel";
            spacingContainerBG.relativePosition = new Vector3(PANEL_PADDING, TITLEBAR_HEIGHT);
            spacingContainerBG.fillDirection = UIFillDirection.Horizontal;
            #endregion spacingContainerBG
            UISlicedSprite spacingPanelBG = AddUIComponent<UISlicedSprite>();
            #region spacingPanelBG
            spacingPanelBG.size = new Vector2(100f, spacingContainerBG.size.y);
            spacingPanelBG.opacity = 0.65f;
            spacingPanelBG.color = new Color32(0x97, 0xcf, 0xff, 0x9a);
            spacingPanelBG.spriteName = @"GenericPanel";
            spacingPanelBG.relativePosition = new Vector3(PANEL_PADDING, TITLEBAR_HEIGHT);
            #endregion spacingPanelBG
            UISprite spacingIcon = AddUIComponent<UISprite>();
            #region spacingIcon
            spacingIcon.atlas = ToolBar.m_sharedTextures;
            spacingIcon.spriteName = @"SpacingTitle";
            spacingIcon.size = new Vector2(21f, 16f);
            spacingIcon.relativePosition = new Vector3(spacingPanelBG.relativePosition.x + 5f, spacingPanelBG.relativePosition.y + 5f);
            #endregion spacingIcon
            UILabel spacingLabel = AddUIComponent<UILabel>();
            #region spacingLabel
            spacingLabel.verticalAlignment = UIVerticalAlignment.Middle;
            spacingLabel.textAlignment = UIHorizontalAlignment.Left;
            spacingLabel.textColor = new Color32(0xff, 0xff, 0xff, 0xff);
            spacingLabel.autoSize = false;
            spacingLabel.autoHeight = false;
            spacingLabel.textScale = 0.92f;
            using (UIFontRenderer renderer = spacingLabel.font.ObtainRenderer()) {
                string text = PALocale.GetLocale(@"PLTSpacingTitle");
                Vector2 textSize = renderer.MeasureString(text);
                spacingLabel.size = new Vector2(EMath.Max(textSize.x, PANEL_MINWIDTH), textSize.y);
                spacingLabel.text = text;
                spacingLabel.relativePosition = new Vector3(spacingIcon.relativePosition.x + spacingIcon.size.x + 5f, spacingIcon.relativePosition.y);
            }
            #endregion spacingLabel
            FieldInput spacingField = AddUIComponent<FieldInput>();
            #region spacingField
            spacingField.tooltip = PALocale.GetLocale(@"SpacingFieldTooltip");
            spacingField.GetDefaultVal = () => {
                ItemInfo.SetDefaultSpacing();
                return ItemInfo.Spacing;
            };
            spacingField.size = new Vector2(spacingIcon.size.x + spacingLabel.size.x + 6f, spacingField.size.y);
            spacingField.relativePosition = new Vector3(spacingIcon.relativePosition.x - 1f, spacingIcon.relativePosition.y + spacingIcon.size.y + 10f);
            #endregion spacingField
            UILabel spacingUnit = spacingField.AddUIComponent<UILabel>();
            spacingUnit.verticalAlignment = UIVerticalAlignment.Middle;
            spacingUnit.textAlignment = UIHorizontalAlignment.Left;
            spacingUnit.autoSize = true;
            spacingUnit.textColor = new Color32(0, 0, 0, 255);
            spacingUnit.textScale = 0.90f;
            spacingUnit.padding = new RectOffset(0, 3, 2, 0);
            spacingUnit.text = "±" + m_delta;
            spacingUnit.relativePosition = new Vector3(spacingField.size.x - spacingUnit.size.x - 15f, 0f);

            float spacingHeight = spacingField.relativePosition.y + spacingField.size.y - spacingIcon.relativePosition.y + 10f;
            spacingPanelBG.size = new Vector2(spacingField.size.x + 10f, spacingHeight);

            MultiStateBtn autoSpacing = AddToggleBtn(this, @"AutoSpacing");
            autoSpacing.tooltip = PALocale.GetLocale(@"PLTAutoDefaultSpacingTooltip");
            autoSpacing.activeStateIndex = Settings.AutoDefaultSpacing ? 1 : 0;
            UIButton defaultSpacing = AddButton(this, @"Default");
            defaultSpacing.tooltip = PALocale.GetLocale(@"PLTDefaultTooltip");
            UIButton widthSpacing = AddButton(this, @"Width");
            widthSpacing.tooltip = PALocale.GetLocale(@"PLTWidthTooltip");
            UIButton lengthSpacing = AddButton(this, @"Length");
            lengthSpacing.tooltip = PALocale.GetLocale(@"PLTLengthTooltip");
            if (Settings.VerticalLayout) {

            } else {
                autoSpacing.relativePosition = new Vector3(spacingPanelBG.relativePosition.x + spacingPanelBG.size.x + 5f, spacingPanelBG.relativePosition.y + (spacingHeight - autoSpacing.size.y) / 2f);
                defaultSpacing.relativePosition = new Vector3(autoSpacing.relativePosition.x + autoSpacing.size.x + 5f, autoSpacing.relativePosition.y);
                widthSpacing.relativePosition = new Vector3(defaultSpacing.relativePosition.x + defaultSpacing.size.x + 5f, defaultSpacing.relativePosition.y);
                lengthSpacing.relativePosition = new Vector3(widthSpacing.relativePosition.x + widthSpacing.size.x + 5f, widthSpacing.relativePosition.y);
                spacingContainerBG.size = new Vector2(spacingPanelBG.size.x + autoSpacing.size.x + defaultSpacing.size.x + widthSpacing.size.x + lengthSpacing.size.x + 40f - PANEL_PADDING * 2f, spacingHeight);
            }

            UISlicedSprite angleContainerBG = AddUIComponent<UISlicedSprite>();
            #region angleContainerBG
            angleContainerBG.autoSize = false;
            angleContainerBG.opacity = 0.43f;
            angleContainerBG.spriteName = @"GenericPanel";
            angleContainerBG.relativePosition = new Vector3(PANEL_PADDING, TITLEBAR_HEIGHT + spacingContainerBG.size.y + PANEL_PADDING);
            angleContainerBG.fillDirection = UIFillDirection.Horizontal;
            #endregion angleContainerBG
            UISlicedSprite anglePanelBG = AddUIComponent<UISlicedSprite>();
            #region anglePanelBG
            anglePanelBG.size = new Vector2(100f, spacingContainerBG.size.y);
            anglePanelBG.opacity = 0.65f;
            anglePanelBG.color = new Color32(0x97, 0xcf, 0xff, 0x9a);
            anglePanelBG.spriteName = @"GenericPanel";
            anglePanelBG.relativePosition = new Vector3(PANEL_PADDING, TITLEBAR_HEIGHT + spacingContainerBG.size.y + PANEL_PADDING);
            #endregion anglePanelBG
            UISprite angleIcon = AddUIComponent<UISprite>();
            #region angleIcon
            angleIcon.atlas = ToolBar.m_sharedTextures;
            angleIcon.spriteName = @"Angle";
            angleIcon.size = new Vector2(21f, 16f);
            angleIcon.relativePosition = new Vector3(anglePanelBG.relativePosition.x + 5f, anglePanelBG.relativePosition.y + 5f);
            #endregion angleIcon
            UILabel angleLabel = AddUIComponent<UILabel>();
            #region spacingLabel
            angleLabel.verticalAlignment = UIVerticalAlignment.Middle;
            angleLabel.textAlignment = UIHorizontalAlignment.Left;
            angleLabel.textColor = new Color32(0xff, 0xff, 0xff, 0xff);
            angleLabel.autoSize = false;
            angleLabel.autoHeight = false;
            angleLabel.textScale = 0.92f;
            using (UIFontRenderer renderer = angleLabel.font.ObtainRenderer()) {
                string text = PALocale.GetLocale(@"PLTAngleTitle");
                Vector2 textSize = renderer.MeasureString(text);
                angleLabel.size = new Vector2(EMath.Max(textSize.x, PANEL_MINWIDTH), textSize.y);
                angleLabel.text = text;
                angleLabel.relativePosition = new Vector3(angleIcon.relativePosition.x + angleIcon.size.x + 5f, angleIcon.relativePosition.y);
            }
            #endregion spacingLabel
            FieldInput angleField = AddUIComponent<FieldInput>();
            #region angleField
            angleField.tooltip = PALocale.GetLocale(@"AnglefieldTooltip");
            angleField.GetDefaultVal = () => 0f;
            angleField.size = new Vector2(angleIcon.size.x + angleLabel.size.x + 6f, spacingField.size.y);
            angleField.relativePosition = new Vector3(angleIcon.relativePosition.x - 1f, angleIcon.relativePosition.y + angleIcon.size.y + 10f);
            #endregion angleField
            UILabel angleUnit = angleField.AddUIComponent<UILabel>();
            angleUnit.verticalAlignment = UIVerticalAlignment.Middle;
            angleUnit.textAlignment = UIHorizontalAlignment.Left;
            angleUnit.autoSize = true;
            angleUnit.textColor = new Color32(0, 0, 0, 255);
            angleUnit.textScale = 0.90f;
            angleUnit.padding = new RectOffset(0, 3, 2, 0);
            angleUnit.text = Settings.AngleMode == AngleMode.Dynamic ? "±" + m_delta + @"Δ°" : "±" + m_delta + @"°";
            angleUnit.relativePosition = new Vector3(angleField.size.x - angleUnit.size.x - 15f, 0f);
            float angleHeight = angleField.relativePosition.y + angleField.size.y - angleIcon.relativePosition.y + 10f;
            anglePanelBG.size = new Vector2(angleField.size.x + 10f, angleHeight);

            MultiStateBtn angleDynamic = AddToggleBtn(this, @"AngleDynamic");
            angleDynamic.tooltip = PALocale.GetLocale(@"PLTAngleDynamicTooltip");
            MultiStateBtn angleSingle = AddToggleBtn(this, @"AngleSingle");
            angleSingle.tooltip = PALocale.GetLocale(@"PLTAngleSingleTooltip");
            MultiStateBtn flip180 = AddToggleBtn(this, @"Flip180");
            flip180.tooltip = PALocale.GetLocale(@"PLTFlip180Tooltip");
            MultiStateBtn flip90 = AddToggleBtn(this, @"Flip90");
            flip90.tooltip = PALocale.GetLocale(@"PLTFlip90Tooltip");

            if (Settings.VerticalLayout) {

            } else {
                angleDynamic.relativePosition = new Vector3(anglePanelBG.relativePosition.x + anglePanelBG.size.x + 5f, anglePanelBG.relativePosition.y + (angleHeight - angleDynamic.size.y) / 2f);
                angleSingle.relativePosition = new Vector3(angleDynamic.relativePosition.x + angleDynamic.size.x + 5f, angleDynamic.relativePosition.y);
                flip180.relativePosition = new Vector3(angleSingle.relativePosition.x + angleSingle.size.x + 5f, angleSingle.relativePosition.y);
                flip90.relativePosition = new Vector3(flip180.relativePosition.x + flip180.size.x + 5f, flip180.relativePosition.y);
                angleContainerBG.size = new Vector2(anglePanelBG.size.x + angleDynamic.size.x + angleSingle.size.x + flip180.size.x + flip90.size.x + 40f - PANEL_PADDING * 2f, spacingHeight);
            }

            UISlicedSprite settingContainerBG = AddUIComponent<UISlicedSprite>();
            #region settingContainerBG
            settingContainerBG.autoSize = false;
            settingContainerBG.opacity = 0.43f;
            settingContainerBG.spriteName = @"GenericPanel";
            settingContainerBG.relativePosition = new Vector3(PANEL_PADDING, TITLEBAR_HEIGHT + angleContainerBG.size.y + angleContainerBG.size.y + PANEL_PADDING * 2f);
            settingContainerBG.fillDirection = UIFillDirection.Horizontal;
            settingContainerBG.size = angleContainerBG.size;
            #endregion settingContainerBG
            UISlicedSprite settingPanelBG = AddUIComponent<UISlicedSprite>();
            #region settingPanelBG
            settingPanelBG.size = anglePanelBG.size;
            settingPanelBG.opacity = 0.65f;
            settingPanelBG.color = new Color32(0x97, 0xcf, 0xff, 0x9a);
            settingPanelBG.spriteName = @"GenericPanel";
            settingPanelBG.relativePosition = new Vector3(PANEL_PADDING, settingContainerBG.relativePosition.y);
            #endregion settingPanelBG

            MultiStateBtn spacingMode = AddToggleBtn(this, @"Spacing", EMath.Floor((settingPanelBG.size.x - 15f) / 2f), BTN_SIZEY);
            spacingMode.tooltip = PALocale.GetLocale(@"PLTSpacingTooltip");
            spacingMode.activeStateIndex = Settings.ControlMode == ControlMode.Spacing ? 1 : 0;
            MultiStateBtn itemwiseMode = AddToggleBtn(this, @"ItemWise", spacingMode.size.x, BTN_SIZEY);
            itemwiseMode.tooltip = PALocale.GetLocale(@"PLTItemWiseTooltip");
            itemwiseMode.activeStateIndex = Settings.ControlMode == ControlMode.ItemWise ? 1 : 0;
            MultiStateBtn meshCenter = AddToggleBtn(this, @"MeshCenter");
            meshCenter.tooltip = PALocale.GetLocale(@"PLTMeshCenterCorrectionTooltip");
            meshCenter.activeStateIndex = Settings.UseMeshCenterCorrection ? 1 : 0;
            MultiStateBtn perfectCircle = AddToggleBtn(this, @"PerfectCircle");
            perfectCircle.tooltip = PALocale.GetLocale(@"PLTPerfectCircleTooltip");
            perfectCircle.activeStateIndex = Settings.PerfectCircles ? 1 : 0;
            MultiStateBtn linearFence = AddToggleBtn(this, @"LinearFence");
            linearFence.tooltip = PALocale.GetLocale(@"PLTLinearFenceTooltip");
            linearFence.activeStateIndex = Settings.LinearFenceFill ? 1 : 0;

            if (Settings.VerticalLayout) {

            } else {
                spacingMode.relativePosition = new Vector3(settingPanelBG.relativePosition.x + 5f, settingPanelBG.relativePosition.y + 5f);
                itemwiseMode.relativePosition = new Vector3(spacingMode.relativePosition.x + spacingMode.size.x + 4f, spacingMode.relativePosition.y);
                meshCenter.relativePosition = new Vector3(settingPanelBG.relativePosition.x + settingPanelBG.size.x + 5f, spacingMode.relativePosition.y);
                perfectCircle.relativePosition = new Vector3(meshCenter.relativePosition.x + meshCenter.size.x + 5f, meshCenter.relativePosition.y);
                linearFence.relativePosition = new Vector3(perfectCircle.relativePosition.x + perfectCircle.size.x + 5f, perfectCircle.relativePosition.y);
                size = new Vector2(spacingContainerBG.size.x + PANEL_PADDING * 2f, TITLEBAR_HEIGHT + spacingHeight + angleHeight + angleHeight + PANEL_PADDING * 4f);
                titleBar.width = size.x;
                titleLabel.width = size.x;
            }
            absolutePosition = new Vector3(Screen.width - size.x - 40f, 200f);
            isVisible = false;

            CloseOption = (mode) => Hide();
            OpenOption = (mode) => Show();

            autoSpacing.eventActiveStateIndexChanged += (c, index) => {
                if (index != 0) {
                    Settings.AutoDefaultSpacing = true;
                    ItemInfo.SetDefaultSpacing();
                } else {
                    Settings.AutoDefaultSpacing = false;
                }
                DrawMode.CurActiveMode.UpdatePlacement();
            };
            SetAutoSpacing = (state) => autoSpacing.activeStateIndex = state ? 1 : 0;
            defaultSpacing.eventClicked += (c, p) => {
                ItemInfo.SetDefaultSpacing();
                DrawMode.CurActiveMode.UpdatePlacement();
            };
            lengthSpacing.eventClicked += (c, p) => {
                SetSpacingValue(ItemInfo.Height);
                DrawMode.CurActiveMode.UpdatePlacement();
            };
            widthSpacing.eventClicked += (c, p) => {
                SetSpacingValue(ItemInfo.Width);
                DrawMode.CurActiveMode.UpdatePlacement();
            };
            spacingField.Value = 1f;
            spacingField.MinValue = 0.1f;
            spacingField.eventValueChanged += (c, value) => SegmentState.m_pendingPlacementUpdate = true;
            spacingField.eventMouseWheel += (c, p) => autoSpacing.activeStateIndex = 0;
            spacingField.eventKeyDown += (c, p) => autoSpacing.activeStateIndex = 0;
            SetSpacingValue = (value) => spacingField.Value = value;
            GetSpacingValue = () => spacingField.Value;

            switch (Settings.AngleMode) {
            case AngleMode.Dynamic:
                angleDynamic.activeStateIndex = 1;
                angleSingle.activeStateIndex = 0;
                break;
            case AngleMode.Single:
                angleDynamic.activeStateIndex = 0;
                angleSingle.activeStateIndex = 1;
                break;
            }
            angleDynamic.eventActiveStateIndexChanged += (c, index) => {
                if (index != 0) {
                    Settings.AngleMode = AngleMode.Dynamic;
                    angleUnit.text = "±" + m_delta + @"Δ°";
                    angleSingle.activeStateIndex = 0;
                    SegmentState.m_pendingPlacementUpdate = true;
                }
            };
            angleSingle.eventActiveStateIndexChanged += (c, index) => {
                if (index != 0) {
                    Settings.AngleMode = AngleMode.Single;
                    angleUnit.text = "±" + m_delta + @"°";
                    angleDynamic.activeStateIndex = 0;
                    SegmentState.m_pendingPlacementUpdate = true;
                }
            };
            UpdateUnit = (e) => {
                if ((e.modifiers & EventModifiers.Control) == EventModifiers.Control) {
                    m_delta = 5f;
                } else if ((e.modifiers & EventModifiers.Alt) == EventModifiers.Alt) {
                    m_delta = 0.1f;
                } else {
                    m_delta = 1f;
                }
                switch (Settings.AngleMode) {
                case AngleMode.Dynamic:
                    angleUnit.text = "±" + m_delta + @"Δ°";
                    break;
                case AngleMode.Single:
                    angleUnit.text = "±" + m_delta + @"°";
                    break;
                }
                spacingUnit.text = "±" + m_delta;
            };

            flip180.eventActiveStateIndexChanged += (c, index) => {
                Settings.AngleFlip180 = index != 0;
                DrawMode.CurActiveMode.UpdatePlacement();
            };
            flip90.eventActiveStateIndexChanged += (c, index) => {
                Settings.AngleFlip90 = index != 0;
                DrawMode.CurActiveMode.UpdatePlacement();
            };

            SetAngleMode = (value) => {
                switch (value) {
                case AngleMode.Dynamic:
                    angleDynamic.activeStateIndex = 1;
                    break;
                case AngleMode.Single:
                    angleSingle.activeStateIndex = 0;
                    break;
                }
            };
            float normalHeight = TITLEBAR_HEIGHT + spacingHeight + angleHeight + angleHeight + PANEL_PADDING * 4f;
            float shortHeight = TITLEBAR_HEIGHT + spacingHeight + angleHeight + PANEL_PADDING * 3f;
            SetAngleModeState = (state) => {
                if (state != angleContainerBG.isVisibleSelf) {
                    if (!state) {
                        angleContainerBG.Hide();
                        anglePanelBG.Hide();
                        angleIcon.Hide();
                        angleLabel.Hide();
                        angleField.Hide();
                        angleUnit.Hide();
                        angleDynamic.Hide();
                        angleSingle.Hide();
                        flip180.Hide();
                        flip90.Hide();
                        settingContainerBG.relativePosition = new Vector3(PANEL_PADDING, TITLEBAR_HEIGHT + spacingContainerBG.size.y + PANEL_PADDING);
                        settingPanelBG.relativePosition = new Vector3(PANEL_PADDING, settingContainerBG.relativePosition.y);
                        spacingMode.relativePosition = new Vector3(settingPanelBG.relativePosition.x + 5f, settingPanelBG.relativePosition.y + 5f);
                        itemwiseMode.relativePosition = new Vector3(spacingMode.relativePosition.x + spacingMode.size.x + 4f, spacingMode.relativePosition.y);
                        meshCenter.relativePosition = new Vector3(settingPanelBG.relativePosition.x + settingPanelBG.size.x + 5f, spacingMode.relativePosition.y);
                        perfectCircle.relativePosition = new Vector3(meshCenter.relativePosition.x + meshCenter.size.x + 5f, meshCenter.relativePosition.y);
                        linearFence.relativePosition = new Vector3(perfectCircle.relativePosition.x + perfectCircle.size.x + 5f, perfectCircle.relativePosition.y);
                        size = new Vector2(spacingContainerBG.size.x + PANEL_PADDING * 2f, shortHeight);
                    } else {
                        angleContainerBG.Show();
                        anglePanelBG.Show();
                        angleIcon.Show();
                        angleLabel.Show();
                        angleField.Show();
                        angleUnit.Show();
                        angleDynamic.Show();
                        angleSingle.Show();
                        flip180.Show();
                        flip90.Show();
                        settingContainerBG.relativePosition = new Vector3(PANEL_PADDING, TITLEBAR_HEIGHT + angleContainerBG.size.y + angleContainerBG.size.y + PANEL_PADDING * 2f);
                        settingPanelBG.relativePosition = new Vector3(PANEL_PADDING, settingContainerBG.relativePosition.y);
                        spacingMode.relativePosition = new Vector3(settingPanelBG.relativePosition.x + 5f, settingPanelBG.relativePosition.y + 5f);
                        itemwiseMode.relativePosition = new Vector3(spacingMode.relativePosition.x + spacingMode.size.x + 4f, spacingMode.relativePosition.y);
                        meshCenter.relativePosition = new Vector3(settingPanelBG.relativePosition.x + settingPanelBG.size.x + 5f, spacingMode.relativePosition.y);
                        perfectCircle.relativePosition = new Vector3(meshCenter.relativePosition.x + meshCenter.size.x + 5f, meshCenter.relativePosition.y);
                        linearFence.relativePosition = new Vector3(perfectCircle.relativePosition.x + perfectCircle.size.x + 5f, perfectCircle.relativePosition.y);
                        size = new Vector2(spacingContainerBG.size.x + PANEL_PADDING * 2f, normalHeight);
                    }

                }
            };
            angleField.Value = 0f;
            angleField.eventValueChanged += (c, value) => SegmentState.m_pendingPlacementUpdate = true;
            GetAngleValue = () => angleField.Value;
            SetAngleValue = (value) => angleField.Value = value;
            OnAnglePanelToggle += (mode) => {
                switch (mode) {
                case ItemType.Prop: SetAngleModeState(true); break;
                case ItemType.Tree: SetAngleModeState(false); break;
                }
            };

            spacingMode.eventActiveStateIndexChanged += (c, index) => {
                if (index == 1) {
                    itemwiseMode.activeStateIndex = 0;
                    Settings.ControlMode = ControlMode.Spacing;
                }
            };
            itemwiseMode.eventActiveStateIndexChanged += (c, index) => {
                if (index == 1) {
                    spacingMode.activeStateIndex = 0;
                    Settings.ControlMode = ControlMode.ItemWise;
                }
            };
            meshCenter.eventActiveStateIndexChanged += (c, index) => Settings.UseMeshCenterCorrection = index == 1;
            perfectCircle.eventActiveStateIndexChanged += (c, index) => Settings.PerfectCircles = index == 1;
            linearFence.eventActiveStateIndexChanged += (c, index) => Settings.LinearFenceFill = index == 1;
        }

        public override void Update() {
            base.Update();
            UpdateUnit(Event.current);
        }

        private static UIButton AddButton(UIComponent parent, string name) {
            UIButton btn = parent.AddUIComponent<UIButton>();
            btn.atlas = ToolBar.m_sharedTextures;
            btn.autoSize = false;
            btn.height = BTN_SIZEY;
            btn.width = BTN_SIZEX;
            btn.name = name;
            btn.normalFgSprite = name;
            btn.focusedFgSprite = name;
            btn.hoveredFgSprite = name + @"Hovered";
            btn.pressedFgSprite = name + @"Pressed";
            btn.disabledFgSprite = name;
            btn.playAudioEvents = true;
            return btn;
        }

        private static MultiStateBtn AddToggleBtn(UIComponent parent, string spriteName, float btnWidth = 0f, float btnHeight = 0f) {
            MultiStateBtn toggleBtn = parent.AddUIComponent<MultiStateBtn>();
            toggleBtn.name = spriteName;
            toggleBtn.cachedName = spriteName;
            toggleBtn.atlas = ToolBar.m_sharedTextures;
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
            fgSpriteSetState.AddState();
            bgSpriteSetState.AddState();
            UIMultiStateButton.SpriteSet fgSpriteSet1 = fgSpriteSetState[1];
            UIMultiStateButton.SpriteSet bgSpriteSet1 = bgSpriteSetState[1];
            if (!spriteName.IsNullOrWhiteSpace()) {
                bgSpriteSet0.normal = spriteName;
                bgSpriteSet0.disabled = spriteName;
                bgSpriteSet1.disabled = spriteName;
                bgSpriteSet0.hovered = spriteName + @"Hovered";
                string sprite = spriteName + @"Pressed";
                bgSpriteSet0.focused = sprite;
                bgSpriteSet0.pressed = sprite;
                bgSpriteSet1.normal = sprite;
                bgSpriteSet1.hovered = sprite;
                bgSpriteSet1.focused = sprite;
                bgSpriteSet1.pressed = sprite;
            }
            if (btnWidth == 0) {
                toggleBtn.width = BTN_SIZEX;
                toggleBtn.height = BTN_SIZEY;
            } else {
                toggleBtn.width = btnWidth;
                toggleBtn.height = btnHeight;
            }
            toggleBtn.playAudioEvents = true;
            toggleBtn.state = UIMultiStateButton.ButtonState.Normal;
            toggleBtn.activeStateIndex = 0;
            toggleBtn.foregroundSpriteMode = UIForegroundSpriteMode.Fill;
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
