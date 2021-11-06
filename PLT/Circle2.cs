using System;
using UnityEngine;

namespace PropAnarchy.PLT {
    public struct Circle2 {
        public Vector2 m_center;
        public float m_radius;

        public Circle2 UnitCircle => new Circle2(default, 1f);

        /// <param name="t">Generally from 0 to 1. [0, 1]</param>
        public Vector2 Position(float t) {
            Vector2 result;
            result.x = m_center.x + m_radius * (float)Math.Cos(2 * Mathf.PI * t);
            result.y = m_center.y + m_radius * (float)Math.Sin(2 * Mathf.PI * t);
            return result;
        }

        /// <param name="theta">Angle in radians. [0, 2pi]</param>
        public Vector2 PositionFromAngle(float theta) {
            Vector2 result;
            result.x = m_center.x + m_radius * (float)Math.Cos(theta);
            result.y = m_center.y + m_radius * (float)Math.Sin(theta);
            return result;
        }

        /// <summary>Returns XZ position on a circle in the XZ-plane</summary>
        /// <param name="theta">Angle in radians. [0, 2pi]</param>
        public static Vector3 Position3FromAngleXZ(Vector3 center, float radius, float theta) {
            Vector3 result;
            result.x = center.x + radius * Mathf.Cos(theta);
            result.z = center.z + radius * Mathf.Sin(theta);
            result.y = 0f;
            return result;
        }

        /// <param name="theta">Angle in degrees. [0, 360]</param>
        public Vector2 PositionFromAngleDegrees(float theta) {
            Vector2 result;
            theta *= Mathf.Deg2Rad;
            result.x = m_center.x + m_radius * Mathf.Cos(theta);
            result.y = m_center.y + m_radius * Mathf.Sin(theta);
            return result;
        }

        public Circle2(Vector2 center, float radius) {
            m_center = center;
            m_radius = radius;
        }
    }
}
