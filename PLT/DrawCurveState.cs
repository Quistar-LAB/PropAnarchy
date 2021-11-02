using ColossalFramework.Math;
using System;
using UnityEngine;
using static PropAnarchy.PLT.PropLineTool;

namespace PropAnarchy.PLT {
    public class DrawCurveState : ActiveDrawState {
        private const float TOLERANCE = 0.001f;
        public static Bezier3 m_mainBezier;
        public static Segment3 m_mainArm1;
        public static Segment3 m_mainArm2;

        public DrawCurveState() : base() {
            m_mainBezier = new Bezier3();
            m_mainArm1 = new Segment3();
            m_mainArm2 = new Segment3();
        }

        public override void OnToolGUI(Event e, bool isInsideUI) {
            if (!OnDefaultToolGUI(e, out bool leftMouseDown, out bool rightMouseDown, out bool altDown, out bool ctrlDown)) return;
            ActiveState currentState = m_currentState;
            if (!isInsideUI && leftMouseDown && altDown) {
                switch (currentState) {
                case ActiveState.CreatePointThird:
                case ActiveState.LockIdle:
                case ActiveState.MovePointFirst:
                case ActiveState.MovePointSecond:
                case ActiveState.MovePointThird:
                case ActiveState.MoveSegment:
                case ActiveState.ChangeSpacing:
                case ActiveState.ChangeAngle:
                case ActiveState.ItemwiseLock:
                case ActiveState.MoveItemwiseItem:
                case ActiveState.MaxFillContinue:
                    AddAction(CreateItems(true, true));
                    return;
                }
            }
        }

        public override void OnRenderGeometry(RenderManager.CameraInfo cameraInfo) {
            base.OnRenderGeometry(cameraInfo);
        }

        public override void RenderLines(RenderManager.CameraInfo cameraInfo, ref Color createPointColor, ref Color curveWarningColor) {
            Color lockIdleColor = Settings.m_PLTColor_locked;
            RenderElbow(cameraInfo, m_mainArm1, m_mainArm2, 1.00f, 2f, lockIdleColor, false, true);
            if (m_hoverState == HoverState.SpacingLocus) {
                RenderBezier(cameraInfo, m_mainBezier, 1.00f, lockIdleColor, false, true);
            } else {
                RenderBezier(cameraInfo, m_mainBezier, 1.00f, lockIdleColor, false, false);
            }
            if (!SegmentState.AllItemsValid) {
                RenderBezier(cameraInfo, m_mainBezier, 1.50f, curveWarningColor, false, true);
            }
        }

        public override bool OnRenderOverlay(RenderManager.CameraInfo cameraInfo, Event e, ActiveState curState, ref Color createPointColor, ref Color curveWarningColor, ref Color copyPlaceColor) {
            ControlPoint.PointInfo[] cachedControlPoints = ControlPoint.m_cachedControlPoints;
            switch (curState) {
            case ActiveState.CreatePointSecond:
                RenderLine(cameraInfo, m_mainArm1, 1.00f, 2f, createPointColor, false, false);
                RenderCircle(cameraInfo, cachedControlPoints[0].m_position, 0.10f, createPointColor, false, true);
                RenderCircle(cameraInfo, cachedControlPoints[1].m_position, 0.10f, createPointColor, false, true);
                break;
            case ActiveState.CreatePointThird:
            case ActiveState.MaxFillContinue:
                if ((e.modifiers & EventModifiers.Alt) == EventModifiers.Alt) {
                    createPointColor = copyPlaceColor;
                } else if ((e.modifiers & EventModifiers.Control) == EventModifiers.Control) {
                    createPointColor = Settings.m_PLTColor_locked;
                }
                if (!SegmentState.AllItemsValid) {
                    RenderBezier(cameraInfo, m_mainBezier, 1.50f, curveWarningColor, false, true);
                }
                //for the size for these it should be 1/4 the size for renderline
                RenderElbow(cameraInfo, m_mainArm1, m_mainArm2, 1.00f, 2f, createPointColor, false, true);
                RenderBezier(cameraInfo, m_mainBezier, 1.00f, createPointColor, false, true);
                //MaxFillContinue
                if (SegmentState.IsReadyForMaxContinue || SegmentState.IsMaxFillContinue) {
                    RenderMaxFillContinueMarkers(cameraInfo);
                }
                RenderCircle(cameraInfo, cachedControlPoints[0].m_position, 0.10f, createPointColor, false, true);
                RenderCircle(cameraInfo, cachedControlPoints[1].m_position, 0.10f, createPointColor, false, true);
                RenderCircle(cameraInfo, cachedControlPoints[2].m_position, 0.10f, createPointColor, false, true);
                break;
            }
            return true;
        }

