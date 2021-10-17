using ColossalFramework.Math;
using System;
using UnityEngine;

namespace PropAnarchy.PLT {
    public static class PLTMath {
        //Special Thanks to Tinus on the UnityForums for this!
        /// <summary>Determines the signed angle (-pi to pi) radians between two vectors</summary>
        /// <param name="v1">first vector</param>
        /// <param name="v2">second vector</param>
        /// <param name="n">rotation axis (usually plane normal of v1, v2)</param>
        /// <returns>signed angle (in Radians) between v1 and v2</returns>
        public static float AngleSigned(Vector3 v1, Vector3 v2, Vector3 n) => Mathf.Atan2(Vector3.Dot(n, Vector3.Cross(v1, v2)), Vector3.Dot(v1, v2));

        /// <summary>Normalizes an angle (in degrees) between 0 and 360 degrees</summary>
        /// <param name="inputAngle">angle in Degrees</param>
        /// <returns></returns>
        public static float NormalizeAngle360(float inputAngle) => (inputAngle < 0f) ? -1f * Math.Abs(inputAngle) % 360f : Math.Abs(inputAngle) % 360f;

        /// <summary>Constrains Bezier to XZ plane</summary>
        /// <param name="bezier"></param>
        /// <returns>A bezier curve with y-components set to zero</returns>
        public static Bezier3 BezierXZ(Bezier3 bezier) {
            bezier.a.y = 0f;
            bezier.b.y = 0f;
            bezier.c.y = 0f;
            bezier.d.y = 0f;
            return bezier;
        }

        /// <summary>Constrains input Bezier to XZ plane</summary>
        /// <param name="bezier"></param>
        public static void BezierXZ(ref Bezier3 bezier) {
            bezier.a.y = 0f;
            bezier.b.y = 0f;
            bezier.c.y = 0f;
            bezier.d.y = 0f;
        }

        /// <summary>Constrains Segment to XZ plane</summary>
        /// <param name="bezier"></param>
        /// <returns>A bezier curve with y-components set to zero</returns>
        public static Segment3 SegmentXZ(Segment3 lineSegment) {
            lineSegment.a.y = 0f;
            lineSegment.b.y = 0f;
            return lineSegment;
        }

        /// <summary>Constrains input Segment to XZ plane</summary>
        /// <param name="bezier"></param>
        /// <returns>A bezier curve with y-components set to zero</returns>
        public static void SegmentXZ(ref Segment3 lineSegment) {
            lineSegment.a.y = 0f;
            lineSegment.b.y = 0f;
        }

        //standard conversion
        public static Bezier3 QuadraticToCubicBezier(Vector3 startPoint, Vector3 middlePoint, Vector3 endPoint) {
            Bezier3 bezier;
            bezier.a = startPoint;
            bezier.b = startPoint + (2.0f / 3.0f) * (middlePoint - startPoint);
            bezier.c = endPoint + (2.0f / 3.0f) * (middlePoint - endPoint);
            bezier.d = endPoint;
            return bezier;
        }

        //CO's in-house method
        //uses negative of endDirection
        //rounds out tight re-curves (or tight curves)
        public static Bezier3 QuadraticToCubicBezierCOMethod(Vector3 startPoint, Vector3 startDirection, Vector3 endPoint, Vector3 endDirection /*switch this sign when using!*/) {
            Bezier3 bezier;
            bezier.a = startPoint;
            bezier.d = endPoint;
            NetSegment.CalculateMiddlePoints(startPoint, startDirection, endPoint, endDirection, false, false, out bezier.b, out bezier.c);
            return bezier;
        }

        /// <summary>
        /// Interpolates and Extrapolates the position along a parametric line defined by two points.
        /// </summary>
        /// <param name="segment">Line segment from p0 to p1</param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static Vector3 LinePosition(Segment3 segment, float t) {
            float num = 1.0f - t;
            Vector3 p0 = segment.a;
            Vector3 p1 = segment.b;
            return new Vector3(p1.x + num * (p0.x - p1.x), p1.y + num * (p0.y - p1.y), p1.z + num * (p0.z - p1.z));
        }

