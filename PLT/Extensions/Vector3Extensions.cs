using ColossalFramework;
using EManagersLib;
using PropAnarchy.PLT.MathUtils;
using UnityEngine;

namespace PropAnarchy.PLT.Extensions {
    public static class Vector3Extensions {
        public static float MagnitudeXZ(this Vector3 v) => EMath.Sqrt(v.x * v.x + v.z * v.z);

        public static float SqrMagnitudeXZ(this Vector3 v) => v.x * v.x + v.z * v.z;

        /// <summary>Determines the signed angle (-pi to pi) radians between two vectors</summary>
        /// <param name="v1">first vector</param>
        /// <param name="v2">second vector</param>
        /// <param name="n">rotation axis (usually plane normal of v1, v2)</param>
        /// <returns>signed angle (in Radians) between v1 and v2</returns>
        public static float AngleSigned(Vector3 v1, Vector3 v2, Vector3 n) => (float)EMath.Atan2(Vector3.Dot(n, Vector3.Cross(v1, v2)), Vector3.Dot(v1, v2));

        private const float CENTER_AREA_FRACTION = 0.00390625f;
        public static bool IsCenterAreaSignificant(this Vector3 center, Vector3 size, out VectorXZ centerCorrectionOrtho) {
            if (center.SqrMagnitudeXZ() >= CENTER_AREA_FRACTION * size.SqrMagnitudeXZ()) {
                centerCorrectionOrtho = -center;
                return true;
            }
            centerCorrectionOrtho = default;
            return false;
        }
    }
}
