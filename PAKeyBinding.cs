using ColossalFramework;
using ColossalFramework.UI;
using System.Threading;
using UI;
using UnityEngine;
using static PropAnarchy.PAModule;

namespace PropAnarchy {
    internal class PAKeyBinding : UICustomControl {
        private const string thisCategory = @"PropAnarchy";
        private SavedInputKey m_EditingBinding;

        [RebindableKey(@"TreeAnarchy")]
        private static readonly string togglePropSnapping = @"togglePropSnapping";
        [RebindableKey(@"PropAnarchy")]
        private static readonly string togglePropAnarchy = @"togglePropAnarchy";
        [RebindableKey(@"PropAnarchy")]
        private static readonly string groupProps = @"groupProps";
        [RebindableKey(@"PropAnarchy")]
        private static readonly string ungroupProps = @"ungroupProps";
        [RebindableKey(@"PropAnarchy")]
        private static readonly string incrementPropSize = @"incrPropVariation";
        [RebindableKey(@"PropAnarchy")]
        private static readonly string decrementPropSize = @"decrPropVariation";

        private static readonly InputKey defaultTogglePropSnappingKey = SavedInputKey.Encode(KeyCode.S, false, false, true);
        private static readonly InputKey defaultTogglePropAnarchyKey = SavedInputKey.Encode(KeyCode.A, false, false, true);
        private static readonly InputKey defaultGroupPropKey = SavedInputKey.Encode(KeyCode.G, true, false, false);
        private static readonly InputKey defaultUngroupPropKey = SavedInputKey.Encode(KeyCode.U, true, false, false);
        private static readonly InputKey defaultIncrementPropSizeKey = SavedInputKey.Encode(KeyCode.Period, false, false, false);
        private static readonly InputKey defaultDecrementPropSizeKey = SavedInputKey.Encode(KeyCode.Comma, false, false, false);

        private static readonly SavedInputKey m_propSnapping = new SavedInputKey(togglePropSnapping, KeybindingConfigFile, defaultTogglePropSnappingKey, true);
        private static readonly SavedInputKey m_propAnarchy = new SavedInputKey(togglePropAnarchy, KeybindingConfigFile, defaultTogglePropAnarchyKey, true);
        private static readonly SavedInputKey m_groupProps = new SavedInputKey(groupProps, KeybindingConfigFile, defaultGroupPropKey, true);
        private static readonly SavedInputKey m_ungroupProps = new SavedInputKey(ungroupProps, KeybindingConfigFile, defaultUngroupPropKey, true);
        private static readonly SavedInputKey m_incrPropVariation = new SavedInputKey(incrementPropSize, KeybindingConfigFile, defaultIncrementPropSizeKey, true);
        private static readonly SavedInputKey m_decrPropVariation = new SavedInputKey(decrementPropSize, KeybindingConfigFile, defaultDecrementPropSizeKey, true);

        protected void Update() {
            if (!UIView.HasModalInput() && !UIView.HasInputFocus()) {
                Event e = Event.current;
                if (m_groupProps.IsPressed(e)) {
                    //SingletonLite<PAManager>.instance.GroupProps();
                } else if (m_ungroupProps.IsPressed(e)) {
                    //SingletonLite<PAManager>.instance.UngroupProps();
                } else if (m_propSnapping.IsPressed(e)) {
                    bool state = UsePropSnapping = !UsePropSnapping;
                    PAOptionPanel.SetPropSnapState(state);
                    UIIndicator.SnapIndicator?.SetState(state);
                    ThreadPool.QueueUserWorkItem(SaveSettings);
                } else if (m_propAnarchy.IsPressed(e)) {
                    bool state = UsePropAnarchy = !UsePropAnarchy;
                    PAOptionPanel.SetPropAnarchyState(state);
                    UIIndicator.AnarchyIndicator?.SetState(state);
                    ThreadPool.QueueUserWorkItem(SaveSettings);
                } else if (IsCustomPressed(m_incrPropVariation, e)) {
                    PAManager.IncrementPropSize();
                } else if (IsCustomPressed(m_decrPropVariation, e)) {
                    PAManager.DecrementPropSize();
                }
            }
        }

        protected void Awake() {
            UILabel desc = component.AddUIComponent<UILabel>();
            desc.padding.top = 10;
            desc.width = component.width - 50;
            desc.autoHeight = true;
            desc.wordWrap = true;
            desc.textScale = PAOptionPanel.SmallFontScale;
            desc.text = PALocale.GetLocale(@"KeyBindDescription");
            AddKeymapping(@"PropSnapping", m_propSnapping);
            AddKeymapping(@"PropAnarchy", m_propAnarchy);
            AddKeymapping(@"GroupProps", m_groupProps);
            AddKeymapping(@"UngroupProps", m_ungroupProps);
            AddKeymapping(@"IncreasePropSize", m_incrPropVariation);
            AddKeymapping(@"DecreasePropSize", m_decrPropVariation);
        }

