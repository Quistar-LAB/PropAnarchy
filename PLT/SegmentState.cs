using UnityEngine;
using static PropAnarchy.PLT.PropLineTool;

namespace PropAnarchy.PLT {
    public static class SegmentState {
        public delegate void EventHandler();
        private static event EventHandler EventLastContinueParameterChanged;
        public struct SegmentInfo {
            //used in non-fence mode
            public float m_lastFinalOffset;
            public float m_newFinalOffset;
            //used in fence mode
            public Vector3 m_lastFenceEndpoint;
            public Vector3 m_newFenceEndpoint;
            //used in both
            public bool m_isContinueDrawing;
            public bool m_keepLastOffsets;
            public bool m_maxItemCountExceeded;
            public bool m_isMaxFillContinue;
            //error checking
            public bool m_allItemsValid;
            public bool IsReadyForMaxContinue => m_maxItemCountExceeded;
            public static SegmentInfo Default => new SegmentInfo(0f, 0f, Vector3.down, Vector3.down, false, false, false, false, true);
            public SegmentInfo(float lastFinalOffset, float newFinalOffset, Vector3 lastFenceEndpoint, Vector3 newFenceEndpoint, bool isContinueDrawing, bool keepLastOffsets, bool maxItemCountExceeded, bool isMaxFillContinue, bool allItemsValid) {
                m_lastFinalOffset = lastFinalOffset;
                m_newFinalOffset = newFinalOffset;
                m_lastFenceEndpoint = lastFenceEndpoint;
                m_newFenceEndpoint = newFenceEndpoint;
                m_isContinueDrawing = isContinueDrawing;
                m_keepLastOffsets = keepLastOffsets;
                m_maxItemCountExceeded = maxItemCountExceeded;
                m_isMaxFillContinue = isMaxFillContinue;
                m_allItemsValid = allItemsValid;
            }
        }
        public static SegmentInfo m_segmentInfo = SegmentInfo.Default;
        public static bool m_pendingPlacementUpdate;

        public static void Reset() {
            m_segmentInfo = SegmentInfo.Default;
            ResetLastContinueParameters();
        }

        public static float NewFinalOffset {
            get => m_segmentInfo.m_newFinalOffset;
            set => m_segmentInfo.m_newFinalOffset = value;
        }

        // === non-fence === 
        public static float LastFinalOffset {
            get => m_segmentInfo.m_lastFinalOffset;
            set {
                if (m_segmentInfo.m_lastFinalOffset != value) {
                    m_segmentInfo.m_lastFinalOffset = value;
                    EventLastContinueParameterChanged?.Invoke();
                }
            }
        }

        // === fence === 
        public static Vector3 LastFenceEndpoint {
            get => m_segmentInfo.m_lastFenceEndpoint;
            set {
                if (m_segmentInfo.m_lastFenceEndpoint != value) {
                    m_segmentInfo.m_lastFenceEndpoint = value;
                    EventLastContinueParameterChanged?.Invoke();
                }
            }
        }

        /// <summary>
        /// Returns true if the curve is currently continue-drawing to fill the same curve because max item count threshold was exceeded.
        /// </summary>
        public static bool IsMaxFillContinue {
            get => m_segmentInfo.m_isMaxFillContinue;
            set {
                if (m_segmentInfo.m_isMaxFillContinue != value) {
                    m_segmentInfo.m_isMaxFillContinue = value;
                    EventLastContinueParameterChanged?.Invoke();
                }
            }
        }

        public static bool KeepLastOffsets {
            get => m_segmentInfo.m_keepLastOffsets;
            set => m_segmentInfo.m_keepLastOffsets = value;
        }

        public static bool MaxItemCountExceeded {
            get => m_segmentInfo.m_maxItemCountExceeded;
            set => m_segmentInfo.m_maxItemCountExceeded = value;
        }

