using ColossalFramework;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using UnityEngine;

namespace PropAnarchy {
    public static class AdditiveShader {
        public struct AdditiveShaderInfo {
            public float m_enableTime;
            public float m_disableTime;
            public float m_intensity;
            public float m_fade;
            public bool m_enabled;
            public PrefabInfo m_prefab;
        }

        private static AdditiveShaderInfo[] m_shaderInfos;
        private static Thread m_shaderThread;
        private static volatile bool m_keepAlive = true;

        private static bool IsValidData(string data) {
            if (data[0] == 'A' && data[1] == 'd' && data[2] == 'd' && data[3] == 'i' && data[4] == 't' &&
                data[5] == 'i' && data[6] == 'v' && data[7] == 'e' && data[8] == 'S' && data[9] == 'h' &&
                data[10] == 'a' && data[11] == 'd' && data[12] == 'e' && data[13] == 'r') {
                return true;
            }
            return false;
        }

        private static void ParseData(ref AdditiveShaderInfo si, string data) {
            int length = data.Length;
            int startIndex = 0, occurance = 0;
            for (int i = 0; i < length; i++) {
                if (data[i] == ' ') {
RestartLoop:
                    startIndex = ++i;
                    for (; i < length; i++) {
                        if (data[i] == ' ') {
                            occurance++;
                            switch (occurance) {
                            case 1:
                                si.m_enableTime = float.Parse(data.Substring(startIndex, i - startIndex));
                                goto RestartLoop;
                            case 2:
                                si.m_disableTime = float.Parse(data.Substring(startIndex, i - startIndex));
                                goto RestartLoop;
                            case 3:
                                si.m_fade = float.Parse(data.Substring(startIndex, i - startIndex));
                                goto RestartLoop;
                            }
                        }
                    }
                }
            }
            si.m_intensity = float.Parse(data.Substring(startIndex, length - startIndex));
        }

        public static void Initialize() {
            Color white = Color.white;
            Stopwatch sw = Stopwatch.StartNew();
            List<AdditiveShaderInfo> shaderInfos = new List<AdditiveShaderInfo>();
            foreach (PrefabInfo prefab in Resources.FindObjectsOfTypeAll<PrefabInfo>()) {
                if (prefab is PropInfo prop && !(prop.m_mesh is null) && prop.m_mesh.name is string propData && IsValidData(propData)) {
                    var si = new AdditiveShaderInfo {
                        m_prefab = prop,
                        m_enabled = false
                    };
                    ParseData(ref si, propData);
                    shaderInfos.Add(si);
                    prop.m_lodHasDifferentShader = false;
                    prop.m_material.SetFloat("_InvFade", si.m_fade);
                    SetPropShader(prop, ref si, false);
                    PAModule.PALog("[AdditiveShader] : Loaded a prop - " + prop.name + " marked as having the AdditiveShader");
                } else if (prefab is BuildingInfo building && !(building.m_mesh is null) && building.m_mesh.name is string buildingData && IsValidData(buildingData)) {
                    var si = new AdditiveShaderInfo {
                        m_prefab = building,
                        m_enabled = false
                    };
                    ParseData(ref si, buildingData);
                    shaderInfos.Add(si);
                    building.m_lodHasDifferentShader = false;
                    building.m_lodMissing = true;
                    building.m_material.SetFloat("_InvFade", si.m_fade);
                    SetBuildingShader(building, ref si, false);
                    Vector3[] vertices = building.m_mesh.vertices;
                    Color[] colors = new Color[vertices.Length];
                    for (int i = 0; i < vertices.Length; i++) colors[i] = white;
                    building.m_mesh.colors = colors;
                    PAModule.PALog("[AdditiveShader] : Loaded a building - " + building.name + " marked as having the AdditiveShader");
                } else if (prefab is BuildingInfoSub buildingSub && !(buildingSub.m_mesh is null) && buildingSub.m_mesh.name is string buildingSubData && IsValidData(buildingSubData)) {
                    var si = new AdditiveShaderInfo {
                        m_prefab = buildingSub,
                        m_enabled = false
                    };
                    ParseData(ref si, buildingSubData);
                    shaderInfos.Add(si);
                    buildingSub.m_lodHasDifferentShader = false;
                    buildingSub.m_material.SetFloat("_InvFade", si.m_fade);
                    SetBuildingSubShader(buildingSub, ref si, false);
                    Vector3[] vertices = buildingSub.m_mesh.vertices;
                    Color[] colors = new Color[vertices.Length];
                    for (int i = 0; i < vertices.Length; i++) colors[i] = white;
                    buildingSub.m_mesh.colors = colors;
                    PAModule.PALog("[AdditiveShader] : Loaded a building sub - " + buildingSub.name + " marked as having the AdditiveShader");
                } else if (prefab is VehicleInfoSub vehicleSub && !(vehicleSub.m_mesh is null) && vehicleSub.m_mesh.name is string vehicleSubData && IsValidData(vehicleSubData)) {
                    var si = new AdditiveShaderInfo {
                        m_prefab = vehicleSub,
                        m_enabled = false
                    };
                    ParseData(ref si, vehicleSubData);
                    shaderInfos.Add(si);
                    vehicleSub.m_material.SetFloat("_InvFade", si.m_fade);
                    SetVehicleSubShader(vehicleSub, ref si, false);
                    Vector3[] vertices = vehicleSub.m_mesh.vertices;
                    Color[] colors = new Color[vertices.Length];
                    for (int i = 0; i < vertices.Length; i++) colors[i] = white;
                    vehicleSub.m_mesh.colors = colors;
                    PAModule.PALog("[AdditiveShader] : Loaded a vehicle sub - " + vehicleSub.name + " mesh marked as having the AdditiveShader");
                }
            }
            m_shaderInfos = shaderInfos.ToArray();
            m_keepAlive = true;
            m_shaderThread = new Thread(AdditiveShaderThread) {
                IsBackground = true,
                Priority = System.Threading.ThreadPriority.Lowest
            };
            m_shaderThread.Start();
            sw.Stop();
            PAModule.PALog($"Additive Shader Initiliaization took {sw.ElapsedMilliseconds}ms");
        }

