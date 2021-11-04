using System;
using System.Threading;
using UnityEngine;
using static PropAnarchy.PLT.PropLineTool;

namespace PropAnarchy.PLT {
    public static class ControlPoint {
        public const int MAX_CONTROL_POINTS = 3;
        public struct PointInfo {
            public Vector3 m_position;
            public Vector3 m_direction;
            public bool m_outside;
        }
        public static PointInfo[] m_controlPoints = new PointInfo[MAX_CONTROL_POINTS];
        public static PointInfo[] m_cachedControlPoints = new PointInfo[MAX_CONTROL_POINTS];
        public static PointInfo[] m_lockedControlPoints = new PointInfo[MAX_CONTROL_POINTS];
        public static int m_validPoints = 0;
        public static object m_cacheLock = new object();

        private static void GetFreeformMidPoint(PointInfo[] controlPoints) {
            Vector3 p2_p0 = controlPoints[2].m_position - controlPoints[0].m_position;
            Vector3 dir_p1 = controlPoints[1].m_direction;
            p2_p0.y = 0f;
            dir_p1.y = 0f;
            float sqrMag_2_0 = Vector3.SqrMagnitude(p2_p0);
            p2_p0 = Vector3.Normalize(p2_p0);
            float angle_0 = Mathf.Min(1.17809725f, Mathf.Acos(Vector3.Dot(p2_p0, dir_p1)));
            float dist_p1_p0 = Mathf.Sqrt(0.5f * sqrMag_2_0 / Mathf.Max(0.001f, 1f - Mathf.Cos(3.14159274f - 2f * angle_0)));
            controlPoints[1].m_position = controlPoints[0].m_position + dir_p1 * dist_p1_p0;
            Vector3 dir_p2 = controlPoints[2].m_position - controlPoints[1].m_position;
            dir_p2.y = 0f;
            dir_p2.Normalize();
            controlPoints[2].m_direction = dir_p2;
            //sometimes things don't work corrently
            if (float.IsNaN(controlPoints[1].m_position.x) || float.IsNaN(controlPoints[1].m_position.y) || float.IsNaN(controlPoints[1].m_position.z)) {
                controlPoints[1].m_position = controlPoints[0].m_position + 0.01f * controlPoints[0].m_direction;
                controlPoints[1].m_direction = controlPoints[0].m_direction;
            }
        }

        public static void Add(ref Vector3 position) {
            Vector3 vectorZero; vectorZero.x = 0f; vectorZero.y = 0f; vectorZero.z = 0f;
            PointInfo[] controlPoints = m_controlPoints;
            int validPoints = m_validPoints;
            switch (validPoints) {
            case 0:
                controlPoints[0].m_position = position;
                controlPoints[0].m_direction = vectorZero;
                m_validPoints = 1;
                m_positionChanging = true;
                break;
            case 1:
                Vector3 normVector = position - controlPoints[0].m_position;
                normVector.y = 0f;
                normVector.Normalize();
                controlPoints[1].m_position = position;
                controlPoints[0].m_direction = normVector;
                controlPoints[1].m_direction = normVector;
                m_validPoints = 2;
                m_positionChanging = true;
                break;
            case 2:
                if (DrawMode.Current == DrawMode.FREEFORM) {
                    controlPoints[2].m_position = position;
                    GetFreeformMidPoint(controlPoints);
                } else {//must be curved
                    normVector = position - controlPoints[1].m_position;
                    normVector.y = 0f;
                    normVector.Normalize();
                    controlPoints[2].m_position = position;
                    controlPoints[2].m_direction = normVector;
                }
                m_validPoints = 3;
                m_positionChanging = true;
                break;
            }
        }

        public static void Update(ActiveState currentState, int drawMode) {
            //continuously update control points to follow mouse
            switch (currentState) {
            case ActiveState.CreatePointFirst:
                SegmentState.UpdatePlacement(false, false);
                break;
            case ActiveState.CreatePointSecond:
                Modify(ref m_cachedPosition, 1);
                //ModifyControlPoint(this.m_cachedPosition, 2);
                if (drawMode == DrawMode.STRAIGHT || drawMode == DrawMode.CIRCLE) {
                    SegmentState.UpdatePlacement();
                }
                goto UpdateCurve;
            case ActiveState.CreatePointThird:
                Modify(ref m_cachedPosition, 2);
                //ModifyControlPoint(this.m_cachedPosition, 3);
                if (drawMode == DrawMode.CURVED || drawMode == DrawMode.FREEFORM) {
                    SegmentState.UpdatePlacement();
                }
                goto UpdateCurve;
            case ActiveState.MoveSegment:
            case ActiveState.ChangeSpacing:
            case ActiveState.ChangeAngle:
            case ActiveState.LockIdle:
            case ActiveState.MaxFillContinue:
                SegmentState.UpdatePlacement();
                break;
            case ActiveState.MovePointFirst:
                Modify(ref m_cachedPosition, 0);
                goto UpdatePlacement;
            case ActiveState.MovePointSecond:
                Modify(ref m_cachedPosition, 1);
                goto UpdatePlacement;
            case ActiveState.MovePointThird:
                Modify(ref m_cachedPosition, 2);
                goto UpdatePlacement;
            }
            return;
UpdatePlacement:
            SegmentState.UpdatePlacement();
UpdateCurve:
            DrawMode.CurrentMode.UpdateCurve();
        }

        public static void UpdateCached() => UpdateCached(m_controlPoints);