        public static bool IsContinueDrawing {
            get => m_segmentInfo.m_isContinueDrawing;
            set => m_segmentInfo.m_isContinueDrawing = value;
        }

        public static bool AllItemsValid {
            get => m_segmentInfo.m_allItemsValid;
            set => m_segmentInfo.m_allItemsValid = value;
        }

        public static bool IsReadyForMaxContinue => m_segmentInfo.IsReadyForMaxContinue;

        public static void FinalizeForPlacement(bool continueDrawing) {
            if (continueDrawing) {
                if (!m_segmentInfo.m_keepLastOffsets) {
                    LastFenceEndpoint = m_segmentInfo.m_newFenceEndpoint;
                    LastFinalOffset = m_segmentInfo.m_newFinalOffset;
                }
                IsMaxFillContinue = m_segmentInfo.m_maxItemCountExceeded;
            } else {
                LastFenceEndpoint = m_vectorDown;
                LastFinalOffset = 0f;
                IsMaxFillContinue = false;
                m_segmentInfo.m_keepLastOffsets = false;
            }
            m_segmentInfo.m_newFenceEndpoint = m_vectorDown;
            m_segmentInfo.m_newFinalOffset = 0f;
        }

        public static void ResetLastContinueParameters() {
            LastFenceEndpoint = m_vectorDown;
            LastFinalOffset = 0f;
        }

        public static bool UpdatePlacement() => UpdatePlacement(m_segmentInfo.m_isContinueDrawing, m_segmentInfo.m_keepLastOffsets);

        public static bool UpdatePlacement(bool forceContinueDrawing) => UpdatePlacement(forceContinueDrawing, m_segmentInfo.m_keepLastOffsets);

        public static bool UpdatePlacement(bool forceContinueDrawing, bool forceKeepLastOffsets) {
            bool result;
            if (m_isCopyPlacing) {
                m_segmentInfo.m_keepLastOffsets = true;
                result = CalculateAll(true);
            } else {
                m_segmentInfo.m_keepLastOffsets = forceKeepLastOffsets;
                result = CalculateAll(forceContinueDrawing | m_segmentInfo.m_isContinueDrawing);
            }
            m_segmentInfo.m_isContinueDrawing = forceContinueDrawing;
            return result;
        }

        public static void CheckPendingPlacement() {
            if (m_pendingPlacementUpdate) {
                UpdatePlacement();
                m_pendingPlacementUpdate = false;
            }
        }

        private static bool CalculateAll(bool continueDrawing) {
            m_itemCount = MAX_ITEM_ARRAY_LENGTH;   //not sure about setting m_itemCount here, before CalculateAllPositions
            if (CalculateAllPositions(continueDrawing)) {
                DrawMode.CurrentMode.CalculateAllDirections();
                CalculateAllAnglesBase();
                //SetAllItemPrefabInfos();
                //UpdatePlacementErrors();
                return true;
            }
            m_segmentInfo.m_maxItemCountExceeded = false;
            return false;
        }

        private static bool CalculateAllPositions(bool continueDrawing) {
            switch (m_controlMode) {
            case ControlMode.ITEMWISE:
                return CalculateItemwisePosition(ItemInfo.ItemSpacing, 0f /*continueDrawing ? m_segmentInfo.m_lastFinalOffset : 0f*/, continueDrawing ? m_segmentInfo.m_lastFenceEndpoint : ActiveDrawState.m_mainSegment.b);
            case ControlMode.SPACING:
                return CalculateAllPositionsBySpacing(ItemInfo.ItemSpacing, 0f /*continueDrawing ? m_segmentInfo.m_lastFinalOffset : 0f*/, continueDrawing ? m_segmentInfo.m_lastFenceEndpoint : ActiveDrawState.m_mainSegment.b);
            }
            return false;
        }

