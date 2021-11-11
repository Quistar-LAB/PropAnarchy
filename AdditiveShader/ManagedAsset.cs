using EManagersLib.API;
using System;
using UnityEngine;

namespace PropAnarchy.AdditiveShader {
    public struct ManagedAsset {
        /// <summary>
        /// The type of asset associated with an <see cref="ManagedAsset"/> instance.
        /// </summary>
        public enum AssetType {
            /// <summary>
            /// Denotes an invalid asset type. Should never happen.
            /// </summary>
            None = 0,
            /// <summary>
            /// A <see cref="PropInfo"/> asset.
            /// </summary>
            Prop = 1,
            /// <summary>
            /// A <see cref="BuildingInfo"/> asset.
            /// </summary>
            Building = 1 << 1,
            /// <summary>
            /// A <see cref="BuildingInfoSub"/> asset.
            /// </summary>
            SubBuilding = 1 << 2,
            /// <summary>
            /// A <see cref="VehicleInfoSub"/> asset.
            /// </summary>
            Vehicle = 1 << 3,
            /// <summary>
            /// A <see cref="BuildingInfo"/> asset which contains
            /// a shader-using <see cref="PropInfo"/> asset.
            /// </summary>
            Container = 1 << 4,
        }
        /// <summary>
        /// Used to force visibility update during instantiation.
        /// </summary>
        private const bool FORCE_UPDATE = true;

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

        private readonly PrefabInfo m_prefab;

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
            IsVisible = true;
            CachedRenderDistance = RenderDistance(prefab.m_generatedInfo.m_size);
            Profile = new ShaderProfile(prefab.m_mesh.name);
            backup_lodHasDifferentShader = prefab.m_lodHasDifferentShader;
            backup_lodMissing = false;
            backup_meshColors = null;
            backup_InvFade = prefab.m_material.GetFloat("_InvFade");
            backup_lodRenderDistance = prefab.m_lodRenderDistance;
            backup_maxRenderDistance = prefab.m_maxRenderDistance;
            backup_maxLodDistance = 0f;
            backup_minLodDistance = 0f;
            backup_maxPropDistance = 0f;
            prefab.m_lodHasDifferentShader = false;
            prefab.m_material.SetFloat("_InvFade", Profile.Fade);
            prefab.m_lodRenderDistance = EMath.Max(prefab.m_lodRenderDistance, CachedRenderDistance);
            prefab.m_maxRenderDistance = EMath.Max(prefab.m_maxRenderDistance, CachedRenderDistance);
            SetVisible(Profile.IsAlwaysOn, FORCE_UPDATE);
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
            IsVisible = true;
            CachedRenderDistance = RenderDistance(prefab.m_generatedInfo.m_size);
            Profile = new ShaderProfile(prefab.m_mesh.name);
            backup_lodHasDifferentShader = prefab.m_lodHasDifferentShader;
            backup_lodMissing = prefab.m_lodMissing;
            backup_meshColors = (Color[])prefab.m_mesh.colors.Clone();
            backup_InvFade = prefab.m_material.GetFloat("_InvFade");
            backup_lodRenderDistance = 0f;
            backup_maxRenderDistance = 0f;
            backup_maxLodDistance = prefab.m_maxLodDistance;
            backup_minLodDistance = prefab.m_minLodDistance;
            backup_maxPropDistance = 0f;
            prefab.m_lodHasDifferentShader = false;
            prefab.m_lodMissing = true;
            prefab.m_material.SetFloat("_InvFade", Profile.Fade);
            SetAllColorWhite(prefab.m_mesh.colors);
            prefab.m_maxLodDistance = EMath.Max(prefab.m_maxLodDistance, CachedRenderDistance);
            prefab.m_minLodDistance = EMath.Max(prefab.m_minLodDistance, CachedRenderDistance);
            SetVisible(Profile.IsAlwaysOn, FORCE_UPDATE);
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
            IsContainer = true;
            IsVisible = true;
            CachedRenderDistance = CONTAINER_MAX_PROP_DISTANCE;
            Profile = new ShaderProfile(CONTAINER_BUILDING);
            backup_lodHasDifferentShader = false;
            backup_lodMissing = false;
            backup_meshColors = null;
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
            IsVisible = true;
            CachedRenderDistance = RenderDistance(prefab.m_generatedInfo.m_size);
            Profile = new ShaderProfile(prefab.m_mesh.name);
            backup_lodHasDifferentShader = prefab.m_lodHasDifferentShader;
            backup_lodMissing = false;
            backup_meshColors = prefab.m_mesh.colors.Clone() as Color[];
            backup_InvFade = prefab.m_material.GetFloat("_InvFade");
            backup_lodRenderDistance = 0f;
            backup_maxRenderDistance = 0f;
            backup_maxLodDistance = prefab.m_maxLodDistance;
            backup_minLodDistance = prefab.m_minLodDistance;
            backup_maxPropDistance = 0f;
            prefab.m_lodHasDifferentShader = false;
            prefab.m_material.SetFloat("_InvFade", Profile.Fade);
            SetAllColorWhite(prefab.m_mesh.colors);
            prefab.m_maxLodDistance = EMath.Max(prefab.m_maxLodDistance, CachedRenderDistance);
            prefab.m_minLodDistance = EMath.Max(prefab.m_minLodDistance, CachedRenderDistance);
            SetVisible(Profile.IsAlwaysOn, FORCE_UPDATE);
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
            IsVisible = true;
            CachedRenderDistance = RenderDistance(prefab.m_generatedInfo.m_size);
            Profile = new ShaderProfile(prefab.m_mesh.name);
            backup_lodHasDifferentShader = false;
            backup_lodMissing = false;
            backup_meshColors = prefab.m_mesh.colors.Clone() as Color[];
            backup_InvFade = prefab.m_material.GetFloat("_InvFade");
            backup_lodRenderDistance = prefab.m_lodRenderDistance;
            backup_maxRenderDistance = prefab.m_maxRenderDistance;
            backup_maxLodDistance = 0f;
            backup_minLodDistance = 0f;
            backup_maxPropDistance = 0f;
            prefab.m_material.SetFloat("_InvFade", Profile.Fade);
            SetAllColorWhite(prefab.m_mesh.colors);
            prefab.m_lodRenderDistance = EMath.Max(prefab.m_lodRenderDistance, CachedRenderDistance);
            prefab.m_maxRenderDistance = EMath.Max(prefab.m_maxRenderDistance, CachedRenderDistance);
            SetVisible(Profile.IsAlwaysOn, FORCE_UPDATE);
        }

