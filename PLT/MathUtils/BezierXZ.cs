using EManagersLib;
using System;
using UnityEngine;

namespace PropAnarchy.PLT.MathUtils {
    public struct BezierXZ {
        public VectorXZ a;
        public VectorXZ b;
        public VectorXZ c;
        public VectorXZ d;

        public BezierXZ(VectorXZ a, VectorXZ b, VectorXZ c, VectorXZ d) {
            this.a = a;
            this.b = b;
            this.c = c;
            this.d = d;
        }

        public VectorXZ Position(float t) {
            float num = 1f - t;
            float num2 = t * t * t;
            float num3 = 3f * t * t * num;
            float num4 = 3f * t * num * num;
            float num5 = num * num * num;
            return new VectorXZ(a.x * num5 + b.x * num4 + c.x * num3 + d.x * num2, a.z * num5 + b.z * num4 + c.z * num3 + d.z * num2);
        }

        public static VectorXZ Position(VectorXZ a, VectorXZ b, VectorXZ c, VectorXZ d, float t) {
            float num = 1f - t;
            float num2 = t * t * t;
            float num3 = 3f * t * t * num;
            float num4 = 3f * t * num * num;
            float num5 = num * num * num;
            return new VectorXZ(a.x * num5 + b.x * num4 + c.x * num3 + d.x * num2, a.z * num5 + b.z * num4 + c.z * num3 + d.z * num2);
        }

        public VectorXZ Tangent(float t) {
            float num = t * t;
            float num2 = 3f * num;
            float num3 = 6f * t - 9f * num;
            float num4 = 3f - 12f * t + 9f * num;
            float num5 = 6f * t - 3f - 3f * num;
            return new VectorXZ(a.x * num5 + b.x * num4 + c.x * num3 + d.x * num2, a.z * num5 + b.z * num4 + c.z * num3 + d.z * num2);
        }

        public static VectorXZ Tangent(VectorXZ a, VectorXZ b, VectorXZ c, VectorXZ d, float t) {
            float num = t * t;
            float num2 = 3f * num;
            float num3 = 6f * t - 9f * num;
            float num4 = 3f - 12f * t + 9f * num;
            float num5 = 6f * t - 3f - 3f * num;
            return new VectorXZ(a.x * num5 + b.x * num4 + c.x * num3 + d.x * num2, a.z * num5 + b.z * num4 + c.z * num3 + d.z * num2);
        }

        public BezierXZ Invert() => new BezierXZ(d, c, b, a);

        public VectorXZ Min() => VectorXZ.Min(VectorXZ.Min(a, b), VectorXZ.Min(c, d));

        public VectorXZ Max() => VectorXZ.Max(VectorXZ.Max(a, b), VectorXZ.Max(c, d));

        public Bounds GetBounds() {
            Bounds result = default;
            result.SetMinMax(Min(), Max());
            return result;
        }

        public float Travel(float start, float distance) {
            float Clamp01(float value) {
                if (value < 0f) return 0f;
                else if (value > 1f) return 1f;
                return value;
            }
            float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);
            VectorXZ vector = Position(start);
            if (distance < 0f) {
                distance = -distance;
                float num = 0f;
                float num2 = start;
                float num3 = (a - vector).sqrMagnitude;
                float num4 = 0f;
                for (int i = 0; i < 8; i++) {
                    float num5 = (num + num2) * 0.5f;
                    float num6 = (Position(num5) - vector).sqrMagnitude;
                    if (num6 < distance * distance) {
                        num2 = num5;
                        num4 = num6;
                    } else {
                        num = num5;
                        num3 = num6;
                    }
                }
                num3 = (float)Math.Sqrt(num3);
                num4 = (float)Math.Sqrt(num4);
                float num7 = num3 - num4;
                if (num7 == 0f) return num2;
                return Lerp(num2, num, Clamp01((distance - num4) / num7));
            } else {
                float num = start;
                float num2 = 1f;
                float num3 = 0f;
                float num4 = (d - vector).sqrMagnitude;
                for (int j = 0; j < 8; j++) {
                    float num8 = (num + num2) * 0.5f;
                    float num9 = (Position(num8) - vector).sqrMagnitude;
                    if (num9 < distance * distance) {
                        num = num8;
                        num3 = num9;
                    } else {
                        num2 = num8;
                        num4 = num9;
                    }
                }
                num3 = (float)Math.Sqrt(num3);
                num4 = (float)Math.Sqrt(num4);
                float num10 = num4 - num3;
                if (num10 == 0f) return num;
                return Lerp(num, num2, Clamp01((distance - num3) / num10));
            }
        }