        public static void Destroy() {
            m_keepAlive = false;
            m_shaderThread.Abort();
        }

        public static void AdditiveShaderThread() {
            const int OneMinute = 60000;
            AdditiveShaderInfo[] shaderInfos = m_shaderInfos;
            int prefabCount = shaderInfos.Length;
            SimulationManager smInstance = Singleton<SimulationManager>.instance;
            try {
                while (m_keepAlive) {
                    Thread.Sleep(OneMinute);
                    float time = smInstance.m_currentDayTimeHour;
                    for (int i = 0; i < prefabCount; i++) {
                        float enableTime = shaderInfos[i].m_enableTime;
                        float disableTime = shaderInfos[i].m_disableTime;
                        PrefabInfo prefab = shaderInfos[i].m_prefab;
                        if (enableTime < disableTime) {
                            SetAdditiveShader(prefab, ref shaderInfos[i], time > enableTime && time < disableTime);
                        } else {
                            SetAdditiveShader(prefab, ref shaderInfos[i], time <= disableTime || time >= enableTime);
                        }
                    }
                }
            } catch (ThreadAbortException) {
                PAModule.PALog("AdditiveShader Thread Aborted!");
            }
        }

        public static void SetAdditiveShader(PrefabInfo prefab, ref AdditiveShaderInfo shaderInfo, bool enable) {
            if (prefab is PropInfo propInfo) {
                SetPropShader(propInfo, ref shaderInfo, enable);
            } else if (prefab is BuildingInfo building) {
                SetBuildingShader(building, ref shaderInfo, enable);
            } else if (prefab is BuildingInfoSub buildingSub) {
                SetBuildingSubShader(buildingSub, ref shaderInfo, enable);
            } else if (prefab is VehicleInfoSub vehicleSub) {
                SetVehicleSubShader(vehicleSub, ref shaderInfo, enable);
            }
        }

        public static void SetPropShader(PropInfo prop, ref AdditiveShaderInfo shaderInfo, bool enable) {
            prop.m_material.SetFloat("_Intensity", enable ? shaderInfo.m_intensity : 0f);
            shaderInfo.m_enabled = enable;
        }

        public static void SetBuildingShader(BuildingInfo building, ref AdditiveShaderInfo shaderInfo, bool enable) {
            building.m_material.SetFloat("_Intensity", enable ? shaderInfo.m_intensity : 0f);
            shaderInfo.m_enabled = enable;
        }

        public static void SetBuildingSubShader(BuildingInfoSub buildingSub, ref AdditiveShaderInfo shaderInfo, bool enable) {
            buildingSub.m_material.SetFloat("_Intensity", enable ? shaderInfo.m_intensity : 0f);
            shaderInfo.m_enabled = enable;
        }

        public static void SetVehicleSubShader(VehicleInfoSub vehicleSub, ref AdditiveShaderInfo shaderInfo, bool enable) {
            vehicleSub.m_material.SetFloat("_Intensity", enable ? shaderInfo.m_intensity : 0f);
            shaderInfo.m_enabled = enable;
        }
    }
}