        //used to calculate t in non-fence Curved and Freeform modes
        //for each individual item
        /// <summary>Solves for t-value which would be a length of *distance* along the curve from original point at t = *tStart*</summary>
        /// <param name="bezier"></param>
        /// <param name="tStart"></param>
        /// <param name="distance"></param>
        /// <param name="tolerance"></param>
        /// <param name="tEnd"></param>
        public static void StepDistanceCurve(Bezier3 bezier, float tStart, float distance, float tolerance, out float tEnd) {
            float tCurrent = bezier.Travel(tStart, distance);
            float distCurrent = CubicBezierArcLengthXZGauss04(bezier, tStart, tCurrent);
            float toleranceSqr = tolerance * tolerance;
            for (int i = 0; i < 12 && Pow(distance - distCurrent, 2) > toleranceSqr; i++) {
                distCurrent = CubicBezierArcLengthXZGauss04(bezier, tStart, tCurrent);
                tCurrent += (distance - distCurrent) / CubicSpeedXZ(bezier, tCurrent);
            }
            tEnd = tCurrent;
        }


        //used to calculate t in Fence-Mode-ON Curved and Freeform modes
        //   In Placement Calculator:
        //   use this t to set the fence endpoints (:
        //   then calculate fence midpoints/placement points from the endpoints
        /// <summary>Solves for t-value which would yield a further point on the curve that is a _straight_ length of *lengthOfSegment* from the original point at t = *tStart*</summary>
        /// <param name="bezier"></param>
        /// <param name="tStart"></param>
        /// <param name="lengthOfSegment"></param>
        /// <param name="tolerance"></param>
        /// <param name="tEnd"></param>
        /// <param name="allowBackwards">Set to false to only step forward along the curve in the direction t=0 -> t=1.</param>
        public static bool CircleCurveFenceIntersectXZ(Bezier3 bezier, float tStart, float lengthOfSegment, float tolerance, out float tEnd, bool allowBackwards) {
            const float adjustmentScalar = 1.0f;  //if using multiplicity, _adjustmentScalar = 2
            Bezier3 bezierXZ = BezierXZ(bezier);
            float toleranceSqr = tolerance * tolerance;

            //haven't tested this to see if it will really go backwards
            //original as of 161111 2221
            //lengthOfSegment = Mathf.Abs(lengthOfSegment);
            //new as of 161111 2221
            if (!allowBackwards) lengthOfSegment = Math.Abs(lengthOfSegment);

            if (lengthOfSegment == 0f) {
                tEnd = tStart;
                return false;
            }
            //initial guess setup
            StepDistanceCurve(bezierXZ, tStart, lengthOfSegment, tolerance, out float t0);
            float iteratedDistance = Vector3.Distance(bezierXZ.Position(t0), bezierXZ.Position(tStart));
            for (int i = 0; i < 25 && Pow(iteratedDistance - lengthOfSegment, 2) > toleranceSqr; i++) {
                float errorFunc = PLTErrorFunctionXZ(bezierXZ, t0, tStart, lengthOfSegment);
                float errorPrime = PLTErrorFunctionPrimeXZ(bezierXZ, t0, tStart);
                t0 -= adjustmentScalar * (errorFunc / errorPrime);
                if (!allowBackwards && t0 < tStart) t0 = 1f;
                iteratedDistance = Vector3.Distance(bezierXZ.Position(t0), bezierXZ.Position(tStart));
            }
            tEnd = t0;
            if (Pow(iteratedDistance - lengthOfSegment, 2) > toleranceSqr) return false; // failed to converge
            return true;
        }