        public override void OnSimulationStep() {
            base.OnSimulationStep();
        }

        public override void OnToolLateUpdate() {
            base.OnToolLateUpdate();
        }

        public override void OnToolUpdate() {
            base.OnToolUpdate();
        }

        public override bool ContinueDrawing(ControlPoint.PointInfo[] controlPoints, ref int controlPointCount) {
            return base.ContinueDrawing(controlPoints, ref controlPointCount);
        }

        public override bool PostCheckAndContinue(ControlPoint.PointInfo[] controlPoints, ControlPoint.PointInfo[] cachedControlPoints, ref int controlPointCount) {
            return base.PostCheckAndContinue(controlPoints, cachedControlPoints, ref controlPointCount);
        }

        public override void UpdateCurve(ControlPoint.PointInfo[] points, int pointCount) {
            SegmentState.m_pendingPlacementUpdate = true;
            if (pointCount >= 1) {
                m_mainArm1.a = points[0].m_position;
                m_mainArm1.b = points[1].m_position;
            }
            if (pointCount >= 2) {
                m_mainBezier = PLTMath.QuadraticToCubicBezierCOMethod(points[0].m_position, points[1].m_direction, points[2].m_position, (-points[2].m_direction));
                m_mainArm2.a = points[1].m_position;
                m_mainArm2.b = points[2].m_position;
                //***SUPER-IMPORTANT (for convergence of fenceMode)***
                PLTMath.BezierXZ(ref m_mainBezier);
                //calculate direction here in case controlPoint direction was not set correctly
                Vector3 dirArm1 = (m_mainArm1.b - m_mainArm1.a);
                dirArm1.y = 0f;
                dirArm1.Normalize();
                Vector3 dirArm2 = (m_mainArm2.b - m_mainArm2.a);
                dirArm2.y = 0f;
                dirArm2.Normalize();
                m_mainElbowAngle = Math.Abs(PLTMath.AngleSigned(-dirArm1, dirArm2, Vector3.up));
            }
        }

        public override bool IsLengthLongEnough() => (m_mainBezier.d - m_mainBezier.a).magnitude >= ItemInfo.ItemSpacing;

        public override void RevertDrawingFromLockMode() {
            GoToActiveState(ActiveState.CreatePointThird);
            ControlPoint.Modify(ref m_mousePosition, 3); //update position of second point
            DrawMode.CurrentMode.UpdateCurve();
            SegmentState.UpdatePlacement(false, false);
        }

        public override void CalculateAllDirections() {
            if (GetFenceMode()) {
                for (int i = 0; i < m_itemCount; i++) {
                    m_items[i].SetDirectionsXZ(m_fenceEndPoints[i + 1] - m_fenceEndPoints[i]);
                }
            } else {
                for (int i = 0; i < m_itemCount; i++) {
                    m_items[i].SetDirectionsXZ(m_mainBezier.Tangent(m_items[i].m_t));
                }
            }
        }

        public override bool CalculateItemwisePosition(float fencePieceLength, float initialOffset, Vector3 lastFenceEndpoint) {
            float hoverItemwiseT = m_hoverItemwiseT;
            if (GetFenceMode()) {
                Vector3 positionStart, positionEnd;
                if (fencePieceLength > PLTMath.CubicBezierArcLengthXZGauss12(m_mainBezier, 0f, 1f)) {
                    m_itemCount = 0;
                    return false;
                }
                PLTMath.CircleCurveFenceIntersectXZ(m_mainBezier, hoverItemwiseT, fencePieceLength, TOLERANCE, out float itemTEnd, false);
                //check if out of bounds
                if (itemTEnd > 1f) {
                    //out of bounds? -> attempt to snap to d-end of curve
                    //invert the curve to go "backwards"
                    itemTEnd = 0f;
                    Bezier3 inverseBezier = m_mainBezier.Invert();
                    if (!PLTMath.CircleCurveFenceIntersectXZ(inverseBezier, itemTEnd, fencePieceLength, TOLERANCE, out hoverItemwiseT, false)) {
                        //failed to snap to d-end of curve
                        m_itemCount = 0;
                        return false;
                    } else {
                        hoverItemwiseT = 1f - hoverItemwiseT;
                        itemTEnd = 1f - itemTEnd;
                    }
                }
                m_fenceEndPoints[0] = positionStart = m_mainBezier.Position(hoverItemwiseT);
                m_fenceEndPoints[1] = positionEnd = m_mainBezier.Position(itemTEnd);
                m_items[0].Position = Vector3.Lerp(positionStart, positionEnd, 0.50f);
                return true;
            }
            m_items[0].m_t = hoverItemwiseT;
            m_items[0].Position = m_mainBezier.Position(hoverItemwiseT);
            return true;
        }

