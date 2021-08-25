using System.Runtime.CompilerServices;
using ColossalFramework;
using ColossalFramework.Math;
using UnityEngine;
using MoveIt;

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

        private float CalculateCustomScale(float val, uint treeID) {
            float[] propScales = m_propScales;
            float scale = val + propScales[treeID];
            if (scale > maxScale) propScales[treeID] -= scaleStep;
            else if (scale < minScale) propScales[treeID] += scaleStep;
            return val + propScales[treeID];
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float CalcPropScale(ref Randomizer randomizer, uint propID, PropInfo propInfo) => instance.CalcPropScaleImpl(ref randomizer, propID, propInfo);

        private float CalcPropScaleImpl(ref Randomizer randomizer, uint propID, PropInfo propInfo) =>
            CalculateCustomScale(propInfo.m_minScale + randomizer.Int32(10000u) * (propInfo.m_maxScale - propInfo.m_minScale) * 0.0001f, propID);

        public static float GetSeedPropScale(ref Randomizer randomizer, uint propID, PropInfo propInfo) {
            if (propInfo is null) return 0;
            instance.m_currentPropID = propID;
            return instance.CalculateCustomScale(propInfo.m_minScale + randomizer.Int32(10000u) * (propInfo.m_maxScale - propInfo.m_minScale) * 0.0001f, propID);
        }

        public void IncrementPropSize() {
            PropTool propTool = ToolsModifierControl.GetCurrentTool<PropTool>();
            uint propID = m_currentPropID;
            if (propTool is not null && propTool.m_mode == PropTool.Mode.Single && Cursor.visible && propID > 1) {
                m_propScales[propID] += scaleStep;
            } else if ((MoveItTool.ToolState == MoveItTool.ToolStates.Default) &&
                       UIToolOptionPanel.instance.isVisible && Action.selection.Count > 0) {
                foreach (Instance instance in Action.selection) {
                    if (instance is MoveableProp && !instance.id.IsEmpty && instance.id.Prop > 0) {
                        m_propScales[instance.id.Prop] += scaleStep;
                    }
                }
            }
        }

        public void DecrementPropSize() {
            PropTool propTool = ToolsModifierControl.GetCurrentTool<PropTool>();
            uint propID = m_currentPropID;
            if (propTool is not null && propTool.m_mode == PropTool.Mode.Single && Cursor.visible && propID > 1) {
                m_propScales[propID] -= scaleStep;
            } else if ((MoveItTool.ToolState == MoveItTool.ToolStates.Default) &&
                       UIToolOptionPanel.instance.isVisible && Action.selection.Count > 0) {
                foreach (Instance instance in Action.selection) {
                    if (instance is MoveableTree && !instance.id.IsEmpty && instance.id.Tree > 0) {
                        m_propScales[instance.id.Tree] -= scaleStep;
                    }
                }
            }
        }
    }
}