        private bool IsCustomPressed(SavedInputKey inputKey, Event e) {
            if (e.type != EventType.KeyDown) return false;
            return Input.GetKey(inputKey.Key) &&
                (e.modifiers & EventModifiers.Control) == EventModifiers.Control == inputKey.Control &&
                (e.modifiers & EventModifiers.Shift) == EventModifiers.Shift == inputKey.Shift &&
                (e.modifiers & EventModifiers.Alt) == EventModifiers.Alt == inputKey.Alt;
        }

        private int listCount = 0;
        private void AddKeymapping(string key, SavedInputKey savedInputKey) {
            UIPanel uIPanel = component.AttachUIComponent(UITemplateManager.GetAsGameObject(@"KeyBindingTemplate")) as UIPanel;
            if (listCount++ % 2 == 1) uIPanel.backgroundSprite = null;

            UILabel uILabel = uIPanel.Find<UILabel>(@"Name");
            UIButton uIButton = uIPanel.Find<UIButton>(@"Binding");

            uIButton.eventKeyDown += new KeyPressHandler(OnBindingKeyDown);
            uIButton.eventMouseDown += new MouseEventHandler(OnBindingMouseDown);
            uILabel.stringUserData = key;
            uILabel.text = PALocale.GetLocale(key);
            uIButton.text = savedInputKey.ToLocalizedString(@"KEYNAME");
            uIButton.objectUserData = savedInputKey;
            uIButton.stringUserData = thisCategory; // used for localization TODO:
        }

        private void OnBindingKeyDown(UIComponent comp, UIKeyEventParameter p) {
            if (!(m_EditingBinding is null) && !IsModifierKey(p.keycode)) {
                p.Use();
                UIView.PopModal();
                KeyCode keycode = p.keycode;
                InputKey inputKey = (p.keycode == KeyCode.Escape) ? m_EditingBinding.value : SavedInputKey.Encode(keycode, p.control, p.shift, p.alt);
                if (p.keycode == KeyCode.Backspace) {
                    inputKey = SavedInputKey.Empty;
                }
                m_EditingBinding.value = inputKey;
                UITextComponent uITextComponent = p.source as UITextComponent;
                uITextComponent.text = m_EditingBinding.ToLocalizedString(@"KEYNAME");
                m_EditingBinding = null;
            }
        }

        private void OnBindingMouseDown(UIComponent comp, UIMouseEventParameter p) {
            if (m_EditingBinding is null) {
                p.Use();
                m_EditingBinding = (SavedInputKey)p.source.objectUserData;
                UIButton uIButton = p.source as UIButton;
                uIButton.buttonsMask = (UIMouseButton.Left | UIMouseButton.Right | UIMouseButton.Middle | UIMouseButton.Special0 | UIMouseButton.Special1 | UIMouseButton.Special2 | UIMouseButton.Special3);
                uIButton.text = PALocale.GetLocale(@"PressAnyKey");
                p.source.Focus();
                UIView.PushModal(p.source);
            } else if (!IsUnbindableMouseButton(p.buttons)) {
                p.Use();
                UIView.PopModal();
                InputKey inputKey = SavedInputKey.Encode(ButtonToKeycode(p.buttons), IsControlDown(), IsShiftDown(), IsAltDown());
                m_EditingBinding.value = inputKey;
                UIButton uIButton2 = p.source as UIButton;
                uIButton2.text = m_EditingBinding.ToLocalizedString(@"KEYNAME");
                uIButton2.buttonsMask = UIMouseButton.Left;
                m_EditingBinding = null;
            }
        }

        private KeyCode ButtonToKeycode(UIMouseButton button) {
            switch (button) {
            case UIMouseButton.Left: return KeyCode.Mouse0;
            case UIMouseButton.Right: return KeyCode.Mouse1;
            case UIMouseButton.Middle: return KeyCode.Mouse2;
            case UIMouseButton.Special0: return KeyCode.Mouse3;
            case UIMouseButton.Special1: return KeyCode.Mouse4;
            case UIMouseButton.Special2: return KeyCode.Mouse5;
            case UIMouseButton.Special3: return KeyCode.Mouse6;
            default: return KeyCode.None;
            }
        }

        private bool IsUnbindableMouseButton(UIMouseButton code) => (code == UIMouseButton.Left || code == UIMouseButton.Right);
        private bool IsModifierKey(KeyCode code) => code == KeyCode.LeftControl || code == KeyCode.RightControl || code == KeyCode.LeftShift ||
                                                    code == KeyCode.RightShift || code == KeyCode.LeftAlt || code == KeyCode.RightAlt;
        private bool IsControlDown() => (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl));
        private bool IsShiftDown() => (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
        private bool IsAltDown() => (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt));
    }
}
