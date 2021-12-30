using ColossalFramework;
using ColossalFramework.Math;
using ColossalFramework.UI;
using EManagersLib;
using MoveIt;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
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
        private static System.Action<UIColorPicker, Color> SetPickerColorField;

        private static Color GetColor(float x, float y, float width, float height, Color hue) {
            float num = x / width;
            float num2 = y / height;
            num = num < 0f ? 0f : (num > 1f ? 1f : num);
            num2 = num2 < 0f ? 0f : (num2 > 1f ? 1f : num2);
            Color result = Color.Lerp(Color.white, hue, num) * (1f - num2);
            result.a = 1f;
            return result;
        }

        private static void SetPickerColor(UIColorPicker picker, Color color) {
            UISprite indicator = picker.m_Indicator;
            UITextureSprite HSBField = picker.m_HSBField;
            picker.hue = HSBColor.GetHue(color);
            SetPickerColorField(picker, color);
            HSBColor hSBColor = HSBColor.FromColor(color);
            Vector2 a = new Vector2(hSBColor.s * HSBField.width, (1f - hSBColor.b) * HSBField.height);
            indicator.relativePosition = a - indicator.size * 0.5f;
            if (!(HSBField.renderMaterial is null)) {
                HSBField.renderMaterial.color = picker.hue.gamma;
            }
            Vector2 vector = new Vector2(indicator.relativePosition.x + indicator.size.x * 0.5f, indicator.relativePosition.y + indicator.size.y * 0.5f);
            SetPickerColorField(picker, GetColor(vector.x, vector.y, HSBField.width, HSBField.height, picker.hue));
        }

        internal static void AddPropPainterBtn(UIToolOptionPanel optionPanel, UIButton moreTools, UIPanel mtpBackGround, UIPanel mtpContainer) {
            SetPickerColorField = PAUtils.CreateSetter<UIColorPicker, Color>(typeof(UIColorPicker).GetField("m_Color", BindingFlags.Instance | BindingFlags.NonPublic));
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
            SimulationManager smInstance = Singleton<SimulationManager>.instance;

            /* Finally attach all delegates */
            IEnumerator ProcessColor(HashSet<Instance> selections) {
                yield return new WaitForSeconds(0.1f);
                if (!(selections is null) && selections.Count > 0) {
                    foreach (var selection in selections) {
                        uint propID = selection.id.GetProp32();
                        if (selection.isValid && !selection.id.IsEmpty && propID > 0) {
                            props[propID].m_color = picker.color;
                        }
                    }
                }
            }

            picker.eventColorUpdated += (color) => smInstance.AddAction(ProcessColor(Action.selection));

            ActionAddHandler = (selections) => {
                smInstance.AddAction(() => {
                    foreach (var selection in selections) {
                        uint propID = selection.id.GetProp32();
                        if (selection.isValid && !selection.id.IsEmpty && propID > 0) {
                            SetPickerColor(picker, props[propID].m_color);
                            break;
                        }
                    }
                });
            };

            ActionCloneHandler = (clonedOrigin) => {
                smInstance.AddAction(() => {
                    foreach (KeyValuePair<Instance, Instance> x in clonedOrigin) {
                        if (x.Key.id.Type != InstanceType.Prop) return;
                        props[x.Value.id.GetProp32()].m_color = props[x.Key.id.GetProp32()].m_color;
                    }
                });
            };
        }
    }
}