        public override int CalculateAllPositionsBySpacing(float spacing, float initialOffset, Vector3 lastFenceEndpoint) {
            int numItems, numItemsRaw;
            float t, finalT;
            if (GetFenceMode()) {
                float lengthFull = PLTMath.CubicBezierArcLengthXZGauss12(m_mainBezier, 0f, 1f);
                float lengthAfterFirst = SegmentState.IsMaxFillContinue ? lengthFull - initialOffset : lengthFull;
                numItems = Mathf.Min(m_itemCount, Mathf.Clamp(Mathf.CeilToInt(lengthAfterFirst / spacing), 0, MAX_ITEM_ARRAY_LENGTH));
                if (spacing > lengthFull) {
                    m_itemCount = 0;
                    return 0;
                }
                t = 0f;
                float penultimateT = 0f;
                int forLoopStart = 0;
                //max fill continue
                if (SegmentState.IsMaxFillContinue && initialOffset > 0f) {
                    forLoopStart = 0;
                    PLTMath.StepDistanceCurve(m_mainBezier, 0f, initialOffset, TOLERANCE, out t);
                    goto label_endpointsForLoop;
                }
                //link curves in continuous draw
                else if (initialOffset > 0f && lastFenceEndpoint != Vector3.down) {
                    //first continueDrawing if (1/4)
                    m_fenceEndPoints[0] = lastFenceEndpoint;
                    //second continueDrawing if (2/4)
                    if (!PLTMath.LinkCircleCurveFenceIntersectXZ(m_mainBezier, lastFenceEndpoint, spacing, TOLERANCE, out t, false)) {
                        //could not link segments, so start at t = 0 instead
                        forLoopStart = 0;
                        t = 0f;
                        goto label_endpointsForLoop;
                    }
                    //third continueDrawing if (3/4)
                    m_fenceEndPoints[1] = m_mainBezier.Position(t);
                    //fourth continueDrawing if (4/4)
                    if (!PLTMath.CircleCurveFenceIntersectXZ(m_mainBezier, t, spacing, TOLERANCE, out t, false)) {
                        //failed to converge
                        numItems = 1;
                        goto label_endpointsFinish;
                    }
                    forLoopStart = 2;
                }
label_endpointsForLoop:
                for (int i = forLoopStart; i < numItems + 1; i++) {
                    //this should be the first if (1/3)
                    //this is necessary for bendy fence mode since we didn't estimate count
                    if (t > 1f) {
                        numItems = i - 1;
                        break;
                    }
                    //second if (2/3)
                    m_fenceEndPoints[i] = m_mainBezier.Position(t);
                    penultimateT = t;
                    //third if (3/3)
                    if (!PLTMath.CircleCurveFenceIntersectXZ(m_mainBezier, t, spacing, TOLERANCE, out t, false)) {
                        //failed to converge
                        numItems = i - 1;
                        break;
                    }
                }
label_endpointsFinish:
                numItems = Mathf.Clamp(numItems, 0, MAX_ITEM_ARRAY_LENGTH);
                //then calculate midpoints
                for (int i = 0; i < numItems; i++) {
                    m_items[i].Position = Vector3.Lerp(m_fenceEndPoints[i], m_fenceEndPoints[i + 1], 0.50f);
                }
                //prep for MaxFillContinue
                if (SegmentState.IsReadyForMaxContinue) {
                    SegmentState.NewFinalOffset = PLTMath.CubicBezierArcLengthXZGauss12(m_mainBezier, 0f, penultimateT);
                } else {
                    SegmentState.NewFinalOffset = PLTMath.CubicBezierArcLengthXZGauss04(m_mainBezier, t, 1f);
                }
                return numItems;
            }
            if (m_mainArm1.Length() + m_mainArm2.Length() <= 0.01f) {
                return 0;
            }
            //use ceiling for non-fence, because the point at the beginning is an extra point
            numItemsRaw = Mathf.CeilToInt((PLTMath.CubicBezierArcLengthXZGauss12(m_mainBezier, 0f, 1f) - initialOffset) / spacing);
            numItems = Mathf.Min(m_itemCount, Mathf.Clamp(numItemsRaw, 0, MAX_ITEM_ARRAY_LENGTH));
            t = 0f;
            if (initialOffset > 0f) {
                PLTMath.StepDistanceCurve(m_mainBezier, 0f, initialOffset, TOLERANCE, out t);
            }
            for (int i = 0; i < numItems; i++) {
                m_items[i].m_t = t;
                m_items[i].Position = m_mainBezier.Position(t);
                PLTMath.StepDistanceCurve(m_mainBezier, t, spacing, TOLERANCE, out t);
            }
            finalT = m_items[numItems - 1].m_t;
            if (SegmentState.IsReadyForMaxContinue) {
                SegmentState.NewFinalOffset = spacing + PLTMath.CubicBezierArcLengthXZGauss12(m_mainBezier, 0f, finalT);
            } else {
                SegmentState.NewFinalOffset = spacing - PLTMath.CubicBezierArcLengthXZGauss04(m_mainBezier, finalT, 1f);
            }
            return numItems;
        }

