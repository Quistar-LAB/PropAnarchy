using EManagersLib;
using System;
using UnityEngine;

namespace PropAnarchy.TransparencyLODFix {
    internal static class TransparencyLODFix {
        private const string CLOUDTOKEN = @"_CloudMod";
        private const int CLOUDTOKENLEN = 9;
        private static bool HasRotorShader(string name) => name[0] == 'C' && name[1] == 'u' && name[2] == 's' && name[3] == 't' && name[4] == 'o' &&
                                                           name[5] == 'm' && name[6] == '/' && name[7] == 'V' && name[8] == 'e' && name[9] == 'h' &&
                                                           name[10] == 'i' && name[11] == 'c' && name[12] == 'l' && name[13] == 'e' && name[14] == 's' &&
                                                           name[15] == '/' && name[16] == 'V' && name[17] == 'e' && name[18] == 'h' && name[19] == 'i' &&
                                                           name[20] == 'c' && name[21] == 'l' && name[22] == 'e' && name[23] == '/' && name[24] == 'R' &&
                                                           name[25] == 'o' && name[26] == 't' && name[27] == 'o' && name[28] == 'r' && name[29] == 's';
        internal static void TransparentLodFix(this BuildingInfo building) {
            float lodDistance;
            if (!(building is null) && building.m_material is Material material && !(material is null) && material.shader is Shader shader && building.m_mesh is Mesh mesh && HasRotorShader(shader.name)) {
                building.m_lodMissing = true;
                if ((mesh.name.Length >= CLOUDTOKENLEN && mesh.name.IndexOf(CLOUDTOKEN, StringComparison.Ordinal) != -1) ||
                    (building.name.Length >= CLOUDTOKENLEN && building.name.IndexOf(CLOUDTOKEN, StringComparison.Ordinal) != -1)) {
                    lodDistance = Settings.HideClouds ? 0f : Settings.RenderDistanceThreshold;
                    building.m_maxLodDistance = lodDistance;
                    building.m_minLodDistance = lodDistance;
                    building.m_placementMode = BuildingInfo.PlacementMode.OnTerrain;
                } else if (building.m_generatedInfo.m_triangleArea == 0f || float.IsNaN(building.m_generatedInfo.m_triangleArea)) {
                    lodDistance = Settings.FallbackRenderDistanceBuildings;
                    building.m_maxLodDistance = lodDistance;
                    building.m_minLodDistance = lodDistance;
                } else {
                    lodDistance = EMath.Sqrt(building.m_generatedInfo.m_triangleArea) * RenderManager.LevelOfDetailFactor * Settings.LodFactorMultiplierBuildings + Settings.DistanceOffsetBuildings;
                    building.m_maxLodDistance = lodDistance;
                    building.m_minLodDistance = lodDistance;
                }
                PAModule.PALog("Transparency fixed for: " + building.name);
            }
        }

        internal static void TransparentLodFix(this PropInfo prop) {
            if (!(prop is null) && prop.m_material is Material material && material.shader is Shader shader && prop.m_mesh is Mesh mesh && HasRotorShader(shader.name)) {
                float lodDistance;
                if (prop.m_generatedInfo.m_triangleArea == 0f || float.IsNaN(prop.m_generatedInfo.m_triangleArea)) {
                    prop.m_maxRenderDistance = Settings.FallbackRenderDistanceProps;
                } else {
                    lodDistance = EMath.Sqrt(prop.m_generatedInfo.m_triangleArea) * RenderManager.LevelOfDetailFactor * Settings.LodDistanceMultiplierProps + Settings.DistanceOffsetProps;
                    prop.m_maxRenderDistance = EMath.Min(Settings.RenderDistanceThreshold, lodDistance);
                }
                prop.m_lodRenderDistance = (prop.m_isDecal || prop.m_isMarker) ? 0f : (prop.m_lodMesh is null ? prop.m_maxRenderDistance : prop.m_maxRenderDistance * Settings.LodDistanceMultiplierProps);
                if (prop.m_effects is PropInfo.Effect[] effects) {
                    for (int i = 0; i < effects.Length; i++) {
                        if (!(effects[i].m_effect is null)) {
                            prop.m_maxRenderDistance = EMath.Max(prop.m_maxRenderDistance, effects[i].m_effect.RenderDistance());
                        }
                    }
                    prop.m_maxRenderDistance = EMath.Min(Settings.RenderDistanceThresholdEffects, prop.m_maxRenderDistance);
                }
                PAModule.PALog("Transparency fixed for: " + prop.name);
            }
        }
    }
}
