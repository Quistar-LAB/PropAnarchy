using ColossalFramework;
using ColossalFramework.Math;
using EManagersLib;
using PropAnarchy.PLT.Extensions;
using PropAnarchy.PLT.MathUtils;
using PropAnarchy.PLT.Modes;
using UnityEngine;
using static PropAnarchy.PLT.PropLineTool;

namespace PropAnarchy.PLT {
    internal abstract class DrawMode {
        internal const int Single = 0;
        internal const int Straight = 1;
        internal const int Curved = 2;
        internal const int Freeform = 3;
        internal const int Circle = 4;
        private static int m_curActiveMode;
        protected static Segment3 m_mainSegment = new Segment3();
        protected static SegmentXZ m_mainArm1 = new SegmentXZ();
        protected static SegmentXZ m_mainArm2 = new SegmentXZ();
        protected static Bezier3 m_mainBezier = new Bezier3();
        protected static CircleXZ m_mainCircle = new CircleXZ();
        protected static CircleXZ m_rawCircle = new CircleXZ();
        protected static readonly DrawStraight DrawStraightMode = new DrawStraight();
        protected static readonly DrawCurve DrawCurveMode = new DrawCurve();
        protected static readonly DrawFreeform DrawFreeformMode = new DrawFreeform();
        protected static readonly DrawCircle DrawCircleMode = new DrawCircle();
        internal static int CurrentMode {
            get => m_curActiveMode;
            set {
                m_curActiveMode = value;
                switch (value) {
                case Single:
                    switch (ItemInfo.Type) {
                    case ItemType.Prop:
                        ToolsModifierControl.SetTool<PropTool>();
                        break;
                    case ItemType.Tree:
                        ToolsModifierControl.SetTool<TreeTool>();
                        break;
                    }
                    return;
                case Straight: CurActiveMode = DrawStraightMode; break;
                case Curved: CurActiveMode = DrawCurveMode; break;
                case Freeform: CurActiveMode = DrawFreeformMode; break;
                case Circle: CurActiveMode = DrawCircleMode; break;
                }
                ToolsModifierControl.SetTool<PropLineTool>();
            }
        }
        internal static ActiveState CurActiveState { get; private set; } = ActiveState.CreatePointFirst;
        internal static HoverState CurHoverState { get; set; } = HoverState.Unbound;
        internal static DrawMode CurActiveMode { get; private set; } = DrawStraightMode;
        internal static LockingMode CurLockMode { get; set; } = LockingMode.Off;
        internal static LockingMode PrevLockMode { get; set; } = LockingMode.Off;
        internal static void GotoActiveState(ActiveState state) {
            if (state != CurActiveState) {
                ActiveState prevState = CurActiveState;
                CurActiveState = state;
                switch (state) {
                case ActiveState.CreatePointFirst:
                    ItemInfo.Count = 0;
                    SegmentState.IsContinueDrawing = false;
                    SegmentState.FinalizeForPlacement(false);
                    break;
                case ActiveState.LockIdle:
                    switch (prevState) {
                    case ActiveState.ChangeSpacing:
                    case ActiveState.MaxFillContinue:
                        CurActiveMode.UpdatePlacement(true, false);
                        break;
                    default:
                        CurActiveMode.UpdatePlacement();
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
                    ControlPoint.UpdateLockedBackup();
                    m_lockedBackupCachedPosition = m_cachedPosition;
                    break;
                case ActiveState.ChangeSpacing:
                    ItemInfo.LockedSpacing = ItemInfo.Spacing;
                    break;
                case ActiveState.ChangeAngle:
                    int itemAngleCenterIndex = Settings.ControlMode == ControlMode.ItemWise ? 0 : 1;
                    ItemInfo.LockedAngle = ItemInfo.Angle;
                    ItemInfo.LockedDirection = ItemInfo.Datas[itemAngleCenterIndex].m_direction;
                    break;
                case ActiveState.MoveItemwiseItem:
                    ItemInfo.LockedItemwiseT = ItemInfo.HoverItemwiseT;
                    break;
                case ActiveState.MaxFillContinue:
                    CurActiveMode.UpdatePlacement();
                    break;
                }
            }
        }
        internal abstract void OnToolGUI(Event e, bool isInsideUI);
        public abstract void OnToolUpdate();
        public abstract void OnToolLateUpdate();
        public abstract void OnRenderGeometry(RenderManager.CameraInfo cameraInfo);
        public abstract bool OnRenderOverlay(RenderManager.CameraInfo cameraInfo, ActiveState curState, Color createPointColor, Color curveWarningColor, Color copyPlaceColor);
        public abstract void OnSimulationStep(Vector3 mousePosition);
        public abstract void RenderLines(RenderManager.CameraInfo cameraInfo, Color createPointColor, Color curveWarningColor);
        public abstract bool ContinueDrawing(ControlPoint.PointInfo[] controlPoints, ref int controlPointCount);
        public abstract bool IsLengthLongEnough();
        public abstract bool IsActiveStateAnItemRenderState();
        public bool PostCheckAndContinue() => PostCheckAndContinue(ControlPoint.m_controlPoints, ref ControlPoint.m_validPoints);
        public abstract bool PostCheckAndContinue(ControlPoint.PointInfo[] controlPoints, ref int controlPointCount);
        public void UpdateCurve() => UpdateCurve(ControlPoint.m_controlPoints, ControlPoint.m_validPoints);
        public abstract void UpdateCurve(ControlPoint.PointInfo[] controlPoints, int controlPointCount);
        public abstract void RevertDrawingFromLockMode();
        public abstract void Update();
        public bool UpdatePlacement() => UpdatePlacement(SegmentState.m_segmentInfo.m_isContinueDrawing, SegmentState.m_segmentInfo.m_keepLastOffsets);
        public bool UpdatePlacement(bool forceContinueDrawing) => UpdatePlacement(forceContinueDrawing, SegmentState.m_segmentInfo.m_keepLastOffsets);
        public bool UpdatePlacement(bool forceContinueDrawing, bool forceKeepLastOffsets) {
            bool result;
            if (m_isCopyPlacing) {
                SegmentState.m_segmentInfo.m_keepLastOffsets = true;
                result = CalculateAll(true);
            } else {
                SegmentState.m_segmentInfo.m_keepLastOffsets = forceKeepLastOffsets;
                result = CalculateAll(forceContinueDrawing | SegmentState.m_segmentInfo.m_isContinueDrawing);
            }
            SegmentState.m_segmentInfo.m_isContinueDrawing = forceContinueDrawing;
            return result;
        }
        protected void UpdateFinalPrefab(ItemInfo.ItemData[] items, int numItems) {
            PrefabInfo prefab = ItemInfo.Prefab;
            int itemIndex = 0;
            Randomizer randomizer;
            if (prefab is PropInfo propInfo) {
                foreach (uint likelyID in EPropManager.m_props.NextFreeItems(numItems)) {
                    randomizer = new Randomizer((int)likelyID);
                    items[itemIndex].m_finalPrefab = propInfo.GetVariation(ref randomizer);
                    items[itemIndex].m_color = propInfo.GetColor(ref randomizer);
                    items[itemIndex++].m_scale = propInfo.m_minScale + randomizer.Int32(10000u) * (propInfo.m_maxScale - propInfo.m_minScale) * 0.0001f;
                }
            } else if (prefab is TreeInfo treeInfo) {
                foreach (uint likelyID in Singleton<TreeManager>.instance.m_trees.NextFreeItems(numItems)) {
                    randomizer = new Randomizer((int)likelyID);
                    int random = randomizer.Int32(10000u);
                    items[itemIndex].m_finalPrefab = treeInfo.GetVariation(ref randomizer);
                    items[itemIndex].m_scale = treeInfo.m_minScale + random * (treeInfo.m_maxScale - treeInfo.m_minScale) * 0.0001f;
                    items[itemIndex++].m_brightness = treeInfo.m_minBrightness + random * (treeInfo.m_maxBrightness - treeInfo.m_minBrightness) * 0.0001f;
                }
            }
        }

        private bool CalculateAll(bool continueDrawing) {
            bool fenceMode = ItemInfo.FenceMode;
            ItemInfo.ItemData[] itemDatas = ItemInfo.Datas;
            if (CalculateAllPositions(continueDrawing, fenceMode, itemDatas)) {
                CalculateAllDirections(itemDatas, fenceMode);
                CalculateAllAnglesBase(itemDatas, fenceMode);
                UpdatePlacementErrors(itemDatas);
                return true;
            }
            SegmentState.m_segmentInfo.m_maxItemCountExceeded = false;
            return false;
        }
        private static void UpdatePlacementErrors(ItemInfo.ItemData[] itemDatas) {
            int itemCount = ItemInfo.Count;
            bool validPlacement = true;
            for (int i = 0; i < itemCount; i++) {
                if (!itemDatas[i].UpdatePlacementError()) validPlacement = false;
            }
            SegmentState.AllItemsValid = validPlacement;
        }
        private static void CalculateAllAnglesBase(ItemInfo.ItemData[] items, bool fenceMode) {
            Vector3 xAxis = EMath.Vector3Right;
            Vector3 yAxis = EMath.Vector3Up;
            int itemCount = ItemInfo.Count; ;
            if (fenceMode) {
                float offsetAngle;
                if (Settings.AngleFlip180) {
                    offsetAngle = Mathf.Deg2Rad * (((ItemInfo.Height > ItemInfo.Width ? Mathf.PI / 2f : 0f) + Mathf.PI + ItemInfo.Angle) * Mathf.Rad2Deg % 360f);
                } else if (Settings.AngleFlip90) {
                    offsetAngle = Mathf.Deg2Rad * (((ItemInfo.Height > ItemInfo.Width ? Mathf.PI / 2f : 0f) + (Mathf.PI / 2f) + ItemInfo.Angle) * Mathf.Rad2Deg % 360f);
                } else {
                    offsetAngle = Mathf.Deg2Rad * (((ItemInfo.Height > ItemInfo.Width ? Mathf.PI / 2f : 0f) + ItemInfo.Angle) * Mathf.Rad2Deg % 360f);
                }
                for (int i = 0; i < itemCount; i++) {
                    items[i].m_angle = items[i].m_direction.AngleSigned(xAxis, yAxis) + Mathf.PI + offsetAngle;
                }
            } else {
                float angle;
                if (Settings.AngleFlip180) {
                    angle = Mathf.Deg2Rad * (((ItemInfo.Height > ItemInfo.Width ? Mathf.PI / 2f : 0f) + Mathf.PI + ItemInfo.Angle) * Mathf.Rad2Deg % 360f);
                } else if (Settings.AngleFlip90) {
                    angle = Mathf.Deg2Rad * (((ItemInfo.Height > ItemInfo.Width ? Mathf.PI / 2f : 0f) + (Mathf.PI / 2f) + ItemInfo.Angle) * Mathf.Rad2Deg % 360f);
                } else {
                    angle = Mathf.Deg2Rad * (((ItemInfo.Height > ItemInfo.Width ? Mathf.PI / 2f : 0f) + ItemInfo.Angle) * Mathf.Rad2Deg % 360f);
                }
                switch (Settings.AngleMode) {
                case AngleMode.Dynamic:
                    for (int i = 0; i < itemCount; i++) {
                        items[i].m_angle = items[i].m_direction.AngleSigned(xAxis, yAxis) + Mathf.PI + angle;
                    }
                    break;
                case AngleMode.Single:
                    for (int i = 0; i < itemCount; i++) {
                        items[i].m_angle = angle;
                    }
                    break;
                }
            }
        }
        private bool CalculateAllPositions(bool continueDrawing, bool fenceMode, ItemInfo.ItemData[] items) {
            switch (Settings.ControlMode) {
            case ControlMode.ItemWise:
                if (continueDrawing) return CalculateItemwisePosition(items, fenceMode, ItemInfo.Spacing, SegmentState.LastFinalOffset, SegmentState.LastFenceEndpoint);
                return CalculateItemwisePosition(items, fenceMode, ItemInfo.Spacing, 0f, m_mainSegment.b);
            case ControlMode.Spacing:
                if (continueDrawing) return CalculateAllPositionsBySpacing(items, fenceMode, ItemInfo.Spacing, SegmentState.LastFinalOffset, SegmentState.LastFenceEndpoint);
                return CalculateAllPositionsBySpacing(items, fenceMode, ItemInfo.Spacing, 0f, m_mainSegment.b);
            }
            return false;
        }
        public void CheckPendingPlacement() {
            if (SegmentState.m_pendingPlacementUpdate) {
                UpdateCurve();
                UpdatePlacement();
                SegmentState.m_pendingPlacementUpdate = false;
            }
        }
        public abstract void CalculateAllDirections(ItemInfo.ItemData[] itemDatas, bool fenceMode);
        public abstract bool CalculateItemwisePosition(ItemInfo.ItemData[] itemDatas, bool fenceMode, float fencePieceLength, float initialOffset, VectorXZ lastFenceEndpoint);
        public abstract bool CalculateAllPositionsBySpacing(ItemInfo.ItemData[] itemDatas, bool fenceMode, float spacing, float initialOffset, VectorXZ lastFenceEndpoint);
        public abstract void DiscoverHoverState(VectorXZ position);
        public abstract void UpdateMiscHoverParameters();
        public abstract void RenderProgressiveSpacingFill(RenderManager.CameraInfo cameraInfo, float fillLength, float interval, float size, Color color, bool renderLimits, bool alphaBlend);
        public void RenderMaxFillContinueMarkers(RenderManager.CameraInfo cameraInfo) {
            const float radius = 6f;
            if (Settings.ControlMode == ControlMode.ItemWise) return;
            Color maxFillContinueColor = Settings.m_PLTColor_MaxFillContinue;
            //initial item
            Vector3 initialItemPosition = ItemInfo.FenceMode ? ItemInfo.Datas[0].m_fenceEndPoint : ItemInfo.Datas[0].Position;
            RenderSegment(cameraInfo, new Segment3(initialItemPosition - (Vector3)(ItemInfo.Datas[0].m_offsetDirection * radius), initialItemPosition + (ItemInfo.Datas[0].m_offsetDirection * radius)), 0.25f, 0f, maxFillContinueColor, false, true);
            //final item
            Vector3 finalItemPosition = ItemInfo.FenceMode ? ItemInfo.Datas[ItemInfo.Count - 1].m_fenceEndPoint : ItemInfo.Datas[ItemInfo.Count - 1].Position;
            RenderCircle(cameraInfo, finalItemPosition, 0.5f, maxFillContinueColor, false, true);
            RenderCircle(cameraInfo, finalItemPosition, radius, maxFillContinueColor, false, true);
            RenderSegment(cameraInfo, new Segment3(finalItemPosition - (Vector3)(ItemInfo.Datas[ItemInfo.Count - 1].m_offsetDirection * radius), finalItemPosition + (ItemInfo.Datas[ItemInfo.Count - 1].m_offsetDirection * radius)), 0.25f, 0f, maxFillContinueColor, false, true);
            //mouse indicators
            maxFillContinueColor.a *= 0.40f;
            RenderSegment(cameraInfo, new Segment3(m_mousePosition, initialItemPosition), 0.05f, 3.00f, maxFillContinueColor, false, true);
            RenderSegment(cameraInfo, new Segment3(m_mousePosition, finalItemPosition), 0.05f, 3.00f, maxFillContinueColor, false, true);
        }

        public void ResetDrawState() {
            CurActiveState = ActiveState.CreatePointFirst;
        }
    }
}
