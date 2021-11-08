using System;
using UnityEngine;
using static PropAnarchy.PLT.PropLineTool;

namespace PropAnarchy.PLT {
    public static class ControlPoint {
        public const int MAX_CONTROL_POINTS = 3;
        public struct PointInfo {
            public Vector3 m_position;
            public VectorXZ m_direction;
            public bool m_outside;
        }
        public static PointInfo[] m_controlPoints = new PointInfo[MAX_CONTROL_POINTS];
        public static PointInfo[] m_lockedControlPoints = new PointInfo[MAX_CONTROL_POINTS];
        public static int m_validPoints = 0;

        private static void GetFreeformMidPoint(PointInfo[] controlPoints) {
            VectorXZ p2_p0 = controlPoints[2].m_position - controlPoints[0].m_position;
            VectorXZ dir_p1 = controlPoints[1].m_direction;
            float sqrMag_2_0 = p2_p0.sqrMagnitude;
            p2_p0.Normalize();
            float angle_0 = Math.Min(1.17809725f, (float)Math.Acos(VectorXZ.Dot(p2_p0, dir_p1)));
            float dist_p1_p0 = (float)Math.Sqrt(0.5f * sqrMag_2_0 / Math.Max(0.001f, 1f - (float)Math.Cos(3.14159274f - 2f * angle_0)));
            controlPoints[1].m_position = (controlPoints[0].m_position + dir_p1 * dist_p1_p0);
            VectorXZ dir_p2 = controlPoints[2].m_position - controlPoints[1].m_position;
            dir_p2.Normalize();
            controlPoints[2].m_direction = dir_p2;
            //sometimes things don't work corrently
            if (float.IsNaN(controlPoints[1].m_position.x) || float.IsNaN(controlPoints[1].m_position.y) || float.IsNaN(controlPoints[1].m_position.z)) {
                controlPoints[1].m_position = controlPoints[0].m_position + controlPoints[0].m_direction * 0.01f;
                controlPoints[1].m_direction = controlPoints[0].m_direction;
            }
        }

        public static void Add(Vector3 position) {
            PointInfo[] controlPoints = m_controlPoints;
            switch (m_validPoints) {
            case 0:
                controlPoints[0].m_position = position;
                controlPoints[0].m_direction = default;
                m_validPoints = 1;
                m_positionChanging = true;
                break;
            case 1:
                VectorXZ normVector = position - controlPoints[0].m_position;
                normVector.Normalize();
                controlPoints[0].m_direction = normVector;
                controlPoints[1].m_position = position;
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
                    normVector.Normalize();
                    controlPoints[2].m_position = position;
                    controlPoints[2].m_direction = normVector;
                }
                m_validPoints = 3;
                m_positionChanging = true;
                break;
            }
        }

        public static void Modify(Vector3 position, int index) => Modify(DrawMode.CurrentMode, position, index, ActiveDrawState.m_currentState, DrawMode.Current);

        public static void Modify(ActiveDrawState currentMode, Vector3 position, int index, ActiveState currentState, int drawMode) {
            int validPoints = m_validPoints;
            PointInfo[] controlPoints = m_controlPoints;
            switch (index) {
            case 0:
                if (validPoints <= 1 && currentState == ActiveState.CreatePointFirst) {
                    controlPoints[0].m_position = position;
                    controlPoints[0].m_direction = default;
                } else if ((validPoints == 2 || validPoints == 3) && currentState == ActiveState.MovePointFirst) {
                    VectorXZ normVector = (controlPoints[1].m_position - position);
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
                        VectorXZ normVector = (controlPoints[1].m_position - position);
                        normVector.Normalize();
                        controlPoints[0].m_position = position;
                        controlPoints[0].m_direction = normVector;
                        controlPoints[1].m_direction = normVector;
                        currentMode.UpdateCurve(controlPoints, validPoints);
                        break;
                    }
                }
                break;
            case 1:
                if (validPoints == 1 && currentState == ActiveState.CreatePointSecond) {
                    VectorXZ normVector = (position - controlPoints[0].m_position);
                    normVector.Normalize();
                    controlPoints[0].m_direction = normVector;
                    controlPoints[1].m_position = position;
                    controlPoints[1].m_direction = normVector;
                } else if ((validPoints == 2 || validPoints == 3) && currentState == ActiveState.MovePointSecond) {
                    VectorXZ normVector = (position - controlPoints[0].m_position);
                    normVector.Normalize();
                    controlPoints[0].m_direction = normVector;
                    controlPoints[1].m_position = position;
                    controlPoints[1].m_direction = normVector;
                    normVector = (controlPoints[2].m_position - position);
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
                        VectorXZ normVector = (position - controlPoints[1].m_position);
                        normVector.Normalize();
                        controlPoints[2].m_position = position;
                        controlPoints[2].m_direction = normVector;
                    }
                } else if ((validPoints == 2 || validPoints == 3) && currentState == ActiveState.MovePointThird) {
                    if (drawMode == DrawMode.FREEFORM) {
                        m_controlPoints[2].m_position = position;
                        GetFreeformMidPoint(controlPoints);
                    } else {
                        VectorXZ normVector = (position - controlPoints[1].m_position);
                        normVector.Normalize();
                        controlPoints[2].m_position = position;
                        controlPoints[2].m_direction = normVector;
                    }
                }
                break;
            default:
                return;
            }
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
            switch (m_validPoints) {
            case 0:
                Modify(m_cachedPosition, 0);
                break;
            case 1:
                m_validPoints = 0;
                break;
            case 2:
                m_validPoints = 1;
                m_controlPoints[0].m_direction = m_controlPoints[1].m_direction;
                Modify(m_mousePosition, 1);
                break;
            case 3:
                m_validPoints = 2;
                Modify(m_mousePosition, 2);
                break;
            default:
                return;
            }
            m_positionChanging = true;
            UpdateCachedPosition(true);
        }
    }
}