        //Specialty function
        //used to calculate t in Fence-Mode-ON Curved and Freeform modes
        //   In Placement Calculator:
        //   use this t to set the fence endpoints (:
        //   then calculate fence midpoints/placement points from the endpoints
        /// <summary>
        /// Specialty version of CircleCurveFenceIntersectXZ used to link curves. Solves for t-value which would yield a point on the curve that is a _straight_ length of *lengthOfSegment* from the original point usually off the curve at *startPos*.
        /// </summary>
        /// <param name="_bezier"></param>
        /// <param name="startPos"></param>
        /// <param name="lengthOfSegment"></param>
        /// <param name="tolerance"></param>
        /// <param name="tEnd"></param>
        /// <param name="allowBackwards">Set to false to only step forward along the curve in the direction t=0 -> t=1.</param>
        public static bool LinkCircleCurveFenceIntersectXZ(Bezier3 bezier, Vector3 startPos, float lengthOfSegment, float tolerance, out float tEnd, bool allowBackwards) {
            const float adjustmentScalar = 1.0f;  //if using multiplicity, _adjustmentScalar = 2
            Bezier3 bezierXZ = BezierXZ(bezier);
            float toleranceSqr = tolerance * tolerance;
            lengthOfSegment = Math.Abs(lengthOfSegment);
            if (lengthOfSegment == 0f) {
                tEnd = 0f;
                return false;
            }
            //initial guess setup
            float leftoverLength = lengthOfSegment - Vector3.Distance(startPos, bezierXZ.a);
            StepDistanceCurve(bezierXZ, 0f, leftoverLength, tolerance, out float t0);
            float iteratedDistance = Vector3.Distance(bezierXZ.Position(t0), startPos);
            for (int i = 0; i < 12 && Pow(iteratedDistance - lengthOfSegment, 2) > toleranceSqr; i++) {
                float errorFunc = PLTLinkErrorFunctionXZ(bezierXZ, t0, startPos, lengthOfSegment);
                float errorPrime = PLTLinkErrorFunctionPrimeXZ(bezierXZ, t0, startPos);
                t0 -= adjustmentScalar * (errorFunc / errorPrime);
                iteratedDistance = Vector3.Distance(bezierXZ.Position(t0), startPos);
            }
            tEnd = t0;
            if (Pow(iteratedDistance - lengthOfSegment, 2) > toleranceSqr) return false;
            return true;
        }

        //Uses Legendre-Gauss Quadrature with n = 12.
        /// <param name="bezier"></param>
        /// <param name="t1"></param>
        /// <param name="t2"></param>
        /// <returns>Returns the XZ arclength of a cubic bezier curve between t1 and t2</returns>
        public static float CubicBezierArcLengthXZGauss12(Bezier3 bezier, float t1, float t2) => ((t2 - t1) / 2f) *
            (CubicSpeedXZGaussPoint(bezier, 0.1252334085114689f, 0.2491470458134028f, t1, t2) +
             CubicSpeedXZGaussPoint(bezier, -0.1252334085114689f, 0.2491470458134028f, t1, t2) +
             CubicSpeedXZGaussPoint(bezier, 0.3678314989981802f, 0.2334925365383548f, t1, t2) +
             CubicSpeedXZGaussPoint(bezier, -0.3678314989981802f, 0.2334925365383548f, t1, t2) +
             CubicSpeedXZGaussPoint(bezier, 0.5873179542866175f, 0.2031674267230659f, t1, t2) +
             CubicSpeedXZGaussPoint(bezier, -0.5873179542866175f, 0.2031674267230659f, t1, t2) +
             CubicSpeedXZGaussPoint(bezier, 0.7699026741943047f, 0.1600783285433462f, t1, t2) +
             CubicSpeedXZGaussPoint(bezier, -0.7699026741943047f, 0.1600783285433462f, t1, t2) +
             CubicSpeedXZGaussPoint(bezier, 0.9041172563704749f, 0.1069393259953184f, t1, t2) +
             CubicSpeedXZGaussPoint(bezier, -0.9041172563704749f, 0.1069393259953184f, t1, t2) +
             CubicSpeedXZGaussPoint(bezier, 0.9815606342467192f, 0.0471753363865118f, t1, t2) +
             CubicSpeedXZGaussPoint(bezier, -0.9815606342467192f, 0.0471753363865118f, t1, t2));

        //Uses Legendre-Gauss Quadrature with n = 4.
        /// <param name="bezier"></param>
        /// <param name="t1"></param>
        /// <param name="t2"></param>
        /// <returns>Returns the XZ arclength of a cubic bezier curve between t1 and t2</returns>
        public static float CubicBezierArcLengthXZGauss04(Bezier3 bezier, float t1, float t2) => ((t2 - t1) / 2f) *
            (CubicSpeedXZGaussPoint(bezier, 0.3399810435848563f, 0.6521451548625461f, t1, t2) +
             CubicSpeedXZGaussPoint(bezier, -0.3399810435848563f, 0.6521451548625461f, t1, t2) +
             CubicSpeedXZGaussPoint(bezier, 0.8611363115940526f, 0.3478548451374538f, t1, t2) +
             CubicSpeedXZGaussPoint(bezier, -0.8611363115940526f, 0.3478548451374538f, t1, t2));

