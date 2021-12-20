using EManagersLib;
using System;
using UnityEngine;
using static PropAnarchy.PLT.PropLineTool;

namespace PropAnarchy.PLT {
    public sealed class DrawCircleState : ActiveDrawState {
        private void ContinueDrawingFromLockMode(bool finalizePlacement) {
            //check if in fence mode and line is too short
            if (!GetFenceMode() && m_itemCount > 0 && finalizePlacement && FinalizePlacement(true, false)) {
                if (!PostCheckAndContinue()) {
                    ControlPoint.Reset();
                    GoToActiveState(ActiveState.CreatePointFirst);
                }
            }
        }

        public override void OnToolGUI(Event e, bool isInsideUI) {
            base.OnToolGUI(e, isInsideUI);
            ActiveState currentState = m_currentState;
            if (!isInsideUI && e.type == EventType.MouseDown && e.button == LEFTMOUSEBUTTON && m_keyboardAltDown) {
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
                    AddAction(() => FinalizePlacement(true, true));
                    return;
                }
            }
            switch (currentState) {
            case ActiveState.CreatePointFirst:
                if (!isInsideUI && e.type == EventType.MouseDown && e.button == LEFTMOUSEBUTTON) {
                    SegmentState.FinalizeForPlacement(false);
                    ControlPoint.Add(m_cachedPosition);
                    GoToActiveState(ActiveState.CreatePointSecond);
                    ControlPoint.Modify(m_mousePosition, 1);
                    UpdateCurve();
                    UpdatePlacement(false, false);
                }
                break;
            case ActiveState.CreatePointSecond:
                if (!isInsideUI && e.type == EventType.MouseDown) {
                    if (e.button == LEFTMOUSEBUTTON && IsLengthLongEnough()) {
                        ControlPoint.Add(m_cachedPosition);
                        if (m_keyboardCtrlDown) {
                            m_previousLockingMode = m_lockingMode;
                            GoToActiveState(ActiveState.LockIdle);
                        } else if (m_controlMode == ControlMode.ITEMWISE) {
                            m_previousLockingMode = m_lockingMode;
                            GoToActiveState(ActiveState.ItemwiseLock);
                        } else {
                            AddAction(() => {
                                FinalizePlacement(true, false);
                                if (!PostCheckAndContinue()) {
                                    ControlPoint.Reset();
                                    GoToActiveState(ActiveState.CreatePointFirst);
                                }
                            });
                        }
                    } else if (e.button == RIGHTMOUSEBUTTON) {
                        ControlPoint.Cancel();
                        GoToActiveState(ActiveState.CreatePointFirst);
                        ControlPoint.Modify(m_mousePosition, 0);
                        UpdateCurve();
                    }
                }
                break;
            case ActiveState.LockIdle:
                if (!isInsideUI && e.type == EventType.MouseDown) {
                    if (e.button == LEFTMOUSEBUTTON) {
                        if (m_keyboardCtrlDown) {
                            if (m_controlMode == ControlMode.ITEMWISE) {
                                GoToActiveState(ActiveState.ItemwiseLock);
                            } else {
                                AddAction(() => ContinueDrawingFromLockMode(true));
                            }
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
                            ControlPoint.Modify(m_mousePosition, 0);
                            UpdateCurve();
                            UpdatePlacement();
                            break;
                        case HoverState.ControlPointSecond:
                            GoToActiveState(ActiveState.MovePointSecond);
                            ControlPoint.Modify(m_mousePosition, 1);
                            UpdateCurve();
                            UpdatePlacement();
                            break;
                        case HoverState.ControlPointThird:
                            GoToActiveState(ActiveState.MovePointThird);
                            ControlPoint.Modify(m_mousePosition, 2);
                            UpdateCurve();
                            UpdatePlacement();
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
                    } else if (m_keyboardCtrlDown) {
                        AddAction(() => ContinueDrawingFromLockMode(true));
                    } else if (e.button == RIGHTMOUSEBUTTON) {
                        RevertDrawingFromLockMode();
                    }
                }
                break;
            case ActiveState.MovePointFirst:
                if (!isInsideUI && e.type == EventType.MouseDown) {
                    if (e.button == RIGHTMOUSEBUTTON) {
                        //reset first CP to original position
                        ControlPoint.Modify(ControlPoint.m_lockedControlPoints[0].m_position, 0);
                    }
                    GoToActiveState(ActiveState.LockIdle);
                }
                break;
            case ActiveState.MovePointSecond:
                if (!isInsideUI && e.type == EventType.MouseDown) {
                    if (e.button == RIGHTMOUSEBUTTON) {
                        //reset second CP to original position
                        ControlPoint.Modify(ControlPoint.m_lockedControlPoints[1].m_position, 1);
                    }
                    GoToActiveState(ActiveState.LockIdle);
                }
                break;
            case ActiveState.MoveSegment:
                if (!isInsideUI && e.type == EventType.MouseDown) {
                    if (e.button == RIGHTMOUSEBUTTON) {
                        ControlPoint.PointInfo[] controlPoints = ControlPoint.m_controlPoints;
                        ControlPoint.PointInfo[] lockedBackupPoints = ControlPoint.m_lockedControlPoints;
                        controlPoints[0].m_position = lockedBackupPoints[0].m_position;
                        controlPoints[1].m_position = lockedBackupPoints[1].m_position;
                        controlPoints[2].m_position = lockedBackupPoints[2].m_position;
                        UpdateCurve();
                        UpdatePlacement();
                    }
                    GoToActiveState(ActiveState.LockIdle);
                }
                break;
            case ActiveState.ChangeSpacing:
                if (!isInsideUI && e.type == EventType.MouseDown) {
                    if (e.button == RIGHTMOUSEBUTTON) {
                        //reset spacing to original value
                        ItemInfo.ItemSpacing = m_lockedBackupSpacing;
                    }
                    GoToActiveState(ActiveState.LockIdle);
                }
                break;
            case ActiveState.ChangeAngle:
                if (!isInsideUI && e.type == EventType.MouseDown) {
                    if (e.button == RIGHTMOUSEBUTTON) {
                        //reset angle to original value
                        ItemInfo.m_itemAngleOffset = m_lockedBackupAngleOffset;
                        ItemInfo.m_itemAngleSingle = m_lockedBackupAngleSingle;
                    }
                    GoToActiveState(ActiveState.LockIdle);
                }
                break;
            case ActiveState.ItemwiseLock:
                if (!isInsideUI && e.type == EventType.MouseDown) {
                    if (e.button == LEFTMOUSEBUTTON) {
                        if (m_keyboardCtrlDown) {
                            GoToActiveState(ActiveState.LockIdle);
                        } else if (m_hoverState == HoverState.ItemwiseItem) {
                            AddAction(() => FinalizePlacement(true, true));
                        }
                    } else if (e.button == RIGHTMOUSEBUTTON) {
                        RevertDrawingFromLockMode();
                    } else if (m_keyboardCtrlDown) {
                        AddAction(() => ContinueDrawingFromLockMode(true));
                    }
                } else if (e.type == EventType.MouseDown && e.button == LEFTMOUSEBUTTON) {
                    //UpdatePrefab();
                    UpdatePlacement();
                }
                break;
            case ActiveState.MoveItemwiseItem:
                if (!isInsideUI) {
                    if (e.type == EventType.MouseDown && e.button == RIGHTMOUSEBUTTON) {
                        //reset item back to original position
                        m_hoverItemwiseT = m_lockedBackupItemwiseT;
                    }
                    GoToActiveState(ActiveState.LockIdle);
                }
                break;
            case ActiveState.MaxFillContinue:
                if (m_controlMode == ControlMode.ITEMWISE) {
                    GoToActiveState(ActiveState.ItemwiseLock);
                } else if (!isInsideUI && e.type == EventType.MouseDown) {
                    if ((e.button == LEFTMOUSEBUTTON && m_keyboardCtrlDown) || e.button == RIGHTMOUSEBUTTON) {
                        GoToActiveState(ActiveState.LockIdle);
                    } else if (e.button == LEFTMOUSEBUTTON && IsLengthLongEnough()) {
                        AddAction(() => FinalizePlacement(true, false));
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
            switch (m_currentState) {
            case ActiveState.CreatePointSecond: //creating second control point
            case ActiveState.LockIdle: //in lock mode, awaiting user input
            case ActiveState.MovePointFirst: //in lock mode, moving first control point
            case ActiveState.MovePointSecond: //in lock mode, moving second control point
            case ActiveState.MovePointThird: //in lock mode, moving third control point
            case ActiveState.MoveSegment: //in lock mode, moving full line or curve
            case ActiveState.ChangeSpacing: //in lock mode, changing item-to-item spacing along the line or curve
            case ActiveState.ChangeAngle: //in lock mode, changing initial item (first item's) angle
            case ActiveState.ItemwiseLock:
            case ActiveState.MoveItemwiseItem:
            case ActiveState.MaxFillContinue: //out of bounds
                int itemCount = m_itemCount;
                ItemInfo[] items = m_items;
                for (int i = 0; i < itemCount; i++) {
                    items[i].RenderItem(cameraInfo);
                }
                break;
            }
        }

        public override void RenderLines(RenderManager.CameraInfo cameraInfo, Color createPointColor, Color curveWarningColor) {
            if (!SegmentState.AllItemsValid) {
                RenderMainCircle(cameraInfo, m_mainCircle, 1.50f, curveWarningColor, false, true);
            }
            RenderMainCircle(cameraInfo, m_mainCircle, LINESIZE, createPointColor, false, true);
        }

        public override bool OnRenderOverlay(RenderManager.CameraInfo cameraInfo, ActiveState curState, Color createPointColor, Color curveWarningColor, Color copyPlaceColor) {
            ControlPoint.PointInfo[] controlPoints = ControlPoint.m_controlPoints;
            if (controlPoints[1].m_direction != VectorXZ.zero) {
                switch (curState) {
                case ActiveState.MaxFillContinue:
                case ActiveState.CreatePointSecond:
                    if (m_keyboardAltDown) {
                        createPointColor = copyPlaceColor; ;
                    } else if (m_keyboardCtrlDown) {
                        createPointColor = Settings.m_PLTColor_locked;
                    }
                    RenderLines(cameraInfo, createPointColor, curveWarningColor);
                    if (SegmentState.IsReadyForMaxContinue || SegmentState.IsMaxFillContinue) {
                        RenderMaxFillContinueMarkers(cameraInfo);
                    }
                    RenderCircle(cameraInfo, controlPoints[0].m_position, DOTSIZE, createPointColor, false, true);
                    RenderCircle(cameraInfo, controlPoints[1].m_position, DOTSIZE, createPointColor, false, true);
                    break;
                }
                return true;
            }
            return false;
        }

        public override void OnSimulationStep(Vector3 mousePosition) {
            ControlPoint.PointInfo[] controlPoints = ControlPoint.m_controlPoints;
            //m_cachedPosition = mousePosition;
            switch (m_currentState) {
            case ActiveState.CreatePointFirst:
                UpdatePlacement(false, false);
                break;
            case ActiveState.CreatePointSecond:
            case ActiveState.MovePointSecond:
                ControlPoint.Modify(DrawMode.CurrentMode, mousePosition, 1, m_currentState, DrawMode.Current);
                UpdatePlacement();
                break;
            case ActiveState.CreatePointThird:
            case ActiveState.MovePointThird:
                ControlPoint.Modify(DrawMode.CurrentMode, mousePosition, 2, m_currentState, DrawMode.Current);
                UpdatePlacement();
                break;
            case ActiveState.MovePointFirst:
                ControlPoint.Modify(DrawMode.CurrentMode, mousePosition, 0, m_currentState, DrawMode.Current);
                UpdatePlacement();
                break;
            case ActiveState.MaxFillContinue:
            case ActiveState.LockIdle:
            case ActiveState.MoveSegment:
            case ActiveState.ChangeSpacing:
            case ActiveState.ChangeAngle:
                UpdatePlacement();
                break;
            }
            if (ControlPoint.m_validPoints == 0) return;
            SegmentState.m_pendingPlacementUpdate = true;
            UpdateCurve(controlPoints, ControlPoint.m_validPoints);
            DiscoverHoverState(mousePosition);
            UpdateMiscHoverParameters();
        }

        public override void OnToolLateUpdate() {
            switch (m_currentState) {
            case ActiveState.CreatePointFirst:
                if (SegmentState.IsPositionEqualToLastFenceEndpoint(ControlPoint.m_controlPoints[0].m_position)) {
                    SegmentState.ResetLastContinueParameters();
                } else if (ControlPoint.m_validPoints > 0) {
                    ControlPoint.m_validPoints = 0;
                }
                break;
            case ActiveState.CreatePointThird:
                ControlPoint.Cancel();
                GoToActiveState(ActiveState.CreatePointSecond);
                ControlPoint.Modify(m_mousePosition, 1);
                UpdateCurve();
                return;
            }
            UpdateCachedPosition(false);
            CheckPendingPlacement();
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
            return true;
        }

        public override bool IsLengthLongEnough() => m_mainCircle.Diameter >= ItemInfo.ItemSpacing;

        public override bool IsActiveStateAnItemRenderState() {
            switch (m_currentState) {
            case ActiveState.CreatePointSecond:
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
                return true;
            }
            return false;
        }

        public override void UpdateCurve(ControlPoint.PointInfo[] points, int pointCount) {
            float itemSpacing = 0f;
            SegmentState.m_pendingPlacementUpdate = true;
            if (pointCount >= 1) {
                VectorXZ center = points[0].m_position;
                VectorXZ pointOnCircle = points[1].m_position;
                m_rawCircle = new CircleXZ(center, pointOnCircle);
                if (Settings.PerfectCircles) {
                    switch (m_controlMode) {
                    case ControlMode.ITEMWISE:
                    case ControlMode.SPACING:
                        itemSpacing = ItemInfo.ItemSpacing;
                        break;
                    }
                }
                m_mainCircle = new CircleXZ(center, pointOnCircle, itemSpacing);
            }
        }

        public override bool PostCheckAndContinue(ControlPoint.PointInfo[] controlPoints, ref int controlPointCount) {
            if (m_lockingMode == LockingMode.Off) {
                if (SegmentState.IsReadyForMaxContinue) {
                    UpdatePlacement(true, false);
                    GoToActiveState(ActiveState.MaxFillContinue);
                } else {
                    if (ContinueDrawing(controlPoints, ref controlPointCount)) {
                        GoToActiveState(ActiveState.CreatePointSecond);
                        ControlPoint.Modify(DrawMode.CurrentMode, m_mousePosition, 1, ActiveState.CreatePointSecond, DrawMode.Current);
                        UpdatePlacement(true, false);
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
            ControlPoint.Modify(m_mousePosition, 2); //update position of first point
            UpdateCurve();
            UpdatePlacement(false, false);
        }

        public override void CalculateAllDirections(ItemInfo[] items, Vector3[] fenceEndPoints, bool fenceMode) {
            int itemCount = m_itemCount;
            if (fenceMode) {
                for (int i = 0; i < itemCount; i++) {
                    items[i].SetDirectionsXZ(fenceEndPoints[i + 1] - fenceEndPoints[i]);
                }
            } else {
                CircleXZ mainCircle = m_mainCircle;
                for (int i = 0; i < itemCount; i++) {
                    items[i].SetDirectionsXZ(mainCircle.Tangent(items[i].m_t));
                }
            }
        }

        public override bool CalculateItemwisePosition(ItemInfo[] items, Vector3[] fenceEndPoints, bool fenceMode, float fencePieceLength, float initialOffset, VectorXZ lastFenceEndpoint) {
            float hoverItemwiseT = m_hoverItemwiseT;
            if (fenceMode) {
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
                fenceEndPoints[0] = positionStart = m_mainCircle.Position(itemTStart);
                fenceEndPoints[1] = positionEnd = m_mainCircle.Position(itemTStart + deltaT);
                items[0].Position = Vector3.Lerp(positionStart, positionEnd, 0.50f);
                return true;
            }
            items[0].m_t = hoverItemwiseT;
            items[0].Position = m_mainCircle.Position(hoverItemwiseT);
            return true;
        }

        public override bool CalculateAllPositionsBySpacing(ItemInfo[] items, Vector3[] fenceEndPoints, bool fenceMode, float spacing, float initialOffset, VectorXZ lastFenceEndpoint) {
            Vector3 position, center, radiusVector;
            Quaternion rotation;
            int numItems, numItemsRaw;
            float deltaT, t, penultimateT, finalT;
            CircleXZ mainCircle = m_mainCircle;
            if (fenceMode) {
                float chordAngle = mainCircle.ChordAngle(spacing);
                if (chordAngle <= 0f || chordAngle > Mathf.PI || mainCircle.m_radius <= 0f) {
                    m_itemCount = 0;
                    return false;
                }
                float initialAngle = EMath.Abs(initialOffset) / mainCircle.m_radius;
                float angleAfterFirst = SegmentState.IsMaxFillContinue ? 2f * Mathf.PI - initialAngle : 2f * Mathf.PI;
                if (Settings.PerfectCircles) {
                    numItemsRaw = EMath.RoundToInt(angleAfterFirst / chordAngle);
                    numItems = EMath.Min(m_itemCount, EMath.Clamp(numItemsRaw, 0, MAX_ITEM_ARRAY_LENGTH));
                } else {
                    numItemsRaw = EMath.FloorToInt(angleAfterFirst / chordAngle);
                    numItems = EMath.Min(m_itemCount, EMath.Clamp(numItemsRaw, 0, MAX_ITEM_ARRAY_LENGTH));
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
                    fenceEndPoints[i] = position;
                    radiusVector = rotation * radiusVector;
                    position = center + radiusVector;
                    t += deltaT;
                }
                //then calculate midpoints
                for (int i = 0; i < numItems; i++) {
                    items[i].Position = Vector3.Lerp(fenceEndPoints[i], fenceEndPoints[i + 1], 0.50f);
                }
                //prep for MaxFillContinue
                if (SegmentState.IsReadyForMaxContinue) {
                    SegmentState.NewFinalOffset = mainCircle.ArclengthBetween(0f, penultimateT);
                } else {
                    SegmentState.NewFinalOffset = mainCircle.ArclengthBetween(t, 1f);
                }
                m_itemCount = numItems;
                return true;
            }
            deltaT = mainCircle.DeltaT(spacing);
            if (deltaT <= 0f || deltaT > 1f || mainCircle.m_radius <= 0f) {
                m_itemCount = 0;
                return false;
            }
            t = 0f;
            float remainingSpace = mainCircle.Circumference;
            if (SegmentState.IsMaxFillContinue) {
                if (mainCircle.Circumference > 0f) {
                    t = initialOffset / mainCircle.Circumference;
                    remainingSpace -= initialOffset;
                } else {
                    m_itemCount = 0;
                    return false;
                }
            }
            //use ceiling for non-fence, because the point at the beginning is an extra point
            numItemsRaw = Mathf.CeilToInt(remainingSpace / spacing);
            numItems = Math.Min(m_itemCount, Mathf.Clamp(numItemsRaw, 0, MAX_ITEM_ARRAY_LENGTH));
            position = mainCircle.Position(t);
            center = mainCircle.m_center;
            radiusVector = position - center;
            float deltaAngle = mainCircle.DeltaAngle(spacing);
            rotation = Quaternion.AngleAxis(-1f * deltaAngle * Mathf.Rad2Deg, m_vectorUp);
            for (int i = 0; i < numItems; i++) {
                items[i].m_t = t;
                items[i].Position = position;
                radiusVector = rotation * radiusVector;
                position = center + radiusVector;
                t += deltaT;
            }
            finalT = items[numItems - 1].m_t;
            if (SegmentState.IsReadyForMaxContinue) {
                SegmentState.NewFinalOffset = spacing + mainCircle.ArclengthBetween(0f, finalT);
            } else {
                SegmentState.NewFinalOffset = mainCircle.ArclengthBetween(t, 1f);
            }
            m_itemCount = numItems;
            return true;
        }

        public override void DiscoverHoverState(VectorXZ position) {
            const float pointRadius = HOVER_POINTDISTANCE_THRESHOLD;
            const float anglePointRadius = pointRadius;
            const float angleLocusRadius = HOVER_ANGLELOCUS_DIAMETER;
            const float angleLocusDistanceThreshold = 0.40f;
            switch (m_currentState) {
            case ActiveState.ItemwiseLock:
            case ActiveState.MoveItemwiseItem:
                if (m_controlMode == ControlMode.ITEMWISE && m_mainCircle.IsCloseToCircle3XZ(HOVER_ITEMWISE_CURVEDISTANCE_THRESHOLD, position, out float hoverItemT)) {
                    m_hoverItemwiseT = hoverItemT;
                    m_hoverState = HoverState.ItemwiseItem;
                    return;
                }
                break;
            case ActiveState.LockIdle:
                if (m_itemCount >= (GetFenceMode() ? 1 : 2) || m_controlMode == ControlMode.ITEMWISE) {
                    bool angleObjectMode = m_itemType == ItemType.PROP;
                    VectorXZ angleCenter = m_items[HoverItemAngleCenterIndex].Position;
                    VectorXZ anglePos = CircleXZ.Position3FromAngleXZ(angleCenter, angleLocusRadius, m_hoverAngle);
                    VectorXZ spacingPos = GetFenceMode() ? m_fenceEndPoints[HoverItemPositionIndex] : m_items[HoverItemPositionIndex].Position;
                    if (spacingPos.IsInsideCircleXZ(pointRadius, position)) {
                        m_hoverState = m_controlMode == ControlMode.ITEMWISE ? HoverState.ItemwiseItem : HoverState.SpacingLocus;
                    } else if (angleObjectMode && anglePos.IsInsideCircleXZ(anglePointRadius, position)) {
                        m_hoverState = HoverState.AngleLocus;
                    } else if (angleObjectMode && angleCenter.IsNearCircleOutlineXZ(HOVER_ANGLELOCUS_DIAMETER, position, angleLocusDistanceThreshold)) {
                        m_hoverState = HoverState.AngleLocus;
                    } else if (VectorXZ.IsInsideCircleXZ(ControlPoint.m_controlPoints[0].m_position, pointRadius, position)) {
                        m_hoverState = HoverState.ControlPointFirst;
                    } else if (VectorXZ.IsInsideCircleXZ(ControlPoint.m_controlPoints[1].m_position, pointRadius, position)) {
                        m_hoverState = HoverState.ControlPointSecond;
                    } else if (m_mainCircle.IsCloseToCircle3XZ(HOVER_CURVEDISTANCE_THRESHOLD, position, out _)) {
                        m_hoverState = HoverState.Curve;
                    } else goto default;
                    return;
                }
                break;
            default:
                break;
            }
            m_hoverState = HoverState.Unbound;
        }

        public override void UpdateMiscHoverParameters() {
            bool fenceMode = GetFenceMode();
            if (m_itemCount >= (fenceMode ? 1 : 2) || m_controlMode == ControlMode.ITEMWISE) {
                switch (m_currentState) {
                case ActiveState.MoveSegment:
                    ControlPoint.PointInfo[] controlPoints = ControlPoint.m_controlPoints;
                    ControlPoint.PointInfo[] lockedControlPoints = ControlPoint.m_lockedControlPoints;
                    Vector3 translation = m_cachedPosition - m_lockedBackupCachedPosition;
                    controlPoints[0].m_position = lockedControlPoints[0].m_position + translation;
                    controlPoints[1].m_position = lockedControlPoints[1].m_position + translation;
                    controlPoints[2].m_position = lockedControlPoints[2].m_position + translation;
                    UpdateCurve();
                    UpdatePlacement();
                    break;
                case ActiveState.ChangeSpacing:
                    if (m_mainCircle.IsCloseToCircle3XZ(HOVER_CURVEDISTANCE_THRESHOLD * 12f, m_cachedPosition, out float hoverCurveT)) {
                        CircleXZ circle = (Settings.PerfectCircles) ? m_rawCircle : m_mainCircle;
                        if (fenceMode) {
                            float distance = (circle.Position(EMath.Clamp(hoverCurveT, m_items[0].m_t, 0.500f)) - circle.Position(0f)).MagnitudeXZ();
                            if (Settings.PerfectCircles) {
                                distance = EMath.Clamp(distance, SPACING_MIN, m_rawCircle.Diameter);
                            }
                            ItemInfo.ItemSpacing = distance;
                        } else { //non-fence mode
                            float distance = circle.m_radius * circle.AngleBetween(0f, EMath.Clamp(hoverCurveT, m_items[0].m_t, 0.995f));
                            if (Settings.PerfectCircles) {
                                distance = EMath.Clamp(distance, SPACING_MIN, 0.50f * m_mainCircle.Circumference);
                            }
                            ItemInfo.ItemSpacing = distance;
                        }
                    }
                    UpdateCurve();
                    UpdatePlacement(true, true);
                    break;
                case ActiveState.ChangeAngle:
                    Vector3 xAxis = m_vectorRight;
                    Vector3 yAxis = m_vectorDown;
                    if (m_angleMode == AngleMode.DYNAMIC) {
                        VectorXZ angleVector = m_cachedPosition - m_items[HoverItemAngleCenterIndex].Position;
                        angleVector.Normalize();
                        m_hoverAngle = angleVector.AngleSigned(xAxis, yAxis);
                        ItemInfo.m_itemAngleOffset = angleVector.AngleSigned(m_lockedBackupItemDirection, yAxis);
                    } else if (m_angleMode == AngleMode.SINGLE) {
                        VectorXZ angleVector = m_cachedPosition - m_items[HoverItemAngleCenterIndex].Position;
                        angleVector.Normalize();
                        float angle = angleVector.AngleSigned(xAxis, yAxis);
                        m_hoverAngle = angle;
                        ItemInfo.m_itemAngleSingle = angle + Mathf.PI;
                    }
                    UpdateCurve();
                    UpdatePlacement();
                    break;
                case ActiveState.ItemwiseLock:
                case ActiveState.MoveItemwiseItem:
                    UpdatePlacement();
                    break;
                }
            }
        }

        public override void RenderProgressiveSpacingFill(RenderManager.CameraInfo cameraInfo, float fillLength, float interval, float size, Color color, bool renderLimits, bool alphaBlend) {
            CircleXZ mainCircle = m_mainCircle;
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

        public override void Update() {
            //continuously update control points to follow mouse
            switch (m_currentState) {
            case ActiveState.CreatePointFirst:
                UpdatePlacement(false, false);
                break;
            case ActiveState.CreatePointSecond:
                ControlPoint.Modify(m_cachedPosition, 1);
                UpdatePlacement();
                goto UpdateCurve;
            case ActiveState.MoveSegment:
            case ActiveState.ChangeSpacing:
            case ActiveState.ChangeAngle:
            case ActiveState.LockIdle:
            case ActiveState.MaxFillContinue:
                UpdatePlacement();
                break;
            case ActiveState.MovePointFirst:
                ControlPoint.Modify(m_cachedPosition, 0);
                goto UpdatePlacement;
            case ActiveState.MovePointSecond:
                ControlPoint.Modify(m_cachedPosition, 1);
                goto UpdatePlacement;
            case ActiveState.MovePointThird:
                ControlPoint.Modify(m_cachedPosition, 2);
                goto UpdatePlacement;
            }
            return;
UpdatePlacement:
            UpdatePlacement();
UpdateCurve:
            UpdateCurve();
        }
    }
}