        public static void UpdateCached(PointInfo[] controlPoints) {
            PointInfo[] cachedPoints = m_cachedControlPoints;
            while (!Monitor.TryEnter(m_cacheLock, SimulationManager.SYNCHRONIZE_TIMEOUT)) { }
            try {
                cachedPoints[0] = controlPoints[0];
                cachedPoints[1] = controlPoints[1];
                cachedPoints[2] = controlPoints[2];
            } finally {
                Monitor.Exit(m_cacheLock);
            }
        }

        public static void Modify(ref Vector3 position, int index) => Modify(DrawMode.CurrentMode, ref position, index, ActiveDrawState.m_currentState, DrawMode.Current);

        public static void Modify(ActiveDrawState currentMode, ref Vector3 position, int index, ActiveState currentState, int drawMode) {
            int validPoints = m_validPoints;
            Vector3 vectorZero; vectorZero.x = 0; vectorZero.y = 0; vectorZero.z = 0;
            PointInfo[] controlPoints = m_controlPoints;
            switch (index) {
            case 0:
                if (validPoints <= 1 && currentState == ActiveState.CreatePointFirst) {
                    controlPoints[0].m_position = position;
                    controlPoints[0].m_direction = vectorZero;
                } else if ((validPoints == 2 || validPoints == 3) && currentState == ActiveState.MovePointFirst) {
                    Vector3 normVector = (controlPoints[1].m_position - position);
                    normVector.y = 0f;
                    normVector.Normalize();
                    controlPoints[0].m_position = position;
                    controlPoints[0].m_direction = normVector;
                    controlPoints[1].m_direction = normVector;
                    if (drawMode == DrawMode.FREEFORM) {
                        Reverse(controlPoints);
                        GetFreeformMidPoint(controlPoints);
                        Reverse(controlPoints);
                    }
                } else if ((validPoints == 1 && currentState == ActiveState.CreatePointSecond) || (validPoints == 2 && currentState == ActiveState.CreatePointThird)) {
                    switch (drawMode) {
                    case DrawMode.STRAIGHT:
                    case DrawMode.CIRCLE:
                        Vector3 normVector = (controlPoints[1].m_position - position);
                        normVector.y = 0f;
                        normVector.Normalize();
                        controlPoints[0].m_position = position;
                        controlPoints[0].m_direction = normVector;
                        controlPoints[1].m_direction = normVector;
                        UpdateCached(controlPoints);
                        currentMode.UpdateCurve(m_cachedControlPoints, validPoints);
                        break;
                    }
                }
                break;
            case 1:
                if (validPoints == 1 && currentState == ActiveState.CreatePointSecond) {
                    Vector3 normVector = (position - controlPoints[0].m_position);
                    normVector.y = 0f;
                    normVector.Normalize();
                    controlPoints[1].m_position = position;
                    controlPoints[0].m_direction = normVector;
                    controlPoints[1].m_direction = normVector;
                } else if ((validPoints == 1 || validPoints == 2) && currentState == ActiveState.MovePointSecond) {
                    Vector3 normVector = (position - controlPoints[0].m_position);
                    normVector.y = 0f;
                    normVector.Normalize();
                    controlPoints[1].m_position = position;
                    controlPoints[0].m_direction = normVector;
                    controlPoints[1].m_direction = normVector;
                    normVector = (controlPoints[2].m_position - position);
                    normVector.y = 0f;
                    normVector.Normalize();
                    controlPoints[2].m_direction = normVector;
                    if (drawMode == DrawMode.FREEFORM) {
                        GetFreeformMidPoint(controlPoints);
                    }
                }
                break;
            case 2:
                if (validPoints == 2 && currentState == ActiveState.CreatePointThird) {
                    if (drawMode == DrawMode.FREEFORM) {
                        controlPoints[2].m_position = position;
                        GetFreeformMidPoint(controlPoints);
                    } else {
                        Vector3 normVector = (position - controlPoints[1].m_position);
                        normVector.y = 0f;
                        normVector.Normalize();
                        controlPoints[2].m_position = position;
                        controlPoints[2].m_direction = normVector;
                    }
                } else if ((validPoints == 2 || validPoints == 3) && currentState == ActiveState.MovePointThird) {
                    if (drawMode == DrawMode.FREEFORM) {
                        m_controlPoints[2].m_position = position;
                        GetFreeformMidPoint(controlPoints);
                    } else {
                        Vector3 normVector = (position - controlPoints[1].m_position);
                        normVector.y = 0f;
                        normVector.Normalize();
                        controlPoints[2].m_position = position;
                        controlPoints[2].m_direction = normVector;
                    }
                }
                break;
            default:
                return;
            }
            UpdateCached(controlPoints);
        }

        public static void Reverse(PointInfo[] controlPoints) {
            PointInfo tempPoint = controlPoints[0];
            controlPoints[0] = controlPoints[2];
            controlPoints[2] = tempPoint;
            controlPoints[1].m_direction = controlPoints[0].m_direction = -controlPoints[0].m_direction;
            controlPoints[2].m_direction = -controlPoints[2].m_direction;
        }

        public static void Reset() {
            m_validPoints = 0;
            m_positionChanging = true;
        }

        public static void Cancel() {
            int validPoints = m_validPoints;
            switch (validPoints) {
            case 0:
                Modify(ref m_cachedPosition, 0);
                break;
            case 1:
                m_validPoints = 0;
                break;
            case 2:
                m_validPoints = 1;
                m_controlPoints[0].m_direction = m_controlPoints[1].m_direction;
                Modify(ref m_mousePosition, 1);
                break;
            case 3:
                m_validPoints = 2;
                Modify(ref m_mousePosition, 2);
                break;
            default:
                return;
            }
            m_positionChanging = true;
            UpdateCachedPosition(true);
        }
    }
}
