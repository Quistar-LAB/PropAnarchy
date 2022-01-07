using ColossalFramework;
using ColossalFramework.Math;
using ColossalFramework.UI;
using EManagersLib;
using PropAnarchy.PLT.Extensions;
using PropAnarchy.PLT.MathUtils;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace PropAnarchy.PLT {
    internal sealed class PropLineTool : ToolBase {
        internal const int LEFTMOUSEBUTTON = 0;
        internal const int RIGHTMOUSEBUTTON = 1;
        internal const float BEZIERTOLERANCE = 0.001f;
        internal const float POINT_DIAMETER = 3.00f;
        internal const float DOTSIZE = 0.40f;
        internal const float LINESIZE = 2.20f;
        internal const float HOVER_POINT_DIAMETER = 2.5f;
        internal const float HOVER_ANGLELOCUS_DIAMETER = 10f;
        internal const float HOVER_POINTDISTANCE_THRESHOLD = 1.5f;
        internal const float HOVER_CURVEDISTANCE_THRESHOLD = 1f;
        internal const float HOVER_ITEMWISE_CURVEDISTANCE_THRESHOLD = 12f;
        internal const float SPACING_MAX = 2000f;
        internal const float SPACING_MIN = 0.10f;
        internal const int ITEMWISE_INDEX = 0;
        internal const int ITEMWISE_FENCE_INDEX_START = 0;
        internal const int ITEMWISE_FENCE_INDEX_END = 1;
        internal const int MAX_ITEM_ARRAY_LENGTH = 1024;

        internal delegate T PropertyGetterHandler<T>();
        internal delegate T PropertySetterHandler<T>(T value);
        internal delegate void StandardSetterHandler<T>(T value);
        internal delegate void DrawCircleAPI(RenderManager.CameraInfo cameraInfo, Color color, Vector3 center, float size, float minY, float maxY, bool renderLimits, bool alphaBlend);
        internal delegate void DrawBezierAPI(RenderManager.CameraInfo cameraInfo, Color color, Bezier3 bezier, float size, float cutStart, float cutEnd, float minY, float maxY, bool renderLimits, bool alphaBlend);
        internal delegate void DrawSegmentAPI(RenderManager.CameraInfo cameraInfo, Color color, Segment3 segment, float size, float dashLen, float minY, float maxY, bool renderLimits, bool alphaBlend);
        internal delegate void DrawElbowAPI(RenderManager.CameraInfo cameraInfo, Color color, Segment3 segment1, Segment3 segment2, float size, float dashLen, float minY, float maxY, bool renderLimits, bool alphaBlend);
        internal delegate bool CreatePropAPI(out uint prop, ref Randomizer randomizer, PropInfo info, Vector3 position, float angle, bool single);
        internal delegate bool CreateTreeAPI(out uint tree, ref Randomizer randomizer, TreeInfo info, Vector3 position, bool single);
        internal delegate void DispatchEffectAPI(EffectInfo effect, InstanceID instance, EffectInfo.SpawnArea spawnArea, Vector3 velocity, float acceleration, float magnitude, AudioGroup audioGroup);
        internal delegate void GetHeightMappingAPI(Vector3 worldPos, out Texture _HeightMap, out Vector4 _HeightMapping, out Vector4 _SurfaceMapping);
        internal delegate float SampleHeightAPI(Vector3 worldPos);
        internal delegate AsyncAction AddActionAPI(Action action);
        private static DrawCircleAPI DrawCircle;
        private static DrawBezierAPI DrawBezier;
        private static DrawSegmentAPI DrawSegment;
        private static DrawElbowAPI DrawElbow;
        internal static GetHeightMappingAPI GetHeightMapping;
        internal static SampleHeightAPI SampleTerrainHeight;
        internal static AddActionAPI AddAction;
        internal static CreatePropAPI CreateProp;
        internal static CreateTreeAPI CreateTree;
        internal static DispatchEffectAPI DispatchEffect;
        internal static PropertySetterHandler<float> SetSpacingValue;
        internal static PropertyGetterHandler<float> GetSpacingValue;
        internal static PropertySetterHandler<float> SetAngleValue;
        internal static PropertyGetterHandler<float> GetAngleValue;
        internal static PropertySetterHandler<int> SetAngleMode;
        internal static StandardSetterHandler<bool> SetAngleModeState;
        internal static PropertySetterHandler<bool> SetAutoSpacing;

        internal enum LockingMode : int { Off = 0, Lock = 1 }
        internal enum ItemType : byte { Undefined, Tree, Prop }
        internal enum ControlMode : int { ItemWise, Spacing }
        internal enum AngleMode : int { Dynamic, Single }
        internal enum ActiveState : int {
            Undefined = 0,
            CreatePointFirst = 1,
            CreatePointSecond = 2,
            CreatePointThird = 3,
            LockIdle = 10,
            MovePointFirst = 11,
            MovePointSecond = 12,
            MovePointThird = 13,
            MoveSegment = 14,
            ChangeSpacing = 15,
            ChangeAngle = 16,
            ItemwiseLock = 30,
            MoveItemwiseItem = 31,
            MaxFillContinue = 40
        }
        internal enum HoverState : int {
            Unbound,
            SpacingLocus,
            AngleLocus,
            ControlPointFirst,
            ControlPointSecond,
            ControlPointThird,
            Curve,
            ItemwiseItem
        }

        internal static class ItemInfo {
            internal struct ItemData {
                internal uint m_itemID;
                internal Vector3 m_position;
                internal Vector3 m_fenceEndPoint;
                internal VectorXZ m_direction;
                internal VectorXZ m_offsetDirection;
                internal float m_t;
                internal float m_angle;
                internal bool m_isValidPlacement;
                internal CollisionType m_collisionType;
                internal Vector3 Position {
                    get => Settings.UseMeshCenterCorrection ? m_position + CenterCorrection : m_position;
                    set {
                        m_position = value;
                        m_position.y = SampleTerrainHeight(value);
                    }
                }
                private Vector3 CenterCorrection {
                    get {
                        if (m_prefab is PropInfo propInfo && propInfo.m_mesh is Mesh propMesh &&
                            propMesh.bounds.center.IsCenterAreaSignificant(propMesh.bounds.size, out VectorXZ centerCorrectionOrtho)) {
                            if (centerCorrectionOrtho.magnitude != 0f) {
                                //use negative angle since Unity is left-handed / CW rotation
                                return Quaternion.AngleAxis(-m_angle * Mathf.Rad2Deg, EMath.Vector3Up) * centerCorrectionOrtho;
                            }
                        } else if (m_prefab is TreeInfo treeInfo && treeInfo.m_mesh is Mesh treeMesh &&
                            treeMesh.bounds.center.IsCenterAreaSignificant(treeMesh.bounds.size, out centerCorrectionOrtho)) {
                            if (centerCorrectionOrtho.magnitude != 0f) {
                                //use negative angle since Unity is left-handed / CW rotation
                                return Quaternion.AngleAxis(-m_angle * Mathf.Rad2Deg, EMath.Vector3Up) * centerCorrectionOrtho;
                            }
                        }
                        return default;
                    }
                }
                internal bool UpdatePlacementError() {
                    if (PAModule.UsePropAnarchy) {
                        m_isValidPlacement = true;
                    } else {
                        if (m_prefab is PropInfo propInfo) {
                            m_collisionType = PlacementError.CheckAllCollisionsProp(m_position, propInfo);
                        } else if (m_prefab is TreeInfo treeInfo) {
                            m_collisionType = PlacementError.CheckAllCollisionsTree(m_position, treeInfo);
                        }
                        if (m_collisionType == CollisionType.None) {
                            m_isValidPlacement = true;
                        } else {
                            m_isValidPlacement = false;
                        }
                    }
                    return m_isValidPlacement;
                }
                internal void RenderItem(RenderManager.CameraInfo cameraInfo) {
                    if (m_isValidPlacement) {
                        if (m_prefab is PropInfo propInfo) {
                            InstanceID id = default;
                            if (propInfo.m_requireHeightMap) {
                                GetHeightMapping(m_position, out Texture heightMap, out Vector4 heightMapping, out Vector4 surfaceMapping);
                                EPropInstance.RenderInstance(cameraInfo, propInfo, id, m_position, m_scale, m_angle, m_color, RenderManager.DefaultColorLocation, true, heightMap, heightMapping, surfaceMapping);
                            } else {
                                EPropInstance.RenderInstance(cameraInfo, propInfo, id, m_position, m_scale, m_angle, m_color, RenderManager.DefaultColorLocation, true);
                            }
                        } else if (m_prefab is TreeInfo treeInfo) {
                            TreeInstance.RenderInstance(cameraInfo, treeInfo, m_position, m_scale, m_brightness, RenderManager.DefaultColorLocation);
                        }
                    }
                }
                internal void SetDirectionsXZ(VectorXZ itemDirection) {
                    itemDirection.Normalize();
                    m_direction = itemDirection;
                    m_offsetDirection = new VectorXZ(itemDirection.z, itemDirection.x);
                }
            }
#pragma warning disable IDE0032 // Use auto property
            private static readonly ItemData[] m_datas = new ItemData[MAX_ITEM_ARRAY_LENGTH + 1];
#pragma warning restore IDE0032 // Use auto property
            private static int m_itemCount;
            private static PrefabInfo m_prefab;
            private static VectorXZ m_itemSize;
            private static float m_itemMaxWidth;
            private static float m_scale;
            private static Color m_color;
            private static float m_brightness;

            private static float CalcDefaultSpacing(ItemType itemType, float itemWidth) {
                float res = 0f;
                if (FenceMode) {
                    res = EMath.Clamp(EMath.RoundToInt(itemWidth), 0f, SPACING_MAX);
                } else {
                    float scaleFactor = 1f;
                    switch (itemType) {
                    case ItemType.Prop:
                        if (itemWidth < 4f) {
                            scaleFactor = 2.2f;
                        }
                        res = EMath.Clamp(itemWidth * scaleFactor, 0f, SPACING_MAX);
                        if (res < 2f) return 2f;
                        break;
                    case ItemType.Tree:
                        if (itemWidth > 7f) {
                            scaleFactor = 1.1f;
                        } else {
                            scaleFactor = 2f;
                        }
                        res = EMath.Clamp(itemWidth * scaleFactor, 0f, SPACING_MAX);
                        break;
                    }
                }
                return res != 0f ? res : 8f;
            }

            internal static PrefabInfo Prefab {
                get => m_prefab;
                set {
                    m_prefab = value;
                    if (value is PropInfo propInfo) {
                        Type = ItemType.Prop;
                        m_itemSize.x = propInfo.m_mesh.bounds.extents.x * 2f;
                        m_itemSize.z = propInfo.m_mesh.bounds.extents.z * 2f;
                        m_itemMaxWidth = m_itemSize.x > m_itemSize.z ? m_itemSize.x : m_itemSize.z;
                        if (Settings.AutoDefaultSpacing) {
                            Spacing = CalcDefaultSpacing(ItemType.Prop, m_itemMaxWidth);
                        }
                        Randomizer randomizer = new Randomizer(EPropManager.m_props.NextFreeItem());
                        int random = randomizer.Int32(10000u);
                        m_scale = propInfo.m_minScale + random * (propInfo.m_maxScale - propInfo.m_minScale) * 0.0001f;
                        m_color = propInfo.GetColor(ref randomizer);
                    } else if (value is TreeInfo treeInfo) {
                        Type = ItemType.Tree;
                        m_itemSize.x = treeInfo.m_mesh.bounds.extents.x * 2f;
                        m_itemSize.z = treeInfo.m_mesh.bounds.extents.z * 2f;
                        m_itemMaxWidth = m_itemSize.x > m_itemSize.z ? m_itemSize.x : m_itemSize.z;
                        if (Settings.AutoDefaultSpacing) {
                            Spacing = CalcDefaultSpacing(ItemType.Prop, m_itemMaxWidth);
                        }
                        int random = new Randomizer(Singleton<TreeManager>.instance.m_trees.NextFreeItem()).Int32(10000u);
                        m_scale = treeInfo.m_minScale + random * (treeInfo.m_maxScale - treeInfo.m_minScale) * 0.0001f;
                        m_brightness = treeInfo.m_minBrightness + random * (treeInfo.m_maxBrightness - treeInfo.m_minBrightness) * 0.0001f;
                    }
                }
            }

            internal static ItemType Type { get; set; }

            internal static float Width => m_itemSize.x;

            internal static float Height => m_itemSize.z;

            internal static ItemData[] Datas => m_datas;

            internal static int Count {
                get {
                    if (Settings.ControlMode == ControlMode.ItemWise) {
                        m_itemCount = 1;
                        return 1;
                    }
                    return m_itemCount;
                }
                set => m_itemCount = EMath.Clamp(value, 0, MAX_ITEM_ARRAY_LENGTH);
            }

            internal static float Spacing {
                get => GetSpacingValue();
                set => SetSpacingValue(EMath.Clamp(value, 0.1f, 1000f));
            }

            internal static float Angle {
                get => GetAngleValue() * Mathf.Deg2Rad;
                set => SetAngleValue((value % (2f * Mathf.PI)) * Mathf.Rad2Deg);
            }

            internal static bool FenceMode {
                get => Settings.FenceMode;
                set {
                    Settings.FenceMode = value;
                    Angle = 0f;
                    if (value) {
                        SetAngleMode((int)AngleMode.Dynamic);
                        SetAngleModeState(false);
                        SetAutoSpacing(true);
                        SetDefaultSpacing();
                    } else {
                        SetAngleModeState(true);
                    }
                }
            }

            internal static float AngleFlip180 => Settings.AngleFlip180 ? Mathf.PI : 0f;

            internal static float LockedAngle { get; set; }

            internal static float LockedItemwiseT { get; set; }

            internal static float LockedSpacing { get; set; }

            internal static VectorXZ LockedDirection { get; set; }

            internal static float HoverAngle => m_datas[HoverItemAngleCenterIndex].m_angle - ((m_itemSize.z > m_itemSize.x ? (Mathf.PI / 2f) : 0f) + AngleFlip180) + Mathf.PI;

            internal static float HoverItemwiseT { get; set; }

            internal static void SetDefaultSpacing() => Spacing = CalcDefaultSpacing(Type, m_itemMaxWidth);

            internal static bool FinalizePlacement(bool continueDrawing, bool isCopyPlacing) {
                int itemCount = m_itemCount;
                if (itemCount > 0) {
                    ItemData[] itemDatas = m_datas;
                    Randomizer randomizer;
                    if (Prefab is PropInfo propInfo) {
                        Array32<EPropInstance> props = EPropManager.m_props;
                        ControlMode controlMode = Settings.ControlMode;
                        for (int i = 0; i < itemCount; i++) {
                            if (itemDatas[i].m_isValidPlacement) {
                                randomizer = new Randomizer(props.NextFreeItem());
                                PropInfo newPropInfo = controlMode == ControlMode.ItemWise && i == ITEMWISE_INDEX ? propInfo.GetVariation(ref randomizer) : propInfo;
                                Vector3 position = itemDatas[i].Position;
                                if (Singleton<PropManager>.instance.CreateProp(out uint propID, ref randomizer, newPropInfo, position, itemDatas[i].m_angle, true)) {
                                    itemDatas[i].m_itemID = propID;
                                    DispatchPlacementEffect(position, false);
                                }
                            }
                        }
                    } else if (Prefab is TreeInfo treeInfo) {
                        Array32<TreeInstance> trees = Singleton<TreeManager>.instance.m_trees;
                        for (int i = 0; i < itemCount; i++) {
                            if (itemDatas[i].m_isValidPlacement) {
                                randomizer = new Randomizer(trees.NextFreeItem());
                                TreeInfo newTreeInfo = Settings.ControlMode == ControlMode.ItemWise && i == ITEMWISE_INDEX ? treeInfo.GetVariation(ref randomizer) : treeInfo;
                                Vector3 position = itemDatas[i].Position;
                                if (Singleton<TreeManager>.instance.CreateTree(out uint treeID, ref randomizer, newTreeInfo, position, true)) {
                                    itemDatas[i].m_itemID = treeID;
                                    DispatchPlacementEffect(position, false);
                                }
                            }
                        }
                    }
                    if (!isCopyPlacing) {
                        m_itemCount = 0;
                        SegmentState.FinalizeForPlacement(continueDrawing);
                    }
                    UndoManager.AddEntry(itemCount, itemDatas, FenceMode);
                    return true;
                }
                return false;
            }
        }

        internal static ToolBar m_toolbar;
        internal static OptionPanel m_optionPanel;
        internal static TreeTool m_treeTool;
        internal static PropTool m_propTool;
        internal static bool m_positionChanging;
        internal static bool m_mouseRayValid;
        private static float m_mouseRayLength;
        private static Ray m_mouseRay;
        internal static Vector3 m_mousePosition;
        internal static Vector3 m_cachedPosition;
        internal static Vector3 m_lockedBackupCachedPosition;
        internal static bool m_keyboardCtrlDown;
        internal static bool m_keyboardAltDown;
        internal static bool m_isCopyPlacing;
        private static EffectInfo m_bulldozeEffect;
        private static EffectInfo m_placementEffect;
        private static AudioGroup m_defaultAudioGroup;

        internal static int HoverItemPositionIndex => Settings.ControlMode == ControlMode.ItemWise ? (ItemInfo.FenceMode ? ITEMWISE_FENCE_INDEX_START : ITEMWISE_INDEX) : 1;
        internal static int HoverItemAngleCenterIndex => Settings.ControlMode == ControlMode.ItemWise ? ITEMWISE_INDEX : 1;

        internal static void ResetPLT() {
            m_positionChanging = false;
            m_mouseRayValid = false;
            ControlPoint.Reset();
        }

        protected override void Awake() {
            base.Awake();
            PlacementError.Initialize();
            DrawCircle = Singleton<RenderManager>.instance.OverlayEffect.DrawCircle;
            DrawBezier = Singleton<RenderManager>.instance.OverlayEffect.DrawBezier;
            DrawSegment = Singleton<RenderManager>.instance.OverlayEffect.DrawSegment;
            DrawElbow = Singleton<RenderManager>.instance.OverlayEffect.DrawSegment;
            GetHeightMapping = Singleton<TerrainManager>.instance.GetHeightMapping;
            SampleTerrainHeight = Singleton<TerrainManager>.instance.SampleDetailHeight;
            AddAction = Singleton<SimulationManager>.instance.AddAction;
            CreateProp = Singleton<PropManager>.instance.CreateProp;
            CreateTree = Singleton<TreeManager>.instance.CreateTree;
            DispatchEffect = Singleton<EffectManager>.instance.DispatchEffect;
            m_bulldozeEffect = Singleton<TreeManager>.instance.m_properties.m_bulldozeEffect;
            m_placementEffect = Singleton<TreeManager>.instance.m_properties.m_placementEffect;
            m_defaultAudioGroup = Singleton<AudioManager>.instance.DefaultGroup;
            m_treeTool = ToolsModifierControl.GetTool<TreeTool>();
            m_propTool = ToolsModifierControl.GetTool<PropTool>();
            ResetPLT();
        }

        protected override void OnEnable() {
            base.OnEnable();
            ResetPLT();
        }

        protected override void OnDisable() {
            base.OnDisable();
            ResetPLT();
        }

        internal static void UpdateCachedPosition(bool ignorePosChangingCondition) {
            m_positionChanging = ignorePosChangingCondition ||
                                 (m_cachedPosition - m_mousePosition).sqrMagnitude > (BEZIERTOLERANCE * BEZIERTOLERANCE);
            m_cachedPosition = m_mousePosition;
        }

        public static void DispatchPlacementEffect(Vector3 position, bool isBulldozeEffect) {
            InstanceID id = default;
            EffectInfo effectInfo = isBulldozeEffect ? m_bulldozeEffect : m_placementEffect;
            EffectInfo.SpawnArea spawnArea = new EffectInfo.SpawnArea(position, EMath.Vector3Up, 1f);
            DispatchEffect(effectInfo, id, spawnArea, default, 0f, 1f, m_defaultAudioGroup);
        }

        public override void RenderGeometry(RenderManager.CameraInfo cameraInfo) {
            DrawMode.CurActiveMode.OnRenderGeometry(cameraInfo);
        }

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo) {
            base.RenderOverlay(cameraInfo);
            Color mainCurveColor = Settings.m_PLTColor_locked;
            Color lockIdleColor = Settings.m_PLTColor_locked;
            Color curveWarningColor = Settings.m_PLTColor_curveWarning;
            Color itemwiseLockColor = Settings.m_PLTColor_ItemwiseLock;
            Color copyPlaceColor = DrawMode.CurActiveMode.IsActiveStateAnItemRenderState() && SegmentState.IsReadyForMaxContinue ? Settings.m_PLTColor_MaxFillContinue : Settings.m_PLTColor_copyPlace;
            Color createPointColor = Settings.ControlMode == ControlMode.ItemWise ? Settings.m_PLTColor_ItemwiseLock : Settings.m_PLTColor_default;

            if (m_keyboardAltDown) {
                createPointColor = copyPlaceColor; ;
            } else if (m_keyboardCtrlDown) {
                createPointColor = Settings.m_PLTColor_locked;
                if (Settings.ShowUndoPreviews) {
                    UndoManager.RenderLatestEntryCircles(cameraInfo, Settings.m_PLTColor_undoItemOverlay);
                }
            }
            switch (DrawMode.CurActiveState) {
            case ActiveState.CreatePointFirst: //creating first control point
                if (!m_toolController.IsInsideUI) {
                    RenderCircle(cameraInfo, m_cachedPosition, POINT_DIAMETER, createPointColor, false, false);
                    RenderCircle(cameraInfo, m_cachedPosition, DOTSIZE, createPointColor, false, true);
                }
                break;
            case ActiveState.CreatePointSecond: //creating second control point
                if (!DrawMode.CurActiveMode.OnRenderOverlay(cameraInfo, ActiveState.CreatePointSecond, createPointColor, curveWarningColor, copyPlaceColor)) {
                    goto case ActiveState.CreatePointFirst;
                }
                break;
            case ActiveState.CreatePointThird: //creating third control point
                if (!DrawMode.CurActiveMode.OnRenderOverlay(cameraInfo, ActiveState.CreatePointThird, createPointColor, curveWarningColor, copyPlaceColor)) {
                    goto case ActiveState.CreatePointSecond;
                }
                break;
            case ActiveState.MovePointFirst:
            case ActiveState.MovePointSecond:
            case ActiveState.MovePointThird:
            case ActiveState.MoveSegment:
            case ActiveState.ChangeSpacing:
            case ActiveState.ChangeAngle:
            case ActiveState.LockIdle:
            case ActiveState.ItemwiseLock:
            case ActiveState.MoveItemwiseItem:
                lockIdleColor = DrawMode.CurHoverState == HoverState.SpacingLocus ? Settings.m_PLTColor_lockedStrong : lockIdleColor;
                if (m_keyboardAltDown) {
                    mainCurveColor = copyPlaceColor;
                } else {
                    if (Settings.ControlMode != ControlMode.ItemWise && (SegmentState.IsReadyForMaxContinue || SegmentState.IsMaxFillContinue)) {
                        switch (DrawMode.CurActiveState) {
                        case ActiveState.LockIdle:
                        case ActiveState.MaxFillContinue:
                            RenderMaxFillContinueMarkers(cameraInfo, ItemInfo.Datas);
                            break;
                        }
                    }
                    if (m_keyboardCtrlDown) {
                        if (Settings.ControlMode == ControlMode.ItemWise) {
                            if (DrawMode.CurActiveState == ActiveState.ItemwiseLock) {
                                mainCurveColor = lockIdleColor;
                            } else if (DrawMode.CurActiveState == ActiveState.LockIdle) {
                                mainCurveColor = itemwiseLockColor;
                            }
                        } else { //not in itemwise mode
                            mainCurveColor = createPointColor;
                        }
                    } else {
                        if (Settings.ControlMode == ControlMode.ItemWise) {
                            if (DrawMode.CurActiveState == ActiveState.ItemwiseLock) {
                                mainCurveColor = itemwiseLockColor;
                            } else if (DrawMode.CurActiveState == ActiveState.LockIdle) {
                                mainCurveColor = lockIdleColor;
                            }
                        } else { //not in itemwise mode
                            mainCurveColor = lockIdleColor;
                        }
                        if (DrawMode.CurHoverState == HoverState.Curve && DrawMode.CurActiveState == ActiveState.LockIdle) {
                            mainCurveColor = m_keyboardAltDown ? Settings.m_PLTColor_copyPlaceHighlight : Settings.m_PLTColor_lockedHighlight;
                        }
                    }
                }
                RenderHoverObjectOverlays(cameraInfo);
                DrawMode.CurActiveMode.RenderLines(cameraInfo, mainCurveColor, curveWarningColor);
                ControlPoint.PointInfo[] controlPoints = ControlPoint.m_controlPoints;
                RenderCircle(cameraInfo, controlPoints[0].m_position, DOTSIZE, lockIdleColor, false, true);
                RenderCircle(cameraInfo, controlPoints[1].m_position, DOTSIZE, lockIdleColor, false, true);
                RenderCircle(cameraInfo, controlPoints[2].m_position, DOTSIZE, lockIdleColor, false, true);
                //show adjustment circles
                break;
            case ActiveState.MaxFillContinue:
                DrawMode.CurActiveMode.OnRenderOverlay(cameraInfo, ActiveState.MaxFillContinue, createPointColor, curveWarningColor, copyPlaceColor);
                break;
            }
            RenderPlacementErrorOverlays(cameraInfo);
        }

        public override void SimulationStep() {
            RaycastInput input = new RaycastInput(m_mouseRay, m_mouseRayLength);
            if (m_mouseRayValid && EToolBase.RayCast(input, out EToolBase.RaycastOutput raycastOutput) && !raycastOutput.m_currentEditObject) {
                m_mousePosition = raycastOutput.m_hitPos;
                DrawMode.CurActiveMode.OnSimulationStep(raycastOutput.m_hitPos);
            }
        }

        protected override void OnToolGUI(Event e) {
            bool ctrlDown = (e.modifiers & EventModifiers.Control) == EventModifiers.Control;
            bool altDown = (e.modifiers & EventModifiers.Alt) == EventModifiers.Alt;
            m_keyboardCtrlDown = ctrlDown;
            m_keyboardAltDown = altDown;
            m_isCopyPlacing = altDown;
            switch (e.type) {
            case EventType.KeyDown:
                switch (e.keyCode) {
                case KeyCode.Escape: return;
                case KeyCode.Z:
                    if (ctrlDown) {
                        UndoManager.UndoLatestEntry();
                    }
                    break;
                }
                break;
            }
            DrawMode.CurActiveMode.OnToolGUI(e, m_toolController.IsInsideUI);
        }

        protected override void OnToolLateUpdate() {
            m_mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            m_mouseRayLength = Camera.main.farClipPlane;
            m_mouseRayValid = (!m_toolController.IsInsideUI && Cursor.visible);
            UpdateCachedPosition(false);
            DrawMode.CurActiveMode.OnToolLateUpdate();
        }

        protected override void OnToolUpdate() {
            if (DrawMode.CurrentMode != DrawMode.Single) {
                switch (ItemInfo.Type) {
                case ItemType.Tree:
                    PrefabInfo prefab = ItemInfo.Prefab;
                    TreeInfo treeInfo = m_treeTool.m_prefab;
                    if (!(treeInfo is null) && treeInfo != prefab) {
                        PAModule.PALog($"Prefab: {treeInfo}");
                        ItemInfo.Prefab = treeInfo;
                    }
                    break;
                case ItemType.Prop:
                    ItemInfo.Prefab = m_propTool.m_prefab;
                    prefab = ItemInfo.Prefab;
                    PropInfo propInfo = m_propTool.m_prefab;
                    if (!(propInfo is null) && propInfo != prefab) {
                        PAModule.PALog($"Prefab: {propInfo}");
                        ItemInfo.Prefab = propInfo;
                    }
                    break;
                }
            }
        }

        private void RenderMaxFillContinueMarkers(RenderManager.CameraInfo cameraInfo, ItemInfo.ItemData[] itemDatas) {
            const float radius = 6f;
            int lastItem = ItemInfo.Count - 1;
            Color maxFillContinueColor = Settings.m_PLTColor_MaxFillContinue;
            //initial item
            Vector3 initialItemPosition = Settings.FenceMode ? itemDatas[0].m_fenceEndPoint : itemDatas[0].Position;
            RenderSegment(cameraInfo, new Segment3(new Vector3(initialItemPosition.x - itemDatas[0].m_offsetDirection.x * radius,
                                                               initialItemPosition.y, initialItemPosition.z - itemDatas[0].m_offsetDirection.z * radius),
                                                   new Vector3(initialItemPosition.x + itemDatas[0].m_offsetDirection.x * radius,
                                                               initialItemPosition.y, initialItemPosition.z + itemDatas[0].m_offsetDirection.z * radius)),
                                                   0.25f, 0f, maxFillContinueColor, false, true);
            //final item
            Vector3 finalItemPosition = Settings.FenceMode ? itemDatas[lastItem].m_fenceEndPoint : itemDatas[lastItem].Position;
            RenderCircle(cameraInfo, finalItemPosition, 0.5f, maxFillContinueColor, false, true);
            RenderCircle(cameraInfo, finalItemPosition, radius, maxFillContinueColor, false, true);
            RenderSegment(cameraInfo, new Segment3(new Vector3(finalItemPosition.x - itemDatas[lastItem].m_offsetDirection.x * radius,
                                                               finalItemPosition.y, finalItemPosition.z - itemDatas[lastItem].m_offsetDirection.z * radius),
                                                   new Vector3(finalItemPosition.x + itemDatas[lastItem].m_offsetDirection.x * radius,
                                                               finalItemPosition.y, finalItemPosition.z + itemDatas[lastItem].m_offsetDirection.z * radius)),
                                                   0.25f, 0f, maxFillContinueColor, false, true);
            //mouse indicators
            maxFillContinueColor.a *= 0.40f;
            RenderSegment(cameraInfo, new Segment3(m_mousePosition, initialItemPosition), 0.05f, 3.00f, maxFillContinueColor, false, true);
            RenderSegment(cameraInfo, new Segment3(m_mousePosition, finalItemPosition), 0.05f, 3.00f, maxFillContinueColor, false, true);
        }

        private static void RenderHoverObjectOverlays(RenderManager.CameraInfo cameraInfo) {
            if (ItemInfo.Count >= (ItemInfo.FenceMode ? 1 : 2) || Settings.ControlMode == ControlMode.ItemWise) {
                switch (DrawMode.CurActiveState) {
                case ActiveState.Undefined:
                case ActiveState.CreatePointFirst:
                case ActiveState.CreatePointSecond:
                case ActiveState.CreatePointThird:
                case ActiveState.MaxFillContinue:
                    return;
                }
                //setup highlight colors
                Color32 baseColor, highlightColor;
                Color32 lockIdleColor = Settings.m_PLTColor_locked;
                if (m_keyboardAltDown) {
                    baseColor = Settings.m_PLTColor_hoverCopyPlace;
                    highlightColor = Settings.m_PLTColor_copyPlaceHighlight;
                } else {
                    baseColor = Settings.m_PLTColor_hoverBase;
                    highlightColor = Settings.m_PLTColor_lockedHighlight;
                }
                ControlPoint.PointInfo[] controlPoints = ControlPoint.m_controlPoints;
                ItemInfo.ItemData[] itemDatas = ItemInfo.Datas;
                switch (DrawMode.CurActiveState) {
                case ActiveState.LockIdle:
                    HoverState hoverState = DrawMode.CurHoverState;
                    Color angleColor = baseColor;
                    switch (hoverState) {
                    case HoverState.AngleLocus:
                        angleColor = highlightColor;
                        break;
                    case HoverState.SpacingLocus:
                        // spacing fill indicator
                        DrawMode.CurActiveMode.RenderProgressiveSpacingFill(cameraInfo, ItemInfo.Spacing, LINESIZE, 0.20f, Color.Lerp(highlightColor, lockIdleColor, 0.50f), false, true);
                        break;
                    }
                    RenderCircle(cameraInfo, controlPoints[0].m_position, HOVER_POINT_DIAMETER, hoverState == HoverState.ControlPointFirst ? highlightColor : baseColor, false, false);
                    RenderCircle(cameraInfo, controlPoints[1].m_position, HOVER_POINT_DIAMETER, hoverState == HoverState.ControlPointSecond ? highlightColor : baseColor, false, false);
                    RenderCircle(cameraInfo, ItemInfo.FenceMode ? itemDatas[HoverItemPositionIndex].m_fenceEndPoint : itemDatas[HoverItemPositionIndex].Position, HOVER_POINT_DIAMETER,
                        hoverState == HoverState.SpacingLocus ? highlightColor : baseColor, false, false);
                    switch (DrawMode.CurrentMode) {
                    case DrawMode.Curved:
                    case DrawMode.Freeform:
                        RenderCircle(cameraInfo, controlPoints[2].m_position, HOVER_POINT_DIAMETER, hoverState == HoverState.ControlPointThird ? highlightColor : baseColor, false, false);
                        break;
                    }

                    VectorXZ anglePos;
                    VectorXZ angleCenter;
                    Color32 blendColor;
                    if (ItemInfo.Prefab is PropInfo) {
                        angleCenter = itemDatas[HoverItemAngleCenterIndex].Position;
                        anglePos = CircleXZ.Position3FromAngleXZ(angleCenter, HOVER_ANGLELOCUS_DIAMETER, ItemInfo.HoverAngle);
                        RenderCircle(cameraInfo, anglePos, HOVER_POINT_DIAMETER, angleColor, false, false);
                        blendColor = Color.Lerp(baseColor, angleColor, 0.50f);
                        blendColor.a = 88;
                        RenderCircle(cameraInfo, angleCenter, HOVER_ANGLELOCUS_DIAMETER * 2f, blendColor, false, true);
                        RenderLine(cameraInfo, new Segment3(angleCenter, anglePos), 0.05f, 0.50f, blendColor, false, true);
                    }
                    break;
                case ActiveState.MovePointFirst:
                    RenderCircle(cameraInfo, controlPoints[0].m_position, HOVER_POINT_DIAMETER, highlightColor, false, false);
                    break;
                case ActiveState.MovePointSecond:
                    RenderCircle(cameraInfo, controlPoints[1].m_position, HOVER_POINT_DIAMETER, highlightColor, false, false);
                    break;
                case ActiveState.MovePointThird:
                    switch (DrawMode.CurrentMode) {
                    case DrawMode.Curved:
                    case DrawMode.Freeform:
                        RenderCircle(cameraInfo, controlPoints[2].m_position, HOVER_POINT_DIAMETER, highlightColor, false, false);
                        break;
                    }
                    break;
                case ActiveState.ChangeSpacing:
                    RenderCircle(cameraInfo, ItemInfo.FenceMode ? itemDatas[HoverItemPositionIndex].m_fenceEndPoint : itemDatas[HoverItemPositionIndex].Position, HOVER_POINT_DIAMETER, highlightColor, false, false);
                    if (ItemInfo.FenceMode) {
                        RenderLine(cameraInfo, new Segment3(itemDatas[0].m_fenceEndPoint, itemDatas[1].m_fenceEndPoint), 0.05f, 0.50f, Color.Lerp(baseColor, highlightColor, 0.50f), false, true);
                    } else {
                        DrawMode.CurActiveMode.RenderProgressiveSpacingFill(cameraInfo, ItemInfo.Spacing, LINESIZE, 0.20f, highlightColor, false, true);
                    }
                    break;
                case ActiveState.ChangeAngle:
                    angleCenter = itemDatas[HoverItemAngleCenterIndex].Position;
                    anglePos = CircleXZ.Position3FromAngleXZ(angleCenter, HOVER_ANGLELOCUS_DIAMETER, ItemInfo.HoverAngle);
                    blendColor = Color.Lerp(baseColor, highlightColor, 0.50f);
                    blendColor.a = 88;
                    RenderCircle(cameraInfo, anglePos, HOVER_POINT_DIAMETER, highlightColor, false, false);
                    RenderCircle(cameraInfo, angleCenter, HOVER_ANGLELOCUS_DIAMETER * 2f, blendColor, false, true);
                    RenderLine(cameraInfo, new Segment3(angleCenter, anglePos), 0.05f, 0.50f, blendColor, false, true);
                    break;
                case ActiveState.MoveItemwiseItem:
                    RenderCircle(cameraInfo, ItemInfo.FenceMode ? itemDatas[HoverItemPositionIndex].m_fenceEndPoint : itemDatas[HoverItemPositionIndex].Position, HOVER_POINT_DIAMETER, highlightColor, false, false);
                    break;
                }
            }
        }

        private static void RenderPlacementErrorOverlays(RenderManager.CameraInfo cameraInfo) {
            int itemCount = ItemInfo.Count;
            if ((!SegmentState.AllItemsValid) && itemCount > 0 && DrawMode.CurActiveMode.IsActiveStateAnItemRenderState()) {
                Color32 blockedColor = new Color32(219, 192, 82, 200);
                Color32 invalidPlacementColor = PAModule.UsePropAnarchy ? new Color32(193, 78, 72, 50) : new Color32(193, 78, 72, 200);
                float radius;
                if (ItemInfo.Prefab is PropInfo) {
                    PropInfo propInfo = ItemInfo.Prefab as PropInfo;
                    radius = EMath.Max(propInfo.m_generatedInfo.m_size.x, propInfo.m_generatedInfo.m_size.z) * EMath.Max(propInfo.m_maxScale, propInfo.m_minScale);
                } else if (ItemInfo.Prefab is TreeInfo) {
                    TreeInfo treeInfo = ItemInfo.Prefab as TreeInfo;
                    radius = EMath.Max(treeInfo.m_generatedInfo.m_size.x, treeInfo.m_generatedInfo.m_size.z) * EMath.Max(treeInfo.m_maxScale, treeInfo.m_minScale);
                } else {
                    return;
                }
                ItemInfo.ItemData[] itemDatas = ItemInfo.Datas;
                for (int i = 0; i < itemCount; i++) {
                    if (!itemDatas[i].m_isValidPlacement) {
                        Vector3 itemPos = itemDatas[i].Position;
                        if (itemDatas[i].m_collisionType == CollisionType.Blocked) {
                            RenderCircle(cameraInfo, itemPos, DOTSIZE, blockedColor, false, false);
                            RenderCircle(cameraInfo, itemPos, 2f, blockedColor, false, false);
                            RenderCircle(cameraInfo, itemPos, radius, blockedColor, false, true);
                        } else {
                            RenderCircle(cameraInfo, itemPos, DOTSIZE, invalidPlacementColor, false, false);
                            RenderCircle(cameraInfo, itemPos, 2f, invalidPlacementColor, false, false);
                            RenderCircle(cameraInfo, itemPos, radius, invalidPlacementColor, false, true);
                        }
                    }
                }
            }
        }

        internal static void RenderCircle(RenderManager.CameraInfo cameraInfo, Vector3 position, float size, Color color, bool renderLimits, bool alphaBlend) {
            Singleton<ToolManager>.instance.m_drawCallData.m_overlayCalls++;
            DrawCircle(cameraInfo, color, position, size, -1f, 1280f, renderLimits, alphaBlend);
        }

        internal static void RenderLine(RenderManager.CameraInfo cameraInfo, in Segment3 segment, float size, float dashLength, Color color, bool renderLimits, bool alphaBlend) {
            Singleton<ToolManager>.instance.m_drawCallData.m_overlayCalls++;
            DrawSegment(cameraInfo, color, segment, size, dashLength, -1f, 1280f, renderLimits, alphaBlend);
        }

        internal static void RenderSegment(RenderManager.CameraInfo cameraInfo, in Segment3 segment, float size, float dashLength, Color color, bool renderLimits, bool alphaBlend) {
            Singleton<ToolManager>.instance.m_drawCallData.m_overlayCalls++;
            DrawSegment(cameraInfo, color, segment, size, dashLength, -1f, 1280f, renderLimits, alphaBlend);
        }

        internal static void RenderElbow(RenderManager.CameraInfo cameraInfo, in Segment3 segment1, in Segment3 segment2, float size, float dashLength, Color color, bool renderLimits, bool alphaBlend) {
            Singleton<ToolManager>.instance.m_drawCallData.m_overlayCalls++;
            DrawElbow(cameraInfo, color, segment1, segment2, size, dashLength, -1f, 1280f, renderLimits, alphaBlend);
        }

        internal static void RenderBezier(RenderManager.CameraInfo cameraInfo, in Bezier3 bezier, float size, Color color, bool renderLimits, bool alphaBlend) {
            Singleton<ToolManager>.instance.m_drawCallData.m_overlayCalls++;
            DrawBezier(cameraInfo, color, bezier, size, -100000f, 100000f, -1f, 1280f, renderLimits, alphaBlend);
        }

        internal static void RenderMainCircle(RenderManager.CameraInfo cameraInfo, in CircleXZ circle, float size, Color color, bool renderLimits, bool alphaBlend) {
            if (circle.m_radius > 0f) {
                Singleton<ToolManager>.instance.m_drawCallData.m_overlayCalls += 3;
                DrawSegment(cameraInfo, color, new Segment3(circle.m_center, circle.Position(0f)), 0.05f, 1.00f, -1f, 1280f, false, true);
                DrawCircle(cameraInfo, color, circle.m_center, circle.m_radius * 2f + size, -1f, 1280f, renderLimits, alphaBlend);
                DrawCircle(cameraInfo, color, circle.m_center, circle.m_radius * 2f - size, -1f, 1280f, renderLimits, alphaBlend);
            } else {
                Singleton<ToolManager>.instance.m_drawCallData.m_overlayCalls += 2;
                DrawCircle(cameraInfo, color, circle.m_center, circle.m_radius * 2f + size, -1f, 1280f, renderLimits, alphaBlend);
                DrawCircle(cameraInfo, color, circle.m_center, circle.m_radius * 2f - size, -1f, 1280f, renderLimits, alphaBlend);
            }
        }

        internal static void InitializedPLT() {
            ToolController toolController = FindObjectOfType<ToolController>();
            try {
                PropLineTool propLineTool = toolController.gameObject.GetComponent<PropLineTool>();
                if (propLineTool is null) {
                    propLineTool = toolController.gameObject.AddComponent<PropLineTool>();
                }
                UIView mainView = UIView.GetAView();
                m_toolbar = mainView.AddUIComponent(typeof(ToolBar)) as ToolBar;
                m_optionPanel = mainView.AddUIComponent(typeof(OptionPanel)) as OptionPanel;
                // because of shared textures, make sure to initialize toolbar first before optionpanel
                FieldInfo toolsField = typeof(ToolController).GetField("m_tools", BindingFlags.Instance | BindingFlags.NonPublic);
                ToolBase[] tools = toolsField.GetValue(toolController) as ToolBase[];
                int toolLength = tools.Length;
                Array.Resize(ref tools, toolLength + 1);
                tools[toolLength] = propLineTool;
                toolsField.SetValue(toolController, tools);
                PropLineTool plt = ToolsModifierControl.GetTool<PropLineTool>();
                if (plt is null) {
                    Dictionary<Type, ToolBase> toolsDict = typeof(ToolsModifierControl).GetField("m_Tools", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null) as Dictionary<Type, ToolBase>;
                    toolsDict.Add(typeof(PropLineTool), propLineTool);
                }
            } catch (Exception e) {
                PAModule.PALog("Failed to initialize the PLT Tool");
                Debug.LogException(e);
            }
        }

        public static void UnloadPLT() {
            PropLineTool propLineTool = ToolsModifierControl.toolController.gameObject.GetComponent<PropLineTool>();
            if (!(propLineTool is null)) {
                if (!(m_toolbar is null)) {
                    Destroy(m_toolbar);
                }
                if (!(m_optionPanel is null)) {
                    Destroy(m_optionPanel);
                }
                Destroy(propLineTool);
            }
        }
    }
}
