using ColossalFramework;
using ColossalFramework.UI;
using UnityEngine;
using EManagersLib.API;
using static PropAnarchy.PAModule;

namespace PropAnarchy {
    public class PAOptionPanel : UIPanel {
        private const string m_optionPanelName = "PropAnarchyOptionPanel";
        private const float MIN_SCALE_FACTOR = 1.0f;
        private const float MAX_SCALE_FACTOR = 42f;
        public const float DefaultFontScale = 0.95f;
        public const float SmallFontScale = 0.85f;
        public const float TabFontScale = 0.9f;


        public static UICheckBox m_propAnarchyCB;
        public static UICheckBox m_propSnappingCB;
        public UILabel MaxPropLabel;
        public UISlider PropScaleFactorSlider;

        protected PAOptionPanel() {
            gameObject.name = m_optionPanelName;
            name = m_optionPanelName;
        }

        public override void Awake() {
            base.OnEnable();
            FitTo(m_Parent);
            isLocalized = true;
            m_AutoLayoutDirection = LayoutDirection.Vertical;
            m_AutoLayout = true;
            UITabstrip tabBar = AddUIComponent<UITabstrip>();
            UITabContainer tabContainer = AddUIComponent<UITabContainer>();
            tabBar.tabPages = tabContainer;
            tabContainer.FitTo(m_Parent);

            PALocale locale = SingletonLite<PALocale>.instance;

            UIPanel mainPanel = AddTab(tabBar, locale.GetLocale("MainOptionTab"), 0, true);
            mainPanel.autoLayout = false;
            mainPanel.autoSize = false;
            ShowStandardOptions(mainPanel);

            UIPanel snapPanel = AddTab(tabBar, locale.GetLocale("ExtraFunctionsTab"), 1, true);
            snapPanel.autoLayout = false;
            snapPanel.autoSize = false;
            ShowPropSnapOption(snapPanel);

            AddTab(tabBar, locale.GetLocale("KeyboardShortcutTab"), 2, true).gameObject.AddComponent<PAKeyBinding>();
        }

        private void ShowStandardOptions(UIPanel panel) {
            PALocale locale = SingletonLite<PALocale>.instance;
            m_propAnarchyCB = AddCheckBox(panel, locale.GetLocale("PropAnarchy"), UsePropAnarchy, (_, isChecked) => {
                UsePropAnarchy = isChecked;
                SaveSettings();
            });
            m_propAnarchyCB.AlignTo(panel, UIAlignAnchor.TopLeft);
            m_propAnarchyCB.relativePosition = new Vector3(2, 5);

            m_propSnappingCB = AddCheckBox(panel, locale.GetLocale("PropSnapping"), UsePropSnapping, (_, isChecked) => {
                UsePropSnapping = isChecked;
                SaveSettings();
            });
            m_propSnappingCB.AlignTo(m_propAnarchyCB, UIAlignAnchor.TopLeft);
            m_propSnappingCB.relativePosition = new Vector3(0, m_propAnarchyCB.height);

            UICheckBox decalPropFixCB = AddCheckBox(panel, locale.GetLocale("DecalPropFix"), UseDecalPropFix, (_, isChecked) => {
                UseDecalPropFix = isChecked;
                SaveSettings();
            });
            decalPropFixCB.AlignTo(m_propSnappingCB, UIAlignAnchor.TopLeft);
            decalPropFixCB.relativePosition = new Vector3(0, m_propSnappingCB.height);


            UIPanel ScalePanel = (UIPanel)panel.AttachUIComponent(UITemplateManager.GetAsGameObject("OptionsSliderTemplate"));
            MaxPropLabel = ScalePanel.Find<UILabel>("Label");
            MaxPropLabel.width = panel.width - 100;
            MaxPropLabel.textScale = 1.1f;
            MaxPropLabel.text = string.Format(locale.GetLocale("MaxPropLimit"), EPropManager.MAX_PROP_LIMIT);
            PropScaleFactorSlider = AddSlider(ScalePanel, MIN_SCALE_FACTOR, MAX_SCALE_FACTOR, 0.5f, EPropManager.PROP_LIMIT_SCALE, (_, val) => {
                EPropManager.PROP_LIMIT_SCALE = val;
                MaxPropLabel.text = string.Format(SingletonLite<PALocale>.instance.GetLocale("MaxPropLimit"), EPropManager.MAX_PROP_LIMIT);
                SaveSettings();
            });
            PropScaleFactorSlider.width = panel.width - 150;
            ScalePanel.AlignTo(decalPropFixCB, UIAlignAnchor.TopLeft);
            ScalePanel.relativePosition = new Vector3(0, decalPropFixCB.height);

        }

