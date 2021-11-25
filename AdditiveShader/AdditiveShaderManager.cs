using ColossalFramework;
using System;
using System.Collections;
using UnityEngine;

namespace PropAnarchy.AdditiveShader {
    public class AdditiveShaderManager : Singleton<AdditiveShaderManager> {
        private const string SIGNATURE = @"AdditiveShader";
        private const string MANAGERNAME = @"AdditiveShaderManager";
        public static ManagedAsset[] m_managedAssets;

        /// <summary>
        /// Check if a mesh name contains the additive shader token.
        /// </summary>
        /// <param name="meshName">The <c>m_mesh.name</c> to investigate.</param>
        /// <returns>Returns <c>true</c> if the token is found, otherwise <c>false</c>.</returns>
        public static bool HasValidData(string meshName) => !string.IsNullOrEmpty(meshName) && meshName.StartsWith(SIGNATURE, StringComparison.Ordinal);

        /// <summary>
        /// Because LODs don't support additive shader, if there are any props in the building that use
        /// it we have to increase the <c>m_maxPropDistance</c> for the whole building, _in addition_ to
        /// the props themselves being updated (<see cref="Add_Props(List{ManagedAsset})"/>).
        /// </summary>
        /// <param name="building">The <see cref="BuildingInfo"/> to inspect.</param>
        /// <returns>Returns <c>true</c> if the building contains shader-using props, otherwise <c>false</c>.</returns>
        public static bool ContainsShaderProps(BuildingInfo building) {
            bool result = false;
            BuildingInfo.Prop[] props = building.m_props;
            int len = props.Length;
            for (int i = 0; i < len; i++) {
                PropInfo prop = props[i].m_finalProp;
                if (prop && prop.m_mesh && !prop.m_mesh.name.IsNullOrWhiteSpace() && HasValidData(prop.m_mesh.name)) {
                    building.m_maxPropDistance = 25000f;
                    result = true;
                }
            }
            return result;
        }



        AdditiveShaderManager() {
            name = MANAGERNAME;
        }

#pragma warning disable IDE0051
        private IEnumerator AdditiveShaderThread() {
            const float THREADSLEEPDURATION = .8f;
            ManagedAsset[] assets = m_managedAssets;
            SimulationManager smInstance = Singleton<SimulationManager>.instance;
            while (true) {
                yield return new WaitForSeconds(THREADSLEEPDURATION);
                float time = smInstance.m_currentDayTimeHour;
                bool isNightTime = smInstance.m_isNightTime;
                int prefabCount = assets.Length;
                PAModule.PALog($"Prefab Count to update: {prefabCount}");
                for (int i = 0; i < prefabCount; i++) {
                    ShaderProfile profile = assets[i].Profile;
                    switch (profile.m_profile & ShaderProfile.Profiles.PROFILE_TYPE) {
                    case ShaderProfile.Profiles.OldRonyxProfile:
                        float onTime = profile.OnTime;
                        float offTime = profile.OffTime;
                        if (onTime < offTime) {
                            if (time >= onTime && time < offTime) {
                                assets[i].SetVisible(true);
                            } else if (time < onTime || time >= offTime) {
                                assets[i].SetVisible(false);
                            }
                        } else {
                            if (time >= offTime && time < onTime) {
                                assets[i].SetVisible(false);
                            } else if (time < offTime || time >= onTime) {
                                assets[i].SetVisible(true);
                            }
                        }
                        break;
                    case ShaderProfile.Profiles.Container:
                        assets[i].SetVisible(true);
                        break;
                    default:
                        onTime = profile.OnTime;
                        offTime = profile.OffTime;
                        PAModule.PALog($"Setting by twilight: OnTime: {onTime} -- OffTime: {offTime}");
                        if (isNightTime && assets[i].Profile.IsToggledByTwilight) {
                            PAModule.PALog($"Setting by twilight: OnTime: {onTime} -- OffTime: {offTime}");
                            assets[i].SetVisibleByTwilight(isNightTime);
                        } else {
                            assets[i].SetVisibleByTime(time);
                        }
                        //if (!assets[i].IsContainer && !assets[i].Profile.IsStatic) {
                        //    if (assets[i].Profile.IsToggledByTwilight) assets[i].SetVisibleByTwilight(isNightTime);
                        //    else assets[i].SetVisibleByTime(time);
                        // }

                        break;
                    }
                    yield return null;
                }
            }
        }
#pragma warning restore IDE0051

        public static void RefreshRenderGroups() {
            int buildingLayer = LayerMask.NameToLayer(@"Buildings");
            int propsLayer = LayerMask.NameToLayer(@"Props");
            RenderGroup[] renderGroups = Singleton<RenderManager>.instance.m_groups;
            int len = renderGroups.Length;
            for (int i = 0; i < len; i++) {
                RenderGroup renderGroup = renderGroups[i];
                if (!(renderGroup is null)) {
                    renderGroup.SetLayerDataDirty(buildingLayer);
                    renderGroup.SetLayerDataDirty(propsLayer);
                    renderGroup.UpdateMeshData();
                }
            }
        }
    }
}