        public ShaderProfile Profile { get; }

        /// <summary>
        /// Gets a value indicating whether this asset is just a container for another shader-using asset.
        /// </summary>
        public bool IsContainer { get; }

        /// <summary>
        /// Gets a value indicating whether the additive shader for the asset is currently visible.
        /// </summary>
        public bool IsVisible { get; private set; }

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
        /// Shows the additive shader for this asset.
        /// </summary>
        public void Show() => SetVisible(true);

        /// <summary>
        /// Hides the additive shader for this asset.
        /// </summary>
        public void Hide() => SetVisible(false);

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
        public void SetVisible(bool visible, bool force = false) {
            if (!IsContainer && (force || IsVisible != visible)) {
                IsVisible = visible;
                switch (TypeOfAsset) {
                case AssetType.Prop:
                    (m_prefab as PropInfo).m_material.SetFloat("_Intensity", visible ? Profile.Intensity : 0f);
                    break;
                case AssetType.Building:
                    (m_prefab as BuildingInfo).m_material.SetFloat("_Intensity", visible ? Profile.Intensity : 0f);
                    break;
                case AssetType.SubBuilding:
                    (m_prefab as BuildingInfoSub).m_material.SetFloat("_Intensity", visible ? Profile.Intensity : 0f);
                    break;
                case AssetType.Vehicle:
                    (m_prefab as VehicleInfoSub).m_material.SetFloat("_Intensity", visible ? Profile.Intensity : 0f);
                    break;
                }
            }
        }

        /// <summary>
        /// Additive shader doesn't work on LODs, so the render distance of the
        /// asset is increased, based on its size, to keep the effect visible for longer.
        /// </summary>
        /// <param name="size">The asset mesh size.</param>
        /// <returns>Returns the render distance applicable to the asset.</returns>
        private static float RenderDistance(Vector3 size) => (size.x + 30) * (size.y + 30) * (size.z + 30) * 0.1f;

        private static void SetAllColorWhite(Color[] colors) {
            Color white; white.r = 1; white.g = 1; white.b = 1; white.a = 1;
            int len = colors.Length;
            for (int i = 0; i < len; i++) {
                colors[i] = white;
            }
        }

        /// <summary>
        /// Rstores the original values from backups.
        /// </summary>
        public void RestoreOriginalSettings() {
            switch (TypeOfAsset) {
            case AssetType.Prop:
                PropInfo propInfo = m_prefab as PropInfo;
                propInfo.m_lodHasDifferentShader = backup_lodHasDifferentShader;
                propInfo.m_material.SetFloat("_InvFade", backup_InvFade);
                propInfo.m_lodRenderDistance = backup_lodRenderDistance;
                propInfo.m_maxRenderDistance = backup_maxRenderDistance;
                return;
            case AssetType.Building:
                BuildingInfo buildingInfo = m_prefab as BuildingInfo;
                buildingInfo.m_lodHasDifferentShader = backup_lodHasDifferentShader;
                buildingInfo.m_lodMissing = backup_lodMissing;
                buildingInfo.m_material.SetFloat("_InvFade", backup_InvFade);
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
                subBuilding.m_material.SetFloat("_InvFade", backup_InvFade);
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