        public void Divide(out BezierXZ b1, out BezierXZ b2) {
            VectorXZ vector = (b + c) * 0.5f;
            b1.a = a;
            b2.d = d;
            b1.b = (a + b) * 0.5f;
            b2.c = (c + d) * 0.5f;
            b1.c = (b1.b + vector) * 0.5f;
            b2.b = (b2.c + vector) * 0.5f;
            b1.d = (b1.c + b2.b) * 0.5f;
            b2.a = b1.d;
        }

        public void Divide(out BezierXZ b1, out BezierXZ b2, float t) {
            VectorXZ vector = b + (c - b) * t;
            b1.a = a;
            b2.d = d;
            b1.b = a + (b - a) * t;
            b2.c = c + (d - c) * t;
            b1.c = b1.b + (vector - b1.b) * t;
            b2.b = vector + (b2.c - vector) * t;
            b1.d = b1.c + (b2.b - b1.c) * t;
            b2.a = b1.d;
        }

        public BezierXZ Cut(float t0, float t1) {
            float num = 1f - t0;
            float num2 = 1f - t1;
            BezierXZ result;
            result.a = num * num * num * a + (t0 * num * num + num * t0 * num + num * num * t0) * b + (t0 * t0 * num + num * t0 * t0 + t0 * num * t0) * c + t0 * t0 * t0 * d;
            result.b = num * num * num2 * a + (t0 * num * num2 + num * t0 * num2 + num * num * t1) * b + (t0 * t0 * num2 + num * t0 * t1 + t0 * num * t1) * c + t0 * t0 * t1 * d;
            result.c = num * num2 * num2 * a + (t0 * num2 * num2 + num * t1 * num2 + num * num2 * t1) * b + (t0 * t1 * num2 + num * t1 * t1 + t0 * num2 * t1) * c + t0 * t1 * t1 * d;
            result.d = num2 * num2 * num2 * a + (t1 * num2 * num2 + num2 * t1 * num2 + num2 * num2 * t1) * b + (t1 * t1 * num2 + num2 * t1 * t1 + t1 * num2 * t1) * c + t1 * t1 * t1 * d;
            return result;
        }

        public float DistanceSqr(SegmentXZ segment, out float u, out float v) {
            int i;
            float threshold = 1E+11f;
            u = 0f;
            v = 0f;
            float result = 0f;
            VectorXZ vector = a;
            for (i = 1; i <= 16; i++) {
                VectorXZ vectorSection = Position(i / 16f);
                float distanceSqr = new SegmentXZ(vector, vectorSection).DistanceSqr(segment, out float u1, out float _);
                if (distanceSqr < threshold) {
                    threshold = distanceSqr;
                    u = (i - 1f + u1) / 16f;
                }
                vector = vectorSection;
            }
            threshold = 0.03125f;
            for (i = 0; i < 4; i++) {
                VectorXZ vectorMax = Position(EMath.Max(0f, u - threshold));
                VectorXZ vectorMid = Position(u);
                VectorXZ vectorMin = Position(EMath.Min(1f, u + threshold));
                float maxDistance = new SegmentXZ(vectorMax, vectorMid).DistanceSqr(segment, out float uMax, out float vMax);
                float minDistance = new SegmentXZ(vectorMid, vectorMin).DistanceSqr(segment, out float uMin, out float vMin);
                if (maxDistance < minDistance) {
                    u = EMath.Max(0f, u - threshold * (1f - uMax));
                    v = vMax;
                    result = maxDistance;
                } else {
                    u = EMath.Min(1f, u + threshold * uMin);
                    v = vMin;
                    result = minDistance;
                }
                threshold *= 0.5f;
            }
            return result;
        }

        public float DistanceSqr(VectorXZ p, out float u) {
            int i;
            float threshold = 1E+11f;
            u = 0f;
            float result = 0f;
            VectorXZ vector = a;
            for (i = 1; i <= 16; i++) {
                Vector3 vectorSection = Position(i / 16f);
                float distanceSqr = new SegmentXZ(vector, vectorSection).DistanceSqr(p, out float u1);
                if (distanceSqr < threshold) {
                    threshold = distanceSqr;
                    u = (i - 1f + u1) / 16f;
                }
                vector = vectorSection;
            }
            threshold = 0.03125f;
            for (i = 0; i < 4; i++) {
                Vector3 vectorMax = Position(EMath.Max(0f, u - threshold));
                Vector3 vectorMid = Position(u);
                Vector3 vectorMin = Position(EMath.Min(1f, u + threshold));
                float highSegment = new SegmentXZ(vectorMax, vectorMid).DistanceSqr(p, out float uMax);
                float lowSegment = new SegmentXZ(vectorMid, vectorMin).DistanceSqr(p, out float uMin);
                if (highSegment < lowSegment) {
                    u = EMath.Max(0f, u - threshold * (1f - uMax));
                    result = highSegment;
                } else {
                    u = EMath.Min(1f, u + threshold * uMin);
                    result = lowSegment;
                }
                threshold *= 0.5f;
            }
            return result;
        }
    }
}
