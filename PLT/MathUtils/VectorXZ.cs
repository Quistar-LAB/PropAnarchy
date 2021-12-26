using EManagersLib;
using System;
using UnityEngine;

namespace PropAnarchy.PLT.MathUtils {
    public struct VectorXZ {
        public const float THRESHOLD = 1E-05f;
        public float x, z;
#pragma warning disable IDE1006
        public float magnitude => (float)Math.Sqrt(x * x + z * z);
        public float sqrMagnitude => x * x + z * z;
        public VectorXZ normalized {
            get {
                float num = magnitude;
                if (num > THRESHOLD) return this /= num;
                return zero;
            }
        }
#pragma warning restore IDE1006
        public VectorXZ(float x, float z) {
            this.x = x;
            this.z = z;
        }

        public static VectorXZ zero = new VectorXZ(0, 0);

        public static bool operator !=(VectorXZ lhs, VectorXZ rhs) => !(lhs == rhs);

        public static bool operator ==(VectorXZ lhs, VectorXZ rhs) => (lhs - rhs).sqrMagnitude < 9.99999944E-11f;

        public static VectorXZ operator -(VectorXZ a, VectorXZ b) => new VectorXZ(a.x - b.x, a.z - b.z);

        public static VectorXZ operator -(VectorXZ a, Vector3 b) => new VectorXZ(a.x - b.x, a.z - b.z);

        public static VectorXZ operator -(VectorXZ a) => new VectorXZ(-a.x, -a.z);

        public static VectorXZ operator +(VectorXZ a, VectorXZ b) => new VectorXZ(a.x + b.x, a.z + b.z);

        public static VectorXZ operator +(VectorXZ a, Vector3 b) => new VectorXZ(a.x + b.x, a.z + b.z);

        public static Vector3 operator +(Vector3 a, VectorXZ b) => new Vector3(a.x + b.x, 0f, a.z + b.z);

        public static VectorXZ operator /(VectorXZ a, float d) => new VectorXZ(a.x / d, a.z / d);

        public static VectorXZ operator *(VectorXZ a, float d) => new VectorXZ(a.x * d, a.z * d);

        public static VectorXZ operator *(float d, VectorXZ a) => new VectorXZ(a.x * d, a.z * d);


        public static implicit operator VectorXZ(Vector3 v) => new VectorXZ(v.x, v.z);

        public static implicit operator Vector3(VectorXZ v) => new Vector3(v.x, 0f, v.z);

        public override bool Equals(object other) => other is VectorXZ xZ && x == xZ.x && z == xZ.z;

        public override int GetHashCode() => x.GetHashCode() ^ z.GetHashCode() << 2;

        public static Vector3 Cross(VectorXZ lhs, VectorXZ rhs) => new Vector3(0f, lhs.z * rhs.x - lhs.x * rhs.z, 0f);

        public static float Dot(VectorXZ lhs, in VectorXZ rhs) => lhs.x * rhs.x + lhs.z * rhs.z;

        public static VectorXZ Lerp(VectorXZ a, VectorXZ b, float t) {
            t = EMath.Clamp01(t);
            return new VectorXZ(a.x + (b.x - a.x) * t, a.z + (b.z - a.z) * t);
        }

        public static VectorXZ Max(VectorXZ lhs, VectorXZ rhs) => new VectorXZ(EMath.Max(lhs.x, rhs.x), EMath.Max(lhs.z, rhs.z));

        public static VectorXZ Min(VectorXZ lhs, VectorXZ rhs) => new VectorXZ(EMath.Min(lhs.x, rhs.x), EMath.Min(lhs.z, rhs.z));

        public void Normalize() {
            float num = magnitude;
            if (num > THRESHOLD) {
                this /= num;
            } else {
                this = zero;
            }
        }

        public bool IsInsideCircleXZ(float radius, VectorXZ pointOfInterest) {
            if (radius == 0f) return this == pointOfInterest;
            else if (radius < 0f) radius = -radius;
            return (pointOfInterest - this).sqrMagnitude <= radius * radius;
        }

        public bool IsNearCircleOutlineXZ(float circleRadius, VectorXZ pointOfInterest, float distance) {
            bool isNearCircle(float distanceSqr, float min, float max) => distanceSqr >= min * min && distanceSqr <= max * max;
            if (distance == 0f) return this == pointOfInterest;
            else if (distance < 0f) distance = -distance;
            return isNearCircle((pointOfInterest - this).sqrMagnitude, circleRadius - distance, circleRadius + distance);
        }

        public Vector3 Cross(Vector3 rhs) => new Vector3(0f - z * rhs.y, z * rhs.x - x * rhs.z, x * rhs.y);

        public Vector3 Cross(VectorXZ rhs) => new Vector3(0f, z * rhs.x - x * rhs.z, 0f);

        public float Dot(Vector3 rhs) => x * rhs.x + z * rhs.z;

        public float Dot(VectorXZ rhs) => x * rhs.x + z * rhs.z;

        public float AngleSigned(VectorXZ v2, Vector3 n) => (float)Math.Atan2(Vector3.Dot(n, Cross(v2)), Dot(v2));
        public float AngleSigned(Vector3 v2, Vector3 n) => (float)Math.Atan2(Vector3.Dot(n, Cross(v2)), Dot(v2));

        public static bool IsInsideCircleXZ(VectorXZ circleCenter, float radius, VectorXZ pointOfInterest) {
            if (radius == 0f) return pointOfInterest == circleCenter;
            else if (radius < 0f) radius = EMath.Abs(radius);
            return (pointOfInterest - circleCenter).sqrMagnitude <= radius * radius;
        }

        public static bool IsNearCircleOutlineXZ(VectorXZ circleCenter, float circleRadius, VectorXZ pointOfInterest, float distance) {
            bool isNearCircle(float distanceSqr, float min, float max) => distanceSqr >= min * min && distanceSqr <= max * max;
            if (distance == 0f) return pointOfInterest == circleCenter;
            else if (distance < 0f) distance = EMath.Abs(distance);
            return isNearCircle((pointOfInterest - circleCenter).sqrMagnitude, circleRadius - distance, circleRadius + distance);
        }
    }
}
