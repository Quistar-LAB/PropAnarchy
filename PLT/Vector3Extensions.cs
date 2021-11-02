using ColossalFramework;
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

            public Vector3 ToVector3() {
                Vector3 result;
                if (Singleton<ToolManager>.instance.m_properties.m_mode == ItemClass.Availability.AssetEditor) {
                    result.x = x * 0.0164794922f;
                    result.y = y * 0.015625f;
                    result.z = z * 0.0164794922f;
                } else {
                    result.x = x * 0.263671875f;
                    result.y = y * 0.015625f;
                    result.z = z * 0.263671875f;
                }
                return result;
            }
        }

        public static bool Approximately(ref this Vector3 v, ref Vector3 c) => Mathf.Approximately(v.x, c.x) && Mathf.Approximately(v.y, c.y) && Mathf.Approximately(v.z, c.z);

        public static bool ApproximatelyXZ(ref this Vector3 v, ref Vector3 c) => Mathf.Approximately(v.x, c.x) && Mathf.Approximately(v.x, c.x);

        public static bool EqualOnGameShortGridXZ(ref this Vector3 v, ref Vector3 c) {
            ShortVector3 sV = new ShortVector3(v);
            ShortVector3 sCV = new ShortVector3(c);
            if (sV.x == sCV.x && sV.z == sCV.z) {
                return true;
            }
            return false;
        }

        public static bool NearlyEqualOnGameShortGridXZ(ref this Vector3 v, ref Vector3 c) {
            const int tolerance = 1;
            ShortVector3 sV = new ShortVector3(v);
            ShortVector3 sCV = new ShortVector3(c);
            if ((sV.x >= sCV.x - tolerance) && (sV.x <= sCV.x + tolerance) &&
               (sV.z >= sCV.z - tolerance) && (sV.z <= sCV.z + tolerance)) {
                return true;
            }
            return false;
        }

        public static ShortVector3 ToShortVector3(ref this Vector3 v) {
            ShortVector3 sV;
            if (Singleton<ToolManager>.instance.m_properties.m_mode == ItemClass.Availability.AssetEditor) {
                sV.x = (short)Mathf.Clamp(Mathf.RoundToInt(v.x * 60.68148f), -32767, 32767);
                sV.z = (short)Mathf.Clamp(Mathf.RoundToInt(v.z * 60.68148f), -32767, 32767);
                sV.y = (ushort)Mathf.Clamp(Mathf.RoundToInt(v.y * 64f), 0, 65535);
            } else {
                sV.x = (short)Mathf.Clamp(Mathf.RoundToInt(v.x * 3.79259253f), -32767, 32767);
                sV.z = (short)Mathf.Clamp(Mathf.RoundToInt(v.z * 3.79259253f), -32767, 32767);
                sV.y = (ushort)Mathf.Clamp(Mathf.RoundToInt(v.y * 64f), 0, 65535);
            }
            return sV;
        }

        public static Vector3 QuantizeToGameShortGridXYZ(ref this Vector3 v) => v.ToShortVector3().ToVector3();

        public static float MagnitudeXZ(ref this Vector3 v) => new Vector3(v.x, 0f, v.z).magnitude;

        public static float SqrMagnitudeXZ(ref this Vector3 v) => v.x * v.x + v.z * v.z;

        public static Vector3 NormalizeXZ(this Vector3 v) {
            v.y = 0;
            v.Normalize();
            return v;
        }

        public static float AngleDynamicXZ(ref this Vector3 directionVector) {
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
    }
}