        public override void DiscoverHoverState(Vector3 position) {
            ActiveState currentState = m_currentState;
            //check for itemwise first before classic lock mode
            if (m_controlMode == ControlMode.ITEMWISE && (currentState == ActiveState.ItemwiseLock || currentState == ActiveState.MoveItemwiseItem)) {
                if (PLTMath.IsCloseToCurveXZ(m_mainBezier, HOVER_ITEMWISE_CURVEDISTANCE_THRESHOLD, position, out float hoverItemT)) {
                    m_hoverItemwiseT = hoverItemT;
                    m_hoverState = HoverState.ItemwiseItem;
                } else {
                    m_hoverState = HoverState.Unbound;
                }
            }
            if (currentState == ActiveState.LockIdle) {
                if (m_itemCount >= (GetFenceMode() ? 1 : 2) || m_controlMode == ControlMode.ITEMWISE) {
                    const float pointRadius = HOVER_POINTDISTANCE_THRESHOLD;
                    const float anglePointRadius = pointRadius;
                    const float angleLocusRadius = HOVER_ANGLELOCUS_DIAMETER;
                    const float angleLocusDistanceThreshold = 0.40f;
                    bool angleObjectMode = m_itemType == ItemType.PROP;
                    Vector3 angleCenter = m_items[HoverItemAngleCenterIndex].Position;
                    Vector3 anglePos = Circle2.Position3FromAngleXZ(angleCenter, angleLocusRadius, m_hoverAngle);
                    Vector3 spacingPos = GetFenceMode() ? m_fenceEndPoints[HoverItemPositionIndex] : m_items[HoverItemPositionIndex].Position;
                    if (PLTMath.IsInsideCircleXZ(spacingPos, pointRadius, position)) {
                        m_hoverState = m_controlMode == ControlMode.ITEMWISE ? HoverState.ItemwiseItem : HoverState.SpacingLocus;
                    } else if (angleObjectMode && PLTMath.IsInsideCircleXZ(anglePos, anglePointRadius, position)) {
                        m_hoverState = HoverState.AngleLocus;
                    } else if (angleObjectMode && PLTMath.IsNearCircleOutlineXZ(angleCenter, HOVER_ANGLELOCUS_DIAMETER, position, angleLocusDistanceThreshold)) {
                        m_hoverState = HoverState.AngleLocus;
                    } else if (PLTMath.IsInsideCircleXZ(ControlPoint.m_cachedControlPoints[0].m_position, pointRadius, position)) {
                        m_hoverState = HoverState.ControlPointFirst;
                    } else if (PLTMath.IsInsideCircleXZ(ControlPoint.m_cachedControlPoints[1].m_position, pointRadius, position)) {
                        m_hoverState = HoverState.ControlPointSecond;
                    } else if (PLTMath.IsInsideCircleXZ(ControlPoint.m_cachedControlPoints[2].m_position, pointRadius, position)) {
                        m_hoverState = HoverState.ControlPointThird;
                    } else if (PLTMath.IsCloseToCurveXZ(m_mainBezier, HOVER_CURVEDISTANCE_THRESHOLD, position, out float hoverCurveT)) {
                        m_hoverState = HoverState.Curve;
                    } else {
                        m_hoverState = HoverState.Unbound;
                    }
                } else {
                    m_hoverState = HoverState.Unbound;
                }
            }
        }

