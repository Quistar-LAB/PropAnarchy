using ColossalFramework.Math;
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


    }
}
