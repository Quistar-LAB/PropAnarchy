using System;
using UnityEngine;

namespace PropAnarchy.PLT {
    public static class PLTMath {
        /// <summary>Determines the signed angle (-pi to pi) radians between two vectors</summary>
        /// <param name="v1">first vector</param>
        /// <param name="v2">second vector</param>
        /// <param name="n">rotation axis (usually plane normal of v1, v2)</param>
        /// <returns>signed angle (in Radians) between v1 and v2</returns>
        public static float AngleSigned(Vector3 v1, Vector3 v2, Vector3 n) => (float)Math.Atan2(Vector3.Dot(n, Vector3.Cross(v1, v2)), Vector3.Dot(v1, v2));
        //public static float AngleSigned(VectorXZ v1, VectorXZ v2, Vector3 n) => (float)Math.Atan2(VectorXZ.Dot(n, VectorXZ.Cross(v1, v2)), VectorXZ.Dot(v1, v2));
    }
}
