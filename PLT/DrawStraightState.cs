using ColossalFramework.Math;
using EManagersLib;
using UnityEngine;
using static PropAnarchy.PLT.PropLineTool;

namespace PropAnarchy.PLT {
    public sealed class DrawStraightState : ActiveDrawState {
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
                RenderSegment(cameraInfo, m_mainSegment, 1.50f, 0f, curveWarningColor, false, true);
            }
            RenderSegment(cameraInfo, m_mainSegment, LINESIZE, 0f, createPointColor, false, true);
        }

        public override bool OnRenderOverlay(RenderManager.CameraInfo cameraInfo, ActiveState curState, Color createPointColor, Color curveWarningColor, Color copyPlaceColor) {
            ControlPoint.PointInfo[] controlPoints = ControlPoint.m_controlPoints;
            if (controlPoints[1].m_direction != VectorXZ.zero) {
                switch (curState) {
                case ActiveState.CreatePointSecond:
                case ActiveState.MaxFillContinue:
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
            m_cachedPosition = mousePosition;
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

        public override void OnToolUpdate() {
            base.OnToolUpdate();
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

        public override void DiscoverHoverState(VectorXZ position) {
            const float angleLocusDistanceThreshold = 0.40f;
            switch (m_currentState) {
            case ActiveState.ItemwiseLock:
            case ActiveState.MoveItemwiseItem:
                if (m_controlMode == ControlMode.ITEMWISE && m_mainSegment.IsCloseToSegmentXZ(HOVER_ITEMWISE_CURVEDISTANCE_THRESHOLD, position, out float hoverItemT)) {
                    m_hoverItemwiseT = hoverItemT;
                    m_hoverState = HoverState.ItemwiseItem;
                }
                return;
            case ActiveState.LockIdle:
                if (m_itemCount < (GetFenceMode() ? 1 : 2) && m_controlMode != ControlMode.ITEMWISE) goto default;
                bool angleObjectMode = m_itemType == ItemType.PROP;
                VectorXZ angleCenter = m_items[HoverItemAngleCenterIndex].Position;
                VectorXZ anglePos = CircleXZ.Position3FromAngleXZ(angleCenter, HOVER_ANGLELOCUS_DIAMETER, m_hoverAngle);
                VectorXZ spacingPos = GetFenceMode() ? m_fenceEndPoints[HoverItemPositionIndex] : m_items[HoverItemPositionIndex].Position;
                if (spacingPos.IsInsideCircleXZ(HOVER_POINTDISTANCE_THRESHOLD, position)) {
                    m_hoverState = m_controlMode == ControlMode.ITEMWISE ? HoverState.ItemwiseItem : HoverState.SpacingLocus;
                } else if (angleObjectMode && anglePos.IsInsideCircleXZ(HOVER_POINTDISTANCE_THRESHOLD, position)) {
                    m_hoverState = HoverState.AngleLocus;
                } else if (angleObjectMode && angleCenter.IsNearCircleOutlineXZ(HOVER_ANGLELOCUS_DIAMETER, position, angleLocusDistanceThreshold)) {
                    m_hoverState = HoverState.AngleLocus;
                } else if (VectorXZ.IsInsideCircleXZ(ControlPoint.m_controlPoints[0].m_position, HOVER_POINTDISTANCE_THRESHOLD, position)) {
                    m_hoverState = HoverState.ControlPointFirst;
                } else if (VectorXZ.IsInsideCircleXZ(ControlPoint.m_controlPoints[1].m_position, HOVER_POINTDISTANCE_THRESHOLD, position)) {
                    m_hoverState = HoverState.ControlPointSecond;
                } else if (m_mainSegment.IsCloseToSegmentXZ(HOVER_CURVEDISTANCE_THRESHOLD, position, out _)) {
                    m_hoverState = HoverState.Curve;
                } else {
                    goto default;
                }
                return;
            default:
                m_hoverState = HoverState.Unbound;
                break;
            }
        }

        public override void UpdateMiscHoverParameters() {
            if (m_itemCount >= (GetFenceMode() ? 1 : 2) || m_controlMode == ControlMode.ITEMWISE) {
                switch (m_currentState) {
                case ActiveState.MoveSegment:
                    ControlPoint.PointInfo[] controlPoints = ControlPoint.m_controlPoints;
                    ControlPoint.PointInfo[] lockedControlPoints = ControlPoint.m_lockedControlPoints;
                    VectorXZ translation = m_cachedPosition - m_lockedBackupCachedPosition;
                    controlPoints[0].m_position = lockedControlPoints[0].m_position + translation;
                    controlPoints[1].m_position = lockedControlPoints[1].m_position + translation;
                    controlPoints[2].m_position = lockedControlPoints[2].m_position + translation;
                    UpdateCurve();
                    UpdatePlacement();
                    break;
                case ActiveState.ChangeSpacing:
                    if (m_mainSegment.IsCloseToSegmentXZ(HOVER_CURVEDISTANCE_THRESHOLD * 8f, m_cachedPosition, out float hoverCurveT)) {
                        float curveT = EMath.Clamp(hoverCurveT, m_items[0].m_t, 0.995f);
                        VectorXZ linePosition = m_mainSegment.LinePosition(curveT); ;
                        if (GetFenceMode()) {
                            //since straight fence mode auto snaps to last fence endpoint
                            VectorXZ lineDistance = linePosition - m_mainSegment.LinePosition(0f);
                            ItemInfo.ItemSpacing = lineDistance.magnitude; // lineDistance.MagnitudeXZ();
                        } else { //non-fence mode
                            VectorXZ lineDistance = linePosition - m_mainSegment.LinePosition(m_items[0].m_t);
                            ItemInfo.ItemSpacing = lineDistance.magnitude; // lineDistance.MagnitudeXZ();
                        }
                    }
                    UpdateCurve();
                    UpdatePlacement(true, true);
                    break;
                case ActiveState.ChangeAngle:
                    Vector3 xAxis; xAxis.x = 1; xAxis.y = 0; xAxis.z = 0;
                    Vector3 yAxis; yAxis.x = 0; yAxis.y = 1; yAxis.z = 0;
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

        public override bool IsLengthLongEnough() => m_mainSegment.Length() >= (GetFenceMode() ? 0.75f * ItemInfo.ItemSpacing : ItemInfo.ItemSpacing);

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

        public override void UpdateCurve(ControlPoint.PointInfo[] controlPoints, int controlPointCount) {
            SegmentState.m_pendingPlacementUpdate = true;
            if (controlPointCount > 0) {
                m_mainSegment.a = controlPoints[0].m_position;
                m_mainSegment.b = controlPoints[1].m_position;
            }
        }

        public override bool ContinueDrawing(ControlPoint.PointInfo[] controlPoints, ref int controlPointCount) {
            Vector3 p1 = controlPoints[1].m_position;
            //if (GetFenceMode()) {
            //    Vector3 lastFenceEndPoint = SegmentState.LastFenceEndpoint;
            //    if (lastFenceEndPoint == EMath.Vector3Down) return false;
            //    p1 = lastFenceEndPoint;
            //}
            controlPoints[0].m_position = p1;
            controlPointCount = 1;
            SegmentState.IsContinueDrawing = true;
            return true;
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
                        //if (GetFenceMode()) {
                        //    ControlPoint.Modify(DrawMode.CurrentMode, SegmentState.m_segmentInfo.m_lastFenceEndpoint, 0, ActiveState.CreatePointSecond, DrawMode.Current);
                        //}
                        UpdateCurve(controlPoints, controlPointCount);
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
            Vector3 itemDir = m_mainSegment.Direction();
            for (int i = 0; i < itemCount; i++) {
                items[i].SetDirectionsXZ(itemDir);
            }
        }

        public override bool CalculateItemwisePosition(ItemInfo[] items, Vector3[] fenceEndPoints, bool fenceMode, float fencePieceLength, float initialOffset, VectorXZ lastFenceEndpoint) {
            float hoverItemwiseT = m_hoverItemwiseT;
            ref Segment3 mainSegment = ref m_mainSegment;
            if (fenceMode) {
                float deltaT = fencePieceLength / mainSegment.LinearSpeedXZ();
                float sumT = hoverItemwiseT + deltaT;
                //check if out of bounds
                if (sumT > 1f && mainSegment.LengthXZ() >= fencePieceLength) {
                    hoverItemwiseT += (1f - sumT);
                }
                Vector3 positionStart = mainSegment.LinePosition(hoverItemwiseT);
                fenceEndPoints[0] = positionStart;
                Vector3 positionEnd = mainSegment.LinePosition(hoverItemwiseT + deltaT);
                fenceEndPoints[1] = positionEnd;
                items[0].Position = EMath.Lerp(positionStart, positionEnd, 0.50f);
                return true;
            }
            items[0].m_t = hoverItemwiseT;
            items[0].Position = mainSegment.LinePosition(hoverItemwiseT);
            return true;
        }

        public override bool CalculateAllPositionsBySpacing(ItemInfo[] items, Vector3[] fenceEndPoints, bool fenceMode, float spacing, float initialOffset, VectorXZ lastFenceEndpoint) {
            int numItems, numItemsRaw;
            float initialT, finalT, deltaT;
            initialOffset = EMath.Abs(initialOffset);

            // first early exit condition
            if (spacing == 0 || !IsLengthLongEnough()) {
                m_itemCount = 0;
                return false;
            }
            ref Segment3 mainSegment = ref m_mainSegment;
            if (fenceMode) {
                float lengthFull = mainSegment.LengthXZ();
                float speed = mainSegment.LinearSpeedXZ();
                float lengthAfterFirst = SegmentState.IsMaxFillContinue ? lengthFull - initialOffset : lengthFull;
                float numItemsFloat = EMath.Abs(lengthAfterFirst / spacing);
                numItemsRaw = EMath.FloorToInt(numItemsFloat);
                numItems = EMath.Min(m_itemCount, EMath.Clamp(numItemsRaw, 0, MAX_ITEM_ARRAY_LENGTH));
                //add an extra item at the end if within 75% of spacing
                bool extraItem = false;
                float remainder = lengthAfterFirst % spacing;
                float remFraction = remainder / spacing;
                if (remFraction >= 0.75f && numItems < MAX_ITEM_ARRAY_LENGTH) {
                    numItems += 1;
                    extraItem = true;
                }
                //If not MaxFillContinue:
                //In straight fence mode, no segment-linking occurs
                //   so we don't use initialOffset here
                //the continuous draw resets the first control point to the last fence endpoint
                deltaT = spacing / speed;
                float t = 0f;
                //Max Fill Continue
                if (SegmentState.IsMaxFillContinue && initialOffset > 0f) {
                    t = initialOffset / lengthFull;
                }

                //calculate endpoints
                for (int i = 0; i < numItems + 1; i++) {
                    fenceEndPoints[i] = mainSegment.LinePosition(t);
                    t += deltaT;
                }

                //then calculate midpoints
                for (int i = 0; i < numItems; i++) {
                    items[i].Position = Vector3.Lerp(fenceEndPoints[i], fenceEndPoints[i + 1], 0.50f);
                }

                //linear fence fill
                bool realizedLinearFenceFill = false;
                if (Settings.LinearFenceFill) {
                    if (numItems > 0 && numItems < MAX_ITEM_ARRAY_LENGTH) {
                        realizedLinearFenceFill = true;
                    }
                    // if conditions for linear fence fill are met
                    if (realizedLinearFenceFill) {
                        // account for extra item
                        if (!extraItem) {
                            numItems++;
                        }
                        VectorXZ p0 = mainSegment.a;
                        VectorXZ p1 = mainSegment.b;
                        fenceEndPoints[numItems] = p1;

                        VectorXZ localX = (p1 - p0).normalized;
                        items[numItems - 1].Position = (p1 + (0.5f * spacing) * (p0 - p1).normalized) + (0.00390625f * localX) + (0.00390625f * new VectorXZ(localX.z, -1f * localX.x));
                    }
                }
                finalT = t - deltaT;
                Vector3 finalPos = mainSegment.LinePosition(finalT);

                // Prep for MaxFillContinue
                if (SegmentState.IsReadyForMaxContinue) {
                    SegmentState.NewFinalOffset = Vector3.Distance(mainSegment.a, finalPos);
                } else {
                    SegmentState.NewFinalOffset = Vector3.Distance(finalPos, mainSegment.b);
                }
            } else {
                float lengthFull = mainSegment.LengthXZ();
                float lengthAfterFirst = lengthFull - initialOffset;
                float speed = mainSegment.LinearSpeedXZ();

                //use ceiling for non-fence, because the point at the beginning is an extra point
                numItemsRaw = EMath.CeilToInt(lengthAfterFirst / spacing);
                numItems = EMath.Min(m_itemCount, EMath.Clamp(numItemsRaw, 0, MAX_ITEM_ARRAY_LENGTH));
                if (speed == 0) {
                    return false;
                }
                deltaT = spacing / speed;
                float t = 0f;
                if (initialOffset > 0f) {
                    //calculate initial _t
                    initialT = initialOffset / speed;
                    t = initialT;
                }

                for (int i = 0; i < numItems; i++) {
                    items[i].m_t = t;
                    items[i].Position = mainSegment.LinePosition(t);
                    t += deltaT;
                }
                if (numItems > 0) {
                    if (SegmentState.IsReadyForMaxContinue) {
                        SegmentState.NewFinalOffset = spacing + Vector3.Distance(mainSegment.a, items[numItems - 1].Position);
                    } else {
                        SegmentState.NewFinalOffset = spacing - Vector3.Distance(items[numItems - 1].Position, mainSegment.b);
                    }
                } else {
                    SegmentState.LastFenceEndpoint = EMath.Vector3Down;
                    SegmentState.LastFinalOffset = 0f;
                    //UpdatePlacement();
                }
            }
            m_itemCount = numItems;
            if (EMath.FloorToInt(numItemsRaw) > MAX_ITEM_ARRAY_LENGTH) {
                SegmentState.MaxItemCountExceeded = true;
            } else {
                SegmentState.MaxItemCountExceeded = false;
            }
            return true;
        }

        public override void RenderProgressiveSpacingFill(RenderManager.CameraInfo cameraInfo, float fillLength, float interval, float size, Color color, bool renderLimits, bool alphaBlend) {
            ref Segment3 mainSegment = ref m_mainSegment;
            if (!GetFenceMode()) {
                float firstItemT = m_items[0].m_t;
                RenderSegment(cameraInfo, mainSegment.Cut(firstItemT, firstItemT + fillLength / mainSegment.LinearSpeedXZ()),
                    size, 0f, new Color(color.r, color.g, color.b, 0.75f * color.a), renderLimits, alphaBlend);
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
