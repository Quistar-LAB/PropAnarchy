using EManagersLib;
using UnityEngine;

namespace PropAnarchy {
    public static class DecalPropFix {
        private const float rMarker = 12f / 255f;
        private const float gMarker = 34f / 255f;
        private const float bMarker = 56f / 255f;
        private const float aMarker = 1f;

        public static void AssignFix(PropInfo prefab) {
            if (prefab.m_prefabInitialized && prefab.m_isDecal && !(prefab.m_material is null)) {
                Color color = prefab.m_material.GetColor("_ColorV0");
                if (EMath.IsNearlyEqual(color.r, rMarker) && EMath.IsNearlyEqual(color.g, gMarker) && EMath.IsNearlyEqual(color.b, bMarker) && color.a == aMarker) {
                    Color colorV1 = prefab.m_material.GetColor("_ColorV1");
                    Color colorV2 = prefab.m_material.GetColor("_ColorV2");
                    Vector4 size = new Vector4(colorV1.r * 255f, colorV1.g * 255f, colorV1.b * 255f, 0f);
                    var tiling = new Vector4(colorV2.r * 255f, 0f, colorV2.b * 255f, 0f);
                    prefab.m_material.SetVector("_DecalSize", size);
                    prefab.m_material.SetVector("_DecalTiling", tiling);
                    prefab.m_lodMaterial.SetVector("_DecalSize", size);
                    prefab.m_lodMaterial.SetVector("_DecalTiling", tiling);
                    prefab.m_lodMaterialCombined.SetVector("_DecalSize", size);
                    prefab.m_lodMaterialCombined.SetVector("_DecalTiling", tiling);
                }
            }
        }
    }
}
