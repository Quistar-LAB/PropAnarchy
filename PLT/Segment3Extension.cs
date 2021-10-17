using ColossalFramework.Math;
using UnityEngine;

namespace PropAnarchy.PLT {
    public static class Segment3Extension {
        public static float LengthXZ(ref this Segment3 line) => Vector3.Distance(new Vector3(line.a.x, 0f, line.a.z), new Vector3(line.b.x, 0f, line.b.z));
    }
}
