﻿using EManagersLib;
using PropAnarchy.PLT.Extensions;
using System;
using UnityEngine;

namespace PropAnarchy.PLT.MathUtils {
    /// <summary>
    /// A circle that lies parallel to the XZ plane, with orientation towards start angle, and at a height of center.y.
    /// </summary>
    public readonly struct CircleXZ {
        public readonly Vector3 m_center;
        public readonly float m_radius;

        /// <summary>
        /// The starting angle of the circle, in radians. Aka where the CCW rotation starts from. Conventionally zero radians.
        /// </summary>
        public readonly float m_angleStart;
        public float Circumference => 2f * Mathf.PI * m_radius;
        public float Diameter => 2f * m_radius;

        /// <param name="t">Generally from 0 to 1: [0, 1].</param>
        /// <returns>Returns a point on the circle outline</returns>
        public Vector3 Position(float t) {
            Vector3 center = m_center;
            float radius = m_radius;
            float angleStart = m_angleStart;
            if (radius == 0f) return center;
            Vector3 position;
            position.x = center.x + radius * (float)Math.Cos(angleStart + 2f * Mathf.PI * t);
            position.z = center.z + radius * (float)Math.Sin(angleStart + 2f * Mathf.PI * t);
            position.y = center.y;
            return position;
        }

        /// <summary>
        /// Returns a tangent point on the circle outline.
        /// </summary>
        /// <param name="t">Generally from 0 to 1: [0, 1].</param>
        /// <returns></returns>
        public VectorXZ Tangent(float t) {
            float radius = m_radius;
            float angleStart = m_angleStart;
            if (radius == 0f) return VectorXZ.zero;
            VectorXZ tangent;
            tangent.x = (-2f * Mathf.PI) * radius * (float)Math.Sin(angleStart + (2f * Mathf.PI) * t);
            tangent.z = (2f * Mathf.PI) * radius * (float)Math.Cos(angleStart + (2f * Mathf.PI) * t);
            return tangent;
        }

        //use for non-fence mode
        public float DeltaT(float distance) {
            if (distance == 0f) return 0f;
            if (m_radius == 0f) return 1f;
            return distance / (2f * Mathf.PI * m_radius);
        }

        //use for non-fence mode
        /// <param name="distance"></param>
        /// <returns>Returns the angle, in radians, of the angle subtended by an arc of given length</returns>
        public float DeltaAngle(float distance) => DeltaT(distance) * 2f * Mathf.PI;

        //use for fence mode
        /// <param name="chordLength"></param>
        /// <returns>Returns the angle, in radians, of the angle inscribed by a chord of given length</returns>
        public float ChordAngle(float chordLength) {
            if (chordLength == 0f) return 0f;
            if (m_radius == 0f) return 2f * Mathf.PI;
            if (chordLength > Diameter) return 2f * Mathf.PI;
            return 2f * (float)EMath.Asin(chordLength / (2f * m_radius));
        }

        //use for fence mode?
        public float ChordDeltaT(float chordLength) {
            if (chordLength == 0f) return 0f;
            if (m_radius == 0f) return 1f;
            if (chordLength > Diameter) return 1f;
            return ChordAngle(chordLength) / (2f * Mathf.PI);
        }

        //use for non-fence mode
        public float PerfectRadiusByArcs(float arcLength) {
            if (arcLength > 0f) {
                float radiusPerfect = arcLength * Mathf.Round(EMath.Abs(Circumference / arcLength)) / (2f * Mathf.PI);
                if (radiusPerfect > 0f) {
                    return radiusPerfect;
                }
            }
            return m_radius;
        }

        //use for fence mode
        public float PerfectRadiusByChords(float chordLength) {
            if (chordLength > 0f) {
                float numChordsPerfect = Mathf.Round(EMath.Abs((2f * Mathf.PI) / ChordAngle(chordLength)));
                if (numChordsPerfect <= 0f) return m_radius;
                float radiusPerfect = chordLength / (2f * Mathf.Sin(((2f * Mathf.PI) / numChordsPerfect) / 2f));
                if (radiusPerfect > 0f) {
                    return radiusPerfect;
                }
            }
            return m_radius;
        }

        public float DistanceSqr(Vector3 position, out float u) {
            if (position == m_center) {
                u = 0f;
                return 0f;
            }
            VectorXZ pointVector = position - m_center;
            u = AngleFromStartXZ(position, m_center, Position(0f)) / (2f * Mathf.PI);
            return pointVector.sqrMagnitude;
        }

        /// <summary>
        /// Checks to see whether a given point is within a specified distance from a circle outline. In the XZ-plane. Outputs the closest t-value (to the given point) on the curve.
        /// </summary>
        /// <param name="distanceThreshold"></param>
        /// <param name="pointOfInterest"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public bool IsCloseToCircle3XZ(float distanceThreshold, VectorXZ pointOfInterest, out float t) {
            bool isClose(float distanceSqr, float min, float max) => distanceSqr >= min * min && distanceSqr <= max * max;
            VectorXZ circleCenter = m_center;
            if (distanceThreshold == 0f) {
                t = 0.5f;
                return pointOfInterest == circleCenter;
            } else if (distanceThreshold < 0f) distanceThreshold = EMath.Abs(distanceThreshold);
            float circleRadius = m_radius;
            return isClose(DistanceSqr(pointOfInterest, out t), circleRadius - distanceThreshold, circleRadius + distanceThreshold);
        }

