using ColossalFramework.Math;
using System;
using UnityEngine;

namespace PropAnarchy.PLT {
    public static class Segment3Extension {
        public static float LengthXZ(ref this Segment3 line) => Vector3.Distance(new Vector3(line.a.x, 0f, line.a.z), new Vector3(line.b.x, 0f, line.b.z));

        /// <summary>
        /// Interpolates and Extrapolates the position along a parametric line defined by two points.
        /// </summary>
        /// <param name="segment">Line segment from p0 to p1</param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static Vector3 LinePosition(ref this Segment3 segment, float t) {
            float num = 1.0f - t;
            Vector3 finalVector;
            finalVector.x = segment.b.x + num * (segment.a.x - segment.b.x);
            finalVector.z = segment.b.z + num * (segment.a.z - segment.b.z);
            finalVector.y = segment.b.y + num * (segment.a.y - segment.b.y);
            return finalVector;
        }

        /// <summary>Constrains Segment to XZ plane</summary>
        public static void SegmentXZ(ref this Segment3 lineSegment) {
            lineSegment.a.y = 0f;
            lineSegment.b.y = 0f;
        }

        /// <param name="segment"></param>
        /// <returns>Returns the xz speed of the line in units of distance/(delta-t)</returns>
        public static float LinearSpeedXZ(ref this Segment3 segment) {
            float Pow2(float x) => x * x;
            Vector3 tanVector = segment.b - segment.a;
            return (float)Math.Sqrt(Pow2(tanVector.x) + Pow2(tanVector.z));
        }

        public static Vector3 Direction(ref this Segment3 segment) => segment.b - segment.a;

        /// <summary>
        /// Checks to see whether a given point is within a specified distance from a line segment. In the XZ-plane. Outputs the closest t-value (to the given point) on the curve.
        /// </summary>
        /// <param name="distanceThreshold"></param>
        /// <param name="pointOfInterest"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static bool IsCloseToSegmentXZ(ref this Segment3 lineSegment, float distanceThreshold, Vector3 pointOfInterest, out float t) {
            //constrain to XZ plane
            lineSegment.SegmentXZ();
            pointOfInterest.y = 0f;
            return lineSegment.DistanceSqr(pointOfInterest, out t) <= distanceThreshold * distanceThreshold;
        }

    }
}