        private void ShowPropSnapOption(UIPanel panel) {
            PALocale locale = SingletonLite<PALocale>.instance;
        }

        private static UIPanel AddTab(UITabstrip tabStrip, string tabName, int tabIndex, bool autoLayout) {
            UIButton tabButton = tabStrip.AddTab(tabName);

            tabButton.normalBgSprite = "SubBarButtonBase";
            tabButton.disabledBgSprite = "SubBarButtonBaseDisabled";
            tabButton.focusedBgSprite = "SubBarButtonBaseFocused";
            tabButton.hoveredBgSprite = "SubBarButtonBaseHovered";
            tabButton.pressedBgSprite = "SubBarButtonBasePressed";
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

        private static UICheckBox AddCheckBox(UIPanel panel, string name, bool defaultVal, PropertyChangedEventHandler<bool> callback) {
            UICheckBox cb = (UICheckBox)panel.AttachUIComponent(UITemplateManager.GetAsGameObject("OptionsCheckBoxTemplate"));
            cb.eventCheckChanged += callback;
            cb.text = name;
            cb.height += 10;
            cb.isChecked = defaultVal;
            return cb;
        }

        private static void AddSpace(UIPanel panel, float height) {
            UIPanel space = panel.AddUIComponent<UIPanel>();
            space.name = "Space";
            space.isInteractive = false;
            space.height = height;
        }

        private static UILabel AddDescription(UIPanel panel, string name, UIComponent alignTo, float fontScale, string text) {
            UILabel desc = panel.AddUIComponent<UILabel>();
            if (!(alignTo is null)) desc.AlignTo(alignTo, UIAlignAnchor.BottomLeft);
            desc.name = name;
            desc.width = panel.width - 80;
            desc.wordWrap = true;
            desc.autoHeight = true;
            desc.textScale = fontScale;
            desc.text = text;
            desc.relativePosition = new Vector3(1, 23);
            AddSpace(panel, desc.height);
            return desc;
        }

        private static UISlider AddSlider(UIPanel panel, float min, float max, float step, float defaultVal, PropertyChangedEventHandler<float> callback) {
            UISlider slider = panel.Find<UISlider>("Slider");
            slider.minValue = min;
            slider.maxValue = max;
            slider.stepSize = step;
            slider.value = defaultVal;
            slider.eventValueChanged += callback;
            return slider;
        }

        private static UIDropDown AddDropdown(UIPanel panel, UIComponent alignTo, string text, string[] options, int defaultSelection, PropertyChangedEventHandler<int> callback) {
            UIPanel uiPanel = panel.AttachUIComponent(UITemplateManager.GetAsGameObject("OptionsDropdownTemplate")) as UIPanel;
            uiPanel.AlignTo(alignTo, UIAlignAnchor.BottomLeft);
            UILabel label = uiPanel.Find<UILabel>("Label");
            label.text = text;
            UIDropDown dropDown = uiPanel.Find<UIDropDown>("Dropdown");
            dropDown.width = 340;
            dropDown.items = options;
            dropDown.selectedIndex = defaultSelection;
            dropDown.eventSelectedIndexChanged += callback;
            return dropDown;
        }

        internal static void SetPropAnarchyState(bool state) {

        }
    }
}
