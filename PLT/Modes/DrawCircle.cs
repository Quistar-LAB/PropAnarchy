using EManagersLib;
using PropAnarchy.PLT.Extensions;
using PropAnarchy.PLT.MathUtils;
using UnityEngine;
using static PropAnarchy.PLT.PropLineTool;

namespace PropAnarchy.PLT.Modes {
    internal sealed class DrawCircle : DrawMode {
        private void ContinueDrawingFromLockMode(bool finalizePlacement) {
            //check if in fence mode and line is too short
            if (!ItemInfo.FenceMode && ItemInfo.Count > 0 && finalizePlacement && ItemInfo.FinalizePlacement(true, false)) {
                if (!PostCheckAndContinue()) {
                    ControlPoint.Reset();
                    GotoActiveState(ActiveState.CreatePointFirst);
                }
            }
        }

        internal override void OnToolGUI(Event e, bool isInsideUI) {
            ActiveState currentState = CurActiveState;
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
                    AddAction(() => ItemInfo.FinalizePlacement(true, true));
                    return;
                }
            }
            switch (currentState) {
            case ActiveState.CreatePointFirst:
                if (!isInsideUI && e.type == EventType.MouseDown && e.button == LEFTMOUSEBUTTON) {
                    SegmentState.FinalizeForPlacement(false);
                    ControlPoint.Add(m_cachedPosition);
                    GotoActiveState(ActiveState.CreatePointSecond);
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
                            PrevLockMode = CurLockMode;
                            GotoActiveState(ActiveState.LockIdle);
                        } else if (Settings.ControlMode == ControlMode.ItemWise) {
                            PrevLockMode = CurLockMode;
                            GotoActiveState(ActiveState.ItemwiseLock);
                        } else {
                            AddAction(() => {
                                ItemInfo.FinalizePlacement(true, false);
                                if (!PostCheckAndContinue()) {
                                    ControlPoint.Reset();
                                    GotoActiveState(ActiveState.CreatePointFirst);
                                }
                            });
                        }
                    } else if (e.button == RIGHTMOUSEBUTTON) {
                        ControlPoint.Cancel();
                        GotoActiveState(ActiveState.CreatePointFirst);
                        ControlPoint.Modify(m_mousePosition, 0);
                        UpdateCurve();
                    }
                }
                break;
            case ActiveState.LockIdle:
                if (!isInsideUI && e.type == EventType.MouseDown) {
                    if (e.button == LEFTMOUSEBUTTON) {
                        if (m_keyboardCtrlDown) {
                            if (Settings.ControlMode == ControlMode.ItemWise) {
                                GotoActiveState(ActiveState.ItemwiseLock);
                            } else {
                                AddAction(() => ContinueDrawingFromLockMode(true));
                            }
                        }
                        switch (CurHoverState) {
                        case HoverState.SpacingLocus:
                            GotoActiveState(ActiveState.ChangeSpacing);
                            break;
                        case HoverState.AngleLocus:
                            GotoActiveState(ActiveState.ChangeAngle);
                            break;
                        case HoverState.ControlPointFirst:
                            GotoActiveState(ActiveState.MovePointFirst);
                            ControlPoint.Modify(m_mousePosition, 0);
                            UpdateCurve();
                            UpdatePlacement();
                            break;
                        case HoverState.ControlPointSecond:
                            GotoActiveState(ActiveState.MovePointSecond);
                            ControlPoint.Modify(m_mousePosition, 1);
                            UpdateCurve();
                            UpdatePlacement();
                            break;
                        case HoverState.ControlPointThird:
                            GotoActiveState(ActiveState.MovePointThird);
                            ControlPoint.Modify(m_mousePosition, 2);
                            UpdateCurve();
                            UpdatePlacement();
                            break;
                        case HoverState.Curve:
                            GotoActiveState(ActiveState.MoveSegment);
                            break;
                        case HoverState.ItemwiseItem:
                            if (Settings.ControlMode == ControlMode.ItemWise) {
                                GotoActiveState(ActiveState.MoveItemwiseItem);
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
                    GotoActiveState(ActiveState.LockIdle);
                }
                break;
            case ActiveState.MovePointSecond:
                if (!isInsideUI && e.type == EventType.MouseDown) {
                    if (e.button == RIGHTMOUSEBUTTON) {
                        //reset second CP to original position
                        ControlPoint.Modify(ControlPoint.m_lockedControlPoints[1].m_position, 1);
                    }
                    GotoActiveState(ActiveState.LockIdle);
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
                    GotoActiveState(ActiveState.LockIdle);
                }
                break;
            case ActiveState.ChangeSpacing:
                if (!isInsideUI && e.type == EventType.MouseDown) {
                    if (e.button == RIGHTMOUSEBUTTON) {
                        //reset spacing to original value
                        ItemInfo.Spacing = ItemInfo.LockedSpacing;
                    }
                    GotoActiveState(ActiveState.LockIdle);
                }
                if (Settings.AutoDefaultSpacing) SetAutoSpacing(false);
                break;
            case ActiveState.ChangeAngle:
                if (!isInsideUI && e.type == EventType.MouseDown) {
                    if (e.button == RIGHTMOUSEBUTTON) {
                        //reset angle to original value
                        ItemInfo.Angle = ItemInfo.LockedAngle;
                    }
                    GotoActiveState(ActiveState.LockIdle);
                }
                break;
            case ActiveState.ItemwiseLock:
                if (!isInsideUI && e.type == EventType.MouseDown) {
                    if (e.button == LEFTMOUSEBUTTON) {
                        if (m_keyboardCtrlDown) {
                            GotoActiveState(ActiveState.LockIdle);
                        } else if (CurHoverState == HoverState.ItemwiseItem) {
                            AddAction(() => ItemInfo.FinalizePlacement(true, true));
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
                        ItemInfo.HoverItemwiseT = ItemInfo.LockedItemwiseT;
                    }
                    GotoActiveState(ActiveState.LockIdle);
                }
                break;
            case ActiveState.MaxFillContinue:
                if (Settings.ControlMode == ControlMode.ItemWise) {
                    GotoActiveState(ActiveState.ItemwiseLock);
                } else if (!isInsideUI && e.type == EventType.MouseDown) {
                    if ((e.button == LEFTMOUSEBUTTON && m_keyboardCtrlDown) || e.button == RIGHTMOUSEBUTTON) {
                        GotoActiveState(ActiveState.LockIdle);
                    } else if (e.button == LEFTMOUSEBUTTON && IsLengthLongEnough()) {
                        AddAction(() => ItemInfo.FinalizePlacement(true, false));
                        if (!PostCheckAndContinue()) {
                            ControlPoint.Reset();
                            GotoActiveState(ActiveState.CreatePointFirst);
                        }
                    }
                }
                break;
            }
        }

        public override void OnRenderGeometry(RenderManager.CameraInfo cameraInfo) {
            switch (CurActiveState) {
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
                int itemCount = ItemInfo.Count;
                ItemInfo.ItemData[] items = ItemInfo.Datas;
                for (int i = 0; i < itemCount; i++) {
                    items[i].RenderItem(cameraInfo);
                }
                break;
            }
        }

        public override void RenderLines(RenderManager.CameraInfo cameraInfo, Color createPointColor, Color curveWarningColor) {
            if (SegmentState.AllItemsValid) {
                RenderMainCircle(cameraInfo, m_mainCircle, LINESIZE, createPointColor, false, true);
            } else {
                RenderMainCircle(cameraInfo, m_mainCircle, LINESIZE, curveWarningColor, false, true);
            }
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
            switch (CurActiveState) {
            case ActiveState.CreatePointFirst:
                UpdatePlacement(false, false);
                break;
            case ActiveState.CreatePointSecond:
            case ActiveState.MovePointSecond:
                ControlPoint.Modify(mousePosition, 1);
                UpdatePlacement();
                break;
            case ActiveState.CreatePointThird:
            case ActiveState.MovePointThird:
                ControlPoint.Modify(mousePosition, 2);
                UpdatePlacement();
                break;
            case ActiveState.MovePointFirst:
                ControlPoint.Modify(mousePosition, 0);
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
            switch (CurActiveState) {
            case ActiveState.CreatePointFirst:
                if (SegmentState.IsPositionEqualToLastFenceEndpoint(ControlPoint.m_controlPoints[0].m_position)) {
                    SegmentState.ResetLastContinueParameters();
                } else if (ControlPoint.m_validPoints > 0) {
                    ControlPoint.m_validPoints = 0;
                }
                break;
            case ActiveState.CreatePointThird:
                ControlPoint.Cancel();
                GotoActiveState(ActiveState.CreatePointSecond);
                ControlPoint.Modify(m_mousePosition, 1);
                UpdateCurve();
                return;
            }
            UpdateCachedPosition(false);
            CheckPendingPlacement();
        }

        public override void OnToolUpdate() { }

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

        public override bool IsLengthLongEnough() => m_mainCircle.Diameter >= ItemInfo.Spacing;

        public override bool IsActiveStateAnItemRenderState() {
            switch (CurActiveState) {
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
            SegmentState.m_pendingPlacementUpdate = true;
            if (pointCount >= 1) {
                m_mainCircle = new CircleXZ(points[0].m_position, points[1].m_position, ItemInfo.Spacing);
            }
        }

        public override bool PostCheckAndContinue(ControlPoint.PointInfo[] controlPoints, ref int controlPointCount) {
            if (CurLockMode == LockingMode.Off) {
                if (SegmentState.IsReadyForMaxContinue) {
                    UpdatePlacement(true, false);
                    GotoActiveState(ActiveState.MaxFillContinue);
                } else {
                    if (ContinueDrawing(controlPoints, ref controlPointCount)) {
                        GotoActiveState(ActiveState.CreatePointSecond);
                        ControlPoint.Modify(m_mousePosition, 1);
                        UpdatePlacement(true, false);
                    } else {
                        ControlPoint.Reset();
                        GotoActiveState(ActiveState.CreatePointFirst);
                    }
                }
            } else if (CurLockMode == LockingMode.Lock) { //Locking is enabled
                PrevLockMode = CurLockMode;
                GotoActiveState(ActiveState.LockIdle);
            }
            return true;
        }

        public override void RevertDrawingFromLockMode() {
            GotoActiveState(ActiveState.CreatePointSecond);
            ControlPoint.Modify(m_mousePosition, 1); //update position of first point
            UpdateCurve();
            UpdatePlacement(false, false);
        }

        public override void CalculateAllDirections(ItemInfo.ItemData[] items, bool fenceMode) {
            int itemCount = ItemInfo.Count;
            if (fenceMode) {
                for (int i = 0; i < itemCount; i++) {
                    items[i].SetDirectionsXZ(items[i + 1].m_fenceEndPoint - items[i].m_fenceEndPoint);
                }
            } else {
                CircleXZ mainCircle = m_mainCircle;
                for (int i = 0; i < itemCount; i++) {
                    items[i].SetDirectionsXZ(mainCircle.Tangent(items[i].m_t));
                }
            }
        }

        public override bool CalculateItemwisePosition(ItemInfo.ItemData[] items, bool fenceMode, float fencePieceLength, float initialOffset, VectorXZ lastFenceEndpoint) {
            float hoverItemwiseT = ItemInfo.HoverItemwiseT;
            if (fenceMode) {
                Vector3 positionStart, positionEnd;
                if (m_mainCircle.m_radius == 0f || fencePieceLength > m_mainCircle.Diameter) {
                    ItemInfo.Count = 0;
                    return false;
                }
                float itemTStart = hoverItemwiseT;
                float deltaT = m_mainCircle.ChordDeltaT(fencePieceLength);
                if (deltaT <= 0f || deltaT >= 1f) {
                    ItemInfo.Count = 0;
                    return false;
                }
                items[0].m_fenceEndPoint = positionStart = m_mainCircle.Position(itemTStart);
                items[1].m_fenceEndPoint = positionEnd = m_mainCircle.Position(itemTStart + deltaT);
                items[0].Position = Vector3.Lerp(positionStart, positionEnd, 0.50f);
                return true;
            }
            items[0].m_t = hoverItemwiseT;
            items[0].Position = m_mainCircle.Position(hoverItemwiseT);
            return true;
        }

        public override bool CalculateAllPositionsBySpacing(ItemInfo.ItemData[] items, bool fenceMode, float spacing, float initialOffset, VectorXZ lastFenceEndpoint) {
            Vector3 position, center, radiusVector;
            Quaternion rotation;
            int numItems, numItemsRaw;
            float deltaT, t, penultimateT, finalT;
            CircleXZ mainCircle = m_mainCircle;
            if (fenceMode) {
                float chordAngle = mainCircle.ChordAngle(spacing);
                if (chordAngle <= 0f || chordAngle > Mathf.PI || mainCircle.m_radius <= 0f) {
                    ItemInfo.Count = 0;
                    return false;
                }
                float initialAngle = initialOffset / mainCircle.m_radius;
                float angleAfterFirst = SegmentState.IsMaxFillContinue ? 2f * Mathf.PI - initialAngle : 2f * Mathf.PI;
                if (Settings.PerfectCircles) {
                    numItemsRaw = EMath.Clamp(EMath.RoundToInt(angleAfterFirst / chordAngle), 0, MAX_ITEM_ARRAY_LENGTH);
                    numItems = numItemsRaw;
                } else {
                    numItemsRaw = EMath.Clamp(EMath.FloorToInt(angleAfterFirst / chordAngle), 0, MAX_ITEM_ARRAY_LENGTH);
                    numItems = numItemsRaw;
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
                rotation = Quaternion.AngleAxis(-1f * chordAngle * Mathf.Rad2Deg, EMath.Vector3Up);
                //calculate endpoints
                for (int i = 0; i < numItems + 1; i++) {
                    penultimateT = t;
                    items[i].m_fenceEndPoint = position;
                    radiusVector = rotation * radiusVector;
                    position = center + radiusVector;
                    t += deltaT;
                }
                //then calculate midpoints
                for (int i = 0; i < numItems; i++) {
                    items[i].Position = Vector3.Lerp(items[i].m_fenceEndPoint, items[i + 1].m_fenceEndPoint, 0.50f);
                }
                //prep for MaxFillContinue
                if (SegmentState.IsReadyForMaxContinue) {
                    SegmentState.NewFinalOffset = mainCircle.ArclengthBetween(0f, penultimateT);
                } else {
                    SegmentState.NewFinalOffset = mainCircle.ArclengthBetween(t, 1f);
                }
                UpdateFinalPrefab(items, numItems);
                ItemInfo.Count = numItems;
                SegmentState.NewFenceEndPoint = items[numItems].m_fenceEndPoint;
                return true;
            }
            deltaT = mainCircle.DeltaT(spacing);
            if (deltaT <= 0f || deltaT > 1f || mainCircle.m_radius <= 0f) {
                ItemInfo.Count = 0;
                return false;
            }
            t = 0f;
            float remainingSpace = mainCircle.Circumference;
            if (SegmentState.IsMaxFillContinue) {
                if (mainCircle.Circumference > 0f) {
                    t = initialOffset / mainCircle.Circumference;
                    remainingSpace -= initialOffset;
                } else {
                    ItemInfo.Count = 0;
                    return false;
                }
            }
            //use ceiling for non-fence, because the point at the beginning is an extra point
            numItemsRaw = EMath.CeilToInt(remainingSpace / spacing);
            numItems = EMath.Clamp(numItemsRaw, 0, MAX_ITEM_ARRAY_LENGTH);
            position = mainCircle.Position(t);
            center = mainCircle.m_center;
            radiusVector = position - center;
            float deltaAngle = mainCircle.DeltaAngle(spacing);
            rotation = Quaternion.AngleAxis(-1f * deltaAngle * Mathf.Rad2Deg, EMath.Vector3Up);
            for (int i = 0; i < numItems; i++) {
                items[i].m_t = t;
                items[i].Position = position;
                radiusVector = rotation * radiusVector;
                position = center + radiusVector;
                t += deltaT;
            }
            if (numItems > 0) {
                finalT = items[numItems - 1].m_t;
                if (SegmentState.IsReadyForMaxContinue) {
                    SegmentState.NewFinalOffset = spacing + mainCircle.ArclengthBetween(0f, finalT);
                } else {
                    SegmentState.NewFinalOffset = mainCircle.ArclengthBetween(t, 1f);
                }
                UpdateFinalPrefab(items, numItems);
            }
            ItemInfo.Count = numItems;
            return true;
        }

        public override void DiscoverHoverState(VectorXZ position) {
            const float pointRadius = HOVER_POINTDISTANCE_THRESHOLD;
            const float anglePointRadius = pointRadius;
            const float angleLocusRadius = HOVER_ANGLELOCUS_DIAMETER;
            const float angleLocusDistanceThreshold = 0.40f;
            switch (CurActiveState) {
            case ActiveState.ItemwiseLock:
            case ActiveState.MoveItemwiseItem:
                if (Settings.ControlMode == ControlMode.ItemWise && m_mainCircle.IsCloseToCircle3XZ(HOVER_ITEMWISE_CURVEDISTANCE_THRESHOLD, position, out float hoverItemT)) {
                    ItemInfo.HoverItemwiseT = hoverItemT;
                    CurHoverState = HoverState.ItemwiseItem;
                    return;
                }
                break;
            case ActiveState.LockIdle:
                if (ItemInfo.Count >= (ItemInfo.FenceMode ? 1 : 2) || Settings.ControlMode == ControlMode.ItemWise) {
                    bool angleObjectMode = ItemInfo.Type == ItemType.Prop;
                    VectorXZ angleCenter = ItemInfo.Datas[HoverItemAngleCenterIndex].Position;
                    VectorXZ anglePos = CircleXZ.Position3FromAngleXZ(angleCenter, angleLocusRadius, ItemInfo.HoverAngle);
                    VectorXZ spacingPos = ItemInfo.FenceMode ? ItemInfo.Datas[HoverItemPositionIndex].m_fenceEndPoint : ItemInfo.Datas[HoverItemPositionIndex].Position;
                    if (spacingPos.IsInsideCircleXZ(pointRadius, position)) {
                        CurHoverState = Settings.ControlMode == ControlMode.ItemWise ? HoverState.ItemwiseItem : HoverState.SpacingLocus;
                    } else if (angleObjectMode && anglePos.IsInsideCircleXZ(anglePointRadius, position)) {
                        CurHoverState = HoverState.AngleLocus;
                    } else if (angleObjectMode && angleCenter.IsNearCircleOutlineXZ(HOVER_ANGLELOCUS_DIAMETER, position, angleLocusDistanceThreshold)) {
                        CurHoverState = HoverState.AngleLocus;
                    } else if (VectorXZ.IsInsideCircleXZ(ControlPoint.m_controlPoints[0].m_position, pointRadius, position)) {
                        CurHoverState = HoverState.ControlPointFirst;
                    } else if (VectorXZ.IsInsideCircleXZ(ControlPoint.m_controlPoints[1].m_position, pointRadius, position)) {
                        CurHoverState = HoverState.ControlPointSecond;
                    } else if (m_mainCircle.IsCloseToCircle3XZ(HOVER_CURVEDISTANCE_THRESHOLD, position, out _)) {
                        CurHoverState = HoverState.Curve;
                    } else goto default;
                    return;
                }
                break;
            default:
                break;
            }
            CurHoverState = HoverState.Unbound;
        }

        public override void UpdateMiscHoverParameters() {
            if (ItemInfo.Count >= (ItemInfo.FenceMode ? 1 : 2) || Settings.ControlMode == ControlMode.ItemWise) {
                switch (CurActiveState) {
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
                    CircleXZ circle = m_mainCircle;
                    if (circle.IsCloseToCircle3XZ(HOVER_CURVEDISTANCE_THRESHOLD * 12f, m_cachedPosition, out float hoverCurveT)) {
                        if (ItemInfo.FenceMode) {
                            float curveT = EMath.Clamp(hoverCurveT, ItemInfo.Datas[0].m_t, 0.500f);
                            Vector3 curveDistance = circle.Position(curveT) - circle.Position(0f);
                            float distance = curveDistance.MagnitudeXZ();
                            if (Settings.PerfectCircles) {
                                distance = EMath.Clamp(distance, SPACING_MIN, circle.Diameter);
                            }
                            ItemInfo.Spacing = distance;
                        } else { //non-fence mode
                            float curveT = EMath.Clamp(hoverCurveT, ItemInfo.Datas[0].m_t, 0.995f);
                            float distance = circle.m_radius * circle.AngleBetween(0f, curveT);
                            if (Settings.PerfectCircles) {
                                distance = EMath.Clamp(distance, SPACING_MIN, 0.50f * circle.Circumference);
                            }
                            ItemInfo.Spacing = distance;
                        }
                    }
                    UpdateCurve();
                    UpdatePlacement(true, true);
                    break;
                case ActiveState.ChangeAngle:
                    VectorXZ angleVector = m_cachedPosition - ItemInfo.Datas[HoverItemAngleCenterIndex].Position;
                    angleVector.Normalize();
                    if (Settings.AngleMode == AngleMode.Dynamic) {
                        ItemInfo.Angle = angleVector.AngleSigned(ItemInfo.LockedDirection, EMath.Vector3Up);
                    } else {
                        ItemInfo.Angle = angleVector.AngleSigned(EMath.Vector3Right, EMath.Vector3Up) + Mathf.PI;
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
            if (ItemInfo.FenceMode || mainCircle.m_radius <= 0f) return;
            int numItems = EMath.CeilToInt(fillLength / interval);
            float deltaT = interval / mainCircle.Circumference;
            Quaternion rotation = Quaternion.AngleAxis(deltaT * -360f, EMath.Vector3Up);
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
            switch (CurActiveState) {
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
