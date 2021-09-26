using ColossalFramework;
using ColossalFramework.Math;
using MoveIt;
using System.Runtime.CompilerServices;
using UnityEngine;
using EManagersLib.API;

namespace PropAnarchy {
    public partial class PAManager : SingletonLite<PAManager> {
        public const float minScale = 0.2f;
        public const float maxScale = 5.0f;
        public const float scaleStep = 0.2f;
        public uint m_currentPropID = 0;
        public float[] m_propScales;

        public void SetScaleBuffer(int maxSize) {
            m_propScales = new float[maxSize];
        }

        private float CalculateCustomScale(float val, uint propID) {
            float[] propScales = m_propScales;
            float scale = val + propScales[propID];
            if (scale > maxScale) propScales[propID] -= scaleStep;
            else if (scale < minScale) propScales[propID] += scaleStep;
            return val + propScales[propID];
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float CalcPropScale(ref Randomizer randomizer, uint propID, PropInfo propInfo) =>
            instance.CalculateCustomScale(propInfo.m_minScale + randomizer.Int32(10000u) * (propInfo.m_maxScale - propInfo.m_minScale) * 0.0001f, propID);

        public static float GetSeedPropScale(ref Randomizer randomizer, uint propID, PropInfo propInfo) {
            if (propInfo is null) return 0;
            instance.m_currentPropID = propID;
            return instance.CalculateCustomScale(propInfo.m_minScale + randomizer.Int32(10000u) * (propInfo.m_maxScale - propInfo.m_minScale) * 0.0001f, propID);
        }

        public void IncrementPropSize() {
            PropTool propTool = ToolsModifierControl.GetCurrentTool<PropTool>();
            uint propID = m_currentPropID;
            if (!(propTool is null) && propTool.m_mode == PropTool.Mode.Single && Cursor.visible && propID > 1) {
                m_propScales[propID] += scaleStep;
            } else if ((MoveItTool.ToolState == MoveItTool.ToolStates.Default) &&
                       UIToolOptionPanel.instance.isVisible && Action.selection.Count > 0) {
                foreach (Instance instance in Action.selection) {
                    if (instance is MoveableProp && !instance.id.IsEmpty && instance.id.GetProp32() > 0) {
                        m_propScales[instance.id.GetProp32()] += scaleStep;
                    }
                }
            }
        }

        public void DecrementPropSize() {
            PropTool propTool = ToolsModifierControl.GetCurrentTool<PropTool>();
            uint propID = m_currentPropID;
            if (!(propTool is null) && propTool.m_mode == PropTool.Mode.Single && Cursor.visible && propID > 1) {
                m_propScales[propID] -= scaleStep;
            } else if ((MoveItTool.ToolState == MoveItTool.ToolStates.Default) &&
                       UIToolOptionPanel.instance.isVisible && Action.selection.Count > 0) {
                foreach (Instance instance in Action.selection) {
                    if (instance is MoveableTree && !instance.id.IsEmpty && instance.id.GetProp32() > 0) {
                        m_propScales[instance.id.GetProp32()] -= scaleStep;
                    }
                }
            }
        }
    }
}
