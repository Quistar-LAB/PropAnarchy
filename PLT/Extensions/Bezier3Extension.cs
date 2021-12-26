using ColossalFramework.Math;
using EManagersLib;
using PropAnarchy.PLT.MathUtils;
using UnityEngine;

namespace PropAnarchy.PLT.Extensions {
    public static class Bezier3Extension {
        /// <summary>Normalizes an angle (in degrees) between 0 and 360 degrees</summary>
        /// <param name="inputAngle">angle in Degrees</param>
        /// <returns></returns>
        public static float NormalizeAngle360(float inputAngle) => (inputAngle < 0f) ? -1f * EMath.Abs(inputAngle) % 360f : EMath.Abs(inputAngle) % 360f;

        /// <summary>Constrains input Bezier to XZ plane</summary>
        /// <param name="bezier"></param>
        public static void BezierXZ(ref this Bezier3 bezier) {
            bezier.a.y = 0f;
            bezier.b.y = 0f;
            bezier.c.y = 0f;
            bezier.d.y = 0f;
        }

        //standard conversion
        public static Bezier3 QuadraticToCubicBezier(Vector3 startPoint, Vector3 middlePoint, Vector3 endPoint) =>
            new Bezier3(startPoint, startPoint + (2.0f / 3.0f) * (middlePoint - startPoint), endPoint + (2.0f / 3.0f) * (middlePoint - endPoint), endPoint);

        //CO's in-house method
        //uses negative of endDirection
        //rounds out tight re-curves (or tight curves)
        public static void QuadraticToCubicBezierCOMethod(ref this Bezier3 bezier, Vector3 startPoint, Vector3 startDirection, Vector3 endPoint, Vector3 endDirection) {
            bezier.a = startPoint;
            bezier.d = endPoint;
            NetSegment.CalculateMiddlePoints(startPoint, startDirection, endPoint, -endDirection, false, false, out bezier.b, out bezier.c);
        }

