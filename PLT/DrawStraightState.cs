using ColossalFramework.Math;
using UnityEngine;
using static PropAnarchy.PLT.PropLineTool;

namespace PropAnarchy.PLT {
    public class DrawStraightState : ActiveDrawState {
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
                        if (m_keyboardCtrlDown && m_controlMode == ControlMode.ITEMWISE) {
                            GoToActiveState(ActiveState.ItemwiseLock);
                        } else if (m_keyboardCtrlDown) {
                            AddAction(() => ContinueDrawingFromLockMode(true));
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
            RenderSegment(cameraInfo, m_mainSegment, 1.00f, 0f, createPointColor, false, true);
        }

        public override bool OnRenderOverlay(RenderManager.CameraInfo cameraInfo, ActiveState curState, Color createPointColor, Color curveWarningColor, Color copyPlaceColor) {
            Vector3 vectorZero; vectorZero.x = 0f; vectorZero.y = 0f; vectorZero.z = 0f;
            ControlPoint.PointInfo[] controlPoints = ControlPoint.m_controlPoints;
            if (controlPoints[1].m_direction != vectorZero) {
                switch (curState) {
                case ActiveState.CreatePointSecond:
                case ActiveState.MaxFillContinue:
                    RenderLines(cameraInfo, createPointColor, curveWarningColor);
                    if (SegmentState.IsReadyForMaxContinue || SegmentState.IsMaxFillContinue) {
                        RenderMaxFillContinueMarkers(cameraInfo);
                    }
                    RenderCircle(cameraInfo, controlPoints[0].m_position, 0.10f, createPointColor, false, true);
                    RenderCircle(cameraInfo, controlPoints[1].m_position, 0.10f, createPointColor, false, true);
                    break;
                }
                return true;
            }
            return false;
        }

        public override void OnSimulationStep() {
            switch (m_currentState) {
            case ActiveState.CreatePointFirst:
                ControlPoint.Modify(m_mousePosition, 0);
                UpdateCurve();
                UpdatePlacement(false, false);
                break;
            case ActiveState.CreatePointSecond:
                ControlPoint.Modify(m_mousePosition, 1);
                UpdateCurve();
                UpdatePlacement(false, false);
                break;
            }
        }

        public override void OnToolLateUpdate() {
            base.OnToolLateUpdate();
        }

        public override void OnToolUpdate() {
            base.OnToolUpdate();
        }