        //Uses Legendre-Gauss Quadrature with n = 3.
        /// <param name="bezier"></param>
        /// <param name="t1"></param>
        /// <param name="t2"></param>
        /// <returns>Returns the XZ arclength of a cubic bezier curve between t1 and t2</returns>
        public static float CubicBezierArcLengthXZGauss03(Bezier3 bezier, float t1, float t2) => ((t2 - t1) / 2f) *
            (CubicSpeedXZGaussPoint(bezier, 0.0f, 0.88888888f, t1, t2) +
             CubicSpeedXZGaussPoint(bezier, 0.77459667f, 0.55555555f, t1, t2) +
             CubicSpeedXZGaussPoint(bezier, -0.77459667f, 0.55555555f, t1, t2));

        //returns a single point for Gaussian Quadrature
        //of cubic bezier arc length
        private static float CubicSpeedXZGaussPoint(Bezier3 bezier, float x_i, float w_i, float a, float b) => w_i * CubicSpeedXZ(bezier, ((b - a) / 2f) * x_i + ((a + b) / 2f));

        //returns the integrand of the arc length function for a cubic bezier curve
        //constrained to the XZ-plane
        //at a specific t
        private static float CubicSpeedXZ(Bezier3 bezier, float t) {
            Vector3 tangent = bezier.Tangent(t);
            return (float)Math.Sqrt(Pow(tangent.x, 2) + Pow(tangent.z, 2));
        }

        /// <param name="segment"></param>
        /// <returns>Returns the xz speed of the line in units of distance/(delta-t)</returns>
        public static float LinearSpeedXZ(Segment3 segment) {
            Vector3 tanVector = segment.b - segment.a;
            return (float)Math.Sqrt(Pow(tanVector.x, 2) + Pow(tanVector.z, 2));
        }

        /// <param name="bezier"></param>
        /// <param name="t">Where are you guessing the intersection is?</param>
        /// <param name="tCenter">Center of the circle to intersect the curve.</param>
        /// <param name="radius">Radius of the circle to intersect the curve.</param>
        /// <returns>Returns E(t) = E(tGuess) : +/- Error [meters^2] in (straight-line distance between two points on a bezier curve) vs (radius).</returns>
        private static float PLTErrorFunctionXZ(Bezier3 bezier, float t, float tCenter, float radius) {
            if (t == tCenter) return 0f;
            Vector3 center = bezier.Position(tCenter);
            Vector3 guessPos = bezier.Position(t);
            return Pow(guessPos.x - center.x, 2) + Pow(guessPos.z - center.z, 2) - Pow(radius, 2);
        }

        /// <summary>Specialty Version of PLTErrorFunctionXZ used to link curves</summary>
        /// <param name="bezier"></param>
        /// <param name="t">Where are you guessing the intersection is?</param>
        /// <param name="centerPos">Center of the circle to intersect the curve.</param>
        /// <param name="radius">Radius of the circle to intersect the curve.</param>
        /// <returns>Returns E(t) = E(tGuess) : +/- Error [meters^2] in (straight-line distance between startPoint and point on bezier curve) vs (radius)</returns>
        private static float PLTLinkErrorFunctionXZ(Bezier3 bezier, float t, Vector3 centerPos, float radius) {
            Vector3 guessPos = bezier.Position(t);
            if (guessPos == centerPos) return 0f;
            return Pow(guessPos.x - centerPos.x, 2) + Pow(guessPos.z - centerPos.z, 2) - Pow(radius, 2);
        }

        /// <param name="bezier"></param>
        /// <param name="t">Where are you guessing the intersection is?</param>
        /// <param name="tCenter">Center of the circle to intersect the curve.</param>
        /// <returns>Returns E'(t) = E'(tGuess) : Derivative of +/- Error [meters^2] in (straight-line distance between two points on a bezier curve) vs (radius)</returns>
        private static float PLTErrorFunctionPrimeXZ(Bezier3 bezier, float t, float tCenter) {
            if (t == tCenter) return 0f;
            Vector3 center = bezier.Position(tCenter);
            Vector3 guessPos = bezier.Position(t);
            Vector3 derivPos = bezier.Tangent(t);
            return 2 * (guessPos.x - center.x) * derivPos.x + 2 * (guessPos.z - center.z) * derivPos.z;
        }

