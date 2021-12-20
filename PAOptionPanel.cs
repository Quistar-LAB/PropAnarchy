using ColossalFramework;
using ColossalFramework.UI;
using EManagersLib;
using System.Threading;
using UnityEngine;
using static PropAnarchy.PAModule;

namespace PropAnarchy {
    internal static class PAOptionPanel {
        internal const float TabFontScale = 0.9f;
        internal const float DefaultFontScale = 0.95f;
        internal const float SmallFontScale = 0.85f;
        private const float MIN_SCALE_FACTOR = 1.0f;
        private const float MAX_SCALE_FACTOR = 84f;
        private static readonly Color32 m_greyColor = new Color32(0xe6, 0xe6, 0xe6, 0xee);
        private static readonly Color32 m_greenColor = new Color32(0xcf, 0xf9, 0x8f, 0xff);
        private static readonly Color32 m_orangeColor = new Color32(0xfe, 0xd8, 0x8b, 0xff);
        private static readonly Color32 m_redColor = Color.red;
        public static UICheckBox m_propAnarchyCB;
        public static UICheckBox m_propSnappingCB;
        public static UILabel MaxPropLabel;
        public static UISlider PropScaleFactorSlider;

        private static void UpdateState(bool isInGame) {
            if (isInGame) {
                PropScaleFactorSlider.Disable();
                return;
            }
            PropScaleFactorSlider.Enable();
        }

        internal static void SetupPanel(UIPanel root) {
            UITabstrip tabBar = root.AddUIComponent<UITabstrip>();
            UITabContainer tabContainer = root.AddUIComponent<UITabContainer>();
            tabBar.tabPages = tabContainer;
            tabContainer.size = new Vector2(root.width, 520f);

            UIPanel mainPanel = AddTab(tabBar, PALocale.GetLocale(@"MainOptionTab"), 0, true);
            mainPanel.autoLayout = false;
            mainPanel.autoSize = false;
            ShowStandardOptions(mainPanel);
            UpdateState(IsInGame);

            UIPanel snapPanel = AddTab(tabBar, PALocale.GetLocale(@"ExtraFunctionsTab"), 1, true);
            snapPanel.autoLayout = false;
            snapPanel.autoSize = false;
            ShowPropSnapOption(snapPanel);

            AddTab(tabBar, PALocale.GetLocale(@"KeyboardShortcutTab"), 2, true).gameObject.AddComponent<PAKeyBinding>();

        }

        private const float OFFSETX = 5f;
        private static void ShowStandardOptions(UIPanel panel) {
            UITextureAtlas atlas = PAUtils.CreateTextureAtlas(@"PAOptionAtlas", @"PropAnarchy.Resources.", new string[] { @"thumb", @"treelimitbg" }, 1024);
            m_propAnarchyCB = AddCheckBox(panel, PALocale.GetLocale(@"PropAnarchy"), UsePropAnarchy);
            m_propAnarchyCB.eventClicked += (c, p) => UsePropAnarchy = (c as UICheckBox).isChecked;
            m_propAnarchyCB.relativePosition = new Vector3(OFFSETX, 0f);

            m_propSnappingCB = AddCheckBox(panel, PALocale.GetLocale(@"PropSnapping"), UsePropSnapping);
            m_propSnappingCB.eventClicked += (c, p) => UsePropSnapping = (c as UICheckBox).isChecked;
            m_propSnappingCB.relativePosition = new Vector3(OFFSETX, m_propAnarchyCB.relativePosition.y + m_propAnarchyCB.height);

            UIPanel ScalePanel = (UIPanel)panel.AttachUIComponent(UITemplateManager.GetAsGameObject(@"OptionsSliderTemplate"));
            MaxPropLabel = ScalePanel.Find<UILabel>(@"Label");
            MaxPropLabel.width = panel.width - 70f;
            MaxPropLabel.textScale = 1.1f;
            MaxPropLabel.text = string.Format(PALocale.GetLocale(@"MaxPropLimit"), EPropManager.MAX_PROP_LIMIT);
            PropScaleFactorSlider = AddSlider(ScalePanel, MIN_SCALE_FACTOR, MAX_SCALE_FACTOR, 0.5f, PropLimitScale, (_, val) => {
                PropLimitScale = val;
                MaxPropLabel.text = string.Format(PALocale.GetLocale(@"MaxPropLimit"), EPropManager.MAX_PROP_LIMIT);
            });
            PropScaleFactorSlider.atlas = atlas;
            PropScaleFactorSlider.backgroundSprite = @"treelimitbg";
            PropScaleFactorSlider.size = new Vector2(panel.width - 70f, 21f);
            UISprite sliderThumb = PropScaleFactorSlider.thumbObject as UISprite;
            sliderThumb.atlas = atlas;
            sliderThumb.spriteName = @"thumb";
            sliderThumb.height = 21f;
            ScalePanel.relativePosition = new Vector3(OFFSETX, m_propSnappingCB.relativePosition.y + m_propSnappingCB.height);
            UILabel ImportantLabel = AddDescription(panel, @"ImportantLabel", ScalePanel, DefaultFontScale, PALocale.GetLocale(@"Important"));
            ImportantLabel.relativePosition = new Vector3(OFFSETX, ImportantLabel.relativePosition.y + 13f);
            UILabel decalPropFix = AddFunctionDescription(panel, PALocale.GetLocale(@"DecalPropFix"), true, ImportantLabel, DefaultFontScale);
            UILabel additiveShader = AddFunctionDescription(panel, PALocale.GetLocale(@"AdditiveShader"), true, decalPropFix, DefaultFontScale);
            //UILabel adaptivePropVisibility = AddFunctionDescription(panel, PALocale.GetLocale(@"AdaptivePropVisibility"), true, additiveShader, DefaultFontScale);
            UILabel transparencyLODFix = AddFunctionDescription(panel, PALocale.GetLocale(@"TransparencyLODFix"), true, additiveShader, DefaultFontScale);
            UILabel propLineTool = AddFunctionDescription(panel, PALocale.GetLocale(@"PropLineTool"), true, transparencyLODFix, DefaultFontScale);
            UILabel propPainter = AddFunctionDescription(panel, PALocale.GetLocale(@"PropPainter"), true, propLineTool, DefaultFontScale);
        }

