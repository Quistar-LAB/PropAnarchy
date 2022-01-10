using ColossalFramework.Math;
using EManagersLib;
using PropAnarchy.PLT.Extensions;
using PropAnarchy.PLT.MathUtils;
using UnityEngine;
using static PropAnarchy.PLT.PropLineTool;

namespace PropAnarchy.PLT.Modes {
    internal sealed class DrawFreeform : DrawMode {
        private static float m_mainElbowAngle;
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
                    if (e.button == LEFTMOUSEBUTTON) {
                        ControlPoint.Add(m_cachedPosition);
                        GotoActiveState(ActiveState.CreatePointThird);
                        ControlPoint.Modify(m_mousePosition, 2);
                        UpdateCurve();
                        UpdatePlacement(true, false);
                    } else if (e.button == RIGHTMOUSEBUTTON) {
                        ControlPoint.Cancel();
                        GotoActiveState(ActiveState.CreatePointFirst);
                        ControlPoint.Modify(m_mousePosition, 0);
                        UpdateCurve();
                    }
                }
                break;
            case ActiveState.CreatePointThird:
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
                        GotoActiveState(ActiveState.CreatePointSecond);
                        ControlPoint.Modify(m_mousePosition, 1);
                        UpdateCurve();
                        UpdatePlacement(false, false);
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
                            goto FinalizeControlPoint;
                        case HoverState.ControlPointSecond:
                            GotoActiveState(ActiveState.MovePointSecond);
                            ControlPoint.Modify(m_mousePosition, 1);
                            goto FinalizeControlPoint;
                        case HoverState.ControlPointThird:
                            GotoActiveState(ActiveState.MovePointThird);
                            ControlPoint.Modify(m_mousePosition, 2);
FinalizeControlPoint:
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
                        //AddAction(() => ContinueDrawingFromLockMode(true));
                        ContinueDrawingFromLockMode(true);
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
            case ActiveState.MovePointThird:
                if (!isInsideUI && e.type == EventType.MouseDown) {
                    if (e.button == RIGHTMOUSEBUTTON) {
                        ControlPoint.Modify(ControlPoint.m_lockedControlPoints[2].m_position, 2);
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
                    if ((e.button == LEFTMOUSEBUTTON && (e.modifiers & EventModifiers.Control) == EventModifiers.Control) || e.button == RIGHTMOUSEBUTTON) {
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
            case ActiveState.CreatePointThird: //creating third control point
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
            //Color lockIdleColor = Settings.m_PLTColor_locked;
            RenderElbow(cameraInfo, m_mainArm1, m_mainArm2, LINESIZE, 2f, createPointColor, false, true);
            if (SegmentState.AllItemsValid) {
                RenderBezier(cameraInfo, m_mainBezier, LINESIZE, createPointColor, false, true);
            } else {
                RenderBezier(cameraInfo, m_mainBezier, LINESIZE, curveWarningColor, false, true);
            }
        }

        public override bool OnRenderOverlay(RenderManager.CameraInfo cameraInfo, ActiveState curState, Color createPointColor, Color curveWarningColor, Color copyPlaceColor) {
            ControlPoint.PointInfo[] controlPoints = ControlPoint.m_controlPoints;
            switch (curState) {
            case ActiveState.CreatePointSecond:
                RenderLine(cameraInfo, m_mainArm1, LINESIZE, 2f, createPointColor, false, false);
                RenderCircle(cameraInfo, controlPoints[0].m_position, DOTSIZE, createPointColor, false, true);
                RenderCircle(cameraInfo, controlPoints[1].m_position, DOTSIZE, createPointColor, false, true);
                break;
            case ActiveState.CreatePointThird:
            case ActiveState.MaxFillContinue:
                if (m_keyboardAltDown) {
                    createPointColor = copyPlaceColor;
                } else if (m_keyboardCtrlDown) {
                    createPointColor = Settings.m_PLTColor_locked;
                }
                if (SegmentState.AllItemsValid) {
                    RenderBezier(cameraInfo, m_mainBezier, LINESIZE, createPointColor, false, true);
                } else {
                    RenderBezier(cameraInfo, m_mainBezier, LINESIZE, curveWarningColor, false, true);
                }
                //for the size for these it should be 1/4 the size for renderline
                RenderElbow(cameraInfo, m_mainArm1, m_mainArm2, LINESIZE, 2f, createPointColor, false, true);
                //MaxFillContinue
                if (SegmentState.IsReadyForMaxContinue || SegmentState.IsMaxFillContinue) {
                    RenderMaxFillContinueMarkers(cameraInfo);
                }
                RenderCircle(cameraInfo, controlPoints[0].m_position, DOTSIZE, createPointColor, false, true);
                RenderCircle(cameraInfo, controlPoints[1].m_position, DOTSIZE, createPointColor, false, true);
                RenderCircle(cameraInfo, controlPoints[2].m_position, DOTSIZE, createPointColor, false, true);
                break;
            }
            return true;
        }

        public override void OnSimulationStep(Vector3 mousePosition) {
            ControlPoint.PointInfo[] controlPoints = ControlPoint.m_controlPoints;
            m_cachedPosition = mousePosition;
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
            if (ControlPoint.m_validPoints != 0) {
                SegmentState.m_pendingPlacementUpdate = true;
                UpdateCurve(controlPoints, ControlPoint.m_validPoints);
                DiscoverHoverState(mousePosition);
                UpdateMiscHoverParameters();
            }
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
            case ActiveState.CreatePointSecond:
                if (SegmentState.IsPositionEqualToLastFenceEndpoint(ControlPoint.m_controlPoints[0].m_position)) {
                    SegmentState.ResetLastContinueParameters();
                }
                break;
            }
            UpdateCachedPosition(false);
            CheckPendingPlacement();
        }

        public override void OnToolUpdate() { }

        public override bool ContinueDrawing(ControlPoint.PointInfo[] controlPoints, ref int controlPointCount) {
            controlPoints[0] = controlPoints[2];
            controlPoints[1] = controlPoints[2];
            controlPoints[1].m_position = controlPoints[2].m_position + controlPoints[2].m_direction;
            controlPointCount = 2;
            SegmentState.m_segmentInfo.m_isContinueDrawing = true;
            return true;
        }

        public override bool PostCheckAndContinue(ControlPoint.PointInfo[] controlPoints, ref int controlPointCount) {
            if (CurLockMode == LockingMode.Off) {
                if (SegmentState.IsReadyForMaxContinue) {
                    UpdatePlacement(true, false);
                    GotoActiveState(ActiveState.MaxFillContinue);
                } else {
                    if (ContinueDrawing(controlPoints, ref controlPointCount)) {
                        GotoActiveState(ActiveState.CreatePointThird);
                        Vector3 tempVector = controlPoints[0].m_position + 0.001f * controlPoints[0].m_direction;
                        ControlPoint.Modify(tempVector, 2);
                        UpdateCurve();
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

        public override void UpdateCurve(ControlPoint.PointInfo[] controlPoints, int pointCount) {
            SegmentState.m_pendingPlacementUpdate = true;
            if (pointCount >= 1) {
                m_mainArm1.a = controlPoints[0].m_position;
                m_mainArm1.b = controlPoints[1].m_position;
            }
            if (pointCount >= 2) {
                m_mainBezier.QuadraticToCubicBezierCOMethod(controlPoints[0].m_position, controlPoints[1].m_direction, controlPoints[2].m_position, controlPoints[2].m_direction);
                m_mainArm2.a = controlPoints[1].m_position;
                m_mainArm2.b = controlPoints[2].m_position;
                //***SUPER-IMPORTANT (for convergence of fenceMode)***
                m_mainBezier.BezierXZ();
                //calculate direction here in case controlPoint direction was not set correctly
                VectorXZ dirArm1 = (m_mainArm1.b - m_mainArm1.a);
                dirArm1.Normalize();
                VectorXZ dirArm2 = (m_mainArm2.b - m_mainArm2.a);
                dirArm2.Normalize();
                m_mainElbowAngle = EMath.Abs((-dirArm1).AngleSigned(dirArm2, EMath.Vector3Up));
            }
        }

        public override bool IsLengthLongEnough() => (m_mainBezier.d - m_mainBezier.a).magnitude >= ItemInfo.Spacing;

        public override bool IsActiveStateAnItemRenderState() {
            switch (CurActiveState) {
            case ActiveState.CreatePointThird:
            case ActiveState.LockIdle:
            case ActiveState.MovePointFirst:
            case ActiveState.MovePointSecond:
            case ActiveState.MovePointThird:
            case ActiveState.MoveSegment:
            case ActiveState.ChangeSpacing:
            case ActiveState.ChangeAngle:
            case ActiveState.MaxFillContinue:
                return true;
            }
            return false;
        }

        public override void RevertDrawingFromLockMode() {
            GotoActiveState(ActiveState.CreatePointThird);
            ControlPoint.Modify(m_mousePosition, 2); //update position of second point
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
                Bezier3 mainBezier = m_mainBezier;
                for (int i = 0; i < itemCount; i++) {
                    items[i].SetDirectionsXZ(mainBezier.Tangent(items[i].m_t));
                }
            }
        }

        public override bool CalculateItemwisePosition(ItemInfo.ItemData[] items, bool fenceMode, float fencePieceLength, float initialOffset, VectorXZ lastFenceEndpoint) {
            float hoverItemwiseT = ItemInfo.HoverItemwiseT;
            if (fenceMode) {
                VectorXZ positionStart, positionEnd;
                if (fencePieceLength > m_mainBezier.CubicBezierArcLengthXZGauss12(0f, 1f)) {
                    ItemInfo.Count = 0;
                    return false;
                }
                m_mainBezier.CircleCurveFenceIntersectXZ(hoverItemwiseT, fencePieceLength, BEZIERTOLERANCE, out float itemTEnd, false);
                //check if out of bounds
                if (itemTEnd > 1f) {
                    //out of bounds? -> attempt to snap to d-end of curve
                    //invert the curve to go "backwards"
                    itemTEnd = 0f;
                    Bezier3 inverseBezier = m_mainBezier.Invert();
                    if (!inverseBezier.CircleCurveFenceIntersectXZ(itemTEnd, fencePieceLength, BEZIERTOLERANCE, out hoverItemwiseT, false)) {
                        //failed to snap to d-end of curve
                        ItemInfo.Count = 0;
                        return false;
                    } else {
                        hoverItemwiseT = 1f - hoverItemwiseT;
                        itemTEnd = 1f - itemTEnd;
                    }
                }
                items[0].m_fenceEndPoint = positionStart = m_mainBezier.Position(hoverItemwiseT);
                items[1].m_fenceEndPoint = positionEnd = m_mainBezier.Position(itemTEnd);
                items[0].Position = Vector3.Lerp(positionStart, positionEnd, 0.50f);
                return true;
            }
            items[0].m_t = hoverItemwiseT;
            items[0].Position = m_mainBezier.Position(hoverItemwiseT);
            return true;
        }

        public override bool CalculateAllPositionsBySpacing(ItemInfo.ItemData[] items, bool fenceMode, float spacing, float initialOffset, VectorXZ lastFenceEndpoint) {
            int numItems, numItemsRaw;
            float finalT;
            ref Bezier3 mainBezier = ref m_mainBezier;
            initialOffset = EMath.Abs(initialOffset);

            if (spacing == 0 || !IsLengthLongEnough() || (fenceMode && m_mainElbowAngle * Mathf.Rad2Deg < 5f)) {
                ItemInfo.Count = 0;
                return false;
            }
            if (fenceMode) {
                float lengthFull = mainBezier.CubicBezierArcLengthXZGauss12(0f, 1f);
                float lengthAfterFirst = SegmentState.IsMaxFillContinue ? lengthFull - initialOffset : lengthFull;
                if (spacing > lengthFull) {
                    ItemInfo.Count = 0;
                    return false;
                }
                numItemsRaw = EMath.CeilToInt(lengthAfterFirst / spacing);
                numItems = EMath.Clamp(numItemsRaw, 0, MAX_ITEM_ARRAY_LENGTH);

                float t = 0f;
                float penultimateT = 0f;
                int forLoopStart = 0;
                //max fill continue
                if (SegmentState.IsMaxFillContinue && initialOffset > 0f) {
                    forLoopStart = 0;
                    mainBezier.StepDistanceCurve(0f, initialOffset, BEZIERTOLERANCE, out t);
                    goto label_endpointsForLoop;
                } else if (initialOffset > 0f) {
                    //first continueDrawing if (1/4)
                    items[0].m_fenceEndPoint = lastFenceEndpoint;
                    if (!mainBezier.LinkCircleCurveFenceIntersectXZ(lastFenceEndpoint, spacing, BEZIERTOLERANCE, out t, false)) {
                        forLoopStart = 0;
                        t = 0f;
                        goto label_endpointsForLoop;
                    }
                    //third continueDrawing if (3/4)
                    items[1].m_fenceEndPoint = mainBezier.Position(t);
                    //fourth continueDrawing if (4/4)
                    if (!mainBezier.CircleCurveFenceIntersectXZ(t, spacing, BEZIERTOLERANCE, out t, false)) {
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
                        goto label_endpointsFinish;
                    }
                    //second if (2/3)
                    items[i].m_fenceEndPoint = mainBezier.Position(t);
                    penultimateT = t;
                    //third if (3/3)
                    if (!mainBezier.CircleCurveFenceIntersectXZ(t, spacing, BEZIERTOLERANCE, out t, false)) {
                        numItems = i - 1;
                        goto label_endpointsFinish;
                    }
                }
label_endpointsFinish:
                numItems = EMath.Clamp(numItems, 0, MAX_ITEM_ARRAY_LENGTH);
                //then calculate midpoints
                for (int i = 0; i < numItems; i++) {
                    items[i].Position = EMath.Lerp(items[i].m_fenceEndPoint, items[i + 1].m_fenceEndPoint, 0.50f);
                }
                //prep for MaxFillContinue
                if (SegmentState.IsReadyForMaxContinue) {
                    SegmentState.NewFinalOffset = m_mainBezier.CubicBezierArcLengthXZGauss12(0f, penultimateT);
                } else {
                    SegmentState.NewFinalOffset = m_mainBezier.CubicBezierArcLengthXZGauss04(t, 1f);
                }
                SegmentState.NewFenceEndPoint = ItemInfo.Datas[numItems].m_fenceEndPoint;
            } else {
                if (m_mainArm1.Length() + m_mainArm2.Length() <= 0.01f) {
                    return false;
                }
                float lengthFull = mainBezier.CubicBezierArcLengthXZGauss12(0f, 1f);
                float lengthAfterFirst = lengthFull - initialOffset;
                //use ceiling for non-fence, because the point at the beginning is an extra point
                numItemsRaw = Mathf.CeilToInt(lengthAfterFirst / spacing);
                numItems = EMath.Clamp(numItemsRaw, 0, MAX_ITEM_ARRAY_LENGTH);
                float t = 0f;
                if (initialOffset > 0f) {
                    mainBezier.StepDistanceCurve(0f, initialOffset, BEZIERTOLERANCE, out t);
                }
                for (int i = 0; i < numItems; i++) {
                    items[i].m_t = t;
                    items[i].Position = mainBezier.Position(t);
                    mainBezier.StepDistanceCurve(t, spacing, BEZIERTOLERANCE, out t);
                }
                if (numItems > 0) {
                    finalT = items[numItems - 1].m_t;
                    if (SegmentState.IsReadyForMaxContinue) {
                        SegmentState.NewFinalOffset = spacing + mainBezier.CubicBezierArcLengthXZGauss12(0f, finalT);
                    } else {
                        SegmentState.NewFinalOffset = spacing - mainBezier.CubicBezierArcLengthXZGauss04(finalT, 1f);
                    }
                } else {
                    SegmentState.LastFenceEndpoint = EMath.Vector3Down;
                    SegmentState.LastFinalOffset = 0f;
                    //UpdatePlacement();
                }
            }
            if (numItems > 0) {
                UpdateFinalPrefab(items, numItems);
            }
            ItemInfo.Count = numItems;
            if (EMath.FloorToInt(numItemsRaw) > MAX_ITEM_ARRAY_LENGTH) {
                SegmentState.MaxItemCountExceeded = true;
            } else {
                SegmentState.MaxItemCountExceeded = false;
            }
            return true;
        }

        public override void DiscoverHoverState(VectorXZ position) {
            ActiveState currentState = CurActiveState;
            //check for itemwise first before classic lock mode
            if (Settings.ControlMode == ControlMode.ItemWise && (currentState == ActiveState.ItemwiseLock || currentState == ActiveState.MoveItemwiseItem)) {
                if (m_mainBezier.IsCloseToCurveXZ(HOVER_ITEMWISE_CURVEDISTANCE_THRESHOLD, position, out float hoverItemT)) {
                    ItemInfo.HoverItemwiseT = hoverItemT;
                    CurHoverState = HoverState.ItemwiseItem;
                } else {
                    CurHoverState = HoverState.Unbound;
                }
            }
            if (currentState == ActiveState.LockIdle) {
                if (ItemInfo.Count >= (ItemInfo.FenceMode ? 1 : 2) || Settings.ControlMode == ControlMode.ItemWise) {
                    const float pointRadius = HOVER_POINTDISTANCE_THRESHOLD;
                    const float anglePointRadius = pointRadius;
                    const float angleLocusRadius = HOVER_ANGLELOCUS_DIAMETER;
                    const float angleLocusDistanceThreshold = 0.40f;
                    bool angleObjectMode = ItemInfo.Type == ItemType.Prop;
                    VectorXZ angleCenter = ItemInfo.Datas[HoverItemAngleCenterIndex].Position;
                    VectorXZ anglePos = CircleXZ.Position3FromAngleXZ(angleCenter, angleLocusRadius, ItemInfo.HoverAngle);
                    VectorXZ spacingPos = ItemInfo.FenceMode ? ItemInfo.Datas[HoverItemPositionIndex].m_fenceEndPoint : ItemInfo.Datas[HoverItemPositionIndex].Position;
                    if (VectorXZ.IsInsideCircleXZ(spacingPos, pointRadius, position)) {
                        CurHoverState = Settings.ControlMode == ControlMode.ItemWise ? HoverState.ItemwiseItem : HoverState.SpacingLocus;
                    } else if (angleObjectMode && VectorXZ.IsInsideCircleXZ(anglePos, anglePointRadius, position)) {
                        CurHoverState = HoverState.AngleLocus;
                    } else if (angleObjectMode && VectorXZ.IsNearCircleOutlineXZ(angleCenter, HOVER_ANGLELOCUS_DIAMETER, position, angleLocusDistanceThreshold)) {
                        CurHoverState = HoverState.AngleLocus;
                    } else if (VectorXZ.IsInsideCircleXZ(ControlPoint.m_controlPoints[0].m_position, pointRadius, position)) {
                        CurHoverState = HoverState.ControlPointFirst;
                    } else if (VectorXZ.IsInsideCircleXZ(ControlPoint.m_controlPoints[1].m_position, pointRadius, position)) {
                        CurHoverState = HoverState.ControlPointSecond;
                    } else if (VectorXZ.IsInsideCircleXZ(ControlPoint.m_controlPoints[2].m_position, pointRadius, position)) {
                        CurHoverState = HoverState.ControlPointThird;
                    } else if (m_mainBezier.IsCloseToCurveXZ(HOVER_CURVEDISTANCE_THRESHOLD, position, out float _)) {
                        CurHoverState = HoverState.Curve;
                    } else {
                        CurHoverState = HoverState.Unbound;
                    }
                } else {
                    CurHoverState = HoverState.Unbound;
                }
            }
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
                    if (m_mainBezier.IsCloseToCurveXZ(HOVER_CURVEDISTANCE_THRESHOLD * 8f, m_cachedPosition, out float hoverCurveT)) {
                        float curveT = Mathf.Clamp(hoverCurveT, ItemInfo.Datas[0].m_t, 0.995f);
                        if (ItemInfo.FenceMode) {
                            Vector3 curveDistance = m_mainBezier.Position(curveT) - ItemInfo.Datas[0].m_fenceEndPoint;
                            ItemInfo.Spacing = curveDistance.MagnitudeXZ();
                        } else { //non-fence mode
                            ItemInfo.Spacing = m_mainBezier.CubicBezierArcLengthXZGauss12(ItemInfo.Datas[0].m_t, curveT);
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
            if (!ItemInfo.FenceMode) {
                float firstItemT = ItemInfo.Datas[0].m_t;
                m_mainBezier.StepDistanceCurve(firstItemT, fillLength, BEZIERTOLERANCE * BEZIERTOLERANCE, out float tFill);
                RenderBezier(cameraInfo, m_mainBezier.Cut(firstItemT, tFill), size, new Color(color.r, color.g, color.b, 0.75f * color.a), renderLimits, true);
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
                goto UpdateCurve;
            case ActiveState.CreatePointThird:
                ControlPoint.Modify(m_cachedPosition, 2);
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