        public override void DiscoverHoverState(Vector3 position) {
            //check for itemwise first before classic lock mode
            if (m_controlMode == ControlMode.ITEMWISE && (m_currentState == ActiveState.ItemwiseLock || m_currentState == ActiveState.MoveItemwiseItem)) {
                if (m_mainSegment.IsCloseToSegmentXZ(HOVER_ITEMWISE_CURVEDISTANCE_THRESHOLD, position, out float hoverItemT)) {
                    m_hoverItemwiseT = hoverItemT;
                    m_hoverState = HoverState.ItemwiseItem;
                } else {
                    m_hoverState = HoverState.Unbound;
                    return;
                }
            }
            //check for classic lock mode
            if (m_currentState != ActiveState.LockIdle) {
                return;
            }
            if (m_itemCount < (GetFenceMode() ? 1 : 2) && m_controlMode != ControlMode.ITEMWISE) {
                m_hoverState = HoverState.Unbound;
                return;
            }

            const float angleLocusDistanceThreshold = 0.40f;
            bool angleObjectMode = m_itemType == ItemType.PROP;
            Vector3 angleCenter = m_items[HoverItemAngleCenterIndex].Position;
            Vector3 anglePos = Circle2.Position3FromAngleXZ(angleCenter, HOVER_ANGLELOCUS_DIAMETER, m_hoverAngle);
            Vector3 spacingPos = GetFenceMode() ? m_fenceEndPoints[HoverItemPositionIndex] : m_items[HoverItemPositionIndex].Position;
            if (spacingPos.IsInsideCircleXZ(HOVER_POINTDISTANCE_THRESHOLD, position)) {
                m_hoverState = m_controlMode == ControlMode.ITEMWISE ? HoverState.ItemwiseItem : HoverState.SpacingLocus;
            } else if (angleObjectMode && anglePos.IsInsideCircleXZ(HOVER_POINTDISTANCE_THRESHOLD, position)) {
                m_hoverState = HoverState.AngleLocus;
            } else if (angleObjectMode && angleCenter.IsNearCircleOutlineXZ(HOVER_ANGLELOCUS_DIAMETER, position, angleLocusDistanceThreshold)) {
                m_hoverState = HoverState.AngleLocus;
            } else if (ControlPoint.m_controlPoints[0].m_position.IsInsideCircleXZ(HOVER_POINTDISTANCE_THRESHOLD, position)) {
                m_hoverState = HoverState.ControlPointFirst;
            } else if (ControlPoint.m_controlPoints[1].m_position.IsInsideCircleXZ(HOVER_POINTDISTANCE_THRESHOLD, position)) {
                m_hoverState = HoverState.ControlPointSecond;
            } else if (m_mainSegment.IsCloseToSegmentXZ(HOVER_CURVEDISTANCE_THRESHOLD, position, out _)) {
                m_hoverState = HoverState.Curve;
            } else {
                m_hoverState = HoverState.Unbound;
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
                    UpdateCurve();
                    UpdatePlacement();
                    break;
                case ActiveState.ChangeSpacing:
                    if (m_mainSegment.IsCloseToSegmentXZ(HOVER_CURVEDISTANCE_THRESHOLD * 8f, m_cachedPosition, out float hoverCurveT)) {
                        float curveT = Mathf.Clamp(hoverCurveT, m_items[0].m_t, 0.995f);
                        Vector3 linePosition = m_mainSegment.LinePosition(curveT); ;
                        if (GetFenceMode()) {
                            //since straight fence mode auto snaps to last fence endpoint
                            Vector3 lineDistance = linePosition - m_mainSegment.LinePosition(0f);
                            ItemInfo.ItemSpacing = lineDistance.MagnitudeXZ();
                        } else { //non-fence mode
                            Vector3 lineDistance = linePosition - m_mainSegment.LinePosition(m_items[0].m_t);
                            ItemInfo.ItemSpacing = lineDistance.MagnitudeXZ();
                        }
                    }
                    UpdateCurve();
                    UpdatePlacement(true, true);
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

        public override bool IsLengthLongEnough() => m_mainSegment.LengthXZ() >= (GetFenceMode() ? 0.75f * ItemInfo.ItemSpacing : ItemInfo.ItemSpacing);

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

        public override void UpdateCurve(ControlPoint.PointInfo[] cachedControlPoints, int cachedControlPointCount) {
            SegmentState.m_pendingPlacementUpdate = true;
            if (cachedControlPointCount >= 1) {
                m_mainSegment.a = cachedControlPoints[0].m_position;
                m_mainSegment.b = cachedControlPoints[1].m_position;
            }
        }

        public override bool ContinueDrawing(ControlPoint.PointInfo[] controlPoints, ref int controlPointCount) {
            Vector3 vectorDown; vectorDown.x = 0; vectorDown.y = -1; vectorDown.z = 0;
            Vector3 p1 = controlPoints[1].m_position;
            if (GetFenceMode()) {
                Vector3 lastFenceEndPoint = SegmentState.LastFenceEndpoint;
                if (lastFenceEndPoint == vectorDown) return false;
                p1 = lastFenceEndPoint;
            }
            controlPoints[0].m_position = p1;
            controlPointCount = 1;
            SegmentState.IsContinueDrawing = true;
            return true;
        }

        public override bool PostCheckAndContinue(ControlPoint.PointInfo[] controlPoints, ControlPoint.PointInfo[] cachedControlPoints, ref int controlPointCount) {
            if (m_lockingMode == LockingMode.Off) {
                if (SegmentState.IsReadyForMaxContinue) {
                    UpdatePlacement(true, false);
                    GoToActiveState(ActiveState.MaxFillContinue);
                } else {
                    if (ContinueDrawing(controlPoints, ref controlPointCount)) {
                        GoToActiveState(ActiveState.CreatePointSecond);
                        ControlPoint.Modify(DrawMode.CurrentMode, m_mousePosition, 1, ActiveState.CreatePointSecond, DrawMode.Current);
                        if (GetFenceMode()) {
                            ControlPoint.Modify(DrawMode.CurrentMode, SegmentState.m_segmentInfo.m_lastFenceEndpoint, 0, ActiveState.CreatePointSecond, DrawMode.Current);
                        }
                        UpdateCurve(cachedControlPoints, controlPointCount);
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

        public override void CalculateAllDirections() {
            int itemCount = m_itemCount;
            Vector3 itemDir = m_mainSegment.Direction();
            ItemInfo[] items = m_items;
            for (int i = 0; i < itemCount; i++) {
                items[i].SetDirectionsXZ(itemDir);
            }
        }

        public override bool CalculateItemwisePosition(float fencePieceLength, float initialOffset, Vector3 lastFenceEndpoint) {
            ItemInfo[] items = m_items;
            float hoverItemwiseT = m_hoverItemwiseT;
            ref Segment3 mainSegment = ref m_mainSegment;
            Vector3[] fenceEndPoints = m_fenceEndPoints;
            if (GetFenceMode()) {
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
                items[0].Position = Vector3.Lerp(positionStart, positionEnd, 0.50f);
                return true;
            }
            items[0].m_t = hoverItemwiseT;
            items[0].Position = mainSegment.LinePosition(hoverItemwiseT);
            return true;
        }

        public override bool CalculateAllPositionsBySpacing(float spacing, float initialOffset, Vector3 lastFenceEndpoint) {
            int numItems, numItemsRaw;
            float t, deltaT;
            Segment3 mainSegment = m_mainSegment;
            Vector3[] fenceEndPoints = m_fenceEndPoints;
            ItemInfo[] items = m_items;
            if (GetFenceMode()) {
                float lengthFull = mainSegment.LengthXZ();
                float lengthAfterFirst = SegmentState.IsMaxFillContinue ? lengthFull - initialOffset : lengthFull;
                numItemsRaw = Mathf.FloorToInt(Mathf.Abs(lengthAfterFirst / spacing));
                numItems = Mathf.Min(m_itemCount, Mathf.Clamp(numItemsRaw, 0, MAX_ITEM_ARRAY_LENGTH));
                //add an extra item at the end if within 75% of spacing
                bool extraItem = false;
                if ((lengthAfterFirst % spacing) / spacing >= 0.75f && numItems < MAX_ITEM_ARRAY_LENGTH) {
                    numItems += 1;
                    extraItem = true;
                }
                float finalT;
                deltaT = spacing / mainSegment.LinearSpeedXZ();
                t = 0f;
                if (SegmentState.IsMaxFillContinue && initialOffset > 0f) {
                    t = initialOffset / lengthFull;
                }
                for (int i = 0; i < numItems + 1; i++) {
                    fenceEndPoints[i] = mainSegment.LinePosition(t);
                    t += deltaT;
                }
                for (int i = 0; i < numItems; i++) {
                    items[i].Position = Vector3.Lerp(fenceEndPoints[i], fenceEndPoints[i + 1], 0.50f);
                }
                bool realizedLinearFenceFill = false;
                if (Settings.LinearFenceFill) {
                    if (numItems > 0 && numItems < MAX_ITEM_ARRAY_LENGTH) {
                        if (numItems == 1) {
                            if (lengthFull > spacing) realizedLinearFenceFill = true;
                        } else realizedLinearFenceFill = true;
                    }
                    //if conditions for linear fence fill are met
                    if (realizedLinearFenceFill) {
                        //account for extra item
                        if (!extraItem) numItems++;
                        Vector3 p0 = mainSegment.a;
                        Vector3 p1 = mainSegment.b;
                        p0.y = 0f;
                        p1.y = 0f;
                        fenceEndPoints[numItems] = p1;
                        Vector3 localX = (p1 - p0).normalized;
                        Vector3 localZ = new Vector3(localX.z, 0f, -1f * localX.x);
                        Vector3 finalOffset = (0.00390625f * localX) + (0.00390625f * localZ);
                        Vector3 finalFenceMidpoint = p1 + (0.5f * spacing) * ((p0 - p1).normalized);
                        finalFenceMidpoint += finalOffset;    //correct for z-fighting
                        items[numItems - 1].Position = finalFenceMidpoint;
                    }
                }
                finalT = t - deltaT;
                Vector3 finalPos = mainSegment.LinePosition(finalT);
                //prep for MaxFillContinue
                if (SegmentState.IsReadyForMaxContinue) {
                    SegmentState.NewFinalOffset = Vector3.Distance(mainSegment.a, finalPos);
                } else {
                    SegmentState.NewFinalOffset = Vector3.Distance(finalPos, mainSegment.b);
                }
                m_itemCount = numItems;
                return true;
            }
            float speed = mainSegment.LinearSpeedXZ();
            //use ceiling for non-fence, because the point at the beginning is an extra point
            numItems = Mathf.Min(m_itemCount, Mathf.Clamp(Mathf.CeilToInt((mainSegment.LengthXZ() - initialOffset) / spacing), 0, MAX_ITEM_ARRAY_LENGTH));
            if (speed == 0) {
                m_itemCount = 0;
                return false;
            }
            deltaT = spacing / speed;
            t = 0f;
            if (initialOffset > 0f) {
                t = initialOffset / speed;
            }
            for (int i = 0; i < numItems; i++) {
                items[i].m_t = t;
                items[i].Position = mainSegment.LinePosition(t);
                t += deltaT;
            }
            if (SegmentState.IsReadyForMaxContinue) {
                SegmentState.NewFinalOffset = spacing + Vector3.Distance(mainSegment.a, items[numItems - 1].Position);
            } else {
                SegmentState.NewFinalOffset = spacing - Vector3.Distance(items[numItems - 1].Position, mainSegment.b);
            }
            m_itemCount = numItems;
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