        private static void ShowPropSnapOption(UIPanel panel) {
            AddContainer(panel, null, panel.width - 70f, 380f, PALocale.GetLocale(@"TransparencyLODFix"));
            UICheckBox hideCloudCB = AddCheckBox(panel, PALocale.GetLocale(@"HideCloud"), TransparencyLODFix.Settings.HideClouds);
            hideCloudCB.eventClicked += (c, p) => TransparencyLODFix.Settings.HideClouds = (c as UICheckBox).isChecked;
            hideCloudCB.relativePosition = new Vector3(OFFSETX + 5f, 15f);
            UIButton destroyCloud = panel.AttachUIComponent(UITemplateManager.GetAsGameObject(@"OptionsButtonTemplate")) as UIButton;
            destroyCloud.textScale = 0.90f;
            destroyCloud.wordWrap = false;
            destroyCloud.text = PALocale.GetLocale(@"DestroyAllClouds");
            destroyCloud.relativePosition = new Vector3(panel.width - destroyCloud.size.x - 80f, hideCloudCB.relativePosition.y + 10f);
            destroyCloud.eventClick += (_, p) => {

            };
            UIButton reset = panel.AttachUIComponent(UITemplateManager.GetAsGameObject(@"OptionsButtonTemplate")) as UIButton;
            reset.textScale = 0.90f;
            reset.wordWrap = false;
            reset.text = PALocale.GetLocale(@"Reset");
            reset.relativePosition = new Vector3(destroyCloud.relativePosition.x - reset.size.x - 5f, destroyCloud.relativePosition.y);

            UILabel propLodFixes = panel.AddUIComponent<UILabel>();
            propLodFixes.wordWrap = false;
            propLodFixes.textScale = DefaultFontScale;
            propLodFixes.textColor = m_greyColor;
            propLodFixes.text = PALocale.GetLocale(@"PropLODFixSetting");
            propLodFixes.relativePosition = new Vector3(OFFSETX + 5f, hideCloudCB.relativePosition.y + hideCloudCB.size.y);
            UI.UIFancySlider propLodFactorMultiplier = panel.AddUIComponent<UI.UIFancySlider>();
            propLodFactorMultiplier.width = panel.width - 180f;
            propLodFactorMultiplier.Initialize(PALocale.GetLocale(@"LODFactorMultiplier"), 1f, 1000f, 1f, TransparencyLODFix.Settings.LodFactorMultiplierProps, (_, value) => {
                TransparencyLODFix.Settings.LodFactorMultiplierProps = value;
            });
            propLodFactorMultiplier.relativePosition = new Vector3(OFFSETX + 5f, propLodFixes.relativePosition.y + propLodFixes.size.y);
            UI.UIFancySlider propLodDistanceOffset = panel.AddUIComponent<UI.UIFancySlider>();
            propLodDistanceOffset.width = panel.width - 180f;
            propLodDistanceOffset.Initialize(PALocale.GetLocale(@"LODDistanceOffset"), 1f, 1000f, 1f, TransparencyLODFix.Settings.DistanceOffsetProps, (_, value) => {
                TransparencyLODFix.Settings.DistanceOffsetProps = value;
            });
            propLodDistanceOffset.relativePosition = new Vector3(OFFSETX + 5f, propLodFactorMultiplier.relativePosition.y + propLodFactorMultiplier.size.y + 5f);
            UI.UIFancySlider propLodDistanceMultiplier = panel.AddUIComponent<UI.UIFancySlider>();
            propLodDistanceMultiplier.width = panel.width - 180f;
            propLodDistanceMultiplier.Initialize(PALocale.GetLocale(@"LODDistanceMultiplier"), 0.05f, 1f, 0.05f, TransparencyLODFix.Settings.LodDistanceMultiplierProps, (_, value) => {
                TransparencyLODFix.Settings.LodDistanceMultiplierProps = value;
            });
            propLodDistanceMultiplier.relativePosition = new Vector3(OFFSETX + 5f, propLodDistanceOffset.relativePosition.y + propLodDistanceOffset.size.y + 5f);
            UI.UIFancySlider propFallbackRenderDistance = panel.AddUIComponent<UI.UIFancySlider>();
            propFallbackRenderDistance.width = panel.width - 180f;
            propFallbackRenderDistance.Initialize(PALocale.GetLocale(@"FallbackRenderDistance"), 1000f, 100000f, 1000f, TransparencyLODFix.Settings.FallbackRenderDistanceProps, (_, value) => {
                TransparencyLODFix.Settings.FallbackRenderDistanceProps = value;
            });
            propFallbackRenderDistance.relativePosition = new Vector3(OFFSETX + 5f, propLodDistanceMultiplier.relativePosition.y + propLodDistanceMultiplier.size.y + 5f);

            UILabel buildingLodFixes = panel.AddUIComponent<UILabel>();
            buildingLodFixes.wordWrap = false;
            buildingLodFixes.textColor = m_greyColor;
            buildingLodFixes.textScale = DefaultFontScale;
            buildingLodFixes.text = PALocale.GetLocale(@"BuildingLODFixSetting");
            buildingLodFixes.relativePosition = new Vector3(OFFSETX + 5f, propFallbackRenderDistance.relativePosition.y + propFallbackRenderDistance.size.y + 10f);
            UI.UIFancySlider buildingLodFactorMultiplier = panel.AddUIComponent<UI.UIFancySlider>();
            buildingLodFactorMultiplier.width = panel.width - 180f;
            buildingLodFactorMultiplier.Initialize(PALocale.GetLocale(@"LODFactorMultiplier"), 1f, 1000f, 1f, TransparencyLODFix.Settings.LodFactorMultiplierBuildings, (_, value) => {
                TransparencyLODFix.Settings.LodFactorMultiplierBuildings = value;
            });
            buildingLodFactorMultiplier.relativePosition = new Vector3(OFFSETX + 5f, buildingLodFixes.relativePosition.y + buildingLodFixes.size.y);
            UI.UIFancySlider buildingLodDistanceOffset = panel.AddUIComponent<UI.UIFancySlider>();
            buildingLodDistanceOffset.width = panel.width - 180f;
            buildingLodDistanceOffset.Initialize(PALocale.GetLocale(@"LODDistanceOffset"), 1f, 1000f, 1f, TransparencyLODFix.Settings.DistanceOffsetBuildings, (_, value) => {
                TransparencyLODFix.Settings.DistanceOffsetBuildings = value;
            });
            buildingLodDistanceOffset.relativePosition = new Vector3(OFFSETX + 5f, buildingLodFactorMultiplier.relativePosition.y + buildingLodFactorMultiplier.size.y + 5f);
            UI.UIFancySlider buildingLodDistanceMultiplier = panel.AddUIComponent<UI.UIFancySlider>();
            buildingLodDistanceMultiplier.width = panel.width - 180f;
            buildingLodDistanceMultiplier.Initialize(PALocale.GetLocale(@"LODDistanceMultiplier"), 0.05f, 1f, 0.05f, TransparencyLODFix.Settings.LodDistanceMultiplierBuildings, (_, value) => {
                TransparencyLODFix.Settings.LodDistanceMultiplierBuildings = value;
            });
            buildingLodDistanceMultiplier.relativePosition = new Vector3(OFFSETX + 5f, buildingLodDistanceOffset.relativePosition.y + buildingLodDistanceOffset.size.y + 5f);
            UI.UIFancySlider buildingFallbackRenderDistance = panel.AddUIComponent<UI.UIFancySlider>();
            buildingFallbackRenderDistance.width = panel.width - 180f;
            buildingFallbackRenderDistance.Initialize(PALocale.GetLocale(@"FallbackRenderDistance"), 1000f, 100000f, 1000f, TransparencyLODFix.Settings.FallbackRenderDistanceBuildings, (_, value) => {
                TransparencyLODFix.Settings.FallbackRenderDistanceBuildings = value;
            });
            buildingFallbackRenderDistance.relativePosition = new Vector3(OFFSETX + 5f, buildingLodDistanceMultiplier.relativePosition.y + buildingLodDistanceMultiplier.size.y + 5f);

            reset.eventClick += (_, p) => {
                TransparencyLODFix.Settings.Reset();
                hideCloudCB.isChecked = false;
                propLodFactorMultiplier.value = TransparencyLODFix.Settings.m_lodFactorMultiplierProps;
                propLodDistanceOffset.value = TransparencyLODFix.Settings.m_distanceOffsetProps;
                propLodDistanceMultiplier.value = TransparencyLODFix.Settings.m_lodDistanceMultiplierProps;
                propFallbackRenderDistance.value = TransparencyLODFix.Settings.m_fallbackRenderDistanceProps;
                buildingLodFactorMultiplier.value = TransparencyLODFix.Settings.m_lodFactorMultiplierBuildings;
                buildingLodDistanceOffset.value = TransparencyLODFix.Settings.m_distanceOffsetBuildings;
                buildingLodDistanceMultiplier.value = TransparencyLODFix.Settings.m_lodDistanceMultiplierBuildings;
                buildingFallbackRenderDistance.value = TransparencyLODFix.Settings.m_fallbackRenderDistanceBuildings;
                ThreadPool.QueueUserWorkItem(SaveSettings);
            };
        }

