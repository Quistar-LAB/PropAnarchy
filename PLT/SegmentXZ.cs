using UnityEngine;
using ColossalFramework.Math;
using System;

namespace PropAnarchy.PLT {
    public struct SegmentXZ {
        public VectorXZ a;
        public VectorXZ b;

        public static implicit operator Segment3(SegmentXZ segment) => new Segment3(segment.a, segment.b);

        public SegmentXZ(VectorXZ a, VectorXZ b) {
            this.a = a;
            this.b = b;
        }

        public VectorXZ Min() => VectorXZ.Min(a, b);

        public VectorXZ Max() => VectorXZ.Max(a, b);

        public float Length() => (a - b).magnitude;

        public float LengthXZ() => (a - b).magnitude;

        public float LengthSqr() => (a.x - b.x) * (a.x - b.x) + (a.z - b.z) * (a.z - b.z);

        public Bounds GetBounds() {
            Bounds result = default;
            result.SetMinMax(Min(), Max());
            return result;
        }

        public float DistanceSqr(VectorXZ p) {
            VectorXZ lhs = b - a;
            VectorXZ rhs = a - p;
            if (VectorXZ.Dot(lhs, rhs) >= 0f) {
                return rhs.sqrMagnitude;
            }
            VectorXZ rhs2 = rhs + lhs;
            if (VectorXZ.Dot(lhs, rhs2) <= 0f) {
                return rhs2.sqrMagnitude;
            }
            float sqrMagnitude = lhs.sqrMagnitude;
            if (sqrMagnitude == 0f) {
                return rhs.sqrMagnitude;
            }
            return VectorXZ.Cross(lhs, rhs).sqrMagnitude / sqrMagnitude;
        }

        public static float DistanceSqr(VectorXZ a, VectorXZ b, VectorXZ p) {
            VectorXZ lhs = b - a;
            VectorXZ rhs = a - p;
            if (VectorXZ.Dot(lhs, rhs) >= 0f) {
                return rhs.sqrMagnitude;
            }
            VectorXZ rhs2 = rhs + lhs;
            if (VectorXZ.Dot(lhs, rhs2) <= 0f) {
                return rhs2.sqrMagnitude;
            }
            float sqrMagnitude = lhs.sqrMagnitude;
            if (sqrMagnitude == 0f) {
                return rhs.sqrMagnitude;
            }
            return VectorXZ.Cross(lhs, rhs).sqrMagnitude / sqrMagnitude;
        }

        public float DistanceSqr(VectorXZ p, out float u) {
            VectorXZ lhs = b - a;
            VectorXZ rhs = a - p;
            if (VectorXZ.Dot(lhs, rhs) >= 0f) {
                u = 0f;
                return rhs.sqrMagnitude;
            }
            VectorXZ rhs2 = rhs + lhs;
            if (VectorXZ.Dot(lhs, rhs2) <= 0f) {
                u = 1f;
                return rhs2.sqrMagnitude;
            }
            float sqrMagnitude = lhs.sqrMagnitude;
            if (sqrMagnitude == 0f) {
                u = 0f;
                return rhs.sqrMagnitude;
            }
            u = -VectorXZ.Dot(lhs, rhs) / sqrMagnitude;
            return VectorXZ.Cross(lhs, rhs).sqrMagnitude / sqrMagnitude;
        }

        private static float DistanceSqr(VectorXZ ab, VectorXZ pa, out float u) {
            if (VectorXZ.Dot(ab, pa) >= 0f) {
                u = 0f;
                return pa.sqrMagnitude;
            }
            VectorXZ rhs = pa + ab;
            if (VectorXZ.Dot(ab, rhs) <= 0f) {
                u = 1f;
                return rhs.sqrMagnitude;
            }
            float sqrMagnitude = ab.sqrMagnitude;
            if (sqrMagnitude == 0f) {
                u = 0f;
                return pa.sqrMagnitude;
            }
            u = -VectorXZ.Dot(ab, pa) / sqrMagnitude;
            return VectorXZ.Cross(ab, pa).sqrMagnitude / sqrMagnitude;
        }

