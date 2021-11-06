using ColossalFramework;
using System;
using UnityEngine;

namespace PropAnarchy.PLT {
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
                    x = (short)Mathf.Clamp(Mathf.RoundToInt(pos.x * 60.68148f), -32767, 32767);
                    z = (short)Mathf.Clamp(Mathf.RoundToInt(pos.z * 60.68148f), -32767, 32767);
                    y = (ushort)Mathf.Clamp(Mathf.RoundToInt(pos.y * 64f), 0, 65535);
                } else {
                    x = (short)Mathf.Clamp(Mathf.RoundToInt(pos.x * 3.79259253f), -32767, 32767);
                    z = (short)Mathf.Clamp(Mathf.RoundToInt(pos.z * 3.79259253f), -32767, 32767);
                    y = (ushort)Mathf.Clamp(Mathf.RoundToInt(pos.y * 64f), 0, 65535);
                }
            }

            public Vector3 ToVector3() => Singleton<ToolManager>.instance.m_properties.m_mode == ItemClass.Availability.AssetEditor ?
                new Vector3(x * 0.0164794922f, y * 0.015625f, z * 0.0164794922f) : new Vector3(x * 0.263671875f, y * 0.015625f, z * 0.263671875f);
        }

        public static bool Approximately(this Vector3 v, Vector3 c) => Mathf.Approximately(v.x, c.x) && Mathf.Approximately(v.y, c.y) && Mathf.Approximately(v.z, c.z);

        public static bool ApproximatelyXZ(this Vector3 v, Vector3 c) => Mathf.Approximately(v.x, c.x) && Mathf.Approximately(v.x, c.x);

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

        public static float MagnitudeXZ(this Vector3 v) => (float)Math.Sqrt(v.x * v.x + v.z * v.z);

        public static float SqrMagnitudeXZ(this Vector3 v) => v.x * v.x + v.z * v.z;

        public static Vector3 NormalizeXZ(this Vector3 v) {
            v.y = 0;
            v.Normalize();
            return v;
        }

        public static float AngleDynamicXZ(this Vector3 directionVector) {
            Vector3 vectorZero; vectorZero.x = 0; vectorZero.y = 0; vectorZero.z = 0;
            Vector3 xAxis; xAxis.x = 1; xAxis.y = 0; xAxis.z = 0;
            Vector3 yAxis; yAxis.x = 0; yAxis.y = 1; yAxis.z = 0;
            if (directionVector != vectorZero) {
                directionVector.y = 0f;
                directionVector.Normalize();
                return PLTMath.AngleSigned(directionVector, xAxis, yAxis) + Mathf.PI;
            }
            return 0f;
        }

        // =============  HOVERING STUFF  =============
        /// <summary>Checks to see whether a given point lies within a circle of given center and radius. In the XZ-plane</summary>
        /// <param name="circleCenter"></param>
        /// <param name="radius"></param>
        /// <param name="pointOfInterest"></param>
        /// <returns></returns>
        public static bool IsInsideCircleXZ(this Vector3 circleCenter, float radius, Vector3 pointOfInterest) {
            if (radius == 0f) return pointOfInterest == circleCenter;
            else if (radius < 0f) radius = Math.Abs(radius);
            circleCenter.y = 0f;
            pointOfInterest.y = 0f;
            return (pointOfInterest - circleCenter).sqrMagnitude <= radius * radius;
        }

        /// <summary>Checks to see whether a given point is close to a circle outline of given center and radius. In the XZ-plane.</summary>
        /// <param name="circleCenter"></param>
        /// <param name="radius"></param>
        /// <param name="pointOfInterest"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        public static bool IsNearCircleOutlineXZ(this Vector3 circleCenter, float circleRadius, Vector3 pointOfInterest, float distance) {
            bool isNearCircle(float distanceSqr, float min, float max) => distanceSqr >= min * min && distanceSqr <= max * max;
            if (distance == 0f) return pointOfInterest == circleCenter;
            else if (distance < 0f) distance = Math.Abs(distance);
            circleCenter.y = 0f;
            pointOfInterest.y = 0f;
            return isNearCircle((pointOfInterest - circleCenter).sqrMagnitude, circleRadius - distance, circleRadius + distance);
        }
    }
}