        private static UICanvasSprite AddContainer(UIPanel root, UIComponent alignTo, float width, float height, string label) {
            Color grey = Color.grey;
            UICanvasSprite canvas = root.AddUIComponent<UICanvasSprite>();
            UILabel name = root.AddUIComponent<UILabel>();
            name.wordWrap = false;
            name.relativePosition = new Vector3(40f, 4f);
            name.text = label;
            canvas.size = new Vector2(width, height);
            canvas.desiredCanvasWidth = width;
            canvas.desiredCanvasHeight = height;
            if (alignTo is null) canvas.relativePosition = new Vector3(0f, 10f);
            else canvas.relativePosition = new Vector3(alignTo.relativePosition.x, alignTo.relativePosition.y + alignTo.size.y);
            canvas.MoveTo(2f, 2f);
            canvas.LineTo(canvas.size.x - 2f, 2f, grey);
            canvas.LineTo(canvas.size.x - 2f, canvas.size.y - 2f, grey);
            canvas.LineTo(35f + name.width + 5f, canvas.size.y - 2f, grey);
            canvas.MoveTo(35f, canvas.size.y - 2f);
            canvas.LineTo(2f, canvas.size.y - 2f, grey);
            canvas.LineTo(2f, 2f, grey);
            canvas.ApplyChanges();
            return canvas;
        }

