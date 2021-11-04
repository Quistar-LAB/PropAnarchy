using ColossalFramework;
using System.Collections.Generic;
using UnityEngine;
using static PropAnarchy.PLT.PropLineTool;

namespace PropAnarchy.PLT {
    public class DrawCircleState : ActiveDrawState {
        private IEnumerator<bool> ContinueDrawingFromLockMode(bool finalizePlacement) {
            //check if in fence mode and line is too short
            if (!GetFenceMode() && m_itemCount > 0 && finalizePlacement && FinalizePlacement(true, false)) {
                if (!PostCheckAndContinue()) {
                    ControlPoint.Reset();
                    GoToActiveState(ActiveState.CreatePointFirst);
                }
            }
            yield return true;
        }

        public override void OnToolGUI(Event e, bool isInsideUI) {
            if (!OnDefaultToolGUI(e, out bool leftMouseDown, out bool rightMouseDown, out bool altDown, out bool ctrlDown)) return;
            ActiveState currentState = m_currentState;
            if (!isInsideUI && leftMouseDown && altDown) {
                switch (currentState) {
                case ActiveState.CreatePointSecond:
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
            switch (currentState) {
            case ActiveState.CreatePointFirst:
                if (!isInsideUI && leftMouseDown && !m_prevLeftMouseDown) {
                    m_prevLeftMouseDown = true;
                    SegmentState.FinalizeForPlacement(false);
                    ControlPoint.Add(ref m_cachedPosition);
                    GoToActiveState(ActiveState.CreatePointSecond);
                    ControlPoint.Modify(ref m_mousePosition, 1);
                    UpdateCurve();
                    SegmentState.UpdatePlacement(false, false);
                }
                break;
            case ActiveState.CreatePointSecond:
                if (!isInsideUI) {
                    if (leftMouseDown && !m_prevLeftMouseDown && IsLengthLongEnough()) {
                        m_prevLeftMouseDown = true;
                        ControlPoint.Add(ref m_cachedPosition);
                        if (ctrlDown) {
                            m_previousLockingMode = m_lockingMode;
                            GoToActiveState(ActiveState.LockIdle);
                        } else if (m_controlMode == ControlMode.ITEMWISE) {
                            m_previousLockingMode = m_lockingMode;
                            GoToActiveState(ActiveState.ItemwiseLock);
                        } else {
                                Singleton<SimulationManager>.instance.AddAction(() => {
                                    FinalizePlacement(true, false);
                                    if (!PostCheckAndContinue()) {
                                        ControlPoint.Reset();
                                        GoToActiveState(ActiveState.CreatePointFirst);
                                    }
                                });
                        }
                    } else if (rightMouseDown) {
                        ControlPoint.Cancel();
                        GoToActiveState(ActiveState.CreatePointFirst);
                        ControlPoint.Modify(ref m_mousePosition, 0);
                        UpdateCurve();
                    } else {
                        ControlPoint.Modify(ref m_mousePosition, 1);
                        UpdateCurve();
                        SegmentState.UpdatePlacement(true, false);
                    }
                }
                break;
            case ActiveState.LockIdle:
                if (!isInsideUI) {
                    if (leftMouseDown) {
                        if (m_keyboardCtrlDown && m_controlMode == ControlMode.ITEMWISE) {
                            GoToActiveState(ActiveState.ItemwiseLock);
                        } else if (ctrlDown) {
                            AddAction(ContinueDrawingFromLockMode(true));
                        }
                        switch (m_hoverState) {
                        case HoverState.SpacingLocus:
                            GoToActiveState(ActiveState.ChangeSpacing);
                            break;
                        case HoverState.AngleLocus:
                            GoToActiveState(ActiveState.ChangeAngle);
                            break;
                        case HoverState.ControlPointFirst:
                            GoToActiveState(ActiveState.MovePointFirst);
                            ControlPoint.Modify(ref m_mousePosition, 0);
                            goto FinalizeControlPoint;
                        case HoverState.ControlPointSecond:
                            GoToActiveState(ActiveState.MovePointSecond);
                            ControlPoint.Modify(ref m_mousePosition, 1);
                            goto FinalizeControlPoint;
                        case HoverState.ControlPointThird:
                            GoToActiveState(ActiveState.MovePointThird);
                            ControlPoint.Modify(ref m_mousePosition, 2);
FinalizeControlPoint:
                            UpdateCurve();
                            SegmentState.UpdatePlacement();
                            break;
                        case HoverState.Curve:
                            GoToActiveState(ActiveState.MoveSegment);
                            break;
                        case HoverState.ItemwiseItem:
                            if (m_controlMode == ControlMode.ITEMWISE) {
                                GoToActiveState(ActiveState.MoveItemwiseItem);
                            }
                            break;
                        }
                    } else if (ctrlDown) {
                        AddAction(ContinueDrawingFromLockMode(true));
                    } else if (rightMouseDown) {
                        RevertDrawingFromLockMode();
                    }
                }
                break;
            case ActiveState.MovePointFirst:
                if (!isInsideUI) {
                    if (rightMouseDown) {
                        //reset first CP to original position
                        ControlPoint.Modify(ref ControlPoint.m_lockedControlPoints[0].m_position, 0);
                    }
                    GoToActiveState(ActiveState.LockIdle);
                }
                break;
            case ActiveState.MovePointSecond:
                if (!isInsideUI) {
                    if (rightMouseDown) {
                        //reset second CP to original position
                        ControlPoint.Modify(ref ControlPoint.m_lockedControlPoints[1].m_position, 1);
                    }
                    GoToActiveState(ActiveState.LockIdle);
                }
                break;
            case ActiveState.MoveSegment:
                if (!isInsideUI) {
                    if (rightMouseDown) {
                        ControlPoint.PointInfo[] controlPoints = ControlPoint.m_controlPoints;
                        ControlPoint.PointInfo[] lockedBackupPoints = ControlPoint.m_lockedControlPoints;
                        controlPoints[0].m_position = lockedBackupPoints[0].m_position;
                        controlPoints[1].m_position = lockedBackupPoints[1].m_position;
                        controlPoints[2].m_position = lockedBackupPoints[2].m_position;
                        ControlPoint.UpdateCached(controlPoints);
                        UpdateCurve();
                        SegmentState.UpdatePlacement();
                    }
                    GoToActiveState(ActiveState.LockIdle);
                }
                break;
            case ActiveState.ChangeSpacing:
                if (!isInsideUI) {
                    if (rightMouseDown) {
                        //reset spacing to original value
                        ItemInfo.ItemSpacing = m_lockedBackupSpacing;
                    }
                    GoToActiveState(ActiveState.LockIdle);
                }
                break;
            case ActiveState.ChangeAngle:
                if (!isInsideUI) {
                    if (rightMouseDown) {
                        //reset angle to original value
                        ItemInfo.m_itemAngleOffset = m_lockedBackupAngleOffset;
                        ItemInfo.m_itemAngleSingle = m_lockedBackupAngleSingle;
                    }
                    GoToActiveState(ActiveState.LockIdle);
                }
                break;
            case ActiveState.ItemwiseLock:
                if (!isInsideUI) {
                    if (leftMouseDown && ctrlDown) {
                        GoToActiveState(ActiveState.LockIdle);
                    } else if (ctrlDown) {
                        AddAction(ContinueDrawingFromLockMode(true));
                    } else if (rightMouseDown) {
                        RevertDrawingFromLockMode();
                    } else if (leftMouseDown) {
                        switch (m_hoverState) {
                        case HoverState.ItemwiseItem:
                            AddAction(CreateItems(true, true));
                            break;
                        }
                    }
                } else if (leftMouseDown) {
                    //UpdatePrefab();
                    SegmentState.UpdatePlacement();
                }
                break;
            case ActiveState.MoveItemwiseItem:
                if (!isInsideUI) {
                    if (rightMouseDown) {
                        //reset item back to original position
                        m_hoverItemwiseT = m_lockedBackupItemwiseT;
                    }
                    GoToActiveState(ActiveState.LockIdle);
                }
                break;
            case ActiveState.MaxFillContinue:
                if (m_controlMode == ControlMode.ITEMWISE) {
                    GoToActiveState(ActiveState.ItemwiseLock);
                } else if (!isInsideUI) {
                    if ((leftMouseDown && ctrlDown) || rightMouseDown) {
                        GoToActiveState(ActiveState.LockIdle);
                    } else if (leftMouseDown && IsLengthLongEnough()) {
                        AddAction(CreateItems(true, false));
                        if (!PostCheckAndContinue()) {
                            ControlPoint.Reset();
                            GoToActiveState(ActiveState.CreatePointFirst);
                        }
                    }
                }
                break;
            }
        }

        public override void OnRenderGeometry(RenderManager.CameraInfo cameraInfo) {
        }

        public override void RenderLines(RenderManager.CameraInfo cameraInfo, ref Color createPointColor, ref Color curveWarningColor) {
            if (!SegmentState.AllItemsValid) {
                RenderMainCircle(cameraInfo, m_mainCircle, 1.50f, curveWarningColor, false, true);
            }
            RenderMainCircle(cameraInfo, m_mainCircle, 1.00f, createPointColor, false, true);
        }

        public override bool OnRenderOverlay(RenderManager.CameraInfo cameraInfo, ActiveState curState, ref Color createPointColor, ref Color curveWarningColor, ref Color copyPlaceColor) {
            Vector3 vectorZero; vectorZero.x = 0f; vectorZero.y = 0f; vectorZero.z = 0f;
            ControlPoint.PointInfo[] cachedControlPoints = ControlPoint.m_cachedControlPoints;
            if (cachedControlPoints[1].m_direction != vectorZero) {
                switch (curState) {
                case ActiveState.MaxFillContinue:
                case ActiveState.CreatePointSecond:
                    if (m_keyboardAltDown) {
                        createPointColor = copyPlaceColor; ;
                    } else if (m_keyboardCtrlDown) {
                        createPointColor = Settings.m_PLTColor_locked;
                    }
                    RenderLines(cameraInfo, ref createPointColor, ref curveWarningColor);
                    if (SegmentState.IsReadyForMaxContinue || SegmentState.IsMaxFillContinue) {
                        RenderMaxFillContinueMarkers(cameraInfo);
                    }
                    RenderCircle(cameraInfo, cachedControlPoints[0].m_position, 0.10f, createPointColor, false, true);
                    RenderCircle(cameraInfo, cachedControlPoints[1].m_position, 0.10f, createPointColor, false, true);
                    break;
                }
                return true;
            }
            return false;
        }

        public override void OnToolLateUpdate() {
            base.OnToolLateUpdate();
        }

        public override void OnToolUpdate() {
            base.OnToolUpdate();
        }

        public override bool ContinueDrawing(ControlPoint.PointInfo[] controlPoints, ref int controlPointCount) {
            ControlPoint.PointInfo p0 = ControlPoint.m_controlPoints[0];
            ControlPoint.PointInfo p1 = ControlPoint.m_controlPoints[1];
            ControlPoint.Reset();
            controlPoints[0] = p0;
            controlPoints[1] = p1;
            controlPointCount = 1;
            SegmentState.m_segmentInfo.m_isContinueDrawing = true;
            ControlPoint.UpdateCached(controlPoints);
            return true;
        }

        public override bool IsLengthLongEnough() => m_mainCircle.Diameter >= ItemInfo.ItemSpacing;

        public override void UpdateCurve(ControlPoint.PointInfo[] points, int pointCount) {
            SegmentState.m_pendingPlacementUpdate = true;
            if (pointCount >= 1) {
                Vector3 center = points[0].m_position;
                Vector3 pointOnCircle = points[1].m_position;
                center.y = 0f;
                pointOnCircle.y = 0f;
                Circle3XZ mainCircle = new Circle3XZ(center, pointOnCircle);
                m_rawCircle = mainCircle;
                if (Settings.PerfectCircles) {
                    switch (m_controlMode) {
                    case ControlMode.ITEMWISE:
                    case ControlMode.SPACING:
                        mainCircle.m_radius = GetFenceMode() ? mainCircle.PerfectRadiusByChords(ItemInfo.ItemSpacing) : mainCircle.PerfectRadiusByArcs(ItemInfo.ItemSpacing);
                        break;
                    }
                }
                m_mainCircle = mainCircle;
            }
        }

        public override bool PostCheckAndContinue(ControlPoint.PointInfo[] controlPoints, ControlPoint.PointInfo[] cachedControlPoints, ref int controlPointCount) {
            if (m_lockingMode == LockingMode.Off) {
                if (SegmentState.IsReadyForMaxContinue) {
                    SegmentState.UpdatePlacement(true, false);
                    GoToActiveState(ActiveState.MaxFillContinue);
                } else {
                    if (ContinueDrawing(controlPoints, ref controlPointCount)) {
                        GoToActiveState(ActiveState.CreatePointSecond);
                        ControlPoint.Modify(DrawMode.CurrentMode, ref m_mousePosition, 1, ActiveState.CreatePointSecond, DrawMode.Current);
                        SegmentState.UpdatePlacement(true, false);
                    } else {
                        ControlPoint.Reset();
                        GoToActiveState(ActiveState.CreatePointFirst);
                    }
                }
            } else if (m_lockingMode == LockingMode.Lock) { //Locking is enabled
                m_previousLockingMode = m_lockingMode;
                GoToActiveState(ActiveState.LockIdle);
            }
            return true;
        }

        public override void RevertDrawingFromLockMode() {
            GoToActiveState(ActiveState.CreatePointSecond);
            ControlPoint.Modify(ref m_mousePosition, 2); //update position of first point
            UpdateCurve();
            SegmentState.UpdatePlacement(false, false);
        }

        public override void CalculateAllDirections() {
            int itemCount = m_itemCount;
            ItemInfo[] items = m_items;
            if (GetFenceMode()) {
                Vector3[] fenceEndPoints = m_fenceEndPoints;
                for (int i = 0; i < itemCount; i++) {
                    items[i].SetDirectionsXZ(fenceEndPoints[i + 1] - fenceEndPoints[i]);
                }
            } else {
                Circle3XZ mainCircle = m_mainCircle;
                for (int i = 0; i < itemCount; i++) {
                    items[i].SetDirectionsXZ(mainCircle.Tangent(items[i].m_t));
                }
            }
        }

        public override bool CalculateItemwisePosition(float fencePieceLength, float initialOffset, Vector3 lastFenceEndpoint) {
            float hoverItemwiseT = m_hoverItemwiseT;
            if (GetFenceMode()) {
                Vector3 positionStart, positionEnd;
                if (m_mainCircle.m_radius == 0f || fencePieceLength > m_mainCircle.Diameter) {
                    m_itemCount = 0;
                    return false;
                }
                float itemTStart = hoverItemwiseT;
                float deltaT = m_mainCircle.ChordDeltaT(fencePieceLength);
                if (deltaT <= 0f || deltaT >= 1f) {
                    m_itemCount = 0;
                    return false;
                }
                m_fenceEndPoints[0] = positionStart = m_mainCircle.Position(itemTStart);
                m_fenceEndPoints[1] = positionEnd = m_mainCircle.Position(itemTStart + deltaT);
                m_items[0].Position = Vector3.Lerp(positionStart, positionEnd, 0.50f);
                return true;
            }
            m_items[0].m_t = hoverItemwiseT;
            m_items[0].Position = m_mainCircle.Position(hoverItemwiseT);
            return true;
        }

        public override int CalculateAllPositionsBySpacing(float spacing, float initialOffset, Vector3 lastFenceEndpoint) {
            Vector3 position, center, radiusVector;
            Quaternion rotation;
            int numItems, numItemsRaw;
            float deltaT, t, penultimateT, finalT;
            Circle3XZ mainCircle = m_mainCircle;
            if (GetFenceMode()) {
                float chordAngle = mainCircle.ChordAngle(spacing);
                if (chordAngle <= 0f || chordAngle > Mathf.PI || mainCircle.m_radius <= 0f) {
                    return 0;
                }
                float initialAngle = initialOffset / mainCircle.m_radius;
                float angleAfterFirst = SegmentState.IsMaxFillContinue ? 2f * Mathf.PI - initialAngle : 2f * Mathf.PI;
                if (Settings.PerfectCircles) {
                    numItemsRaw = Mathf.Clamp(Mathf.RoundToInt(angleAfterFirst / chordAngle), 0, MAX_ITEM_ARRAY_LENGTH);
                    numItems = Mathf.Min(m_itemCount, numItemsRaw);
                } else {
                    numItemsRaw = Mathf.Clamp(Mathf.FloorToInt(angleAfterFirst / chordAngle), 0, MAX_ITEM_ARRAY_LENGTH);
                    numItems = Mathf.Min(m_itemCount, numItemsRaw);
                }
                deltaT = mainCircle.ChordDeltaT(spacing);
                t = 0f;
                penultimateT = 0f;
                //Max Fill Continue
                if (SegmentState.IsMaxFillContinue && initialOffset > 0f) {
                    t = mainCircle.DeltaT(initialOffset);
                    penultimateT = t;
                }
                position = mainCircle.Position(t);
                center = mainCircle.m_center;
                radiusVector = position - center;
                rotation = Quaternion.AngleAxis(-1f * chordAngle * Mathf.Rad2Deg, m_vectorUp);
                //calculate endpoints
                for (int i = 0; i < numItems + 1; i++) {
                    penultimateT = t;
                    m_fenceEndPoints[i] = position;
                    radiusVector = rotation * radiusVector;
                    position = center + radiusVector;
                    t += deltaT;
                }
                //then calculate midpoints
                for (int i = 0; i < numItems; i++) {
                    m_items[i].Position = Vector3.Lerp(m_fenceEndPoints[i], m_fenceEndPoints[i + 1], 0.50f);
                }
                //prep for MaxFillContinue
                if (SegmentState.IsReadyForMaxContinue) {
                    SegmentState.NewFinalOffset = mainCircle.ArclengthBetween(0f, penultimateT);
                } else {
                    SegmentState.NewFinalOffset = mainCircle.ArclengthBetween(t, 1f);
                }
                return numItems;
            }
            deltaT = mainCircle.DeltaT(spacing);
            if (deltaT <= 0f || deltaT > 1f || mainCircle.m_radius <= 0f) {
                return 0;
            }
            t = 0f;
            float remainingSpace = mainCircle.Circumference;
            if (SegmentState.IsMaxFillContinue) {
                if (mainCircle.Circumference > 0f) {
                    t = initialOffset / mainCircle.Circumference;
                    remainingSpace -= initialOffset;
                } else {
                    return 0;
                }
            }
            //use ceiling for non-fence, because the point at the beginning is an extra point
            numItemsRaw = Mathf.CeilToInt(remainingSpace / spacing);
            numItems = Mathf.Min(m_itemCount, Mathf.Clamp(numItemsRaw, 0, MAX_ITEM_ARRAY_LENGTH));
            position = mainCircle.Position(t);
            center = mainCircle.m_center;
            radiusVector = position - center;
            float deltaAngle = mainCircle.DeltaAngle(spacing);
            rotation = Quaternion.AngleAxis(-1f * deltaAngle * Mathf.Rad2Deg, m_vectorUp);
            for (int i = 0; i < numItems; i++) {
                m_items[i].m_t = t;
                m_items[i].Position = position;
                radiusVector = rotation * radiusVector;
                position = center + radiusVector;
                t += deltaT;
            }
            finalT = m_items[numItems - 1].m_t;
            if (SegmentState.IsReadyForMaxContinue) {
                SegmentState.NewFinalOffset = spacing + mainCircle.ArclengthBetween(0f, finalT);
            } else {
                SegmentState.NewFinalOffset = mainCircle.ArclengthBetween(t, 1f);
            }
            return numItems;
        }

        public override void DiscoverHoverState(Vector3 position) {
            ActiveState currentState = m_currentState;
            //check for itemwise first before classic lock mode
            if (m_controlMode == ControlMode.ITEMWISE && (currentState == ActiveState.ItemwiseLock || currentState == ActiveState.MoveItemwiseItem)) {
                if (PLTMath.IsCloseToCircle3XZ(m_mainCircle, HOVER_ITEMWISE_CURVEDISTANCE_THRESHOLD, position, out float hoverItemT)) {
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
                    } else if (PLTMath.IsCloseToCircle3XZ(m_mainCircle, HOVER_CURVEDISTANCE_THRESHOLD, position, out _)) {
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
                    UpdateCurve();
                    SegmentState.UpdatePlacement();
                    break;
                case ActiveState.ChangeSpacing:
                    if (PLTMath.IsCloseToCircle3XZ(m_mainCircle, HOVER_CURVEDISTANCE_THRESHOLD * 12f, m_cachedPosition, out float hoverCurveT)) {
                        Circle3XZ circle = (Settings.PerfectCircles) ? m_rawCircle : m_mainCircle;
                        if (GetFenceMode()) {
                            float curveT = Mathf.Clamp(hoverCurveT, m_items[0].m_t, 0.500f);
                            Vector3 curveDistance = circle.Position(curveT) - circle.Position(0f);
                            float distance = curveDistance.MagnitudeXZ();
                            if (Settings.PerfectCircles) {
                                distance = Mathf.Clamp(distance, SPACING_MIN, m_rawCircle.Diameter);
                            }
                            ItemInfo.ItemSpacing = distance;
                        } else { //non-fence mode
                            float curveT = Mathf.Clamp(hoverCurveT, m_items[0].m_t, 0.995f);
                            float distance = circle.m_radius * circle.AngleBetween(0f, curveT);
                            if (Settings.PerfectCircles) {
                                distance = Mathf.Clamp(distance, SPACING_MIN, 0.50f * m_mainCircle.Circumference);
                            }
                            ItemInfo.ItemSpacing = distance;
                        }
                    }
                    ControlPoint.UpdateCached();
                    UpdateCurve();
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
                    UpdateCurve();
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
            Circle3XZ mainCircle = m_mainCircle;
            if (GetFenceMode() || mainCircle.m_radius <= 0f) return;
            int numItems = Mathf.CeilToInt(fillLength / interval);
            float deltaT = interval / mainCircle.Circumference;
            Quaternion rotation = Quaternion.AngleAxis(deltaT * -360f, m_vectorUp);
            Vector3 position = mainCircle.Position(0f);
            Vector3 center = mainCircle.m_center;
            Vector3 radiusVector = position - center;
            for (int i = 0; i < numItems; i++) {
                RenderCircle(cameraInfo, position, size, color, renderLimits, alphaBlend);
                radiusVector = rotation * radiusVector;
                position = center + radiusVector;
            }
        }
    }
}
