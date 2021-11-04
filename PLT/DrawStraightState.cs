using ColossalFramework;
using ColossalFramework.Math;
using System.Collections.Generic;
using UnityEngine;
using static PropAnarchy.PLT.PropLineTool;

namespace PropAnarchy.PLT {
    public class DrawStraightState : ActiveDrawState {
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
                        //ControlPoint.Add(ref m_cachedPosition);
                        //GoToActiveState(ActiveState.CreatePointThird);
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
                RenderSegment(cameraInfo, m_mainSegment, 1.50f, 0f, curveWarningColor, false, true);
            }
            RenderSegment(cameraInfo, m_mainSegment, 1.00f, 0f, createPointColor, false, true);
        }

        public override bool OnRenderOverlay(RenderManager.CameraInfo cameraInfo, ActiveState curState, ref Color createPointColor, ref Color curveWarningColor, ref Color copyPlaceColor) {
            Vector3 vectorZero; vectorZero.x = 0f; vectorZero.y = 0f; vectorZero.z = 0f;
            ControlPoint.PointInfo[] cachedControlPoints = ControlPoint.m_cachedControlPoints;
            if (cachedControlPoints[1].m_direction != vectorZero) {
                switch (curState) {
                case ActiveState.CreatePointSecond:
                case ActiveState.MaxFillContinue:
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

        public override void DiscoverHoverState(Vector3 position) {
            //check for itemwise first before classic lock mode
            if (m_controlMode == ControlMode.ITEMWISE && (m_currentState == ActiveState.ItemwiseLock || m_currentState == ActiveState.MoveItemwiseItem)) {
                if (PLTMath.IsCloseToSegmentXZ(m_mainSegment, HOVER_ITEMWISE_CURVEDISTANCE_THRESHOLD, position, out float hoverItemT)) {
                    m_hoverItemwiseT = hoverItemT;
                    m_hoverState = HoverState.ItemwiseItem;
                } else {
                    m_hoverState = HoverState.Unbound;
                }
            }
            if (m_currentState == ActiveState.LockIdle) {
                if ((m_itemCount >= (GetFenceMode() ? 1 : 2) || m_controlMode == ControlMode.ITEMWISE) && PLTMath.IsCloseToSegmentXZ(m_mainSegment, HOVER_CURVEDISTANCE_THRESHOLD, position, out _)) {
                    m_hoverState = HoverState.Curve;
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
                    if (PLTMath.IsCloseToSegmentXZ(m_mainSegment, HOVER_CURVEDISTANCE_THRESHOLD * 8f, m_cachedPosition, out float hoverCurveT)) {
                        float curveT = Mathf.Clamp(hoverCurveT, m_items[0].m_t, 0.995f);
                        Vector3 linePosition = m_mainSegment.LinePosition(curveT);;
                        if (GetFenceMode()) {
                            //since straight fence mode auto snaps to last fence endpoint
                            Vector3 lineDistance = linePosition - m_mainSegment.LinePosition(0f);
                            ItemInfo.ItemSpacing = lineDistance.MagnitudeXZ();
                        } else { //non-fence mode
                            Vector3 lineDistance = linePosition - m_mainSegment.LinePosition(m_items[0].m_t);
                            ItemInfo.ItemSpacing = lineDistance.MagnitudeXZ();
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

        public override bool IsLengthLongEnough() => m_mainSegment.LengthXZ() >= (GetFenceMode() ? 0.75f * ItemInfo.ItemSpacing : ItemInfo.ItemSpacing);

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
            ControlPoint.UpdateCached();
            return true;
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
                        if (GetFenceMode()) {
                            ControlPoint.Modify(DrawMode.CurrentMode, ref SegmentState.m_segmentInfo.m_lastFenceEndpoint, 0, ActiveState.CreatePointSecond, DrawMode.Current);
                        }
                        UpdateCurve(cachedControlPoints, controlPointCount);
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
            Vector3 itemDir = m_mainSegment.b - m_mainSegment.a;
            ItemInfo[] items = m_items;
            for (int i = 0; i < itemCount; i++) {
                items[i].SetDirectionsXZ(itemDir);
            }
        }

        public override bool CalculateItemwisePosition(float fencePieceLength, float initialOffset, Vector3 lastFenceEndpoint) {
            float hoverItemwiseT = m_hoverItemwiseT;
            if (GetFenceMode()) {
                float deltaT = fencePieceLength / PLTMath.LinearSpeedXZ(m_mainSegment);
                float sumT = hoverItemwiseT + deltaT;
                //check if out of bounds
                if (sumT > 1f && m_mainSegment.LengthXZ() >= fencePieceLength) {
                    hoverItemwiseT += (1f - sumT);
                }
                Vector3 positionStart = m_mainSegment.LinePosition(hoverItemwiseT);
                m_fenceEndPoints[0] = positionStart;
                Vector3 positionEnd = m_mainSegment.LinePosition(hoverItemwiseT + deltaT);
                m_fenceEndPoints[1] = positionEnd;
                m_items[0].Position = Vector3.Lerp(positionStart, positionEnd, 0.50f);
                return true;
            }
            m_items[0].m_t = hoverItemwiseT;
            m_items[0].Position = m_mainSegment.LinePosition(hoverItemwiseT);
            return true;
        }

        public override int CalculateAllPositionsBySpacing(float spacing, float initialOffset, Vector3 lastFenceEndpoint) {
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
                deltaT = spacing / PLTMath.LinearSpeedXZ(mainSegment);
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
                return numItems;
            }
            float speed = PLTMath.LinearSpeedXZ(mainSegment);
            //use ceiling for non-fence, because the point at the beginning is an extra point
            numItems = Mathf.Min(m_itemCount, Mathf.Clamp(Mathf.CeilToInt((mainSegment.LengthXZ() - initialOffset) / spacing), 0, MAX_ITEM_ARRAY_LENGTH));
            if (speed == 0) {
                return 0;
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
            return numItems;
        }

        public override void RenderProgressiveSpacingFill(RenderManager.CameraInfo cameraInfo, float fillLength, float interval, float size, Color color, bool renderLimits, bool alphaBlend) {
            if (!GetFenceMode()) {
                float firstItemT = m_items[0].m_t;
                RenderSegment(cameraInfo, m_mainSegment.Cut(firstItemT, firstItemT + fillLength / PLTMath.LinearSpeedXZ(m_mainSegment)),
                    size, 0f, new Color(color.r, color.g, color.b, 0.75f * color.a), renderLimits, alphaBlend);
            }
        }
    }
}