        private static UIPanel AddTab(UITabstrip tabStrip, string tabName, int tabIndex, bool autoLayout) {
            UIButton tabButton = tabStrip.AddTab(tabName);

            tabButton.normalBgSprite = @"SubBarButtonBase";
            tabButton.disabledBgSprite = @"SubBarButtonBaseDisabled";
            tabButton.focusedBgSprite = @"SubBarButtonBaseFocused";
            tabButton.hoveredBgSprite = @"SubBarButtonBaseHovered";
            tabButton.pressedBgSprite = @"SubBarButtonBasePressed";
            tabButton.tooltip = tabName;
            tabButton.width = 175;
            tabButton.textScale = TabFontScale;

            tabStrip.selectedIndex = tabIndex;

            UIPanel rootPanel = tabStrip.tabContainer.components[tabIndex] as UIPanel;
            rootPanel.autoLayout = autoLayout;
            if (autoLayout) {
                rootPanel.autoLayoutDirection = LayoutDirection.Vertical;
                rootPanel.autoLayoutPadding.top = 0;
                rootPanel.autoLayoutPadding.bottom = 0;
                rootPanel.autoLayoutPadding.left = 5;
            }
            return rootPanel;
        }

        private static UICheckBox AddCheckBox(UIPanel panel, string name, bool defaultVal) {
            UICheckBox cb = (UICheckBox)panel.AttachUIComponent(UITemplateManager.GetAsGameObject(@"OptionsCheckBoxTemplate"));
            cb.autoSize = true;
            cb.isLocalized = true;
            cb.label.textScale = 0.95f;
            cb.label.padding = new RectOffset(0, 0, 3, 0);
            cb.label.textColor = m_orangeColor;
            cb.text = name;
            cb.height += 20f;
            cb.isChecked = defaultVal;
            return cb;
        }

