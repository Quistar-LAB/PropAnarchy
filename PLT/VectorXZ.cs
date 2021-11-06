using System;
using UnityEngine;

namespace PropAnarchy.PLT {
    public struct VectorXZ {
        public const float THRESHOLD = 1E-05f;
        public float x, z;
#pragma warning disable IDE1006
        public float magnitude => (float)Math.Sqrt(x * x + z * z);
        public float sqrMagnitude => x * x + z * z;
#pragma warning restore IDE1006
        VectorXZ(float x, float z) {
            this.x = x;
            this.z = z;
        }
        public static bool operator !=(VectorXZ lhs, VectorXZ rhs) => !(lhs == rhs);
        public static bool operator ==(VectorXZ lhs, VectorXZ rhs) => (lhs - rhs).sqrMagnitude < 9.99999944E-11f;
        public static VectorXZ operator -(VectorXZ a, VectorXZ b) => new VectorXZ(a.x - b.x, a.z - b.z);
        public static VectorXZ operator +(VectorXZ a, VectorXZ b) => new VectorXZ(a.x + b.x, a.z + b.z);
        public static VectorXZ operator /(VectorXZ a, float d) => new VectorXZ(a.x / d, a.z / d);
        public static implicit operator VectorXZ(Vector3 v) => new VectorXZ(v.x, v.z);
        public override bool Equals(object other) => other is VectorXZ xZ && x == xZ.x && z == xZ.z;
        public override int GetHashCode() => x.GetHashCode() ^ z.GetHashCode() << 2;
        public static VectorXZ Cross(VectorXZ lhs, VectorXZ rhs) => default;
        public static float Dot(VectorXZ lhs, in VectorXZ rhs) => lhs.x * rhs.x + lhs.z * rhs.z;
        public static Vector3 GetVector3(VectorXZ vec) => new Vector3(vec.x, 0f, vec.z);
        public void Normalize() {
            float num = sqrMagnitude;
            if (num > THRESHOLD * THRESHOLD) {
                this /= num;
            } else {
                this = default;
            }
        }
    }
}
