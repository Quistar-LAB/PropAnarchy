using ColossalFramework;
using EManagersLib;
using MoveIt;
using UnityEngine;
using Action = MoveIt.Action;

namespace PropAnarchy {
    public partial class PAManager : SingletonLite<PAManager> {
        public static void ToggleTerrainConform() {
            EPropInstance[] props = EPropManager.m_props.m_buffer;
            PropTool propTool = ToolsModifierControl.GetCurrentTool<PropTool>();
            uint propID = m_currentPropID;
            if (!(propTool is null) && propTool.m_mode == PropTool.Mode.Single && Cursor.visible && propID > 1) {
                props[propID].Conformed = !props[propID].Conformed;
            } else if ((MoveItTool.ToolState == MoveItTool.ToolStates.Default) &&
                       UIToolOptionPanel.instance.isVisible && Action.selection.Count > 0) {
                foreach (Instance instance in Action.selection) {
                    if (instance is MoveableProp && !instance.id.IsEmpty) {
                        propID = instance.id.GetProp32();
                        if (propID > 0) {
                            props[propID].Conformed = !props[propID].Conformed;
                        }
                    }
                }
            }
        }
    }
}