        /// <summary>Checks to see whether a given point is close to a circle outline. In the XZ-plane</summary>
        /// <param name="pointOfInterest"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        public bool IsNearCircleOutlineXZ(VectorXZ pointOfInterest, float distance) {
            bool isNearCircle(float distanceSqr, float min, float max) => distanceSqr >= min * min && distanceSqr <= max * max;
            VectorXZ circleCenter = m_center;
            if (distance == 0f) return pointOfInterest == circleCenter;
            else if (distance < 0f) distance = EMath.Abs(distance);
            float circleRadius = m_radius;
            return isNearCircle((pointOfInterest - circleCenter).sqrMagnitude, circleRadius - distance, circleRadius + distance);
        }

        /// <param name="position"></param>
        /// <returns>Returns the angle, in radians, between a line pointing from the center to a given point, and the line pointing from the center to the start point (t = 0f)</returns>
        public float AngleFromStartXZ(VectorXZ position, VectorXZ center, VectorXZ outlinePos) {
            VectorXZ pointVector = position - center;
            pointVector.Normalize();
            VectorXZ zeroVector = outlinePos - center;
            zeroVector.Normalize();
            float angleFromStart = Vector3Extensions.AngleSigned(pointVector, zeroVector, EMath.Vector3Up);
            if (angleFromStart < 0f) {
                angleFromStart += 2f * Mathf.PI;
            }
            return angleFromStart;
        }

        /// <param name="t1">Generally [0, 1].</param>
        /// <param name="t2">Generally [0, 1].</param>
        /// <returns>Returns the angle, in radians, between two points on the circle</returns>
        public float AngleBetween(float t1, float t2) => (t2 - t1) * 2f * Mathf.PI;

        /// <param name="t1">Generally [0, 1].</param>
        /// <param name="t2">Generally [0, 1].</param>
        /// <returns>Returns the distance traveled, in units of distance, between two points on the circle</returns>
        public float ArclengthBetween(float t1, float t2) => AngleBetween(t1, t2) * m_radius;

        public CircleXZ(Vector3 center, float radius) {
            m_center = center;
            m_radius = radius;
            m_angleStart = 0f;
        }

        public static Vector3 Position3FromAngleXZ(Vector3 center, float radius, float theta) {
            Vector3 result;
            result.y = 0f;
            result.x = center.x + radius * (float)Math.Cos(theta);
            result.z = center.z + radius * (float)Math.Sin(theta);
            return result;
        }

        /// <param name="angleStart">Angle in radians.</param>
        public CircleXZ(Vector3 center, float radius, float angleStart) {
            m_center = center;
            m_radius = radius;
            m_angleStart = angleStart;
        }

        public CircleXZ(Vector3 center, Vector3 pointOnCircle, float spacing) {
            if (pointOnCircle == center) {
                m_center = center;
                m_radius = 0f;
                m_angleStart = 0f;
                return;
            }
            m_center = center;
            VectorXZ radiusVector = pointOnCircle - center;
            m_radius = radiusVector.magnitude;
            m_angleStart = radiusVector.AngleSigned(EMath.Vector3Right, EMath.Vector3Up);
            if (Settings.PerfectCircles) {
                m_radius = PropLineTool.ItemInfo.FenceMode ? PerfectRadiusByChords(spacing) : PerfectRadiusByArcs(spacing);
            }
        }

        public CircleXZ(VectorXZ center, VectorXZ pointOnCircle, float perfectRadius) {
            if (pointOnCircle == center) {
                m_center = center;
                m_radius = 0f;
                m_angleStart = 0f;
                return;
            }
            m_center = center;
            VectorXZ radiusVector = pointOnCircle - center;
            m_radius = radiusVector.magnitude;
            m_angleStart = radiusVector.AngleSigned(EMath.Vector3Right, EMath.Vector3Up);
            if (Settings.PerfectCircles) {
                m_radius = PropLineTool.ItemInfo.FenceMode ? PerfectRadiusByChords(perfectRadius) : PerfectRadiusByArcs(perfectRadius);
            }
        }
    }
}
