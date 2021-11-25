using ColossalFramework;
using EManagersLib;
using MoveIt;
using UnityEngine;

namespace PropAnarchy {
    public partial class PAManager : SingletonLite<PAManager> {
        public const float minScale = 0.2f;
        public const float maxScale = 5.0f;
        public const float scaleStep = 0.2f;
        public static uint m_currentPropID = 0;

        public static void IncrementPropSize() {
            float scale;
            EPropInstance[] props = EPropManager.m_props.m_buffer;
            PropTool propTool = ToolsModifierControl.GetCurrentTool<PropTool>();
            uint propID = m_currentPropID;
            if (!(propTool is null) && propTool.m_mode == PropTool.Mode.Single && Cursor.visible && propID > 1) {
                scale = props[propID].m_scale + scaleStep;
                props[propID].m_scale = scale > maxScale ? maxScale : scale;
            } else if ((MoveItTool.ToolState == MoveItTool.ToolStates.Default) &&
                       UIToolOptionPanel.instance.isVisible && Action.selection.Count > 0) {
                foreach (Instance instance in Action.selection) {
                    if (instance is MoveableProp && !instance.id.IsEmpty) {
                        propID = instance.id.GetProp32();
                        if (propID > 0) {
                            scale = props[propID].m_scale + scaleStep;
                            props[propID].m_scale = scale > maxScale ? maxScale : scale;
                            EPropManager.UpdateProp(propID);
                        }
                    }
                }
            }
        }

        public static void DecrementPropSize() {
            float scale;
            EPropInstance[] props = EPropManager.m_props.m_buffer;
            PropTool propTool = ToolsModifierControl.GetCurrentTool<PropTool>();
            uint propID = m_currentPropID;
            if (!(propTool is null) && propTool.m_mode == PropTool.Mode.Single && Cursor.visible && propID > 1) {
                scale = props[propID].m_scale - scaleStep;
                props[propID].m_scale = scale < minScale ? minScale : scale;
            } else if ((MoveItTool.ToolState == MoveItTool.ToolStates.Default) &&
                       UIToolOptionPanel.instance.isVisible && Action.selection.Count > 0) {
                foreach (Instance instance in Action.selection) {
                    if (instance is MoveableProp && !instance.id.IsEmpty) {
                        propID = instance.id.GetProp32();
                        if (propID > 0) {
                            scale = props[propID].m_scale - scaleStep;
                            props[propID].m_scale = scale < minScale ? minScale : scale;
                            EPropManager.UpdateProp(propID);
                        }
                    }
                }
            }
        }
    }
}