        /// <summary>Specialty Version of PLTErrorFunctionPrimeXZ used to link curves</summary>
        /// <param name="bezier"></param>
        /// <param name="t">Where are you guessing the intersection is?</param>
        /// <param name="centerPos">Center of the circle to intersect the curve.</param>
        /// <returns>Returns E'(t) = E'(tGuess) : Derivative of +/- Error [meters^2] in (straight-line distance between startPoint and point on bezier curve) vs (radius)</returns>
        private static float PLTLinkErrorFunctionPrimeXZ(Bezier3 bezier, float t, Vector3 centerPos) {
            Vector3 guessPos = bezier.Position(t);
            if (guessPos == centerPos) return 0f;
            Vector3 derivPos = bezier.Tangent(t);
            return 2 * (guessPos.x - centerPos.x) * derivPos.x + 2 * (guessPos.z - centerPos.z) * derivPos.z;
        }

        // =============  HOVERING STUFF  =============
        /// <summary>Checks to see whether a given point lies within a circle of given center and radius. In the XZ-plane</summary>
        /// <param name="circleCenter"></param>
        /// <param name="radius"></param>
        /// <param name="pointOfInterest"></param>
        /// <returns></returns>
        public static bool IsInsideCircleXZ(Vector3 circleCenter, float radius, Vector3 pointOfInterest) {
            if (radius == 0f) return pointOfInterest == circleCenter;
            else if (radius < 0f) radius = Mathf.Abs(radius);
            //constrain to XZ plane
            circleCenter.y = 0f;
            pointOfInterest.y = 0f;
            if ((pointOfInterest - circleCenter).sqrMagnitude <= radius * radius) return true;
            return false;
        }