        private static void CalculateAllAnglesBase() {
            Vector3 xAxis = Vector3.right;
            Vector3 yAxis = Vector3.up;
            int itemCount = m_itemCount;
            if (GetFenceMode()) {
                float offsetAngle = Mathf.Deg2Rad * (((ItemInfo.m_itemModelZ > ItemInfo.m_itemModelX ? Mathf.PI / 2f : 0f) + (Settings.AngleFlip180 ? Mathf.PI : 0f) + ItemInfo.m_itemAngleOffset) * Mathf.Rad2Deg % 360f);
                for (int i = 0; i < itemCount; i++) {
                    m_items[i].m_angle = PLTMath.AngleSigned(m_items[i].m_itemDirection, xAxis, yAxis) + Mathf.PI + offsetAngle;
                }
            } else {
                switch (m_angleMode) {
                case AngleMode.DYNAMIC:
                    float offsetAngle = Mathf.Deg2Rad * (((ItemInfo.m_itemModelZ > ItemInfo.m_itemModelX ? Mathf.PI / 2f : 0f) + (Settings.AngleFlip180 ? Mathf.PI : 0f) + ItemInfo.m_itemAngleOffset) * Mathf.Rad2Deg % 360f);
                    for (int i = 0; i < itemCount; i++) {
                        m_items[i].m_angle = PLTMath.AngleSigned(m_items[i].m_itemDirection, xAxis, yAxis) + Mathf.PI + offsetAngle;
                    }
                    break;
                case AngleMode.SINGLE:
                    float singleAngle = Mathf.Deg2Rad * (((ItemInfo.m_itemModelZ > ItemInfo.m_itemModelX ? Mathf.PI / 2f : 0f) + (Settings.AngleFlip180 ? Mathf.PI : 0f) + ItemInfo.m_itemAngleSingle) * Mathf.Rad2Deg % 360f);
                    for (int i = 0; i < itemCount; i++) {
                        m_items[i].m_angle = singleAngle;
                    }
                    break;
                }
            }
        }

        private static bool CalculateItemwisePosition(float fencePieceLength, float initialOffset, Vector3 lastFenceEndpoint) {
            m_itemCount = 1;
            return DrawMode.CurrentMode.CalculateItemwisePosition(fencePieceLength, initialOffset, lastFenceEndpoint);
        }

        private static bool CalculateAllPositionsBySpacing(float spacing, float initialOffset, Vector3 lastFenceEndpoint) {
            int drawMode = DrawMode.Current;
            initialOffset = Mathf.Abs(initialOffset);
            if (spacing == 0 || !DrawMode.CurrentMode.IsLengthLongEnough() || ((drawMode == DrawMode.CURVED || drawMode == DrawMode.FREEFORM) && GetFenceMode() && m_mainElbowAngle * Mathf.Rad2Deg < 5f)) {
                m_itemCount = 0;
                return false;
            }
            m_itemCount = DrawMode.CurrentMode.CalculateAllPositionsBySpacing(spacing, initialOffset, lastFenceEndpoint);
            return true;
        }

        public static void RevertLastContinueParameters(float lastFinalOffsetValue, ref Vector3 lastFenceEndpointVector) {
            m_segmentInfo.m_keepLastOffsets = false;
            LastFinalOffset = lastFinalOffsetValue;
            LastFenceEndpoint = lastFenceEndpointVector;
        }

        public static bool AreLastContinueParametersZero() => (m_segmentInfo.m_lastFenceEndpoint == m_vectorDown || m_segmentInfo.m_lastFenceEndpoint == m_vectorZero) && m_segmentInfo.m_lastFinalOffset == 0f;

        public static bool AreNewContinueParametersEmpty() => (m_segmentInfo.m_newFenceEndpoint == m_vectorDown || m_segmentInfo.m_newFenceEndpoint == m_vectorZero) && m_segmentInfo.m_newFinalOffset == 0f;

        public static bool IsPositionEqualToLastFenceEndpoint(Vector3 position) => Vector3.Distance(position, m_segmentInfo.m_lastFenceEndpoint) <= 0.002f;
    }
}
