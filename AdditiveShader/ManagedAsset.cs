using EManagersLib;
using System;
using UnityEngine;

namespace PropAnarchy.AdditiveShader {
    public readonly struct ManagedAsset {
        private const string FADEPROPERTY = "_InvFade";
        private const string INTENSITYPROPERTY = "_Intensity";
        /// <summary>
        /// The type of asset associated with an <see cref="ManagedAsset"/> instance.
        /// </summary>
        public enum AssetType : int {
            None,
            Prop,
            Building,
            SubBuilding,
            Vehicle,
            Container,
        }

        /// <summary>
        /// Fake <c>m_mesh.name</c> for <see cref="AssetType.Container"/> assets.
        /// </summary>
        /// <remarks>
        /// It is passed to constructor of <see cref="ShaderInfo"/> class
        /// which will treat it as 'Continer' profile.
        /// </remarks>
        private const string CONTAINER_BUILDING = "AdditiveShader Container 0 0 container-building";

        /// <summary>
        /// If a building contains a prop which uses additive shader,
        /// the <see cref="BuildingInfo.m_maxPropDistance"/> must be
        /// increased to prevent its props using LOD.
        /// </summary>
        private const float CONTAINER_MAX_PROP_DISTANCE = 25000f;

        // Backup original values (in constructors) so they can be restored on exit.
        // Each AssetType uses as _subset_ of these backups.
        private readonly bool backup_lodHasDifferentShader;  // PropInfo, BuildingInfo, BuildingInfoSub
        private readonly bool backup_lodMissing;             // BuildingInfo
        private readonly Color[] backup_meshColors;          // BuildingInfo, BuildingInfoSub, VehicleInfoSub
        private readonly float backup_InvFade;               // PropInfo, BuildingInfo, BuildingInfoSub, VehicleInfoSub
        private readonly float backup_lodRenderDistance;     // PropInfo, VehicleInfoSub
        private readonly float backup_maxRenderDistance;     // PropInfo, VehicleInfoSub
        private readonly float backup_maxLodDistance;        // BuildingInfo, BuildingInfoSub
        private readonly float backup_minLodDistance;        // BuildingInfo, BuildingInfoSub
        private readonly float backup_maxPropDistance;       // Container (BuildingInfo)

        public readonly PrefabInfo m_prefab;

        /// <summary>
        /// Initializes a new instance of the <see cref="ManagedAsset"/> class
        /// for a <see cref="PropInfo"/> asset.
        /// </summary>
        /// <param name="asset">The <see cref="PropInfo"/> which uses the shader.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="asset"/> is <c>null</c>.</exception>
        public ManagedAsset(PropInfo prefab) {
            TypeOfAsset = AssetType.Prop;
            m_prefab = prefab;
            IsContainer = false;
            CachedRenderDistance = RenderDistance(prefab.m_generatedInfo.m_size);
            Profile = new ShaderProfile(prefab.m_mesh.name);
            backup_lodHasDifferentShader = prefab.m_lodHasDifferentShader;
            backup_lodMissing = false;
            backup_meshColors = prefab.m_mesh.colors;
            backup_InvFade = prefab.m_material.GetFloat(FADEPROPERTY);
            backup_lodRenderDistance = prefab.m_lodRenderDistance;
            backup_maxRenderDistance = prefab.m_maxRenderDistance;
            backup_maxLodDistance = 0f;
            backup_minLodDistance = 0f;
            backup_maxPropDistance = 0f;
            prefab.m_lodHasDifferentShader = false;
            prefab.m_material.SetFloat(FADEPROPERTY, Profile.Fade);
            prefab.m_lodRenderDistance = EMath.Max(prefab.m_lodRenderDistance, CachedRenderDistance);
            prefab.m_maxRenderDistance = EMath.Max(prefab.m_maxRenderDistance, CachedRenderDistance);
            SetVisible(Profile.IsAlwaysOn);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ManagedAsset"/> class
        /// for a <see cref="BuildingInfo"/> asset.
        /// </summary>
        /// <param name="asset">The <see cref="BuildingInfo"/> which uses the shader.</param>
        /// <exception cref="ArgumentNullException">Thrown if asset <c>m_mesh.name</c> is <c>null</c>.</exception>
        /// <exception cref="FormatException">Thrown if asset <c>m_mesh.name</c> format is invalid.</exception>
        public ManagedAsset(BuildingInfo prefab) {
            TypeOfAsset = AssetType.Building;
            m_prefab = prefab;
            IsContainer = false;
            CachedRenderDistance = RenderDistance(prefab.m_generatedInfo.m_max);
            Profile = new ShaderProfile(prefab.m_mesh.name);
            backup_lodHasDifferentShader = prefab.m_lodHasDifferentShader;
            backup_lodMissing = prefab.m_lodMissing;
            backup_meshColors = prefab.m_mesh.colors;
            backup_InvFade = prefab.m_material.GetFloat(FADEPROPERTY);
            backup_lodRenderDistance = 0f;
            backup_maxRenderDistance = 0f;
            backup_maxLodDistance = prefab.m_maxLodDistance;
            backup_minLodDistance = prefab.m_minLodDistance;
            backup_maxPropDistance = 0f;
            prefab.m_lodHasDifferentShader = false;
            prefab.m_lodMissing = true;
            prefab.m_material.SetFloat(FADEPROPERTY, Profile.Fade);
            prefab.m_mesh.colors = AssignNewColors(prefab.m_mesh.vertices.Length);
            prefab.m_maxLodDistance = EMath.Max(prefab.m_maxLodDistance, CachedRenderDistance);
            prefab.m_minLodDistance = EMath.Max(prefab.m_minLodDistance, CachedRenderDistance);
            SetVisible(Profile.IsAlwaysOn);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ManagedAsset"/> class
        /// for a <see cref="BuildingInfo"/> asset which contains a shader-using
        /// <see cref="PropInfo"/> asset.
        /// </summary>
        /// <remarks>
        /// This is distinct from the other ShaderAsset types in that the building
        /// itself is not usually directly shader-using (if it is, a separate
        /// ShaderAsset will be created for it).
        /// </remarks>
        /// <param name="asset">The <see cref="BuildingInfo"/> which uses the shader.</param>
        /// <param name="isContainer">Ignored - just there to differentiate the overload.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="isContainer"/> is not <c>true</c>.</exception>
        /// <exception cref="ArgumentNullException">Thrown if asset <c>m_mesh.name</c> is <c>null</c>.</exception>
        /// <exception cref="FormatException">Thrown if asset <c>m_mesh.name</c> format is invalid.</exception>
        public ManagedAsset(BuildingInfo prefab, bool isContainer) {
            TypeOfAsset = AssetType.Container;
            m_prefab = prefab;
            IsContainer = isContainer;
            CachedRenderDistance = CONTAINER_MAX_PROP_DISTANCE;
            Profile = new ShaderProfile(CONTAINER_BUILDING);
            backup_lodHasDifferentShader = false;
            backup_lodMissing = false;
            backup_meshColors = prefab.m_mesh.colors;
            backup_InvFade = 0f;
            backup_lodRenderDistance = 0f;
            backup_maxRenderDistance = 0f;
            backup_maxLodDistance = 0f;
            backup_minLodDistance = 0f;
            backup_maxPropDistance = prefab.m_maxPropDistance;
            prefab.m_maxPropDistance = EMath.Max(prefab.m_maxPropDistance, CachedRenderDistance);
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="ManagedAsset"/> class
        /// for a <see cref="BuildingInfoSub"/> asset.
        /// </summary>
        /// <param name="asset">The <see cref="BuildingInfoSub"/> which uses the shader.</param>
        /// <exception cref="ArgumentNullException">Thrown if asset <c>m_mesh.name</c> is <c>null</c>.</exception>
        /// <exception cref="FormatException">Thrown if asset <c>m_mesh.name</c> format is invalid.</exception>
        public ManagedAsset(BuildingInfoSub prefab) {
            TypeOfAsset = AssetType.SubBuilding;
            m_prefab = prefab;
            IsContainer = false;
            CachedRenderDistance = RenderDistance(prefab.m_generatedInfo.m_max);
            Profile = new ShaderProfile(prefab.m_mesh.name);
            backup_lodHasDifferentShader = prefab.m_lodHasDifferentShader;
            backup_lodMissing = false;
            backup_meshColors = prefab.m_mesh.colors;
            backup_InvFade = prefab.m_material.GetFloat(FADEPROPERTY);
            backup_lodRenderDistance = 0f;
            backup_maxRenderDistance = 0f;
            backup_maxLodDistance = prefab.m_maxLodDistance;
            backup_minLodDistance = prefab.m_minLodDistance;
            backup_maxPropDistance = 0f;
            prefab.m_lodHasDifferentShader = false;
            prefab.m_material.SetFloat(FADEPROPERTY, Profile.Fade);
            prefab.m_mesh.colors = AssignNewColors(prefab.m_mesh.vertices.Length);
            prefab.m_maxLodDistance = EMath.Max(prefab.m_maxLodDistance, CachedRenderDistance);
            prefab.m_minLodDistance = EMath.Max(prefab.m_minLodDistance, CachedRenderDistance);
            SetVisible(Profile.IsAlwaysOn);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ManagedAsset"/> class
        /// for a <see cref="VehicleInfoSub"/> asset.
        /// </summary>
        /// <param name="asset">The <see cref="VehicleInfoSub"/> which uses the shader.</param>
        /// <exception cref="ArgumentNullException">Thrown if asset <c>m_mesh.name</c> is <c>null</c>.</exception>
        /// <exception cref="FormatException">Thrown if asset <c>m_mesh.name</c> format is invalid.</exception>
        public ManagedAsset(VehicleInfoSub prefab) {
            TypeOfAsset = AssetType.Vehicle;
            m_prefab = prefab;
            IsContainer = false;
            CachedRenderDistance = RenderDistance(prefab.m_generatedInfo.m_size);
            try {
                Profile = new ShaderProfile(prefab.m_mesh.name);
            } catch {
                PAModule.PALog($"Failed to create Additive Shader profile for: {prefab.name}");
                PAModule.PALog($"Additive signature is: {prefab.m_mesh.name}");
                Profile = new ShaderProfile(ShaderProfile.Profiles.STATIC);
            }
            backup_lodHasDifferentShader = false;
            backup_lodMissing = false;
            backup_meshColors = prefab.m_mesh.colors.Clone() as Color[];
            backup_InvFade = prefab.m_material.GetFloat(FADEPROPERTY);
            backup_lodRenderDistance = prefab.m_lodRenderDistance;
            backup_maxRenderDistance = prefab.m_maxRenderDistance;
            backup_maxLodDistance = 0f;
            backup_minLodDistance = 0f;
            backup_maxPropDistance = 0f;
            prefab.m_material.SetFloat(FADEPROPERTY, Profile.Fade);
            prefab.m_mesh.colors = AssignNewColors(prefab.m_mesh.vertices.Length);
            prefab.m_lodRenderDistance = EMath.Max(prefab.m_lodRenderDistance, CachedRenderDistance);
            prefab.m_maxRenderDistance = EMath.Max(prefab.m_maxRenderDistance, CachedRenderDistance);
            SetVisible(Profile.IsAlwaysOn);
        }

        public ShaderProfile Profile { get; }

        /// <summary>
        /// Gets a value indicating whether this asset is just a container for another shader-using asset.
        /// </summary>
        public bool IsContainer { get; }

        /// <summary>
        /// Gets a cached render distance applicable to this asset.
        /// </summary>
        public float CachedRenderDistance { get; }

        /// <summary>
        /// <para>Gets a value indicating what type of asset this instance represents.</para>
        /// <para>
        /// Depending on the type, the asset will be stored in one of the following members:
        /// <list type="bullet">
        /// <item><see cref="Prop"/></item> -- for <see cref="AssetType.Prop"/>
        /// <item><see cref="Building"/></item> -- for <see cref="AssetType.Building"/> or <see cref="AssetType.Container"/>
        /// <item><see cref="SubBuilding"/></item> -- for <see cref="AssetType.SubBuilding"/>
        /// <item><see cref="Vehicle"/></item> -- for <see cref="AssetType.Vehicle"/>
        /// </list>
        /// </para>
        /// </summary>
        public AssetType TypeOfAsset { get; }

        /// <summary>
        /// Show or hide the additive shader for this asset based on game world time.
        /// </summary>
        /// <param name="time">The game time of day.</param>
        public void SetVisibleByTime(float time) => SetVisible((Profile is ShaderProfile profile) &&
            profile.OverlapsMidnight ? time < profile.OffTime || profile.OnTime <= time : profile.OnTime <= time && time < profile.OffTime);

        /// <summary>
        /// Show or hide the additive shader for this asset based on night vs. day.
        /// </summary>
        /// <param name="currentlyNightTime">Set <c>true</c> if it is now night time in game world.</param>
        public void SetVisibleByTwilight(bool currentlyNightTime) => SetVisible(currentlyNightTime == Profile.IsNightTimeOnly);

        /// <summary>
        /// Show or hide the additive shader for this asset.
        /// </summary>
        /// <param name="visible">If <c>true</c>, the shader will be shown, otherwise it will be hidden.</param>
        /// <param name="force">If <c>true</c>, don't check current state. Defaults to <c>false</c>.</param>
        public void SetVisible(bool visible) {
            switch (TypeOfAsset) {
            case AssetType.Prop:
                PropInfo propInfo = m_prefab as PropInfo;
                propInfo.m_lodRenderDistance = propInfo.m_maxRenderDistance = CachedRenderDistance;
                propInfo.m_material.SetFloat(INTENSITYPROPERTY, visible ? Profile.Intensity : 0f);
                break;
            case AssetType.Building:
                BuildingInfo building = m_prefab as BuildingInfo;
                building.m_maxLodDistance = building.m_minLodDistance = CachedRenderDistance;
                building.m_material.SetFloat(INTENSITYPROPERTY, visible ? Profile.Intensity : 0f);
                break;
            case AssetType.SubBuilding:
                BuildingInfoSub subBuilding = m_prefab as BuildingInfoSub;
                subBuilding.m_maxLodDistance = subBuilding.m_minLodDistance = CachedRenderDistance;
                subBuilding.m_material.SetFloat(INTENSITYPROPERTY, visible ? Profile.Intensity : 0f);
                break;
            case AssetType.Vehicle:
                (m_prefab as VehicleInfoSub).m_material.SetFloat(INTENSITYPROPERTY, visible ? Profile.Intensity : 0f);
                break;
            }
        }

        /// <summary>
        /// Additive shader doesn't work on LODs, so the render distance of the
        /// asset is increased, based on its size, to keep the effect visible for longer.
        /// </summary>
        /// <param name="size">The asset mesh size.</param>
        /// <returns>Returns the render distance applicable to the asset.</returns>
        private static float RenderDistance(Vector3 size) => (size.x + 30f) * (size.y + 30f) * (size.z + 30f) * 0.1f;

        public Color[] AssignNewColors(int verticesCount) {
            Color white = Color.white;
            Color[] colors = new Color[verticesCount];
            for (int i = 0; i < verticesCount; i++) colors[i] = white;
            return colors;
        }

        /// <summary>
        /// Rstores the original values from backups.
        /// </summary>
        public void RestoreOriginalSettings() {
            switch (TypeOfAsset) {
            case AssetType.Prop:
                PropInfo propInfo = m_prefab as PropInfo;
                propInfo.m_lodHasDifferentShader = backup_lodHasDifferentShader;
                propInfo.m_material.SetFloat(FADEPROPERTY, backup_InvFade);
                propInfo.m_lodRenderDistance = backup_lodRenderDistance;
                propInfo.m_maxRenderDistance = backup_maxRenderDistance;
                return;
            case AssetType.Building:
                BuildingInfo buildingInfo = m_prefab as BuildingInfo;
                buildingInfo.m_lodHasDifferentShader = backup_lodHasDifferentShader;
                buildingInfo.m_lodMissing = backup_lodMissing;
                buildingInfo.m_material.SetFloat(FADEPROPERTY, backup_InvFade);
                buildingInfo.m_mesh.colors = backup_meshColors;
                buildingInfo.m_maxLodDistance = backup_maxLodDistance;
                buildingInfo.m_minLodDistance = backup_minLodDistance;
                return;
            case AssetType.Container:
                buildingInfo = m_prefab as BuildingInfo;
                buildingInfo.m_maxPropDistance = backup_maxPropDistance;
                return;
            case AssetType.SubBuilding:
                BuildingInfoSub subBuilding = m_prefab as BuildingInfoSub;
                subBuilding.m_lodHasDifferentShader = backup_lodHasDifferentShader;
                subBuilding.m_material.SetFloat(FADEPROPERTY, backup_InvFade);
                subBuilding.m_mesh.colors = backup_meshColors;
                subBuilding.m_maxLodDistance = backup_maxLodDistance;
                subBuilding.m_minLodDistance = backup_maxRenderDistance;
                return;
            case AssetType.Vehicle:
                VehicleInfoSub vehicle = m_prefab as VehicleInfoSub;
                vehicle.m_lodRenderDistance = backup_lodRenderDistance;
                vehicle.m_maxRenderDistance = backup_maxRenderDistance;
                return;
            }
        }
    }
}
