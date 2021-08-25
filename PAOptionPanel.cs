using ColossalFramework;
using ColossalFramework.UI;
using UnityEngine;
using static PropAnarchy.PAModule;

namespace PropAnarchy {
    public class PAOptionPanel : UIPanel {
        private const string m_optionPanelName = "PropAnarchyOptionPanel";
        public const float DefaultFontScale = 0.95f;
        public const float SmallFontScale = 0.85f;
        public const float TabFontScale = 0.9f;

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

            UIPanel snapPanel = AddTab(tabBar, locale.GetLocale("PropSnap"), 1, true);
            snapPanel.autoLayout = false;
            snapPanel.autoSize = false;
            ShowPropSnapOption(snapPanel);

            AddTab(tabBar, locale.GetLocale("KeyboardShortcutTab"), 2, true).gameObject.AddComponent<PAKeyBinding>();
        }

        private void ShowStandardOptions(UIPanel panel) {
            PALocale locale = SingletonLite<PALocale>.instance;
            UICheckBox indicatorCB = AddCheckBox(panel, locale.GetLocale("EnableCustomLimit"), UseCustomPropLimit, (_, isChecked) => {
                UseCustomPropLimit = isChecked;
                SaveSettings();
            });
            indicatorCB.AlignTo(panel, UIAlignAnchor.TopLeft);
            indicatorCB.relativePosition = new Vector3(2, 5);


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
    }
}
