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
        private const int LEFTMOUSEBUTTON = 0;
        private const int RIGHTMOUSEBUTTON = 1;
        public static ActiveState m_currentState;
        public static Segment3 m_mainSegment;

        public virtual void OnRenderGeometry(RenderManager.CameraInfo cameraInfo) { }
        public virtual void RenderLines(RenderManager.CameraInfo cameraInfo, ref Color createPointColor, ref Color curveWarningColor) { }
        public virtual bool OnRenderOverlay(RenderManager.CameraInfo cameraInfo, Event e, ActiveState curState, ref Color createPointColor, ref Color curveWarningColor, ref Color copyPlaceColor) { return false; }
        public bool OnDefaultToolGUI(Event e, out bool leftMouseDown, out bool rightMouseDown, out bool altDown, out bool ctrlDown) {
            bool result = true;
            ctrlDown = false;
            altDown = false;
            switch (e.modifiers) {
            case EventModifiers.Alt: altDown = true; break;
            case EventModifiers.Control: ctrlDown = true; break;
            }
            m_keyboardCtrlDown = ctrlDown;
            m_keyboardAltDown = altDown;
            m_isCopyPlacing = altDown;
            switch (e.type) {
            case EventType.MouseDown:
                switch (e.button) {
                case LEFTMOUSEBUTTON: m_mouseLeftDown = true; break;
                case RIGHTMOUSEBUTTON: m_mouseRightDown = true; break;
                }
                break;
            case EventType.MouseUp:
                switch (e.button) {
                case LEFTMOUSEBUTTON: m_mouseLeftDown = false; break;
                case RIGHTMOUSEBUTTON: m_mouseRightDown = false; break;
                }
                break;
            case EventType.KeyDown:
                switch (e.keyCode) {
                case KeyCode.Z:
                    if (ctrlDown) {
                        // perform undo
                    }
                    break;
                case KeyCode.Escape:
                    ResetPLT();
                    result = false;
                    break;
                }
                break;
            }
            leftMouseDown = m_mouseLeftDown;
            rightMouseDown = m_mouseRightDown;
            return result;
        }
        public virtual void OnToolGUI(Event e, bool isInsideUI) { }
        public virtual void OnToolUpdate() { }
        public virtual void OnToolLateUpdate() { }
        public virtual void OnSimulationStep() { }
        public virtual bool ContinueDrawing(ControlPoint.PointInfo[] controlPoints, ref int controlPointCount) { return false; }
        public virtual bool IsLengthLongEnough() { return false; }
        public virtual bool PostCheckAndContinue() => PostCheckAndContinue(ControlPoint.m_controlPoints, ControlPoint.m_cachedControlPoints, ref ControlPoint.m_validPoints);
        public virtual bool PostCheckAndContinue(ControlPoint.PointInfo[] controlPoints, ControlPoint.PointInfo[] cachedControlPoints, ref int controlPointCount) { return false; }
        public virtual void UpdateCurve() => UpdateCurve(ControlPoint.m_controlPoints, ControlPoint.m_validPoints);
        public virtual void UpdateCurve(ControlPoint.PointInfo[] cachedControlPoints, int cachedControlPointCount) { }
        public virtual void RevertDrawingFromLockMode() { }
        public virtual void CalculateAllDirections() { }
        public virtual bool CalculateItemwisePosition(float fencePieceLength, float initialOffset, Vector3 lastFenceEndpoint) { return false; }
        public virtual int CalculateAllPositionsBySpacing(float spacing, float initialOffset, Vector3 lastFenceEndpoint) { return 0; }
        public virtual void DiscoverHoverState(Vector3 position) { }
        public virtual void UpdateMiscHoverParameters() { }
        public virtual void RenderProgressiveSpacingFill(RenderManager.CameraInfo cameraInfo, float fillLength, float interval, float size, Color color, bool renderLimits, bool alphaBlend) { }
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

        public ActiveState CurrentState => m_currentState;

        public void ResetDrawState() {
            m_currentState = ActiveState.CreatePointFirst;
        }
    }
}
