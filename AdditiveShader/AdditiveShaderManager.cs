using ColossalFramework;
using System.Collections;
using UnityEngine;

namespace PropAnarchy.AdditiveShader {
    public static class AdditiveShaderManager {
        public static ManagedAsset[] m_managedAssets;

        /// <summary>
        /// Check if a mesh name contains the additive shader token.
        /// </summary>
        /// <param name="data">The <c>m_mesh.name</c> to investigate.</param>
        /// <returns>Returns <c>true</c> if the token is found, otherwise <c>false</c>.</returns>
        public static bool HasValidData(string data) => !(data is null) && data.Length > 14 && data[0] == 'A' && data[1] == 'd' && data[2] == 'd' && data[3] == 'i' &&
                                                        data[4] == 't' && data[5] == 'i' && data[6] == 'v' && data[7] == 'e' &&
                                                        data[8] == 'S' && data[9] == 'h' && data[10] == 'a' && data[11] == 'd' &&
                                                        data[12] == 'e' && data[13] == 'r';

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
            for (int i = 0; i < props.Length; i++) {
                PropInfo prop = props[i].m_finalProp;
                if (prop && prop.m_mesh && HasValidData(prop.m_mesh.name)) {
                    building.m_maxPropDistance = 25000f;
                    result = true;
                }
            }
            return result;
        }

        internal static IEnumerator AdditiveShaderThread() {
            const float THREADSLEEPDURATION = 2.8f;
            ManagedAsset[] assets = m_managedAssets;
            SimulationManager smInstance = Singleton<SimulationManager>.instance;
            WaitForSeconds waitForSeconds = new WaitForSeconds(THREADSLEEPDURATION);
            while (true) {
                yield return waitForSeconds;
                float time = smInstance.m_currentDayTimeHour;
                bool isNightTime = smInstance.m_isNightTime;
                for (int i = 0; i < assets.Length; i++) {
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
                        if (!assets[i].IsContainer && !assets[i].Profile.IsStatic) {
                            if (assets[i].Profile.IsToggledByTwilight) assets[i].SetVisibleByTwilight(isNightTime);
                            else assets[i].SetVisibleByTime(time);
                        }
                        break;
                    }
                    yield return null;
                }
            }
        }

        public static void RefreshRenderGroups() {
            int buildingLayer = LayerMask.NameToLayer(@"Buildings");
            int propsLayer = LayerMask.NameToLayer(@"Props");
            RenderGroup[] renderGroups = Singleton<RenderManager>.instance.m_groups;
            for (int i = 0; i < renderGroups.Length; i++) {
                RenderGroup renderGroup = renderGroups[i];
                if (!(renderGroup is null)) {
                    renderGroup.SetLayerDataDirty(buildingLayer);
                    renderGroup.SetLayerDataDirty(propsLayer);
                    //renderGroup.UpdateMeshData();
                }
            }
        }
    }
}
