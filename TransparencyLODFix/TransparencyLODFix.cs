using EManagersLib;
using System;
using System.Collections.Generic;
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

        internal static PrefabInfo[] m_rotorShaderPrefabs;

        internal static void TransparentLodFix(this BuildingInfo building) {
            building.m_lodMissing = true;
            if (building.m_mesh.name.Contains(CLOUDTOKEN) || building.name.Contains(CLOUDTOKEN)) {
                building.m_minLodDistance = (building.m_maxLodDistance = (Settings.HideClouds ? 0f : 100000f));
                building.m_placementMode = BuildingInfo.PlacementMode.OnTerrain;
                return;
            }
            if (building.m_generatedInfo.m_triangleArea == 0f || float.IsNaN(building.m_generatedInfo.m_triangleArea)) {
                building.m_minLodDistance = (building.m_maxLodDistance = Settings.FallbackRenderDistanceBuildings);
                return;
            }
            float num = RenderManager.LevelOfDetailFactor * Settings.LodFactorMultiplierBuildings;
            building.m_minLodDistance = (building.m_maxLodDistance = (float)(Math.Sqrt(building.m_generatedInfo.m_triangleArea) * num + Settings.DistanceOffsetBuildings));
            PAModule.PALog("Transparency fixed for: " + building.name);
        }

        internal static void TransparentLodFix(this PropInfo prop) {
            if (prop.m_generatedInfo.m_triangleArea == 0f || float.IsNaN(prop.m_generatedInfo.m_triangleArea)) {
                prop.m_maxRenderDistance = Settings.FallbackRenderDistanceProps;
            } else {
                float num = RenderManager.LevelOfDetailFactor * Settings.LodFactorMultiplierProps;
                prop.m_maxRenderDistance = (float)(Math.Sqrt(prop.m_generatedInfo.m_triangleArea) * num + Settings.DistanceOffsetProps);
                prop.m_maxRenderDistance = EMath.Min(100000f, prop.m_maxRenderDistance);
            }
            prop.m_lodRenderDistance = (prop.m_isDecal || prop.m_isMarker) ? 0f : ((prop.m_lodMesh is null) ? prop.m_maxRenderDistance : (prop.m_maxRenderDistance * Settings.LodDistanceMultiplierProps));
            if (!(prop.m_effects is null)) {
                for (int i = 0; i < prop.m_effects.Length; i++) {
                    if (!(prop.m_effects[i].m_effect is null)) {
                        prop.m_maxRenderDistance = EMath.Max(prop.m_maxRenderDistance, prop.m_effects[i].m_effect.RenderDistance());
                    }
                }
                prop.m_maxRenderDistance = EMath.Min(100000f, prop.m_maxRenderDistance);
            }
            PAModule.PALog("Transparency fixed for: " + prop.name);
        }

        internal static void CheckRotorSignature(this BuildingInfo building, List<PrefabInfo> tempBuffer) {
            if (building) {
                Material material = building.m_material;
                if (material) {
                    Shader shader = material.shader;
                    Mesh mesh = building.m_mesh;
                    if (shader && mesh && HasRotorShader(shader.name) && !tempBuffer.Contains(building)) {
                        building.TransparentLodFix();
                        tempBuffer.Add(building);
                    }
                }
            }
        }

        internal static void CheckRotorSignature(this PropInfo prop, List<PrefabInfo> tempBuffer) {
            if (prop.m_material is Material material) {
                Shader shader = material.shader;
                Mesh mesh = prop.m_mesh;
                if (shader && mesh && HasRotorShader(shader.name) && !tempBuffer.Contains(prop)) {
                    prop.TransparentLodFix();
                    tempBuffer.Add(prop);
                }
            }
        }

        internal static void Update() {
            if (m_rotorShaderPrefabs is PrefabInfo[] rotorShaderPrefabs && rotorShaderPrefabs.Length > 0) {
                for (int i = 0; i < rotorShaderPrefabs.Length; i++) {
                    if (rotorShaderPrefabs[i] is PropInfo prop) {
                        prop.TransparentLodFix();
                    } else if (rotorShaderPrefabs[i] is BuildingInfo building) {
                        building.TransparentLodFix();
                    }
                }
            }
        }
    }
}