        private static UILabel AddFunctionDescription(UIPanel panel, string name, bool state, UIComponent alignTo, float fontScale) {
            UILabel nameLabel = panel.AddUIComponent<UILabel>();
            UILabel stateLabel = panel.AddUIComponent<UILabel>();
            nameLabel.wordWrap = false;
            nameLabel.textScale = fontScale;
            nameLabel.textColor = m_greyColor;
            nameLabel.text = PALocale.GetLocale(@"builtinFunction") + @" [" + name + @"] ";
            stateLabel.wordWrap = false;
            stateLabel.textScale = fontScale;
            stateLabel.textColor = state ? m_greenColor : m_redColor;
            stateLabel.text = state ? PALocale.GetLocale(@"isEnabled") : PALocale.GetLocale(@"isDisabled");
            if (alignTo is null) {
                nameLabel.relativePosition = new Vector3(OFFSETX, 0f);
            } else {
                nameLabel.relativePosition = new Vector3(OFFSETX, alignTo.relativePosition.y + alignTo.size.y + 5f);
            }
            stateLabel.relativePosition = new Vector3(nameLabel.relativePosition.x + nameLabel.size.x, nameLabel.relativePosition.y);
            return nameLabel;
        }

        private static UILabel AddDescription(UIPanel panel, string name, UIComponent alignTo, float fontScale, string text) {
            UILabel desc = panel.AddUIComponent<UILabel>();
            desc.name = name;
            desc.width = panel.width - 80;
            desc.wordWrap = true;
            desc.autoHeight = true;
            desc.textScale = fontScale;
            desc.textColor = m_greyColor;
            desc.text = text;
            desc.relativePosition = new Vector3(alignTo.relativePosition.x + 26f, alignTo.relativePosition.y + alignTo.height - 5f);
            return desc;
        }

        private static UISlider AddSlider(UIPanel panel, float min, float max, float step, float defaultVal, PropertyChangedEventHandler<float> callback) {
            UISlider slider = panel.Find<UISlider>(@"Slider");
            slider.minValue = min;
            slider.maxValue = max;
            slider.stepSize = step;
            slider.value = defaultVal;
            slider.eventValueChanged += callback;
            return slider;
        }

        private static UIDropDown AddDropdown(UIPanel panel, UIComponent alignTo, string text, string[] options, int defaultSelection, PropertyChangedEventHandler<int> callback) {
            UIPanel uiPanel = panel.AttachUIComponent(UITemplateManager.GetAsGameObject(@"OptionsDropdownTemplate")) as UIPanel;
            UILabel label = uiPanel.Find<UILabel>(@"Label");
            if (text.IsNullOrWhiteSpace()) {
                label.Hide();
            } else {
                label.autoSize = true;
                label.textScale = 0.95f;
                label.textColor = m_orangeColor;
                label.text = text;
            }
            UIDropDown dropDown = uiPanel.Find<UIDropDown>(@"Dropdown");
            dropDown.width = 380;
            dropDown.items = options;
            dropDown.selectedIndex = defaultSelection;
            dropDown.eventSelectedIndexChanged += callback;
            uiPanel.relativePosition = new Vector3(alignTo.relativePosition.x, alignTo.relativePosition.y + alignTo.height);
            return dropDown;
        }
    }
}
