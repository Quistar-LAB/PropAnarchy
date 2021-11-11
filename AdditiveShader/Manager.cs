using ColossalFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace PropAnarchy.AdditiveShader {
    public static class Manager {
        private const string SIGNATURE = "AdditiveShader";
        private static ManagedAsset[] m_managedAssets;
        private static Thread m_shaderThread;
        private static volatile bool m_keepAlive = true;

        /// <summary>
        /// Check if a mesh name contains the additive shader token.
        /// </summary>
        /// <param name="meshName">The <c>m_mesh.name</c> to investigate.</param>
        /// <returns>Returns <c>true</c> if the token is found, otherwise <c>false</c>.</returns>
        private static bool HasValidData(string meshName) => !string.IsNullOrEmpty(meshName) && meshName.StartsWith(SIGNATURE);

        /// <summary>
        /// Because LODs don't support additive shader, if there are any props in the building that use
        /// it we have to increase the <c>m_maxPropDistance</c> for the whole building, _in addition_ to
        /// the props themselves being updated (<see cref="Add_Props(List{ManagedAsset})"/>).
        /// </summary>
        /// <param name="building">The <see cref="BuildingInfo"/> to inspect.</param>
        /// <returns>Returns <c>true</c> if the building contains shader-using props, otherwise <c>false</c>.</returns>
        private static bool ContainsShaderProps(BuildingInfo building) => building.m_props.Any(prop =>
                                                                            prop.m_finalProp &&
                                                                            prop.m_finalProp.m_isCustomContent &&
                                                                            HasValidData(prop.m_finalProp.m_mesh.name));

        public static void Initialize() {
            List<ManagedAsset> assets = new List<ManagedAsset>();
            try {
                foreach (PrefabInfo prefab in Resources.FindObjectsOfTypeAll<PrefabInfo>()) {
                    if (prefab is PropInfo prop && prop.m_isCustomContent && prop.m_mesh && prop.m_mesh.name is string propData && HasValidData(propData)) {
                        assets.Add(new ManagedAsset(prop));
                        PAModule.PALog("[AdditiveShader] : Loaded a prop - " + prop.name + " marked as having the AdditiveShader");
                    } else if (prefab is BuildingInfo building && building.m_isCustomContent) {
                        if (building.m_mesh && HasValidData(building.m_mesh.name)) {
                            assets.Add(new ManagedAsset(building));
                        }
                        if (!(building.m_props is null) && ContainsShaderProps(building)) {
                            assets.Add(new ManagedAsset(building, true));
                        }
                        PAModule.PALog("[AdditiveShader] : Loaded a building - " + building.name + " marked as having the AdditiveShader");
                    } else if (prefab is BuildingInfoSub buildingSub && buildingSub.m_isCustomContent && buildingSub.m_mesh && buildingSub.m_mesh.name is string buildingSubData && HasValidData(buildingSubData)) {
                        assets.Add(new ManagedAsset(buildingSub));
                        PAModule.PALog("[AdditiveShader] : Loaded a building sub - " + buildingSub.name + " marked as having the AdditiveShader");
                    } else if (prefab is VehicleInfoSub vehicleSub && vehicleSub.m_isCustomContent && vehicleSub.m_mesh && vehicleSub.m_mesh.name is string vehicleSubData && HasValidData(vehicleSubData)) {
                        assets.Add(new ManagedAsset(vehicleSub));
                        PAModule.PALog("[AdditiveShader] : Loaded a vehicle sub - " + vehicleSub.name + " mesh marked as having the AdditiveShader");
                    }
                }
            } catch (Exception e) {
                UnityEngine.Debug.LogException(e);
            } finally {
                if (assets.Count > 0) {
                    m_managedAssets = assets.ToArray();
                    foreach (var asset in assets) {
                        if (asset.IsContainer || asset.Profile.IsStatic) {
                            asset.SetVisible(true);
                        } else {
                            asset.SetVisible(false);
                        }
                    }
                    m_keepAlive = true;
                    m_shaderThread = new Thread(AdditiveShaderThread) {
                        IsBackground = true,
                        Priority = System.Threading.ThreadPriority.Lowest
                    };
                    RefreshRenderGroups();
                    m_shaderThread.Start();
                }
            }
        }

        public static void Destroy() {
            m_keepAlive = false;
            m_shaderThread.Abort();
            int len = m_managedAssets.Length;
            for (int i = 0; i < len; i++) {
                m_managedAssets[i].RestoreOriginalSettings();
            }
        }


        public static void AdditiveShaderThread() {
            const int THREADSLEEPDURATION = 10000;
            ManagedAsset[] assets = m_managedAssets;
            SimulationManager smInstance = Singleton<SimulationManager>.instance;
            try {
                while (m_keepAlive) {
                    Thread.Sleep(THREADSLEEPDURATION);
                    float time = smInstance.m_currentDayTimeHour;
                    bool isNightTime = smInstance.m_isNightTime;
                    int prefabCount = assets.Length;
                    PAModule.PALog($"Prefab Count to update: {prefabCount}");
                    for (int i = 0; i < prefabCount; i++) {
                        if (!assets[i].IsContainer && !assets[i].Profile.IsStatic) {
                            if (assets[i].Profile.IsToggledByTwilight) assets[i].SetVisibleByTwilight(isNightTime);
                            else assets[i].SetVisibleByTime(time);
                        }
                    }
                }
            } catch (ThreadAbortException) {
                PAModule.PALog("AdditiveShader Thread Aborted!");
            }
        }

        private static void RefreshRenderGroups() {
            int buildingLayer = LayerMask.NameToLayer("Buildings");
            int propsLayer = LayerMask.NameToLayer("Props");
            foreach (var renderGroup in RenderManager.instance.m_groups) {
                if (!(renderGroup is null)) {
                    renderGroup.SetLayerDataDirty(buildingLayer);
                    renderGroup.SetLayerDataDirty(propsLayer);
                    renderGroup.UpdateMeshData();
                }
            }
        }
    }
}
