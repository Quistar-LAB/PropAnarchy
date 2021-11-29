using ColossalFramework.Math;
using UnityEngine;
using static PropAnarchy.PLT.PropLineTool;

namespace PropAnarchy.PLT {
    public enum ActiveState : int {
        Undefined = 0,
        CreatePointFirst = 1,
        CreatePointSecond = 2,
        CreatePointThird = 3,
        LockIdle = 10,
        MovePointFirst = 11,
        MovePointSecond = 12,
        MovePointThird = 13,
        MoveSegment = 14,
        ChangeSpacing = 15,
        ChangeAngle = 16,
        ItemwiseLock = 30,
        MoveItemwiseItem = 31,
        MaxFillContinue = 40
    }

    public abstract class ActiveDrawState {
        protected const int LEFTMOUSEBUTTON = 0;
        protected const int RIGHTMOUSEBUTTON = 1;
        public static ActiveState m_currentState;
        public static Segment3 m_mainSegment = new Segment3();
        public static SegmentXZ m_mainArm1 = new SegmentXZ();
        public static SegmentXZ m_mainArm2 = new SegmentXZ();
        public static Bezier3 m_mainBezier = new Bezier3();
        public static CircleXZ m_mainCircle = new CircleXZ();
        public virtual void OnToolGUI(Event e, bool isInsideUI) {
            m_isCopyPlacing = m_keyboardAltDown = (e.modifiers & EventModifiers.Alt) == EventModifiers.Alt;
            m_keyboardCtrlDown = (e.modifiers & EventModifiers.Control) == EventModifiers.Control;
        }
        public virtual void OnToolUpdate() { }
        public abstract void OnToolLateUpdate();
        public abstract void OnRenderGeometry(RenderManager.CameraInfo cameraInfo);
        public abstract bool OnRenderOverlay(RenderManager.CameraInfo cameraInfo, ActiveState curState, Color createPointColor, Color curveWarningColor, Color copyPlaceColor);
        public abstract void OnSimulationStep(Vector3 mousePosition);
        public abstract void RenderLines(RenderManager.CameraInfo cameraInfo, Color createPointColor, Color curveWarningColor);
        public abstract bool ContinueDrawing(ControlPoint.PointInfo[] controlPoints, ref int controlPointCount);
        public abstract bool IsLengthLongEnough();
        public abstract bool IsActiveStateAnItemRenderState();
        public bool PostCheckAndContinue() => PostCheckAndContinue(ControlPoint.m_controlPoints, ref ControlPoint.m_validPoints);
        public abstract bool PostCheckAndContinue(ControlPoint.PointInfo[] controlPoints, ref int controlPointCount);
        public void UpdateCurve() => UpdateCurve(ControlPoint.m_controlPoints, ControlPoint.m_validPoints);
        public abstract void UpdateCurve(ControlPoint.PointInfo[] controlPoints, int controlPointCount);
        public abstract void RevertDrawingFromLockMode();
        public abstract void Update();
        public bool UpdatePlacement() => UpdatePlacement(SegmentState.m_segmentInfo.m_isContinueDrawing, SegmentState.m_segmentInfo.m_keepLastOffsets);
        public bool UpdatePlacement(bool forceContinueDrawing) => UpdatePlacement(forceContinueDrawing, SegmentState.m_segmentInfo.m_keepLastOffsets);
        public bool UpdatePlacement(bool forceContinueDrawing, bool forceKeepLastOffsets) {
            bool result;
            if (m_isCopyPlacing) {
                SegmentState.m_segmentInfo.m_keepLastOffsets = true;
                result = CalculateAll(true);
            } else {
                SegmentState.m_segmentInfo.m_keepLastOffsets = forceKeepLastOffsets;
                result = CalculateAll(forceContinueDrawing | SegmentState.m_segmentInfo.m_isContinueDrawing);
            }
            SegmentState.m_segmentInfo.m_isContinueDrawing = forceContinueDrawing;
            return result;
        }
        private bool CalculateAll(bool continueDrawing) {
            bool fenceMode = GetFenceMode();
            ItemInfo[] items = m_items;
            Vector3[] fenceEndPoints = m_fenceEndPoints;
            m_itemCount = MAX_ITEM_ARRAY_LENGTH;   //not sure about setting m_itemCount here, before CalculateAllPositions
            if (CalculateAllPositions(continueDrawing, fenceMode, items, fenceEndPoints)) {
                CalculateAllDirections(items, fenceEndPoints, fenceMode);
                CalculateAllAnglesBase(items, fenceMode);
                //UpdatePlacementErrors();
                return true;
            }
            SegmentState.m_segmentInfo.m_maxItemCountExceeded = false;
            return false;
        }
        private void CalculateAllAnglesBase(ItemInfo[] items, bool fenceMode) {
            Vector3 xAxis = m_vectorRight;
            Vector3 yAxis = m_vectorUp;
            int itemCount = m_itemCount;
            if (fenceMode) {
                float offsetAngle = Mathf.Deg2Rad * (((ItemInfo.m_itemModelZ > ItemInfo.m_itemModelX ? Mathf.PI / 2f : 0f) + (Settings.AngleFlip180 ? Mathf.PI : 0f) + ItemInfo.m_itemAngleOffset) * Mathf.Rad2Deg % 360f);
                for (int i = 0; i < itemCount; i++) {
                    items[i].m_angle = Vector3Extensions.AngleSigned(items[i].m_itemDirection, xAxis, yAxis) + Mathf.PI + offsetAngle;
                }
            } else {
                switch (m_angleMode) {
                case AngleMode.DYNAMIC:
                    float offsetAngle = Mathf.Deg2Rad * (((ItemInfo.m_itemModelZ > ItemInfo.m_itemModelX ? Mathf.PI / 2f : 0f) + (Settings.AngleFlip180 ? Mathf.PI : 0f) + ItemInfo.m_itemAngleOffset) * Mathf.Rad2Deg % 360f);
                    for (int i = 0; i < itemCount; i++) {
                        items[i].m_angle = Vector3Extensions.AngleSigned(items[i].m_itemDirection, xAxis, yAxis) + Mathf.PI + offsetAngle;
                    }
                    break;
                case AngleMode.SINGLE:
                    float singleAngle = Mathf.Deg2Rad * (((ItemInfo.m_itemModelZ > ItemInfo.m_itemModelX ? Mathf.PI / 2f : 0f) + (Settings.AngleFlip180 ? Mathf.PI : 0f) + ItemInfo.m_itemAngleSingle) * Mathf.Rad2Deg % 360f);
                    for (int i = 0; i < itemCount; i++) {
                        items[i].m_angle = singleAngle;
                    }
                    break;
                }
            }
        }
        private bool CalculateAllPositions(bool continueDrawing, bool fenceMode, ItemInfo[] items, Vector3[] fenceEndPoints) {
            switch (m_controlMode) {
            case ControlMode.ITEMWISE:
                if (continueDrawing) return CalculateItemwisePosition(items, fenceEndPoints, fenceMode, ItemInfo.ItemSpacing, SegmentState.LastFinalOffset, SegmentState.LastFenceEndpoint);
                return CalculateItemwisePosition(items, fenceEndPoints, fenceMode, ItemInfo.ItemSpacing, 0f, m_mainSegment.b);
            case ControlMode.SPACING:
                if (continueDrawing) return CalculateAllPositionsBySpacing(items, fenceEndPoints, fenceMode, ItemInfo.ItemSpacing, SegmentState.LastFinalOffset, SegmentState.LastFenceEndpoint);
                return CalculateAllPositionsBySpacing(items, fenceEndPoints, fenceMode, ItemInfo.ItemSpacing, 0f, m_mainSegment.b);
            }
            return false;
        }
        public void CheckPendingPlacement() {
            if (SegmentState.m_pendingPlacementUpdate) {
                UpdateCurve();
                UpdatePlacement();
                SegmentState.m_pendingPlacementUpdate = false;
            }
        }
        public abstract void CalculateAllDirections(ItemInfo[] items, Vector3[] fenceEndPoints, bool fenceMode);
        public abstract bool CalculateItemwisePosition(ItemInfo[] items, Vector3[] fenceEndPoints, bool fenceMode, float fencePieceLength, float initialOffset, VectorXZ lastFenceEndpoint);
        public abstract bool CalculateAllPositionsBySpacing(ItemInfo[] items, Vector3[] fenceEndPoints, bool fenceMode, float spacing, float initialOffset, VectorXZ lastFenceEndpoint);
        public abstract void DiscoverHoverState(VectorXZ position);
        public abstract void UpdateMiscHoverParameters();
        public abstract void RenderProgressiveSpacingFill(RenderManager.CameraInfo cameraInfo, float fillLength, float interval, float size, Color color, bool renderLimits, bool alphaBlend);
        public void RenderMaxFillContinueMarkers(RenderManager.CameraInfo cameraInfo) {
            const float radius = 6f;
            if (m_controlMode == ControlMode.ITEMWISE) return;
            Color maxFillContinueColor = Settings.m_PLTColor_MaxFillContinue;
            //initial item
            Vector3 initialItemPosition = GetFenceMode() ? m_fenceEndPoints[0] : m_items[0].Position;
            RenderSegment(cameraInfo, new Segment3(initialItemPosition - (m_items[0].m_offsetDirection * radius), initialItemPosition + (m_items[0].m_offsetDirection * radius)), 0.25f, 0f, maxFillContinueColor, false, true);
            //final item
            Vector3 finalItemPosition = GetFenceMode() ? m_fenceEndPoints[m_itemCount - 1] : m_items[m_itemCount - 1].Position;
            RenderCircle(cameraInfo, finalItemPosition, 0.5f, maxFillContinueColor, false, true);
            RenderCircle(cameraInfo, finalItemPosition, radius, maxFillContinueColor, false, true);
            RenderSegment(cameraInfo, new Segment3(finalItemPosition - (m_items[m_itemCount - 1].m_offsetDirection * radius), finalItemPosition + (m_items[m_itemCount - 1].m_offsetDirection * radius)), 0.25f, 0f, maxFillContinueColor, false, true);
            //mouse indicators
            maxFillContinueColor.a *= 0.40f;
            RenderSegment(cameraInfo, new Segment3(m_mousePosition, initialItemPosition), 0.05f, 3.00f, maxFillContinueColor, false, true);
            RenderSegment(cameraInfo, new Segment3(m_mousePosition, finalItemPosition), 0.05f, 3.00f, maxFillContinueColor, false, true);
        }

        public void ResetDrawState() {
            m_currentState = ActiveState.CreatePointFirst;
        }
    }
}
