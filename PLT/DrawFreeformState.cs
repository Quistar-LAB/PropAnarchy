using ColossalFramework.Math;
using System;
using UnityEngine;
using static PropAnarchy.PLT.PropLineTool;

namespace PropAnarchy.PLT {
    public class DrawFreeformState : ActiveDrawState {
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
                    if (e.button == LEFTMOUSEBUTTON) {
                        ControlPoint.Add(m_cachedPosition);
                        GoToActiveState(ActiveState.CreatePointThird);
                        ControlPoint.Modify(m_mousePosition, 2);
                        UpdateCurve();
                        UpdatePlacement(true, false);
                    } else if (e.button == RIGHTMOUSEBUTTON) {
                        ControlPoint.Cancel();
                        GoToActiveState(ActiveState.CreatePointFirst);
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
                        GoToActiveState(ActiveState.CreatePointSecond);
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
                            goto FinalizeControlPoint;
                        case HoverState.ControlPointSecond:
                            GoToActiveState(ActiveState.MovePointSecond);
                            ControlPoint.Modify(m_mousePosition, 1);
                            goto FinalizeControlPoint;
                        case HoverState.ControlPointThird:
                            GoToActiveState(ActiveState.MovePointThird);
                            ControlPoint.Modify(m_mousePosition, 2);
FinalizeControlPoint:
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
            case ActiveState.MovePointThird:
                if (!isInsideUI && e.type == EventType.MouseDown) {
                    if (e.button == RIGHTMOUSEBUTTON) {
                        ControlPoint.Modify(ControlPoint.m_lockedControlPoints[2].m_position, 2);
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
                    if ((e.button == LEFTMOUSEBUTTON && (e.modifiers & EventModifiers.Control) == EventModifiers.Control) || e.button == RIGHTMOUSEBUTTON) {
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
                int itemCount = m_itemCount;
                ItemInfo[] items = m_items;
                for (int i = 0; i < itemCount; i++) {
                    items[i].RenderItem(cameraInfo);
                }
                break;
            }
        }

        public override void RenderLines(RenderManager.CameraInfo cameraInfo, Color createPointColor, Color curveWarningColor) {
            Color lockIdleColor = Settings.m_PLTColor_locked;
            RenderElbow(cameraInfo, m_mainArm1, m_mainArm2, LINESIZE, 2f, lockIdleColor, false, true);
            if (m_hoverState == HoverState.SpacingLocus) {
                RenderBezier(cameraInfo, m_mainBezier, LINESIZE, lockIdleColor, false, true);
            } else {
                RenderBezier(cameraInfo, m_mainBezier, LINESIZE, createPointColor, false, false);
            }
            if (!SegmentState.AllItemsValid) {
                RenderBezier(cameraInfo, m_mainBezier, 1.50f, curveWarningColor, false, true);
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
                if (!SegmentState.AllItemsValid) {
                    RenderBezier(cameraInfo, m_mainBezier, 1.50f, curveWarningColor, false, true);
                }
                //for the size for these it should be 1/4 the size for renderline
                RenderElbow(cameraInfo, m_mainArm1, m_mainArm2, LINESIZE, 2f, createPointColor, false, true);
                RenderBezier(cameraInfo, m_mainBezier, LINESIZE, createPointColor, false, true);
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
            case ActiveState.CreatePointSecond:
                if (SegmentState.IsPositionEqualToLastFenceEndpoint(ControlPoint.m_controlPoints[0].m_position)) {
                    SegmentState.ResetLastContinueParameters();
                }
                break;
            }
            UpdateCachedPosition(false);
            CheckPendingPlacement();
        }

        public override void OnToolUpdate() {
            base.OnToolUpdate();
        }

        public override bool ContinueDrawing(ControlPoint.PointInfo[] controlPoints, ref int controlPointCount) {
            controlPoints[0] = controlPoints[2];
            controlPoints[1] = controlPoints[2];
            controlPoints[1].m_position = controlPoints[2].m_position + controlPoints[2].m_direction;
            controlPointCount = 2;
            SegmentState.m_segmentInfo.m_isContinueDrawing = true;
            return true;
        }

        public override bool PostCheckAndContinue(ControlPoint.PointInfo[] controlPoints, ControlPoint.PointInfo[] cachedControlPoints, ref int controlPointCount) {
            if (m_lockingMode == LockingMode.Off) {
                if (SegmentState.IsReadyForMaxContinue) {
                    UpdatePlacement(true, false);
                    GoToActiveState(ActiveState.MaxFillContinue);
                } else {
                    if (ContinueDrawing(controlPoints, ref controlPointCount)) {
                        GoToActiveState(ActiveState.CreatePointThird);
                        Vector3 tempVector = controlPoints[0].m_position + 0.001f * controlPoints[0].m_direction;
                        ControlPoint.Modify(tempVector, 2);
                        UpdateCurve();
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

        public override void UpdateCurve(ControlPoint.PointInfo[] cachedControlPoints, int pointCount) {
            SegmentState.m_pendingPlacementUpdate = true;
            if (pointCount >= 1) {
                m_mainArm1.a = cachedControlPoints[0].m_position;
                m_mainArm1.b = cachedControlPoints[1].m_position;
            }
            if (pointCount >= 2) {
                m_mainBezier.QuadraticToCubicBezierCOMethod(cachedControlPoints[0].m_position, cachedControlPoints[1].m_direction, cachedControlPoints[2].m_position, cachedControlPoints[2].m_direction);
                m_mainArm2.a = cachedControlPoints[1].m_position;
                m_mainArm2.b = cachedControlPoints[2].m_position;
                //***SUPER-IMPORTANT (for convergence of fenceMode)***
                m_mainBezier.BezierXZ();
                //calculate direction here in case controlPoint direction was not set correctly
                Vector3 dirArm1 = (m_mainArm1.b - m_mainArm1.a);
                dirArm1.y = 0f;
                dirArm1.Normalize();
                Vector3 dirArm2 = (m_mainArm2.b - m_mainArm2.a);
                dirArm2.y = 0f;
                dirArm2.Normalize();
                m_mainElbowAngle = Math.Abs(PLTMath.AngleSigned(-dirArm1, dirArm2, m_vectorUp));
            }
        }

        public override bool IsLengthLongEnough() => (m_mainBezier.d - m_mainBezier.a).magnitude >= ItemInfo.ItemSpacing;

        public override bool IsActiveStateAnItemRenderState() {
            switch (m_currentState) {
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
            GoToActiveState(ActiveState.CreatePointThird);
            ControlPoint.Modify(m_mousePosition, 3); //update position of second point
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
                Bezier3 mainBezier = m_mainBezier;
                for (int i = 0; i < itemCount; i++) {
                    items[i].SetDirectionsXZ(mainBezier.Tangent(items[i].m_t));
                }
            }
        }

        public override bool CalculateItemwisePosition(ItemInfo[] items, Vector3[] fenceEndPoints, bool fenceMode, float fencePieceLength, float initialOffset, VectorXZ lastFenceEndpoint) {
            float hoverItemwiseT = m_hoverItemwiseT;
            if (fenceMode) {
                VectorXZ positionStart, positionEnd;
                if (fencePieceLength > m_mainBezier.CubicBezierArcLengthXZGauss12(0f, 1f)) {
                    m_itemCount = 0;
                    return false;
                }
                m_mainBezier.CircleCurveFenceIntersectXZ(hoverItemwiseT, fencePieceLength, TOLERANCE, out float itemTEnd, false);
                //check if out of bounds
                if (itemTEnd > 1f) {
                    //out of bounds? -> attempt to snap to d-end of curve
                    //invert the curve to go "backwards"
                    itemTEnd = 0f;
                    Bezier3 inverseBezier = m_mainBezier.Invert();
                    if (!inverseBezier.CircleCurveFenceIntersectXZ(itemTEnd, fencePieceLength, TOLERANCE, out hoverItemwiseT, false)) {
                        //failed to snap to d-end of curve
                        m_itemCount = 0;
                        return false;
                    } else {
                        hoverItemwiseT = 1f - hoverItemwiseT;
                        itemTEnd = 1f - itemTEnd;
                    }
                }
                fenceEndPoints[0] = positionStart = m_mainBezier.Position(hoverItemwiseT);
                fenceEndPoints[1] = positionEnd = m_mainBezier.Position(itemTEnd);
                items[0].Position = Vector3.Lerp(positionStart, positionEnd, 0.50f);
                return true;
            }
            items[0].m_t = hoverItemwiseT;
            items[0].Position = m_mainBezier.Position(hoverItemwiseT);
            return true;
        }

        public override bool CalculateAllPositionsBySpacing(ItemInfo[] items, Vector3[] fenceEndPoints, bool fenceMode, float spacing, float initialOffset, VectorXZ lastFenceEndpoint) {
            int numItems, numItemsRaw;
            float t = 0f, finalT;
            Bezier3 mainBezier = m_mainBezier;
            if (initialOffset < 0f) initialOffset = Math.Abs(initialOffset);
            float lengthFull = mainBezier.CubicBezierArcLengthXZGauss12(0f, 1f);
            if (fenceMode) {
                numItemsRaw = Mathf.CeilToInt((SegmentState.IsMaxFillContinue ? lengthFull - initialOffset : lengthFull) / spacing);
                numItems = Mathf.Min(m_itemCount, Mathf.Clamp(numItemsRaw, 0, MAX_ITEM_ARRAY_LENGTH));
                if (spacing > lengthFull) {
                    m_itemCount = 0;
                    return false;
                }
                if (numItems > MAX_ITEM_ARRAY_LENGTH) numItems = MAX_ITEM_ARRAY_LENGTH;
                float penultimateT = 0f;
                int forLoopStart = 0;
                //max fill continue
                if (SegmentState.IsMaxFillContinue && initialOffset > 0f) {
                    forLoopStart = 0;
                    mainBezier.StepDistanceCurve(0f, initialOffset, TOLERANCE, out t);
                    goto label_endpointsForLoop;
                } else if (initialOffset > 0f) {
                    fenceEndPoints[0] = lastFenceEndpoint;
                    if (!mainBezier.LinkCircleCurveFenceIntersectXZ(lastFenceEndpoint, spacing, TOLERANCE, out t, false)) {
                        forLoopStart = 0;
                        t = 0f;
                        goto label_endpointsForLoop;
                    }
                    //third continueDrawing if (3/4)
                    fenceEndPoints[1] = m_mainBezier.Position(t);
                    //fourth continueDrawing if (4/4)
                    if (!mainBezier.CircleCurveFenceIntersectXZ(t, spacing, TOLERANCE, out t, false)) {
                        //failed to converge
                        numItems = 1;
                        goto label_endpointsFinish;
                    }
                    forLoopStart = 2;
                }
label_endpointsForLoop:
                for (int i = forLoopStart; i < numItems + 1; i++) {
                    if (t > 1f) {
                        numItems = i - 1;
                        goto label_endpointsFinish;
                    }
                    fenceEndPoints[i] = mainBezier.Position(t);
                    penultimateT = t;
                    if (!mainBezier.CircleCurveFenceIntersectXZ(t, spacing, TOLERANCE, out t, false)) {
                        //failed to converge
                        numItems = i - 1;
                        goto label_endpointsFinish;
                    }
                }
label_endpointsFinish:
                numItems = Mathf.Clamp(numItems, 0, MAX_ITEM_ARRAY_LENGTH);
                finalT = t;
                //then calculate midpoints
                for (int i = 0; i < numItems; i++) {
                    items[i].Position = Vector3.Lerp(fenceEndPoints[i], fenceEndPoints[i + 1], 0.50f);
                }
                //prep for MaxFillContinue
                if (SegmentState.IsReadyForMaxContinue) {
                    SegmentState.NewFinalOffset = mainBezier.CubicBezierArcLengthXZGauss12(0f, penultimateT);
                } else {
                    SegmentState.NewFinalOffset = mainBezier.CubicBezierArcLengthXZGauss04(finalT, 1f);
                }
                m_itemCount = numItems;
                return true;
            }
            if (m_mainArm1.Length() + m_mainArm2.Length() <= 0.01f) {
                m_itemCount = 0;
                return false;
            }
            //use ceiling for non-fence, because the point at the beginning is an extra point
            numItemsRaw = Mathf.CeilToInt((lengthFull - initialOffset) / spacing);
            numItems = Math.Min(m_itemCount, Mathf.Clamp(numItemsRaw, 0, MAX_ITEM_ARRAY_LENGTH));
            if (initialOffset > 0f) {
                mainBezier.StepDistanceCurve(0f, initialOffset, TOLERANCE, out t);
            }
            if (numItems > 0) {
                for (int i = 0; i < numItems; i++) {
                    items[i].m_t = t;
                    items[i].Position = mainBezier.Position(t);
                    mainBezier.StepDistanceCurve(t, spacing, TOLERANCE, out t);
                }
                finalT = items[numItems - 1].m_t;
                if (SegmentState.IsReadyForMaxContinue) {
                    SegmentState.NewFinalOffset = spacing + mainBezier.CubicBezierArcLengthXZGauss12(0f, finalT);
                } else {
                    SegmentState.NewFinalOffset = spacing + mainBezier.CubicBezierArcLengthXZGauss04(finalT, 1f);
                }
                m_itemCount = numItems;
                return true;
            }
            return false;
        }

        public override void DiscoverHoverState(VectorXZ position) {
            ActiveState currentState = m_currentState;
            //check for itemwise first before classic lock mode
            if (m_controlMode == ControlMode.ITEMWISE && (currentState == ActiveState.ItemwiseLock || currentState == ActiveState.MoveItemwiseItem)) {
                if (m_mainBezier.IsCloseToCurveXZ(HOVER_ITEMWISE_CURVEDISTANCE_THRESHOLD, position, out float hoverItemT)) {
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
                    if (spacingPos.IsInsideCircleXZ(pointRadius, position)) {
                        m_hoverState = m_controlMode == ControlMode.ITEMWISE ? HoverState.ItemwiseItem : HoverState.SpacingLocus;
                    } else if (angleObjectMode && anglePos.IsInsideCircleXZ(anglePointRadius, position)) {
                        m_hoverState = HoverState.AngleLocus;
                    } else if (angleObjectMode && angleCenter.IsNearCircleOutlineXZ(HOVER_ANGLELOCUS_DIAMETER, position, angleLocusDistanceThreshold)) {
                        m_hoverState = HoverState.AngleLocus;
                    } else if (ControlPoint.m_controlPoints[0].m_position.IsInsideCircleXZ(pointRadius, position)) {
                        m_hoverState = HoverState.ControlPointFirst;
                    } else if (ControlPoint.m_controlPoints[1].m_position.IsInsideCircleXZ(pointRadius, position)) {
                        m_hoverState = HoverState.ControlPointSecond;
                    } else if (ControlPoint.m_controlPoints[2].m_position.IsInsideCircleXZ(pointRadius, position)) {
                        m_hoverState = HoverState.ControlPointThird;
                    } else if (m_mainBezier.IsCloseToCurveXZ(HOVER_CURVEDISTANCE_THRESHOLD, position, out float _)) {
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
                    UpdateCurve();
                    UpdatePlacement();
                    break;
                case ActiveState.ChangeSpacing:
                    if (m_mainBezier.IsCloseToCurveXZ(HOVER_CURVEDISTANCE_THRESHOLD * 8f, m_cachedPosition, out float hoverCurveT)) {
                        float curveT = Mathf.Clamp(hoverCurveT, m_items[0].m_t, 0.995f);
                        if (GetFenceMode()) {
                            Vector3 curveDistance = m_mainBezier.Position(curveT) - m_fenceEndPoints[0];
                            ItemInfo.ItemSpacing = curveDistance.MagnitudeXZ();
                        } else { //non-fence mode
                            ItemInfo.ItemSpacing = m_mainBezier.CubicBezierArcLengthXZGauss12(m_items[0].m_t, curveT);
                        }
                    }
                    UpdateCurve();
                    UpdatePlacement(true, true);
                    break;
                case ActiveState.ChangeAngle:
                    Vector3 yAxis = m_vectorUp;
                    if (m_angleMode == AngleMode.DYNAMIC) {
                        Vector3 angleVector = m_cachedPosition - m_items[HoverItemAngleCenterIndex].Position;
                        angleVector.y = 0f;
                        angleVector.Normalize();
                        m_hoverAngle = PLTMath.AngleSigned(angleVector, m_vectorRight, yAxis);
                        ItemInfo.m_itemAngleOffset = PLTMath.AngleSigned(angleVector, m_lockedBackupItemDirection, yAxis);
                    } else if (m_angleMode == AngleMode.SINGLE) {
                        Vector3 angleVector = m_cachedPosition - m_items[HoverItemAngleCenterIndex].Position;
                        angleVector.y = 0f;
                        angleVector.Normalize();
                        float angle = PLTMath.AngleSigned(angleVector, m_vectorRight, yAxis);
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
            if (!GetFenceMode()) {
                float firstItemT = m_items[0].m_t;
                m_mainBezier.StepDistanceCurve(firstItemT, fillLength, TOLERANCE * TOLERANCE, out float tFill);
                RenderBezier(cameraInfo, m_mainBezier.Cut(firstItemT, tFill), size, new Color(color.r, color.g, color.b, 0.75f * color.a), renderLimits, true);
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