        //used to calculate t in non-fence Curved and Freeform modes
        //for each individual item
        /// <summary>Solves for t-value which would be a length of *distance* along the curve from original point at t = *tStart*</summary>
        /// <param name="bezier"></param>
        /// <param name="tStart"></param>
        /// <param name="distance"></param>
        /// <param name="tolerance"></param>
        /// <param name="tEnd"></param>
        public static void StepDistanceCurve(ref this Bezier3 bezier, float tStart, float distance, float toleranceSqr, out float tEnd) {
            float Pow2(float x) => x * x;
            tEnd = bezier.Travel(tStart, distance);
            float distCurrent = bezier.CubicBezierArcLengthXZGauss04(tStart, tEnd);
            for (int i = 0; i < 12 && Pow2(distance - distCurrent) > toleranceSqr; i++) {
                distCurrent = bezier.CubicBezierArcLengthXZGauss04(tStart, tEnd);
                tEnd += (distance - distCurrent) / bezier.CubicSpeedXZ(tEnd);
            }
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
        public static bool CircleCurveFenceIntersectXZ(ref this Bezier3 bezier, float tStart, float lengthOfSegment, float tolerance, out float tEnd, bool allowBackwards) {
            float Pow2(float x) => x * x;
            const float adjustmentScalar = 1.0f;  //if using multiplicity, _adjustmentScalar = 2
            bezier.BezierXZ();
            float toleranceSqr = tolerance * tolerance;
            if (!allowBackwards) lengthOfSegment = EMath.Abs(lengthOfSegment);
            if (lengthOfSegment == 0f) {
                tEnd = tStart;
                return false;
            }
            bezier.StepDistanceCurve(tStart, lengthOfSegment, toleranceSqr, out float t0);
            int counter = 0;
            float iteratedDistance;
            do {
                t0 -= adjustmentScalar * (bezier.PLTErrorFunctionXZ(t0, tStart, lengthOfSegment) / bezier.PLTErrorFunctionPrimeXZ(t0, tStart));
                if (!allowBackwards && t0 < tStart) {
                    t0 = 1f;
                }
                iteratedDistance = (bezier.Position(t0) - bezier.Position(tStart)).magnitude;
                counter++;
            } while (counter < 25 && Pow2(iteratedDistance - lengthOfSegment) > toleranceSqr);
            tEnd = t0;
            return !(Pow2(iteratedDistance - lengthOfSegment) > toleranceSqr);
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
        public static bool LinkCircleCurveFenceIntersectXZ(ref this Bezier3 bezier, Vector3 startPos, float lengthOfSegment, float tolerance, out float tEnd, bool allowBackwards) {
            float Pow2(float x) => x * x;
            const float adjustmentScalar = 1.0f;  //if using multiplicity, _adjustmentScalar = 2
            float iteratedDistance;
            int counter = 0;
            bezier.BezierXZ();
            float toleranceSqr = tolerance * tolerance;
            if (lengthOfSegment == 0f) {
                tEnd = 0f;
                return false;
            } else if (lengthOfSegment < 0f) lengthOfSegment = EMath.Abs(lengthOfSegment);
            bezier.StepDistanceCurve(0f, lengthOfSegment - Vector3.Distance(startPos, bezier.a), toleranceSqr, out float t0);
            do {
                t0 -= adjustmentScalar * (bezier.PLTLinkErrorFunctionXZ(t0, startPos, lengthOfSegment) / bezier.PLTLinkErrorFunctionPrimeXZ(t0, startPos));
                if (!allowBackwards && t0 < 0f) t0 = 1f;
                iteratedDistance = (bezier.Position(t0) - startPos).magnitude;
                counter++;
            } while (counter < 12 && Pow2(iteratedDistance - lengthOfSegment) > toleranceSqr);
            tEnd = t0;
            return !(Pow2(iteratedDistance - lengthOfSegment) > toleranceSqr);
        }

        //Uses Legendre-Gauss Quadrature with n = 12.
        /// <param name="bezier"></param>
        /// <param name="t1"></param>
        /// <param name="t2"></param>
        /// <returns>Returns the XZ arclength of a cubic bezier curve between t1 and t2</returns>
        public static float CubicBezierArcLengthXZGauss12(ref this Bezier3 bezier, float t1, float t2) => ((t2 - t1) / 2f) *
            (bezier.CubicSpeedXZGaussPoint(0.1252334085114689f, 0.2491470458134028f, t1, t2) +
             bezier.CubicSpeedXZGaussPoint(-0.1252334085114689f, 0.2491470458134028f, t1, t2) +
             bezier.CubicSpeedXZGaussPoint(0.3678314989981802f, 0.2334925365383548f, t1, t2) +
             bezier.CubicSpeedXZGaussPoint(-0.3678314989981802f, 0.2334925365383548f, t1, t2) +
             bezier.CubicSpeedXZGaussPoint(0.5873179542866175f, 0.2031674267230659f, t1, t2) +
             bezier.CubicSpeedXZGaussPoint(-0.5873179542866175f, 0.2031674267230659f, t1, t2) +
             bezier.CubicSpeedXZGaussPoint(0.7699026741943047f, 0.1600783285433462f, t1, t2) +
             bezier.CubicSpeedXZGaussPoint(-0.7699026741943047f, 0.1600783285433462f, t1, t2) +
             bezier.CubicSpeedXZGaussPoint(0.9041172563704749f, 0.1069393259953184f, t1, t2) +
             bezier.CubicSpeedXZGaussPoint(-0.9041172563704749f, 0.1069393259953184f, t1, t2) +
             bezier.CubicSpeedXZGaussPoint(0.9815606342467192f, 0.0471753363865118f, t1, t2) +
             bezier.CubicSpeedXZGaussPoint(-0.9815606342467192f, 0.0471753363865118f, t1, t2));

        //Uses Legendre-Gauss Quadrature with n = 4.
        /// <param name="bezier"></param>
        /// <param name="t1"></param>
        /// <param name="t2"></param>
        /// <returns>Returns the XZ arclength of a cubic bezier curve between t1 and t2</returns>
        public static float CubicBezierArcLengthXZGauss04(ref this Bezier3 bezier, float t1, float t2) => ((t2 - t1) / 2f) *
            (bezier.CubicSpeedXZGaussPoint(0.3399810435848563f, 0.6521451548625461f, t1, t2) +
             bezier.CubicSpeedXZGaussPoint(-0.3399810435848563f, 0.6521451548625461f, t1, t2) +
             bezier.CubicSpeedXZGaussPoint(0.8611363115940526f, 0.3478548451374538f, t1, t2) +
             bezier.CubicSpeedXZGaussPoint(-0.8611363115940526f, 0.3478548451374538f, t1, t2));

        //Uses Legendre-Gauss Quadrature with n = 3.
        /// <param name="bezier"></param>
        /// <param name="t1"></param>
        /// <param name="t2"></param>
        /// <returns>Returns the XZ arclength of a cubic bezier curve between t1 and t2</returns>
        public static float CubicBezierArcLengthXZGauss03(ref this Bezier3 bezier, float t1, float t2) => ((t2 - t1) / 2f) *
            (bezier.CubicSpeedXZGaussPoint(0.0f, 0.88888888f, t1, t2) +
             bezier.CubicSpeedXZGaussPoint(0.77459667f, 0.55555555f, t1, t2) +
             bezier.CubicSpeedXZGaussPoint(-0.77459667f, 0.55555555f, t1, t2));

        //returns a single point for Gaussian Quadrature
        //of cubic bezier arc length
        private static float CubicSpeedXZGaussPoint(ref this Bezier3 bezier, float x_i, float w_i, float a, float b) => w_i * bezier.CubicSpeedXZ((b - a) / 2f * x_i + ((a + b) / 2f));

        //returns the integrand of the arc length function for a cubic bezier curve
        //constrained to the XZ-plane
        //at a specific t
        private static float CubicSpeedXZ(ref this Bezier3 bezier, float t) {
            float sqrSpeed(in Vector3 tangent) => tangent.x * tangent.x + tangent.z * tangent.z;
            return (float)EMath.Sqrt(sqrSpeed(bezier.Tangent(t)));
        }


        /// <param name="bezier"></param>
        /// <param name="t">Where are you guessing the intersection is?</param>
        /// <param name="tCenter">Center of the circle to intersect the curve.</param>
        /// <param name="radius">Radius of the circle to intersect the curve.</param>
        /// <returns>Returns E(t) = E(tGuess) : +/- Error [meters^2] in (straight-line distance between two points on a bezier curve) vs (radius).</returns>
        private static float PLTErrorFunctionXZ(ref this Bezier3 bezier, float t, float tCenter, float radius) {
            float Pow2(float x) => x * x;
            float ErrorFuncXZ(in Vector3 guessPos, in Vector3 center) => Pow2(guessPos.x - center.x) + Pow2(guessPos.z - center.z) - Pow2(radius);
            if (t == tCenter) return 0f;
            return ErrorFuncXZ(bezier.Position(t), bezier.Position(tCenter));
        }

        /// <summary>Specialty Version of PLTErrorFunctionXZ used to link curves</summary>
        /// <param name="bezier"></param>
        /// <param name="t">Where are you guessing the intersection is?</param>
        /// <param name="centerPos">Center of the circle to intersect the curve.</param>
        /// <param name="radius">Radius of the circle to intersect the curve.</param>
        /// <returns>Returns E(t) = E(tGuess) : +/- Error [meters^2] in (straight-line distance between startPoint and point on bezier curve) vs (radius)</returns>
        private static float PLTLinkErrorFunctionXZ(ref this Bezier3 bezier, float t, Vector3 centerPos, float radius) {
            float Pow2(float x) => x * x;
            float ErrorFuncXZ(in Vector3 guessPos, in Vector3 center) => guessPos == center ? 0f : Pow2(guessPos.x - center.x) + Pow2(guessPos.z - center.z) - Pow2(radius);
            return ErrorFuncXZ(bezier.Position(t), centerPos);
        }

        /// <param name="bezier"></param>
        /// <param name="t">Where are you guessing the intersection is?</param>
        /// <param name="tCenter">Center of the circle to intersect the curve.</param>
        /// <returns>Returns E'(t) = E'(tGuess) : Derivative of +/- Error [meters^2] in (straight-line distance between two points on a bezier curve) vs (radius)</returns>
        private static float PLTErrorFunctionPrimeXZ(ref this Bezier3 bezier, float t, float tCenter) {
            float ErrorFuncPrimeXZ(Vector3 guessPos, Vector3 center, Vector3 derivPos) => 2 * (guessPos.x - center.x) * derivPos.x + 2 * (guessPos.z - center.z) * derivPos.z;
            return t == tCenter ? 0f : ErrorFuncPrimeXZ(bezier.Position(t), bezier.Position(tCenter), bezier.Tangent(t));
        }

        /// <summary>Specialty Version of PLTErrorFunctionPrimeXZ used to link curves</summary>
        /// <param name="bezier"></param>
        /// <param name="t">Where are you guessing the intersection is?</param>
        /// <param name="centerPos">Center of the circle to intersect the curve.</param>
        /// <returns>Returns E'(t) = E'(tGuess) : Derivative of +/- Error [meters^2] in (straight-line distance between startPoint and point on bezier curve) vs (radius)</returns>
        private static float PLTLinkErrorFunctionPrimeXZ(ref this Bezier3 bezier, float t, in Vector3 centerPos) {
            float ErrorFuncPrimeXZ(Vector3 guessPos, Vector3 center, Vector3 derivPos) => guessPos == center ? 0f : 2 * (guessPos.x - center.x) * derivPos.x + 2 * (guessPos.z - center.z) * derivPos.z;
            return ErrorFuncPrimeXZ(bezier.Position(t), centerPos, bezier.Tangent(t));
        }

        /// <summary>
        /// Checks to see whether a given point is within a specified distance from a bezier curve. In the XZ-plane. Outputs the closes t-value (to the given point) on the curve.
        /// </summary>
        /// <param name="curve"></param>
        /// <param name="distanceThreshold"></param>
        /// <param name="pointOfInterest"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static bool IsCloseToCurveXZ(ref this Bezier3 curve, float distanceThreshold, VectorXZ pointOfInterest, out float t) {
            float distanceSqr = 0f, num = 1E+11f;
            Bezier3 bezier = curve;
            bezier.BezierXZ();
            t = 0f;
            Vector3 vector = bezier.a;
            vector.y = 0f;
            for (int i = 1; i <= 16; i++) {
                Vector3 vector2 = bezier.Position(i / 16f);
                float segDistanceSqr = new Segment3(vector, vector2).DistanceSqr(pointOfInterest, out float u);
                if (segDistanceSqr < num) {
                    num = segDistanceSqr;
                    t = (i - 1f + u) / 16f;
                }
                vector = vector2;
            }
            float num4 = 0.03125f;
            for (int i = 0; i < 4; i++) {
                Vector3 vector3 = bezier.Position(EMath.Max(0f, t - num4));
                Vector3 vector4 = bezier.Position(t);
                Vector3 vector5 = bezier.Position(EMath.Min(1f, t + num4));
                float num5 = new Segment3(vector3, vector4).DistanceSqr(pointOfInterest, out float num6);
                float num7 = new Segment3(vector4, vector5).DistanceSqr(pointOfInterest, out float num8);
                if (num5 < num7) {
                    t = EMath.Max(0f, t - num4 * (1f - num6));
                    distanceSqr = num5;
                } else {
                    t = EMath.Min(1f, t + num4 * num8);
                    distanceSqr = num7;
                }
                num4 *= 0.5f;
            }
            return distanceSqr <= distanceThreshold * distanceThreshold;
        }
    }
}