        public override void UpdateMiscHoverParameters() {
            if (m_itemCount >= (GetFenceMode() ? 1 : 2) || m_controlMode == ControlMode.ITEMWISE) {
                switch (m_currentState) {
                case ActiveState.MoveSegment:
                    ControlPoint.PointInfo[] controlPoints = ControlPoint.m_controlPoints;
                    ControlPoint.PointInfo[] lockedControlPoints = ControlPoint.m_lockedControlPoints;
                    Vector3 translation = m_cachedPosition - m_lockedBackupCachedPosition;
                    controlPoints[0].m_position = lockedControlPoints[0].m_position + translation;
                    controlPoints[1].m_position = lockedControlPoints[1].m_position + translation;
                    controlPoints[2].m_position = lockedControlPoints[2].m_position + translation;
                    ControlPoint.UpdateCached(controlPoints);
                    DrawMode.CurrentMode.UpdateCurve();
                    SegmentState.UpdatePlacement();
                    break;
                case ActiveState.ChangeSpacing:
                    if (PLTMath.IsCloseToCurveXZ(m_mainBezier, HOVER_CURVEDISTANCE_THRESHOLD * 8f, m_cachedPosition, out float hoverCurveT)) {
                        float curveT = Mathf.Clamp(hoverCurveT, m_items[0].m_t, 0.995f);
                        if (GetFenceMode()) {
                            Vector3 curveDistance = m_mainBezier.Position(curveT) - m_fenceEndPoints[0];
                            ItemInfo.ItemSpacing = curveDistance.MagnitudeXZ();
                        } else { //non-fence mode
                            ItemInfo.ItemSpacing = PLTMath.CubicBezierArcLengthXZGauss12(m_mainBezier, m_items[0].m_t, curveT);
                        }
                    }
                    ControlPoint.UpdateCached();
                    DrawMode.CurrentMode.UpdateCurve();
                    SegmentState.UpdatePlacement(true, true);
                    break;
                case ActiveState.ChangeAngle:
                    Vector3 xAxis; xAxis.x = 1; xAxis.y = 0; xAxis.z = 0;
                    Vector3 yAxis; yAxis.x = 0; yAxis.y = 1; yAxis.z = 0;
                    if (m_angleMode == AngleMode.DYNAMIC) {
                        Vector3 angleVector = m_cachedPosition - m_items[HoverItemAngleCenterIndex].Position;
                        angleVector.y = 0f;
                        angleVector.Normalize();
                        m_hoverAngle = PLTMath.AngleSigned(angleVector, xAxis, yAxis);
                        ItemInfo.m_itemAngleOffset = PLTMath.AngleSigned(angleVector, m_lockedBackupItemDirection, yAxis);
                    } else if (m_angleMode == AngleMode.SINGLE) {
                        Vector3 angleVector = m_cachedPosition - m_items[HoverItemAngleCenterIndex].Position;
                        angleVector.y = 0f;
                        angleVector.Normalize();
                        float angle = PLTMath.AngleSigned(angleVector, xAxis, yAxis);
                        m_hoverAngle = angle;
                        ItemInfo.m_itemAngleSingle = angle + Mathf.PI;
                    }
                    ControlPoint.UpdateCached();
                    DrawMode.CurrentMode.UpdateCurve();
                    SegmentState.UpdatePlacement();
                    break;
                case ActiveState.ItemwiseLock:
                case ActiveState.MoveItemwiseItem:
                    SegmentState.UpdatePlacement();
                    break;
                }
            }
        }

        public override void RenderProgressiveSpacingFill(RenderManager.CameraInfo cameraInfo, float fillLength, float interval, float size, Color color, bool renderLimits, bool alphaBlend) {
            if (GetFenceMode()) return;
            float firstItemT = m_items[0].m_t;
            PLTMath.StepDistanceCurve(m_mainBezier, firstItemT, fillLength, TOLERANCE, out float tFill);
            RenderBezier(cameraInfo, m_mainBezier.Cut(firstItemT, tFill), size, new Color(color.r, color.g, color.b, 0.75f * color.a), renderLimits, true);
        }
    }
}