        public static float DistanceSqr(VectorXZ a, VectorXZ b, VectorXZ p, out float u) {
            VectorXZ lhs = b - a;
            VectorXZ rhs = a - p;
            if (VectorXZ.Dot(lhs, rhs) >= 0f) {
                u = 0f;
                return rhs.sqrMagnitude;
            }
            VectorXZ rhs2 = rhs + lhs;
            if (VectorXZ.Dot(lhs, rhs2) <= 0f) {
                u = 1f;
                return rhs2.sqrMagnitude;
            }
            float sqrMagnitude = lhs.sqrMagnitude;
            if (sqrMagnitude == 0f) {
                u = 0f;
                return rhs.sqrMagnitude;
            }
            u = -VectorXZ.Dot(lhs, rhs) / sqrMagnitude;
            return VectorXZ.Cross(lhs, rhs).sqrMagnitude / sqrMagnitude;
        }

        public float DistanceSqr(in SegmentXZ segment, out float u, out float v) {
            VectorXZ vector = b - a;
            VectorXZ vector2 = segment.b - segment.a;
            VectorXZ vector3 = a - segment.a;
            VectorXZ pa = segment.a - a;
            u = 0f;
            float num = DistanceSqr(vector2, pa, out v);
            float num2 = DistanceSqr(vector2, pa - vector, out float num3);
            if (num2 < num) {
                num = num2;
                u = 1f;
                v = num3;
            }
            num2 = DistanceSqr(vector, vector3, out num3);
            if (num2 < num) {
                num = num2;
                u = num3;
                v = 0f;
            }
            num2 = DistanceSqr(vector, vector3 - vector2, out num3);
            if (num2 < num) {
                num = num2;
                u = num3;
                v = 1f;
            }
            return num;
        }

        public bool Clip(Bounds bounds) {
            VectorXZ vector = b - a;
            float num = bounds.min.x - a.x;
            if (num >= 1E-06f) {
                if (vector.x < num) {
                    return false;
                }
                a += vector * (num / vector.x);
                vector = b - a;
            }
            num = bounds.min.z - a.z;
            if (num >= 1E-06f) {
                if (vector.z < num) {
                    return false;
                }
                a += vector * (num / vector.z);
                vector = b - a;
            }
            num = bounds.max.x - a.x;
            if (num <= -1E-06f) {
                if (vector.x > num) {
                    return false;
                }
                a += vector * (num / vector.x);
                vector = b - a;
            }
            num = bounds.max.z - a.z;
            if (num <= -1E-06f) {
                if (vector.z > num) {
                    return false;
                }
                a += vector * (num / vector.z);
            }
            vector = a - b;
            num = bounds.min.x - b.x;
            if (num >= 1E-06f) {
                if (vector.x < num) {
                    return false;
                }
                b += vector * (num / vector.x);
                vector = a - b;
            }
            num = bounds.min.z - b.z;
            if (num >= 1E-06f) {
                if (vector.z < num) {
                    return false;
                }
                b += vector * (num / vector.z);
                vector = a - b;
            }
            num = bounds.max.x - this.b.x;
            if (num <= -1E-06f) {
                if (vector.x > num) {
                    return false;
                }
                b += vector * (num / vector.x);
                vector = a - b;
            }
            num = bounds.max.z - b.z;
            if (num <= -1E-06f) {
                if (vector.z > num) {
                    return false;
                }
            }
            return true;
        }

        public SegmentXZ Cut(float t0, float t1) => new SegmentXZ(VectorXZ.Lerp(a, b, t0), VectorXZ.Lerp(a, b, t1));

        public VectorXZ LinePosition(float t) {
            float num = 1.0f - t;
            VectorXZ finalVector;
            finalVector.x = b.x + num * (a.x - b.x);
            finalVector.z = b.z + num * (a.z - b.z);
            return finalVector;
        }

        public float LinearSpeedXZ() {
            float Pow2(float x) => x * x;
            VectorXZ tanVector = b - a;
            return (float)Math.Sqrt(Pow2(tanVector.x) + Pow2(tanVector.z));
        }

        public VectorXZ Direction() => b - a;

        public VectorXZ Position(float t) => VectorXZ.Lerp(a, b, t);

        public bool IsCloseToSegmentXZ(float distanceThreshold, VectorXZ pointOfInterest, out float t) => DistanceSqr(pointOfInterest, out t) <= distanceThreshold * distanceThreshold;
    }
}