        /// <summary>Checks to see whether a given point is close to a circle outline of given center and radius. In the XZ-plane.</summary>
        /// <param name="circleCenter"></param>
        /// <param name="radius"></param>
        /// <param name="pointOfInterest"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        public static bool IsNearCircleOutlineXZ(Vector3 circleCenter, float circleRadius, Vector3 pointOfInterest, float distance) {
            if (distance == 0f) return pointOfInterest == circleCenter;
            distance = Math.Abs(distance);
            //constrain to XZ plane
            circleCenter.y = 0f;
            pointOfInterest.y = 0f;
            float distanceSqr = (pointOfInterest - circleCenter).sqrMagnitude;
            if (distanceSqr >= Pow(circleRadius - distance, 2) && distanceSqr <= Pow(circleRadius + distance, 2)) return true;
            return false;
        }

        /// <summary>Checks to see whether a given point is close to a circle outline. In the XZ-plane</summary>
        /// <param name="circleCenter"></param>
        /// <param name="radius"></param>
        /// <param name="pointOfInterest"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        public static bool IsNearCircleOutlineXZ(Circle3XZ circle, Vector3 pointOfInterest, float distance) {
            Vector3 circleCenter = circle.m_center;
            float circleRadius = circle.m_radius;
            if (distance == 0f) return pointOfInterest == circleCenter;
            distance = Math.Abs(distance);
            //constrain to XZ plane
            circleCenter.y = 0f;
            pointOfInterest.y = 0f;
            float distanceSqr = (pointOfInterest - circleCenter).sqrMagnitude;
            if (distanceSqr >= Pow(circleRadius - distance, 2) && distanceSqr <= Pow(circleRadius + distance, 2)) return true;
            return false;
        }

        /// <summary>
        /// Checks to see whether a given point is within a specified distance from a bezier curve. In the XZ-plane. Outputs the closes t-value (to the given point) on the curve.
        /// </summary>
        /// <param name="curve"></param>
        /// <param name="distanceThreshold"></param>
        /// <param name="pointOfInterest"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static bool IsCloseToCurveXZ(Bezier3 curve, float distanceThreshold, Vector3 pointOfInterest, out float t) {
            //constrain to XZ plane
            curve = BezierXZ(curve);
            pointOfInterest.y = 0f;
            if (curve.DistanceSqr(pointOfInterest, out t) <= distanceThreshold * distanceThreshold) return true;
            return false;
        }

        /// <summary>
        /// Checks to see whether a given point is within a specified distance from a line segment. In the XZ-plane. Outputs the closest t-value (to the given point) on the curve.
        /// </summary>
        /// <param name="curve"></param>
        /// <param name="distanceThreshold"></param>
        /// <param name="pointOfInterest"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static bool IsCloseToSegmentXZ(Segment3 lineSegment, float distanceThreshold, Vector3 pointOfInterest, out float t) {
            //constrain to XZ plane
            lineSegment = SegmentXZ(lineSegment);
            pointOfInterest.y = 0f;
            if (lineSegment.DistanceSqr(pointOfInterest, out t) <= distanceThreshold * distanceThreshold) return true;
            return false;
        }

        /// <summary>
        /// Checks to see whether a given point is within a specified distance from a circle outline. In the XZ-plane. Outputs the closest t-value (to the given point) on the curve.
        /// </summary>
        /// <param name="curve"></param>
        /// <param name="distanceThreshold"></param>
        /// <param name="pointOfInterest"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static bool IsCloseToCircle3XZ(Circle3XZ circle, float distanceThreshold, Vector3 pointOfInterest, out float t) {
            Vector3 circleCenter = circle.m_center;
            float circleRadius = circle.m_radius;
            //constrain to XZ plane
            circleCenter.y = 0f;
            pointOfInterest.y = 0f;
            //initialize output t
            t = 0.5f;
            if (distanceThreshold == 0f) return pointOfInterest == circleCenter;
            distanceThreshold = Mathf.Abs(distanceThreshold);
            float distanceSqr = circle.DistanceSqr(pointOfInterest, out t);
            if (distanceSqr >= Pow(circleRadius - distanceThreshold, 2) && distanceSqr <= Pow(circleRadius + distanceThreshold, 2)) return true;
            return false;
        }

        private static float Pow(float num, int exp) {
            float result = 1.0f;
            while (exp > 0) {
                if (exp % 2 == 1) result *= num;
                exp >>= 1;
                num *= num;
            }
            return result;
        }
    }


    public struct Circle2 {
        public Vector2 m_center;
        public float m_radius;

        public Circle2 UnitCircle => new Circle2(Vector2.zero, 1f);

        /// <param name="t">Generally from 0 to 1. [0, 1]</param>
        public Vector2 Position(float t) {
            Vector2 result;
            Vector2 center = m_center;
            float radius = m_radius;
            result.x = center.x + radius * (float)Math.Cos(2 * Mathf.PI * t);
            result.y = center.y + radius * (float)Math.Sin(2 * Mathf.PI * t);
            return result;
        }

        /// <param name="theta">Angle in radians. [0, 2pi]</param>
        public Vector2 PositionFromAngle(float theta) {
            Vector2 result;
            Vector2 center = m_center;
            float radius = m_radius;
            result.x = center.x + radius * Mathf.Cos(theta);
            result.y = center.y + radius * Mathf.Sin(theta);
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
            Vector2 center = m_center;
            float radius = m_radius;
            result.x = center.x + radius * Mathf.Cos(theta);
            result.y = center.y + radius * Mathf.Sin(theta);
            return result;
        }

        public Circle2(Vector2 center, float radius) {
            m_center = center;
            m_radius = radius;
        }
    }

    /// <summary>
    /// A circle that lies parallel to the XZ plane, with orientation towards start angle, and at a height of center.y.
    /// </summary>
    public struct Circle3XZ {
        public Vector3 m_center;
        public float m_radius;

        /// <summary>
        /// The starting angle of the circle, in radians. Aka where the CCW rotation starts from. Conventionally zero radians.
        /// </summary>
        public float m_angleStart;
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
            position.x = center.x + radius * Mathf.Cos(angleStart + 2f * Mathf.PI * t);
            position.z = center.z + radius * Mathf.Sin(angleStart + 2f * Mathf.PI * t);
            position.y = center.y;
            return position;
        }

        /// <summary>
        /// Returns a tangent point on the circle outline.
        /// </summary>
        /// <param name="t">Generally from 0 to 1: [0, 1].</param>
        /// <returns></returns>
        public Vector3 Tangent(float t) {
            float radius = m_radius;
            float angleStart = m_angleStart;
            if (radius == 0f) return Vector3.zero;
            Vector3 tangent;
            tangent.x = (-2f * Mathf.PI) * radius * Mathf.Sin(angleStart + (2f * Mathf.PI) * t);
            tangent.z = (2f * Mathf.PI) * radius * Mathf.Cos(angleStart + (2f * Mathf.PI) * t);
            tangent.y = 0f;
            return tangent;
        }

        //use for non-fence mode
        public float DeltaT(float distance) {
            if (distance == 0f) return 0f;
            if (m_radius == 0f) return 1f;
            return distance / ((2f * Mathf.PI) * m_radius);
        }

        //use for non-fence mode
        /// <param name="distance"></param>
        /// <returns>Returns the angle, in radians, of the angle subtended by an arc of given length</returns>
        public float DeltaAngle(float distance) => DeltaT(distance) * (2f * Mathf.PI);

        //use for fence mode
        /// <param name="chordLength"></param>
        /// <returns>Returns the angle, in radians, of the angle inscribed by a chord of given length</returns>
        public float ChordAngle(float chordLength) {
            if (chordLength == 0f) return 0f;
            if (m_radius == 0f) return 2f * Mathf.PI;
            if (chordLength > Diameter) return 2f * Mathf.PI;
            return 2f * Mathf.Asin(chordLength / (2f * m_radius));
        }

        //use for fence mode?
        public float ChordDeltaT(float chordLength) {
            if (chordLength == 0f) return 0f;
            if (m_radius == 0f) return 1f;
            if (chordLength > Diameter) return 1f;

            //float _chordDeltaT = ChordAngle(chordLength) / this.circumference;
            float _chordDeltaT = ChordAngle(chordLength) / (2f * Mathf.PI);

            return _chordDeltaT;
        }

        //use for non-fence mode
        public float PerfectRadiusByArcs(float arcLength) {
            float Round(float val) {
                if (val >= 0) return val + 0.5f;
                return val - 0.5f;
            }
            if (arcLength == 0f) return m_radius;
            float radiusPerfect = arcLength * Round(Math.Abs(Circumference / arcLength)) / (2f * Mathf.PI);
            if (radiusPerfect > 0f) return radiusPerfect;
            return m_radius;
        }

        //use for fence mode
        public float PerfectRadiusByChords(float chordLength) {
            float Round(float val) {
                if (val >= 0) return val + 0.5f;
                return val - 0.5f;
            }
            if (chordLength == 0f) return m_radius;
            float numChordsPerfect = Round(Math.Abs(2f * Mathf.PI / ChordAngle(chordLength)));
            if (numChordsPerfect <= 0f) return m_radius;
            float radiusPerfect = chordLength / (2f * (float)Math.Sin(2f * Mathf.PI / numChordsPerfect / 2f));
            if (radiusPerfect > 0f) return radiusPerfect;
            return m_radius;
        }

        public float DistanceSqr(Vector3 position, out float u) {
            if (position == m_center) {
                u = 0f;
                return 0f;
            }
            Vector3 pointVector = position - m_center;
            pointVector.y = 0f;
            u = AngleFromStartXZ(position) / (2f * Mathf.PI);
            return pointVector.sqrMagnitude;
        }

        /// <param name="position"></param>
        /// <returns>Returns the angle, in radians, between a line pointing from the center to a given point, and the line pointing from the center to the start point (t = 0f)</returns>
        public float AngleFromStartXZ(Vector3 position) {
            Vector3 pointVector = position - m_center;
            pointVector.y = 0f;
            pointVector.Normalize();
            Vector3 zeroVector = Position(0f) - m_center;
            zeroVector.y = 0f;
            zeroVector.Normalize();
            float angleFromStart = PLTMath.AngleSigned(pointVector, zeroVector, Vector3.up);
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

        public Circle3XZ(Vector3 center, float radius) {
            m_center = center;
            m_radius = radius;
            m_angleStart = 0f;
        }

        /// <param name="angleStart">Angle in radians.</param>
        public Circle3XZ(Vector3 center, float radius, float angleStart) {
            m_center = center;
            m_radius = radius;
            m_angleStart = angleStart;
        }

        public Circle3XZ(Vector3 center, Vector3 pointOnCircle) {
            if (pointOnCircle == center) {
                m_center = center;
                m_radius = 0f;
                m_angleStart = 0f;
                return;
            }
            m_center = center;
            center.y = 0f;
            pointOnCircle.y = 0f;
            Vector3 radiusVector = pointOnCircle - center;
            m_radius = radiusVector.magnitude;
            m_angleStart = PLTMath.AngleSigned(radiusVector, Vector3.right, Vector3.up);
        }
    }
}
