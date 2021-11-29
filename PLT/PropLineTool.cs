using ColossalFramework;
using ColossalFramework.Math;
using ColossalFramework.UI;
using EManagersLib;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace PropAnarchy.PLT {
    public class PropLineTool : ToolBase {
        private const string PLTHarmonyID = @"com.Quistar.PropAnarchyPLT";
        public delegate void DrawCircle(RenderManager.CameraInfo cameraInfo, Color color, Vector3 center, float size, float minY, float maxY, bool renderLimits, bool alphaBlend);
        public delegate void DrawBezier(RenderManager.CameraInfo cameraInfo, Color color, Bezier3 bezier, float size, float cutStart, float cutEnd, float minY, float maxY, bool renderLimits, bool alphaBlend);
        public delegate void DrawSegment(RenderManager.CameraInfo cameraInfo, Color color, Segment3 segment, float size, float dashLen, float minY, float maxY, bool renderLimits, bool alphaBlend);
        public delegate void DrawElbow(RenderManager.CameraInfo cameraInfo, Color color, Segment3 segment1, Segment3 segment2, float size, float dashLen, float minY, float maxY, bool renderLimits, bool alphaBlend);
        public delegate bool CreateProp(out uint prop, ref Randomizer randomizer, PropInfo info, Vector3 position, float angle, bool single);
        public delegate bool CreateTree(out uint tree, ref Randomizer randomizer, TreeInfo info, Vector3 position, bool single);
        public delegate void DispatchEffect(EffectInfo effect, InstanceID instance, EffectInfo.SpawnArea spawnArea, Vector3 velocity, float acceleration, float magnitude, AudioGroup audioGroup);
        public delegate float SampleHeight(Vector3 worldPos);
        public delegate AsyncAction SimulationManagerAddAction(Action action);
        public delegate T PropertyGetterHandler<T>();
        public delegate void PropertySetterHandler<T>(T value);
        public delegate void PropertyChangedEventHandler<T>(T val);
        public const float TOLERANCE = 0.001f;
        public const float POINTSIZE = 3.00f;
        public const float DOTSIZE = 0.40f;
        public const float LINESIZE = 2.20f;
        public const int ITEMWISE_INDEX = 0;
        public const int ITEMWISE_FENCE_INDEX_START = 0;
        public const int ITEMWISE_FENCE_INDEX_END = 1;
        internal static readonly Vector3 m_vectorDown = Vector3.down;
        internal static readonly Vector3 m_vectorZero = Vector3.zero;
        internal static readonly Vector3 m_vectorUp = Vector3.up;
        internal static readonly Vector3 m_vectorRight = Vector3.right;

        public enum LockingMode : int { Off = 0, Lock = 1 }
        public enum ItemType : byte { UNDEFINED, TREE, PROP }
        public enum ControlMode : byte { ITEMWISE, SPACING }
        public enum AngleMode : byte { DYNAMIC, SINGLE }
        public enum HoverState {
            Unbound,
            SpacingLocus,
            AngleLocus,
            ControlPointFirst,
            ControlPointSecond,
            ControlPointThird,
            Curve,
            ItemwiseItem
        }
        public static class DrawMode {
            public const int SINGLE = 0;
            public const int STRAIGHT = 1;
            public const int CURVED = 2;
            public const int FREEFORM = 3;
            public const int CIRCLE = 4;
            public delegate void SetDrawModeHandler(int value);
            private static int m_currentMode;
            public static ActiveDrawState CurrentMode;
            public static SetDrawModeHandler SetCurrentSelected;
            private static readonly DrawStraightState DrawStraightProc = new DrawStraightState();
            private static readonly DrawCurveState DrawCurveProc = new DrawCurveState();
            private static readonly DrawFreeformState DrawFreeformProc = new DrawFreeformState();
            private static readonly DrawCircleState DrawCircleProc = new DrawCircleState();
            public static int Current {
                get => m_currentMode;
                set {
                    m_currentMode = value;
                    switch (value) {
                    case SINGLE:
                        switch (m_itemType) {
                        case ItemType.PROP:
                            ToolsModifierControl.SetTool<PropTool>();
                            break;
                        case ItemType.TREE:
                            ToolsModifierControl.SetTool<TreeTool>();
                            break;
                        }
                        break;
                    case STRAIGHT: CurrentMode = DrawStraightProc; break;
                    case CURVED: CurrentMode = DrawCurveProc; break;
                    case FREEFORM: CurrentMode = DrawFreeformProc; break;
                    case CIRCLE: CurrentMode = DrawCircleProc; break;
                    }
                }
            }
        }
        public struct ItemInfo {
            public bool m_isValidPlacement;
            public ItemCollisionType m_CollisionType;
            public uint m_itemID;
            public float m_t;
            public float m_angle;
            private Vector3 m_position;
            public Vector3 m_centerCorrection;
            public Vector3 m_fenceEndPoint;
            public Vector3 m_itemDirection;
            public Vector3 m_offsetDirection;
            private static PrefabInfo m_prefab;
            public static float m_itemModelX;
            public static float m_itemModelZ;
            private static Color32 m_color;
            private static float m_brightness;
            public static float m_scale;
            public static float m_itemWidth;
            public static float m_itemLength;
            public static float m_itemAngleOffset;
            public static float m_itemAngleSingle;
            public Vector3 Position {
                get => Settings.UseMeshCenterCorrection ? m_position + m_centerCorrection : m_position;
                set {
                    m_position = value;
                    m_position.y = SampleTerrainHeight(value);
                }
            }
            public static PrefabInfo Prefab {
                get => m_prefab;
                set {
                    m_prefab = value;
                    float itemWidth, itemLength, itemMaxWidth;
                    if (value is PropInfo propInfo) {
                        m_itemModelX = itemWidth = propInfo.m_mesh.bounds.extents.x * 2f;
                        m_itemModelZ = itemLength = propInfo.m_mesh.bounds.extents.z * 2f;
                        m_itemWidth = itemWidth < itemLength ? itemWidth : itemLength;
                        m_itemLength = itemMaxWidth = itemWidth < itemLength ? itemLength : itemWidth;
                        if (Settings.AutoDefaultSpacing) {
                            float spacing = itemMaxWidth;
                            if (GetFenceMode() && spacing == 0) spacing = 8f;
                            else if (spacing < 2f) spacing = 2f;
                            else if (spacing < 4f) spacing *= 2.2f;
                            ItemSpacing = spacing;
                        }
                        uint seed = EPropManager.m_props.NextFreeItem();
                        Randomizer randomizer = new Randomizer(seed);
                        m_color = propInfo.GetColor(ref randomizer);
                        m_scale = propInfo.m_minScale + randomizer.Int32(10000u) * (propInfo.m_maxScale - propInfo.m_minScale) * 0.0001f;
                    } else if (value is TreeInfo treeInfo) {
                        m_itemModelX = itemWidth = treeInfo.m_mesh.bounds.extents.x * 2f;
                        m_itemModelZ = itemLength = treeInfo.m_mesh.bounds.extents.z * 2f;
                        m_itemWidth = itemWidth < itemLength ? itemWidth : itemLength;
                        m_itemLength = itemMaxWidth = itemWidth < itemLength ? itemLength : itemWidth;
                        if (Settings.AutoDefaultSpacing) {
                            float spacing = itemMaxWidth;
                            if (GetFenceMode() && spacing == 0) spacing = 8f;
                            else if (spacing > 7f) spacing *= 1.1f;
                            ItemSpacing = spacing;
                        }
                        uint seed = Singleton<TreeManager>.instance.m_trees.NextFreeItem();
                        Randomizer randomizer = new Randomizer(seed);
                        m_brightness = treeInfo.m_minBrightness + randomizer.Int32(10000u) * (treeInfo.m_maxBrightness - treeInfo.m_minBrightness) * 0.0001f;
                        m_scale = treeInfo.m_minScale + randomizer.Int32(10000u) * (treeInfo.m_maxScale - treeInfo.m_minScale) * 0.0001f;
                    }
                    //UpdatePlacement();
                }
            }
            public static float ItemDefaultSpacing {
                get {
                    if (m_prefab is PropInfo) {
                        float spacing = m_itemLength;
                        if (GetFenceMode() && spacing == 0) spacing = 8f;
                        else if (spacing < 2f) spacing = 2f;
                        else if (spacing < 4f) spacing *= 2.2f;
                        return spacing;
                    } else {
                        float spacing = m_itemLength;
                        if (GetFenceMode() && spacing == 0) spacing = 8f;
                        else if (spacing > 7f) spacing *= 1.1f;
                        return spacing;
                    }
                }
            }
            public static float ItemLength => m_itemLength;
            public static float ItemWidth => m_itemWidth;
            public static float ItemSpacing {
                get => GetSpacingValue();
                set => SetSpacingValue(value < 0 ? 0 : value);
            }
            public static float AngleSingleOffset => (m_itemModelZ > m_itemModelX ? Mathf.PI / 2f : 0f) + (Settings.AngleFlip180 ? Mathf.PI : 0f);
            public static void SetDefaultSpacing() => ItemSpacing = ItemDefaultSpacing;
            public void RenderItem(RenderManager.CameraInfo cameraInfo) {
                if (m_prefab is PropInfo propInfo) {
                    InstanceID id = default;
                    if (propInfo.m_requireHeightMap) {
                        Singleton<TerrainManager>.instance.GetHeightMapping(m_position, out Texture heightMap, out Vector4 heightMapping, out Vector4 surfaceMapping);
                        EPropInstance.RenderInstance(cameraInfo, propInfo, id, m_position, m_scale, m_angle, m_color, RenderManager.DefaultColorLocation, true, heightMap, heightMapping, surfaceMapping);
                    } else {
                        EPropInstance.RenderInstance(cameraInfo, propInfo, id, m_position, m_scale, m_angle, m_color, RenderManager.DefaultColorLocation, true);
                    }
                } else if (m_prefab is TreeInfo treeInfo) {
                    TreeInstance.RenderInstance(cameraInfo, treeInfo, m_position, m_scale, m_brightness, RenderManager.DefaultColorLocation);
                }
            }
            public void SetDirectionsXZ(Vector3 itemDirection) {
                itemDirection.y = 0f;
                itemDirection.Normalize();
                m_itemDirection = itemDirection;
                m_offsetDirection = new Vector3(itemDirection.z, 0f, itemDirection.x);
            }
        }
        public const int MAX_ITEM_ARRAY_LENGTH = 1024;
        public const float SPACING_TILE_MAX = 1920f;
        public const float SPACING_MAX = 2000f;
        public const float SPACING_MIN = 0.10f;
        public const float HOVER_POINT_DIAMETER = 1.5f;
        public const float HOVER_ANGLELOCUS_DIAMETER = 10f;
        public const float HOVER_POINTDISTANCE_THRESHOLD = 1.5f;
        public const float HOVER_CURVEDISTANCE_THRESHOLD = 1f;
        public const float HOVER_ITEMWISE_CURVEDISTANCE_THRESHOLD = 12f;
        private static DrawCircle m_drawCircleFunc;
        private static DrawBezier m_drawBezierFunc;
        private static DrawSegment m_drawSegmentFunc;
        private static DrawElbow m_drawElbowFunc;
        public static SampleHeight SampleTerrainHeight;
        public static SimulationManagerAddAction AddAction;
        internal static PropertyGetterHandler<bool> GetFenceMode;
        internal static PropertySetterHandler<float> SetSpacingValue;
        internal static PropertyGetterHandler<float> GetSpacingValue;
        internal static CreateProp CreateItemProp;
        internal static CreateTree CreateItemTree;
        internal static DispatchEffect DispatchItemEffect;
        internal static EffectInfo m_bulldozeEffect;
        internal static EffectInfo m_placementEffect;
        private static AudioGroup m_defaultAudioGroup;
        internal static Vector3[] m_fenceEndPoints;
        internal static ToolBar m_toolBar = null;
        internal static OptionPanel m_optionPanel;
        private static PropTool m_propTool;
        private static TreeTool m_treeTool;
        internal static ItemType m_itemType;
        internal static ControlMode m_controlMode;
        internal static AngleMode m_angleMode;
        internal static bool m_isCopyPlacing;
        internal static ItemInfo[] m_items;
        internal static int m_itemCount;
        internal Randomizer m_randomizer;
        internal static float m_mainElbowAngle;
        internal static Vector3 m_mousePosition;
        internal static Vector3 m_cachedPosition;
        internal static bool m_positionChanging;
        internal static bool m_mouseRayValid;
        private static float m_mouseRayLength;
        private static Ray m_mouseRay;
        internal static bool m_keyboardCtrlDown;
        internal static bool m_keyboardAltDown;

        internal static LockingMode m_lockingMode;
        internal static LockingMode m_previousLockingMode;
        internal static float m_lockedBackupSpacing = 8f;
        internal static float m_lockedBackupAngleSingle = 0f;
        internal static float m_lockedBackupAngleOffset = 0f;
        internal static float m_lockedBackupItemSecondAngle = 0f;
        internal static Vector3 m_lockedBackupCachedPosition = default;
        internal static Vector3 m_lockedBackupItemDirection = m_vectorRight;
        internal static float m_lockedBackupItemwiseT = 0f;

        public static float m_hoverAngle = 0f;
        //Hovered Curve Position for Itemwise Placement
        public static float m_hoverItemwiseT = 0f;
        public static HoverState m_hoverState = HoverState.Unbound;

        public static int HoverItemPositionIndex => m_controlMode == ControlMode.ITEMWISE ? (GetFenceMode() ? ITEMWISE_FENCE_INDEX_START : ITEMWISE_INDEX) : 1;

        public static int HoverItemAngleCenterIndex => m_controlMode == ControlMode.ITEMWISE ? ITEMWISE_INDEX : 1;

        public static void ResetPLT() {
            m_positionChanging = false;
            GoToActiveState(ActiveState.CreatePointFirst);
            ControlPoint.Reset();
            m_mouseRayValid = false;
            SegmentState.Reset();

            //update undo previews
            //m_undoManager.CheckItemsStillExist();
        }

        protected override void Awake() {
            base.Awake();
            m_drawCircleFunc = Singleton<RenderManager>.instance.OverlayEffect.DrawCircle;
            m_drawBezierFunc = Singleton<RenderManager>.instance.OverlayEffect.DrawBezier;
            m_drawSegmentFunc = Singleton<RenderManager>.instance.OverlayEffect.DrawSegment;
            m_drawElbowFunc = Singleton<RenderManager>.instance.OverlayEffect.DrawSegment;
            SampleTerrainHeight = Singleton<TerrainManager>.instance.SampleDetailHeight;
            AddAction = Singleton<SimulationManager>.instance.AddAction;
            CreateItemProp = Singleton<PropManager>.instance.CreateProp;
            CreateItemTree = Singleton<TreeManager>.instance.CreateTree;
            DispatchItemEffect = Singleton<EffectManager>.instance.DispatchEffect;
            m_defaultAudioGroup = Singleton<AudioManager>.instance.DefaultGroup;
            m_bulldozeEffect = Singleton<PropManager>.instance.m_properties.m_bulldozeEffect;
            m_placementEffect = Singleton<PropManager>.instance.m_properties.m_placementEffect;
            m_treeTool = ToolsModifierControl.GetTool<TreeTool>();
            m_propTool = ToolsModifierControl.GetTool<PropTool>();
            m_items = new ItemInfo[MAX_ITEM_ARRAY_LENGTH];
            m_fenceEndPoints = new Vector3[MAX_ITEM_ARRAY_LENGTH + 1];
            m_controlMode = ControlMode.SPACING;
            m_itemCount = 0;
            m_randomizer = new Randomizer((int)DateTime.Now.Ticks);
            if (Settings.AnarchyPLTOnByDefault) {
                Settings.ShowErrorGuides = false;
                Settings.PlaceBlockedItems = true;
                Settings.AnarchyPLT = true;
                Settings.ErrorChecking = false;
            }
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

        protected override void OnToolGUI(Event e) => DrawMode.CurrentMode.OnToolGUI(e, m_toolController.IsInsideUI);

        private static bool IsVectorXZPositionChanging(VectorXZ oldPosition, VectorXZ newPosition) => (newPosition - oldPosition).sqrMagnitude > TOLERANCE * TOLERANCE;

        public static void UpdateCachedPosition(bool ignorePosChangingCondition) {
            m_positionChanging = ignorePosChangingCondition || IsVectorXZPositionChanging(m_cachedPosition, m_mousePosition);
            m_cachedPosition = m_mousePosition;
        }

        public static bool FinalizePlacement(bool continueDrawing, bool isCopyPlacing) {
            int itemCount = m_itemCount;
            if (itemCount > 0) {
                DrawMode.CurrentMode.UpdatePlacement();
                ItemInfo[] items = m_items;
                if (ItemInfo.Prefab is PropInfo propInfo) {
                    for (int i = 0; i < itemCount; i++) {
                        Randomizer randomizer = new Randomizer(EPropManager.m_props.NextFreeItem());
                        PropInfo newPropInfo = m_controlMode == ControlMode.ITEMWISE && i == ITEMWISE_INDEX ? propInfo.GetVariation(ref randomizer) : propInfo;
                        Vector3 position = items[i].Position;
                        position = Settings.RenderAndPlacePosResVanilla ? position.QuantizeToGameShortGridXYZ() : position;
                        if (Singleton<PropManager>.instance.CreateProp(out uint propID, ref randomizer, newPropInfo, position, items[i].m_angle, true)) {
                            items[i].m_itemID = propID;
                            DispatchPlacementEffect(ref position, false);
                        }
                    }
                    //undoManager.AddEntry(_itemCount, m_placementInfo, ObjectMode.Props, fenceMode, placementCalculator.segmentState);
                } else if (ItemInfo.Prefab is TreeInfo treeInfo) {
                    for (int i = 0; i < itemCount; i++) {
                        Randomizer randomizer = new Randomizer(Singleton<TreeManager>.instance.m_trees.NextFreeItem());
                        TreeInfo newTreeInfo = m_controlMode == ControlMode.ITEMWISE && i == ITEMWISE_INDEX ? treeInfo.GetVariation(ref randomizer) : treeInfo;
                        Vector3 position = items[i].Position;
                        position = Settings.RenderAndPlacePosResVanilla ? position.QuantizeToGameShortGridXYZ() : position;
                        if (Singleton<TreeManager>.instance.CreateTree(out uint treeID, ref randomizer, newTreeInfo, position, true)) {
                            items[i].m_itemID = treeID;
                            DispatchPlacementEffect(ref position, false);
                        }
                    }
                    //undoManager.AddEntry(_itemCount, m_placementInfo, ObjectMode.Trees, fenceMode, placementCalculator.segmentState);
                }
                if (!isCopyPlacing) {
                    m_itemCount = 0;
                    SegmentState.FinalizeForPlacement(continueDrawing);
                }
                return true;
            }
            return false;
        }

        public static void DispatchPlacementEffect(ref Vector3 position, bool isBulldozeEffect) {
            InstanceID id = default;
            EffectInfo effectInfo = isBulldozeEffect ? m_bulldozeEffect : m_placementEffect;
            EffectInfo.SpawnArea spawnArea = new EffectInfo.SpawnArea(position, m_vectorUp, 1f);
            DispatchItemEffect(effectInfo, id, spawnArea, default, 0f, 1f, m_defaultAudioGroup);
        }

        public static void GoToActiveState(ActiveState state) {
            if (state != ActiveDrawState.m_currentState) {
                ActiveState oldState = ActiveDrawState.m_currentState;
                ActiveDrawState.m_currentState = state;
                switch (state) {
                case ActiveState.CreatePointFirst:
                    m_itemCount = 0;
                    SegmentState.IsContinueDrawing = false;
                    SegmentState.FinalizeForPlacement(false);
                    break;
                case ActiveState.LockIdle:
                    switch (oldState) {
                    case ActiveState.ChangeSpacing:
                    case ActiveState.MaxFillContinue:
                        DrawMode.CurrentMode.UpdatePlacement(true, false);
                        break;
                    default:
                        DrawMode.CurrentMode.UpdatePlacement();
                        break;
                    }
                    break;
                case ActiveState.MovePointFirst:
                    ControlPoint.m_lockedControlPoints[0] = ControlPoint.m_controlPoints[0];
                    break;
                case ActiveState.MovePointSecond:
                    ControlPoint.m_lockedControlPoints[1] = ControlPoint.m_controlPoints[1];
                    break;
                case ActiveState.MovePointThird:
                    ControlPoint.m_lockedControlPoints[2] = ControlPoint.m_controlPoints[2];
                    break;
                case ActiveState.MoveSegment:
                    ControlPoint.PointInfo[] controlPoints = ControlPoint.m_controlPoints;
                    ControlPoint.PointInfo[] lockedControlPoints = ControlPoint.m_lockedControlPoints;
                    lockedControlPoints[0].m_position = controlPoints[0].m_position;
                    lockedControlPoints[1].m_position = controlPoints[1].m_position;
                    lockedControlPoints[2].m_position = controlPoints[2].m_position;
                    m_lockedBackupCachedPosition = m_cachedPosition;
                    break;
                case ActiveState.ChangeSpacing:
                    m_lockedBackupSpacing = ItemInfo.ItemSpacing;
                    break;
                case ActiveState.ChangeAngle:
                    int itemAngleCenterIndex = m_controlMode == ControlMode.ITEMWISE ? 0 : 1;
                    m_lockedBackupAngleSingle = ItemInfo.m_itemAngleSingle;
                    m_lockedBackupAngleOffset = ItemInfo.m_itemAngleOffset;
                    m_lockedBackupItemSecondAngle = m_items[itemAngleCenterIndex].m_angle;
                    m_lockedBackupItemDirection = m_items[itemAngleCenterIndex].m_itemDirection;
                    break;
                case ActiveState.MoveItemwiseItem:
                    m_lockedBackupItemwiseT = m_hoverItemwiseT;
                    break;
                case ActiveState.MaxFillContinue:
                    DrawMode.CurrentMode.UpdatePlacement();
                    break;
                }
            }
        }

        protected override void OnToolUpdate() {
        }

        protected override void OnToolLateUpdate() {
            m_mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            m_mouseRayLength = Camera.main.farClipPlane;
            m_mouseRayValid = (!m_toolController.IsInsideUI && Cursor.visible);
            DrawMode.CurrentMode.OnToolLateUpdate();
            //Singleton<TerrainManager>.instance.RenderZones = false;
        }

        public override void RenderGeometry(RenderManager.CameraInfo cameraInfo) {
            DrawMode.CurrentMode.OnRenderGeometry(cameraInfo);
            base.RenderGeometry(cameraInfo);
        }

        public void RenderMaxFillContinueMarkers(RenderManager.CameraInfo cameraInfo) {
            const float radius = 6f;
            if (m_controlMode == ControlMode.ITEMWISE) return;
            Color maxFillContinueColor = Settings.m_PLTColor_MaxFillContinue;
            //initial item
            Vector3 initialItemPosition = GetFenceMode() ? m_fenceEndPoints[0] : m_items[0].Position;
            RenderSegment(cameraInfo, new Segment3(initialItemPosition - (m_items[0].m_offsetDirection * radius), initialItemPosition + (m_items[0].m_offsetDirection * radius)), 0.25f, 0f, maxFillContinueColor, false, true);
            //final item
            Vector3 finalItemPosition = GetFenceMode() ? m_fenceEndPoints[m_itemCount - 1] : m_items[m_itemCount - 1].Position;
            RenderCircle(cameraInfo, finalItemPosition, 0.5f, maxFillContinueColor, false, true);
            RenderCircle(cameraInfo, finalItemPosition, radius, maxFillContinueColor, false, true);
            RenderSegment(cameraInfo, new Segment3(finalItemPosition - (m_items[m_itemCount - 1].m_offsetDirection * radius), finalItemPosition + (m_items[m_itemCount - 1].m_offsetDirection * radius)), 0.25f, 0f, maxFillContinueColor, false, true);
            //mouse indicators
            maxFillContinueColor.a *= 0.40f;
            RenderSegment(cameraInfo, new Segment3(m_mousePosition, initialItemPosition), 0.05f, 3.00f, maxFillContinueColor, false, true);
            RenderSegment(cameraInfo, new Segment3(m_mousePosition, finalItemPosition), 0.05f, 3.00f, maxFillContinueColor, false, true);
        }

        public override void SimulationStep() {
            RaycastInput input = new RaycastInput(m_mouseRay, m_mouseRayLength);
            if (m_mouseRayValid && EToolBase.RayCast(input, out EToolBase.RaycastOutput raycastOutput) && !raycastOutput.m_currentEditObject) {
                m_mousePosition = raycastOutput.m_hitPos;
                DrawMode.CurrentMode.OnSimulationStep(raycastOutput.m_hitPos);
            }
        }

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo) {
            base.RenderOverlay(cameraInfo);
            Color mainCurveColor = Settings.m_PLTColor_locked;
            Color lockIdleColor = Settings.m_PLTColor_locked;
            Color curveWarningColor = Settings.m_PLTColor_curveWarning;
            Color itemwiseLockColor = Settings.m_PLTColor_ItemwiseLock;
            Color copyPlaceColor = DrawMode.CurrentMode.IsActiveStateAnItemRenderState() && SegmentState.IsReadyForMaxContinue ? Settings.m_PLTColor_MaxFillContinue : Settings.m_PLTColor_copyPlace;
            Color createPointColor = m_controlMode == ControlMode.ITEMWISE ? Settings.m_PLTColor_ItemwiseLock : Settings.m_PLTColor_default;

            if (m_keyboardAltDown) {
                createPointColor = copyPlaceColor; ;
            } else if (m_keyboardCtrlDown) {
                createPointColor = Settings.m_PLTColor_locked;
                if (Settings.ShowUndoPreviews) {
                    //undoManager.RenderLatestEntryCircles(cameraInfo, m_PLTColor_undoItemOverlay);
                }
            }
            switch (ActiveDrawState.m_currentState) {
            case ActiveState.CreatePointFirst: //creating first control point
                if (!m_toolController.IsInsideUI) {
                    RenderCircle(cameraInfo, m_cachedPosition, POINTSIZE, createPointColor, false, false);
                    RenderCircle(cameraInfo, m_cachedPosition, DOTSIZE, createPointColor, false, true);
                }
                break;
            case ActiveState.CreatePointSecond: //creating second control point
                if (!DrawMode.CurrentMode.OnRenderOverlay(cameraInfo, ActiveState.CreatePointSecond, createPointColor, curveWarningColor, copyPlaceColor)) {
                    goto case ActiveState.CreatePointFirst;
                }
                break;
            case ActiveState.CreatePointThird: //creating third control point
                if (DrawMode.CurrentMode.OnRenderOverlay(cameraInfo, ActiveState.CreatePointThird, createPointColor, curveWarningColor, copyPlaceColor)) {
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
                lockIdleColor = m_hoverState == HoverState.SpacingLocus ? Settings.m_PLTColor_lockedStrong : lockIdleColor;
                if (m_keyboardAltDown) {
                    mainCurveColor = copyPlaceColor;
                } else {
                    //if ((ActiveDrawState.m_currentState == ActiveState.LockIdle || ActiveDrawState.m_currentState == ActiveState.MaxFillContinue) &&
                    //    (SegmentState.IsReadyForMaxContinue || SegmentState.IsMaxFillContinue)) {
                    //    RenderMaxFillContinueMarkers(cameraInfo);
                    //}
                    if (m_keyboardCtrlDown) {
                        if (m_controlMode == ControlMode.ITEMWISE) {
                            if (ActiveDrawState.m_currentState == ActiveState.ItemwiseLock) {
                                mainCurveColor = lockIdleColor;
                            } else if (ActiveDrawState.m_currentState == ActiveState.LockIdle) {
                                mainCurveColor = itemwiseLockColor;
                            }
                        } else { //not in itemwise mode
                            mainCurveColor = createPointColor;
                        }
                    } else {
                        if (m_controlMode == ControlMode.ITEMWISE) {
                            if (ActiveDrawState.m_currentState == ActiveState.ItemwiseLock) {
                                mainCurveColor = itemwiseLockColor;
                            } else if (ActiveDrawState.m_currentState == ActiveState.LockIdle) {
                                mainCurveColor = lockIdleColor;
                            }
                        } else { //not in itemwise mode
                            mainCurveColor = lockIdleColor;
                        }
                        //show adjustment circles
                        RenderHoverObjectOverlays(cameraInfo);
                        if (m_hoverState == HoverState.Curve && ActiveDrawState.m_currentState == ActiveState.LockIdle) {
                            mainCurveColor = m_keyboardAltDown ? Settings.m_PLTColor_copyPlaceHighlight : Settings.m_PLTColor_lockedHighlight;
                        }
                    }
                }
                DrawMode.CurrentMode.RenderLines(cameraInfo, mainCurveColor, curveWarningColor);
                ControlPoint.PointInfo[] controlPoints = ControlPoint.m_controlPoints;
                RenderCircle(cameraInfo, controlPoints[0].m_position, DOTSIZE, lockIdleColor, false, true);
                RenderCircle(cameraInfo, controlPoints[1].m_position, DOTSIZE, lockIdleColor, false, true);
                RenderCircle(cameraInfo, controlPoints[2].m_position, DOTSIZE, lockIdleColor, false, true);
                break;
            case ActiveState.MaxFillContinue:
                DrawMode.CurrentMode.OnRenderOverlay(cameraInfo, ActiveState.MaxFillContinue, createPointColor, curveWarningColor, copyPlaceColor);
                break;
            }
            if (Settings.ShowErrorGuides) {
                RenderPlacementErrorOverlays(cameraInfo);
            }
        }

        public void RenderHoverObjectOverlays(RenderManager.CameraInfo cameraInfo) {
            switch (ActiveDrawState.m_currentState) {
            case ActiveState.Undefined:
            case ActiveState.CreatePointFirst:
            case ActiveState.CreatePointSecond:
            case ActiveState.CreatePointThird:
            case ActiveState.MaxFillContinue:
                return;
            }
            if (m_itemCount < (GetFenceMode() ? 1 : 2) && m_controlMode != ControlMode.ITEMWISE) {
                return;
            }
            //setup highlight colors
            Color32 baseColor = Settings.m_PLTColor_hoverBase;
            Color32 lockIdleColor = Settings.m_PLTColor_locked;
            Color32 highlightColor = Settings.m_PLTColor_lockedHighlight;
            if (m_keyboardAltDown) {
                baseColor = Settings.m_PLTColor_hoverCopyPlace;
                highlightColor = Settings.m_PLTColor_copyPlaceHighlight;
            }
            ControlPoint.PointInfo[] controlPoints = ControlPoint.m_controlPoints;
            switch (ActiveDrawState.m_currentState) {
            case ActiveState.LockIdle:
                RenderCircle(cameraInfo, controlPoints[0].m_position, HOVER_POINT_DIAMETER, m_hoverState == HoverState.ControlPointFirst ? highlightColor : baseColor, false, false);
                RenderCircle(cameraInfo, controlPoints[1].m_position, HOVER_POINT_DIAMETER, m_hoverState == HoverState.ControlPointSecond ? highlightColor : baseColor, false, false);
                if (DrawMode.Current == DrawMode.CURVED || DrawMode.Current == DrawMode.FREEFORM) {
                    RenderCircle(cameraInfo, controlPoints[2].m_position, HOVER_POINT_DIAMETER, m_hoverState == HoverState.ControlPointThird ? highlightColor : baseColor, false, false);
                }
                RenderCircle(cameraInfo, GetFenceMode() ? m_fenceEndPoints[HoverItemPositionIndex] : m_items[HoverItemPositionIndex].Position, HOVER_POINT_DIAMETER,
                    m_hoverState == HoverState.SpacingLocus || m_hoverState == HoverState.ItemwiseItem ? highlightColor : baseColor, false, false);

                //spacing fill indicator
                if (m_hoverState == HoverState.SpacingLocus) {
                    DrawMode.CurrentMode.RenderProgressiveSpacingFill(cameraInfo, ItemInfo.ItemSpacing, LINESIZE, 0.20f, Color.Lerp(highlightColor, lockIdleColor, 0.50f), false, true);
                }
                VectorXZ anglePos;
                VectorXZ angleCenter;
                Color32 blendColor;
                if (ItemInfo.Prefab is PropInfo) {
                    angleCenter = m_items[HoverItemAngleCenterIndex].Position;
                    anglePos = CircleXZ.Position3FromAngleXZ(angleCenter, HOVER_ANGLELOCUS_DIAMETER, m_hoverAngle);
                    Color32 angleColor = m_hoverState == HoverState.AngleLocus ? highlightColor : baseColor;
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
                switch (DrawMode.Current) {
                case DrawMode.CURVED:
                case DrawMode.FREEFORM:
                    RenderCircle(cameraInfo, controlPoints[2].m_position, HOVER_POINT_DIAMETER, highlightColor, false, false);
                    break;
                }
                break;
            case ActiveState.ChangeSpacing:
                RenderCircle(cameraInfo, GetFenceMode() ? m_fenceEndPoints[HoverItemPositionIndex] : m_items[HoverItemPositionIndex].Position, HOVER_POINT_DIAMETER, highlightColor, false, false);
                if (GetFenceMode()) {
                    RenderLine(cameraInfo, new Segment3(m_fenceEndPoints[0], m_fenceEndPoints[1]), 0.05f, 0.50f, Color.Lerp(baseColor, highlightColor, 0.50f), false, true);
                } else {
                    DrawMode.CurrentMode.RenderProgressiveSpacingFill(cameraInfo, ItemInfo.ItemSpacing, LINESIZE, 0.20f, highlightColor, false, true);
                }
                break;
            case ActiveState.ChangeAngle:
                angleCenter = m_items[HoverItemAngleCenterIndex].Position;
                anglePos = CircleXZ.Position3FromAngleXZ(angleCenter, HOVER_ANGLELOCUS_DIAMETER, m_hoverAngle);
                blendColor = Color.Lerp(baseColor, highlightColor, 0.50f);
                blendColor.a = 88;
                RenderCircle(cameraInfo, anglePos, HOVER_POINT_DIAMETER, highlightColor, false, false);
                RenderCircle(cameraInfo, angleCenter, HOVER_ANGLELOCUS_DIAMETER * 2f, blendColor, false, true);
                RenderLine(cameraInfo, new Segment3(angleCenter, anglePos), 0.05f, 0.50f, blendColor, false, true);
                break;
            case ActiveState.MoveItemwiseItem:
                RenderCircle(cameraInfo, GetFenceMode() ? m_fenceEndPoints[HoverItemPositionIndex] : m_items[HoverItemPositionIndex].Position, HOVER_POINT_DIAMETER, highlightColor, false, false);
                break;
            }
        }

        public void RenderPlacementErrorOverlays(RenderManager.CameraInfo cameraInfo) {
            int itemCount = m_itemCount;
            bool anarchy = Settings.AnarchyPLT;
            bool @override = anarchy || (!anarchy && Settings.PlaceBlockedItems);
            if ((!SegmentState.AllItemsValid || @override) && itemCount > 0 && DrawMode.CurrentMode.IsActiveStateAnItemRenderState()) {
                Color32 blockedColor = @override ? new Color32(219, 192, 82, 80) : new Color32(219, 192, 82, 200);
                Color32 invalidPlacementColor = anarchy ? new Color32(193, 78, 72, 50) : new Color32(193, 78, 72, 200);
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
                ItemInfo[] items = m_items;
                for (int i = 0; i < itemCount; i++) {
                    if (!items[i].m_isValidPlacement || @override) {
                        Vector3 itemPos = items[i].Position;
                        if (items[i].m_CollisionType == ItemCollisionType.Blocked) {
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

        public static void RenderCircle(RenderManager.CameraInfo cameraInfo, Vector3 position, float size, Color color, bool renderLimits, bool alphaBlend) {
            Singleton<ToolManager>.instance.m_drawCallData.m_overlayCalls++;
            m_drawCircleFunc(cameraInfo, color, position, size, -1f, 1280f, renderLimits, alphaBlend);
        }

        public static void RenderLine(RenderManager.CameraInfo cameraInfo, in Segment3 segment, float size, float dashLength, Color color, bool renderLimits, bool alphaBlend) {
            Singleton<ToolManager>.instance.m_drawCallData.m_overlayCalls++;
            m_drawSegmentFunc(cameraInfo, color, segment, size, dashLength, -1f, 1280f, renderLimits, alphaBlend);
        }

        public static void RenderSegment(RenderManager.CameraInfo cameraInfo, in Segment3 segment, float size, float dashLength, Color color, bool renderLimits, bool alphaBlend) {
            Singleton<ToolManager>.instance.m_drawCallData.m_overlayCalls++;
            m_drawSegmentFunc(cameraInfo, color, segment, size, dashLength, -1f, 1280f, renderLimits, alphaBlend);
        }

        public static void RenderElbow(RenderManager.CameraInfo cameraInfo, in Segment3 segment1, in Segment3 segment2, float size, float dashLength, Color color, bool renderLimits, bool alphaBlend) {
            Singleton<ToolManager>.instance.m_drawCallData.m_overlayCalls++;
            m_drawElbowFunc(cameraInfo, color, segment1, segment2, size, dashLength, -1f, 1280f, renderLimits, alphaBlend);
        }

        public static void RenderBezier(RenderManager.CameraInfo cameraInfo, in Bezier3 bezier, float size, Color color, bool renderLimits, bool alphaBlend) {
            Singleton<ToolManager>.instance.m_drawCallData.m_overlayCalls++;
            m_drawBezierFunc(cameraInfo, color, bezier, size, -100000f, 100000f, -1f, 1280f, renderLimits, alphaBlend);
        }

        public static void RenderMainCircle(RenderManager.CameraInfo cameraInfo, in CircleXZ circle, float size, Color color, bool renderLimits, bool alphaBlend) {
            Singleton<ToolManager>.instance.m_drawCallData.m_overlayCalls += 2;
            m_drawCircleFunc(cameraInfo, color, circle.m_center, circle.m_radius * 2f + size, -1f, 1280f, renderLimits, alphaBlend);
            m_drawCircleFunc(cameraInfo, color, circle.m_center, circle.m_radius * 2f - size, -1f, 1280f, renderLimits, alphaBlend);
            if (circle.m_radius > 0f) {
                RenderLine(cameraInfo, new Segment3(circle.m_center, circle.Position(0f)), 0.05f, 1.00f, color, false, true);
            }
        }

        private static void BeautificationPanelOnClickPostfix(UIComponent comp) {
            object objectUserData = comp.objectUserData;
            if (objectUserData is TreeInfo treeInfo) {
                ItemInfo.Prefab = treeInfo;
            } else if (objectUserData is PropInfo propInfo) {
                ItemInfo.Prefab = propInfo;
            }
        }

        public static void InitializedPLT() {
            ToolController toolController = ToolsModifierControl.toolController;
            try {
                PropLineTool propLineTool = toolController.gameObject.GetComponent<PropLineTool>();
                if (propLineTool is null) {
                    propLineTool = toolController.gameObject.AddComponent<PropLineTool>();
                }
                // because of shared textures, make sure to initialize toolbar first before optionpanel
                m_toolBar = UIView.GetAView().AddUIComponent(typeof(ToolBar)) as ToolBar;
                m_optionPanel = UIView.GetAView().AddUIComponent(typeof(OptionPanel)) as OptionPanel;
                FieldInfo toolsField = typeof(ToolController).GetField("m_tools", BindingFlags.Instance | BindingFlags.NonPublic);
                ToolBase[] tools = toolsField.GetValue(toolController) as ToolBase[];
                int toolLength = tools.Length;
                Array.Resize(ref tools, toolLength + 1);
                tools[toolLength] = propLineTool;
                Dictionary<Type, ToolBase> toolsDict = typeof(ToolsModifierControl).GetField("m_Tools", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null) as Dictionary<Type, ToolBase>;
                toolsDict.Add(typeof(PropLineTool), propLineTool);
                toolsField.SetValue(toolController, tools);
                Harmony harmony = new Harmony(PLTHarmonyID);
                harmony.Patch(AccessTools.Method(typeof(ToolController), @"SetTool"),
                    prefix: new HarmonyMethod(AccessTools.Method(typeof(ToolBar), nameof(ToolBar.SetToolPrefix))),
                    postfix: new HarmonyMethod(AccessTools.Method(typeof(ToolBar), nameof(ToolBar.SetToolPostfix))));
                harmony.Patch(AccessTools.Method(typeof(BeautificationPanel), @"OnButtonClicked"),
                    postfix: new HarmonyMethod(AccessTools.Method(typeof(PropLineTool), nameof(BeautificationPanelOnClickPostfix))));
            } catch (Exception e) {
                Debug.LogException(e);
            }
        }

        public static void UnloadPLT() {
            new Harmony(PLTHarmonyID).Unpatch(AccessTools.Method(typeof(ToolController), @"SetTool"), HarmonyPatchType.Postfix, PLTHarmonyID);
            PropLineTool propLineTool = ToolsModifierControl.toolController.gameObject.GetComponent<PropLineTool>();
            if (!(propLineTool is null)) {
                if (!(m_toolBar is null)) {
                    Destroy(m_toolBar);
                }
                if (!(m_optionPanel is null)) {
                    Destroy(m_optionPanel);
                }
                Destroy(propLineTool);
            }
        }
    }
}
