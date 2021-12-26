using ColossalFramework;
using EManagersLib;
using PropAnarchy.PLT.MathUtils;
using UnityEngine;

namespace PropAnarchy.PLT.Extensions {
    public static class Vector3Extensions {
        public struct ShortVector3 {
            public short x;
            public ushort y;
            public short z;
            public ShortVector3(short x, ushort y, short z) {
                this.x = x;
                this.y = y;
                this.z = z;
            }
            public ShortVector3(Vector3 pos) {
                if (Singleton<ToolManager>.instance.m_properties.m_mode == ItemClass.Availability.AssetEditor) {
                    x = (short)EMath.Clamp(EMath.RoundToInt(pos.x * 60.68148f), -32767, 32767);
                    z = (short)EMath.Clamp(EMath.RoundToInt(pos.z * 60.68148f), -32767, 32767);
                    y = (ushort)EMath.Clamp(EMath.RoundToInt(pos.y * 64f), 0, 65535);
                } else {
                    x = (short)EMath.Clamp(EMath.RoundToInt(pos.x * 3.79259253f), -32767, 32767);
                    z = (short)EMath.Clamp(EMath.RoundToInt(pos.z * 3.79259253f), -32767, 32767);
                    y = (ushort)EMath.Clamp(EMath.RoundToInt(pos.y * 64f), 0, 65535);
                }
            }

            public Vector3 ToVector3() => Singleton<ToolManager>.instance.m_properties.m_mode == ItemClass.Availability.AssetEditor ?
                new Vector3(x * 0.0164794922f, y * 0.015625f, z * 0.0164794922f) : new Vector3(x * 0.263671875f, y * 0.015625f, z * 0.263671875f);
        }

        public static bool Approximately(this Vector3 v, Vector3 c) => EMath.Approximately(v.x, c.x) && EMath.Approximately(v.y, c.y) && EMath.Approximately(v.z, c.z);

        public static bool ApproximatelyXZ(this Vector3 v, Vector3 c) => EMath.Approximately(v.x, c.x) && EMath.Approximately(v.x, c.x);

        public static bool EqualOnGameShortGridXZ(this Vector3 v, Vector3 c) {
            ShortVector3 sV = new ShortVector3(v);
            ShortVector3 sCV = new ShortVector3(c);
            if (sV.x == sCV.x && sV.z == sCV.z) {
                return true;
            }
            return false;
        }

        public static bool NearlyEqualOnGameShortGridXZ(this Vector3 v, Vector3 c) {
            const int tolerance = 1;
            ShortVector3 sV = new ShortVector3(v);
            ShortVector3 sCV = new ShortVector3(c);
            if ((sV.x >= sCV.x - tolerance) && (sV.x <= sCV.x + tolerance) &&
               (sV.z >= sCV.z - tolerance) && (sV.z <= sCV.z + tolerance)) {
                return true;
            }
            return false;
        }

        public static ShortVector3 ToShortVector3(this Vector3 v) => new ShortVector3(v);

        public static Vector3 QuantizeToGameShortGridXYZ(this Vector3 v) => v.ToShortVector3().ToVector3();

        public static float MagnitudeXZ(this Vector3 v) => EMath.Sqrt(v.x * v.x + v.z * v.z);

        public static float SqrMagnitudeXZ(this Vector3 v) => v.x * v.x + v.z * v.z;

        public static Vector3 NormalizeXZ(this Vector3 v) {
            float num = EMath.Sqrt(v.x * v.x + v.z + v.z);
            if (num > 1E-05f) {
                return new Vector3(v.x / num, 0f, v.z / num);
            }
            return EMath.Vector3Zero;
        }

        /// <summary>Determines the signed angle (-pi to pi) radians between two vectors</summary>
        /// <param name="v1">first vector</param>
        /// <param name="v2">second vector</param>
        /// <param name="n">rotation axis (usually plane normal of v1, v2)</param>
        /// <returns>signed angle (in Radians) between v1 and v2</returns>
        public static float AngleSigned(Vector3 v1, Vector3 v2, Vector3 n) => (float)EMath.Atan2(Vector3.Dot(n, Vector3.Cross(v1, v2)), Vector3.Dot(v1, v2));

        public static float AngleDynamicXZ(this VectorXZ directionVector) {
            Vector3 xAxis = new Vector3(1f, 0f, 0f);
            Vector3 yAxis = new Vector3(0f, 1f, 0f);
            if (directionVector != VectorXZ.zero) {
                directionVector.Normalize();
                return AngleSigned(directionVector, xAxis, yAxis) + Mathf.PI;
            }
            return 0f;
        }

        // =============  HOVERING STUFF  =============
        /// <summary>Checks to see whether a given point lies within a circle of given center and radius. In the XZ-plane</summary>
        /// <param name="circleCenter"></param>
        /// <param name="radius"></param>
        /// <param name="pointOfInterest"></param>
        /// <returns></returns>
        public static bool IsInsideCircleXZ(this VectorXZ circleCenter, float radius, VectorXZ pointOfInterest) {
            if (radius == 0f) return pointOfInterest == circleCenter;
            else if (radius < 0f) radius = EMath.Abs(radius);
            return (pointOfInterest - circleCenter).sqrMagnitude <= radius * radius;
        }

        /// <summary>Checks to see whether a given point is close to a circle outline of given center and radius. In the XZ-plane.</summary>
        /// <param name="circleCenter"></param>
        /// <param name="radius"></param>
        /// <param name="pointOfInterest"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        public static bool IsNearCircleOutlineXZ(this VectorXZ circleCenter, float circleRadius, VectorXZ pointOfInterest, float distance) {
            bool isNearCircle(float distanceSqr, float min, float max) => distanceSqr >= min * min && distanceSqr <= max * max;
            if (distance == 0f) return pointOfInterest == circleCenter;
            else if (distance < 0f) distance = EMath.Abs(distance);
            return isNearCircle((pointOfInterest - circleCenter).sqrMagnitude, circleRadius - distance, circleRadius + distance);
        }

        private const float CENTER_AREA_FRACTION = 0.00390625f;
        public static bool IsCenterAreaSignificant(this Vector3 center, Vector3 size, bool constrainToXZ, out Vector3 centerCorrectionOrtho) {
            if (constrainToXZ && center.SqrMagnitudeXZ() >= CENTER_AREA_FRACTION * size.SqrMagnitudeXZ()) {
                //negate center vector
                centerCorrectionOrtho = -center;
                centerCorrectionOrtho.y = 0f;
                return true;
            } else if (center.sqrMagnitude >= CENTER_AREA_FRACTION * size.sqrMagnitude) {
                //negate center vector
                centerCorrectionOrtho = -center;
                return true;
            }
            centerCorrectionOrtho = EMath.Vector3Zero;
            return false;
        }
    }
}
