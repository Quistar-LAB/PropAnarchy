using ColossalFramework.UI;
using EManagersLib;
using MoveIt;
using System.Collections.Generic;
using UnityEngine;

namespace PropAnarchy {
    internal static class PAPainter {
        private const string PAINTERBTN_NAME = @"PAPainterButton";
        private const string COLORFIELD_NAME = @"PAPainterColorField";
        private const string COLORPICKER_NAME = @"PAPaintercolorPicker";
        internal delegate void ActionHandler(HashSet<Instance> selection);
        internal delegate void CloneHandler(Dictionary<Instance, Instance> clonedOrigin);
        internal static ActionHandler ActionAddHandler;
        internal static CloneHandler ActionCloneHandler;

        internal static void AddPropPainterBtn(UIToolOptionPanel optionPanel, UIButton moreTools, UIPanel mtpBackGround, UIPanel mtpContainer) {
            EPropInstance[] props = EPropManager.m_props.m_buffer;
            UIColorField field = UnityEngine.Object.Instantiate(UITemplateManager.Get<UIPanel>("LineTemplate").Find<UIColorField>("LineColor"));
            field.isVisible = true;
            field.name = COLORFIELD_NAME;
            UIColorPicker picker = UnityEngine.Object.Instantiate(field.colorPicker);
            optionPanel.AttachUIComponent(picker.gameObject);
            picker.color = Color.white;
            picker.name = COLORPICKER_NAME;

            UIPanel pickerPanel = picker.component as UIPanel;
            pickerPanel.color = Color.white;
            pickerPanel.backgroundSprite = @"InfoPanelBack";
            pickerPanel.isVisible = false;
            // re-adjust moretools panel
            Vector2 containerSize = mtpContainer.size;
            containerSize.y += 40f;
            optionPanel.m_moreToolsPanel.size = containerSize;
            mtpContainer.size = containerSize;
            Vector2 backgroundSize = mtpBackGround.size;
            backgroundSize.y += 40f;
            mtpBackGround.size = backgroundSize;
            optionPanel.m_moreToolsPanel.absolutePosition = moreTools.absolutePosition + new Vector3(0, 10 - optionPanel.m_moreToolsPanel.height);

            UIMultiStateButton painterBtn = mtpContainer.AddUIComponent<UIMultiStateButton>();
            painterBtn.name = PAINTERBTN_NAME;
            painterBtn.cachedName = PAINTERBTN_NAME;
            painterBtn.tooltip = PALocale.GetLocale(@"PainterTooltip");
            painterBtn.playAudioEvents = true;
            painterBtn.size = new Vector2(36f, 36f);
            painterBtn.atlas = optionPanel.m_picker.atlas;
            painterBtn.spritePadding = new RectOffset(2, 2, 2, 2);
            painterBtn.backgroundSprites.AddState();
            painterBtn.foregroundSprites.AddState();
            painterBtn.backgroundSprites[0].normal = "OptionBase";
            painterBtn.backgroundSprites[0].focused = "OptionBase";
            painterBtn.backgroundSprites[0].hovered = "OptionBaseHovered";
            painterBtn.backgroundSprites[0].pressed = "OptionBasePressed";
            painterBtn.backgroundSprites[0].disabled = "OptionBaseDisabled";
            painterBtn.foregroundSprites[0].normal = "EyeDropper";
            painterBtn.backgroundSprites[1].normal = "OptionBaseFocused";
            painterBtn.backgroundSprites[1].focused = "OptionBaseFocused";
            painterBtn.backgroundSprites[1].hovered = "OptionBaseHovered";
            painterBtn.backgroundSprites[1].pressed = "OptionBasePressed";
            painterBtn.backgroundSprites[1].disabled = "OptionBaseDisabled";
            painterBtn.foregroundSprites[1].normal = "EyeDropper";
            painterBtn.activeStateIndex = 0;
            Vector2 parentSize = painterBtn.parent.size;
            painterBtn.parent.parent.size = new Vector2(parentSize.x, parentSize.y + 40f);
            painterBtn.parent.size = painterBtn.parent.parent.size;
            painterBtn.eventActiveStateIndexChanged += (_, index) => {
                pickerPanel.isVisible = index == 1;
            };
            pickerPanel.absolutePosition = painterBtn.absolutePosition - new Vector3(pickerPanel.width, pickerPanel.height - 50f);

            /* Finally attach all delegates */
            picker.m_HSBField.eventClicked += (c, p) => {
                HashSet<Instance> selections = MoveIt.Action.selection;
                if (!(selections is null) && selections.Count > 0) {
                    foreach (var selection in selections) {
                        uint propID = selection.id.GetProp32();
                        if (selection.isValid && !selection.id.IsEmpty && propID > 0) {
                            props[propID].m_color = picker.color;
                        }
                    }
                }
            };

            picker.m_HueSlider.eventClicked += (c, p) => {
                HashSet<Instance> selections = MoveIt.Action.selection;
                if (!(selections is null) && selections.Count > 0) {
                    foreach (var selection in selections) {
                        uint propID = selection.id.GetProp32();
                        if (selection.isValid && !selection.id.IsEmpty && propID > 0) {
                            props[propID].m_color = picker.color;
                        }
                    }
                }
            };

            ActionAddHandler = (selections) => {
                foreach (var selection in selections) {
                    uint propID = selection.id.GetProp32();
                    if (selection.isValid && !selection.id.IsEmpty && propID > 0) {
                        picker.color = props[propID].m_color;
                        break;
                    }
                }
            };

            ActionCloneHandler = (clonedOrigin) => {
                foreach (KeyValuePair<Instance, Instance> x in clonedOrigin) {
                    if (x.Key.id.Type != InstanceType.Prop) return;
                    props[x.Value.id.GetProp32()].m_color = props[x.Key.id.GetProp32()].m_color;
                }
            };
        }
    }
}
