using ColossalFramework.Math;
using EManagersLib;
using PropAnarchy.PLT.Extensions;
using PropAnarchy.PLT.MathUtils;
using UnityEngine;
using static PropAnarchy.PLT.PropLineTool;

namespace PropAnarchy.PLT.Modes {
    internal sealed class DrawStraight : DrawMode {
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
                        /*
                    case HoverState.ControlPointThird:
                        GotoActiveState(ActiveState.MovePointThird);
                        ControlPoint.Modify(m_mousePosition, 2);
                        UpdateCurve();
                        UpdatePlacement();
                        break;
                        */
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
                    if (IsLengthLongEnough()) {
                        GotoActiveState(ActiveState.LockIdle);
                    }
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
                if (!isInsideUI) {
                    if (e.type == EventType.MouseDown) {
                        if (e.button == LEFTMOUSEBUTTON) {
                            if (m_keyboardCtrlDown) {
                                GotoActiveState(ActiveState.LockIdle);
                            } else if (CurHoverState == HoverState.ItemwiseItem) {
                                AddAction(() => ItemInfo.FinalizePlacement(true, true));
                            } else {
                                UpdatePlacement();
                            }
                        } else if (e.button == RIGHTMOUSEBUTTON) {
                            RevertDrawingFromLockMode();
                        }
                    } else if (m_keyboardCtrlDown) {
                        ContinueDrawingFromLockMode(false);
                    }
                }
                break;
            case ActiveState.MoveItemwiseItem:
                if (!isInsideUI && e.type == EventType.MouseDown) {
                    if (e.button == RIGHTMOUSEBUTTON) {
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
                    if (e.button == RIGHTMOUSEBUTTON && m_keyboardCtrlDown) {
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
                RenderSegment(cameraInfo, m_mainSegment, LINESIZE, 0f, createPointColor, false, true);
            } else {
                RenderSegment(cameraInfo, m_mainSegment, LINESIZE, 0f, curveWarningColor, false, true);
            }
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
            switch (CurActiveState) {
            case ActiveState.CreatePointFirst:
                UpdatePlacement(false, false);
                break;
            case ActiveState.MovePointFirst:
                ControlPoint.Modify(mousePosition, 0);
                UpdatePlacement();
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

        public override void OnToolUpdate() { }

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
                break;
            }
            UpdateCachedPosition(false);
            CheckPendingPlacement();
        }

        public override void DiscoverHoverState(VectorXZ position) {
            const float angleLocusDistanceThreshold = 0.40f;
            switch (CurActiveState) {
            case ActiveState.ItemwiseLock:
            case ActiveState.MoveItemwiseItem:
                if (Settings.ControlMode == ControlMode.ItemWise && m_mainSegment.IsCloseToSegmentXZ(HOVER_ITEMWISE_CURVEDISTANCE_THRESHOLD, position, out float hoverItemT)) {
                    ItemInfo.HoverItemwiseT = hoverItemT;
                    CurHoverState = HoverState.ItemwiseItem;
                }
                break;
            case ActiveState.LockIdle:
                if (ItemInfo.Count < (ItemInfo.FenceMode ? 1 : 2) && Settings.ControlMode != ControlMode.ItemWise) {
                    CurHoverState = HoverState.Unbound;
                    break;
                }
                bool angleObjectMode = ItemInfo.Type == ItemType.Prop;
                ItemInfo.ItemData[] itemDatas = ItemInfo.Datas;
                VectorXZ angleCenter = itemDatas[HoverItemAngleCenterIndex].Position;
                VectorXZ anglePos = CircleXZ.Position3FromAngleXZ(angleCenter, HOVER_ANGLELOCUS_DIAMETER, ItemInfo.HoverAngle);
                VectorXZ spacingPos = ItemInfo.FenceMode ? itemDatas[HoverItemPositionIndex].m_fenceEndPoint : itemDatas[HoverItemPositionIndex].Position;
                if (spacingPos.IsInsideCircleXZ(HOVER_POINTDISTANCE_THRESHOLD, position)) {
                    CurHoverState = Settings.ControlMode == ControlMode.ItemWise ? HoverState.ItemwiseItem : HoverState.SpacingLocus;
                } else if (angleObjectMode && anglePos.IsInsideCircleXZ(HOVER_POINTDISTANCE_THRESHOLD, position)) {
                    CurHoverState = HoverState.AngleLocus;
                } else if (angleObjectMode && angleCenter.IsNearCircleOutlineXZ(HOVER_ANGLELOCUS_DIAMETER, position, angleLocusDistanceThreshold)) {
                    CurHoverState = HoverState.AngleLocus;
                } else if (VectorXZ.IsInsideCircleXZ(ControlPoint.m_controlPoints[0].m_position, HOVER_POINTDISTANCE_THRESHOLD, position)) {
                    CurHoverState = HoverState.ControlPointFirst;
                } else if (VectorXZ.IsInsideCircleXZ(ControlPoint.m_controlPoints[1].m_position, HOVER_POINTDISTANCE_THRESHOLD, position)) {
                    CurHoverState = HoverState.ControlPointSecond;
                } else if (m_mainSegment.IsCloseToSegmentXZ(HOVER_CURVEDISTANCE_THRESHOLD, position, out _)) {
                    CurHoverState = HoverState.Curve;
                } else {
                    CurHoverState = HoverState.Unbound;
                }
                break;
            }
        }

        public override void UpdateMiscHoverParameters() {
            if (ItemInfo.Count >= (ItemInfo.FenceMode ? 1 : 2) || Settings.ControlMode == ControlMode.ItemWise) {
                switch (CurActiveState) {
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
                        float curveT = EMath.Clamp(hoverCurveT, ItemInfo.Datas[0].m_t, 0.995f);
                        VectorXZ linePosition = m_mainSegment.LinePosition(curveT); ;
                        if (ItemInfo.FenceMode) {
                            //since straight fence mode auto snaps to last fence endpoint
                            VectorXZ lineDistance = linePosition - m_mainSegment.LinePosition(0f);
                            ItemInfo.Spacing = lineDistance.magnitude; // lineDistance.MagnitudeXZ();
                        } else { //non-fence mode
                            VectorXZ lineDistance = linePosition - m_mainSegment.LinePosition(ItemInfo.Datas[0].m_t);
                            ItemInfo.Spacing = lineDistance.magnitude; // lineDistance.MagnitudeXZ();
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

        public override bool IsLengthLongEnough() => m_mainSegment.Length() >= (ItemInfo.FenceMode ? 0.75f * ItemInfo.Spacing : ItemInfo.Spacing);

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

        public override void UpdateCurve(ControlPoint.PointInfo[] controlPoints, int controlPointCount) {
            SegmentState.m_pendingPlacementUpdate = true;
            if (controlPointCount > 0) {
                m_mainSegment.a = controlPoints[0].m_position;
                m_mainSegment.b = controlPoints[1].m_position;
            }
        }

        public override bool ContinueDrawing(ControlPoint.PointInfo[] controlPoints, ref int controlPointCount) {
            Vector3 p1 = controlPoints[1].m_position;
            if (Settings.FenceMode) {
                Vector3 lastFenceEndPoint = SegmentState.LastFenceEndpoint;
                if (lastFenceEndPoint == EMath.Vector3Down) return false;
                p1 = lastFenceEndPoint;
            }
            controlPoints[0].m_position = p1;
            controlPointCount = 1;
            SegmentState.IsContinueDrawing = true;
            return true;
        }

        public override bool PostCheckAndContinue(ControlPoint.PointInfo[] controlPoints, ref int controlPointCount) {
            switch (CurLockMode) {
            case LockingMode.Off:
                if (SegmentState.IsReadyForMaxContinue) {
                    UpdatePlacement(true, false);
                    GotoActiveState(ActiveState.MaxFillContinue);
                } else {
                    if (ContinueDrawing(controlPoints, ref controlPointCount)) {
                        GotoActiveState(ActiveState.CreatePointSecond);
                        ControlPoint.Modify(m_mousePosition, 1);
                        if (Settings.FenceMode) {
                            ControlPoint.Modify(SegmentState.m_segmentInfo.m_lastFenceEndpoint, 0);
                        }
                        UpdateCurve(controlPoints, controlPointCount);
                        UpdatePlacement(true, false);
                    } else {
                        ControlPoint.Reset();
                        GotoActiveState(ActiveState.CreatePointFirst);
                    }
                }
                break;
            case LockingMode.Lock:
                PrevLockMode = CurLockMode;
                GotoActiveState(ActiveState.LockIdle);
                break;
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
            Vector3 itemDir = m_mainSegment.Direction();
            for (int i = 0; i < itemCount; i++) {
                items[i].SetDirectionsXZ(itemDir);
            }
        }

        public override bool CalculateItemwisePosition(ItemInfo.ItemData[] items, bool fenceMode, float fencePieceLength, float initialOffset, VectorXZ lastFenceEndpoint) {
            float hoverItemwiseT = ItemInfo.HoverItemwiseT;
            ref Segment3 mainSegment = ref m_mainSegment;
            if (fenceMode) {
                float deltaT = fencePieceLength / mainSegment.LinearSpeedXZ();
                float sumT = hoverItemwiseT + deltaT;
                //check if out of bounds
                if (sumT > 1f && mainSegment.LengthXZ() >= fencePieceLength) {
                    hoverItemwiseT += (1f - sumT);
                }
                Vector3 positionStart = mainSegment.LinePosition(hoverItemwiseT);
                items[0].m_fenceEndPoint = positionStart;
                Vector3 positionEnd = mainSegment.LinePosition(hoverItemwiseT + deltaT);
                items[1].m_fenceEndPoint = positionEnd;
                items[0].Position = EMath.Lerp(positionStart, positionEnd, 0.50f);
            } else {
                items[0].m_t = hoverItemwiseT;
                items[0].Position = mainSegment.LinePosition(hoverItemwiseT);
            }
            return true;
        }

        public override bool CalculateAllPositionsBySpacing(ItemInfo.ItemData[] items, bool fenceMode, float spacing, float initialOffset, VectorXZ lastFenceEndpoint) {
            int numItems, numItemsRaw;
            float initialT, finalT, deltaT;
            initialOffset = EMath.Abs(initialOffset);

            // first early exit condition
            if (spacing == 0 || !IsLengthLongEnough()) {
                ItemInfo.Count = 0;
                return false;
            }
            ref Segment3 mainSegment = ref m_mainSegment;
            if (fenceMode) {
                float lengthFull = mainSegment.LengthXZ();
                float speed = mainSegment.LinearSpeedXZ();
                float lengthAfterFirst = SegmentState.IsMaxFillContinue ? lengthFull - initialOffset : lengthFull;
                float numItemsFloat = EMath.Abs(lengthAfterFirst / spacing);
                numItemsRaw = EMath.FloorToInt(numItemsFloat);
                numItems = EMath.Clamp(numItemsRaw, 0, MAX_ITEM_ARRAY_LENGTH);
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
                    items[i].m_fenceEndPoint = mainSegment.LinePosition(t);
                    t += deltaT;
                }

                //then calculate midpoints
                for (int i = 0; i < numItems; i++) {
                    items[i].Position = EMath.Lerp(items[i].m_fenceEndPoint, items[i + 1].m_fenceEndPoint, 0.50f);
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
                        items[numItems].m_fenceEndPoint = p1;

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
                SegmentState.NewFenceEndPoint = ItemInfo.Datas[numItems].m_fenceEndPoint;
            } else {
                float lengthFull = mainSegment.LengthXZ();
                float lengthAfterFirst = lengthFull - initialOffset;
                float speed = mainSegment.LinearSpeedXZ();

                //use ceiling for non-fence, because the point at the beginning is an extra point
                numItemsRaw = Mathf.CeilToInt(lengthAfterFirst / spacing);
                numItems = EMath.Clamp(numItemsRaw, 0, MAX_ITEM_ARRAY_LENGTH);
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
            ItemInfo.Count = numItems;
            if (EMath.FloorToInt(numItemsRaw) > MAX_ITEM_ARRAY_LENGTH) {
                SegmentState.MaxItemCountExceeded = true;
            } else {
                SegmentState.MaxItemCountExceeded = false;
            }
            return true;
        }

        public override void RenderProgressiveSpacingFill(RenderManager.CameraInfo cameraInfo, float fillLength, float interval, float size, Color color, bool renderLimits, bool alphaBlend) {
            ref Segment3 mainSegment = ref m_mainSegment;
            if (!ItemInfo.FenceMode) {
                float firstItemT = ItemInfo.Datas[0].m_t;
                RenderSegment(cameraInfo, mainSegment.Cut(firstItemT, firstItemT + fillLength / mainSegment.LinearSpeedXZ()),
                    size, 0f, new Color(color.r, color.g, color.b, 0.75f * color.a), renderLimits, alphaBlend);
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
