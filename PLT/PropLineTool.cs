using ColossalFramework;
using ColossalFramework.Math;
using ColossalFramework.UI;
using EManagersLib.API;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace PropAnarchy.PLT {
    public class PropLineTool : ToolBase {
        public const int MAX_ITEM_ARRAY_LENGTH = 256;
        public const int MAX_CONTROLPOINT_LENGTH = 3;
        public const float SPACING_TILE_MAX = 1920f;
        public const float SPACING_MAX = 2000f;
        public const float SPACING_MIN = 0.10f;
        public const int ITEMWISE_INDEX = 0;
        public const int ITEMWISE_FENCE_INDEX_START = 0;
        public const int ITEMWISE_FENCE_INDEX_END = 1;
        private delegate void DrawCircle(RenderManager.CameraInfo cameraInfo, Color color, Vector3 center, float size, float minY, float maxY, bool renderLimits, bool alphaBlend);
        private delegate void DrawBezier(RenderManager.CameraInfo cameraInfo, Color color, Bezier3 bezier, float size, float cutStart, float cutEnd, float minY, float maxY, bool renderLimits, bool alphaBlend);
        private delegate void DrawSegment(RenderManager.CameraInfo cameraInfo, Color color, Segment3 segment, float size, float dashLen, float minY, float maxY, bool renderLimits, bool alphaBlend);
        private delegate void DrawElbow(RenderManager.CameraInfo cameraInfo, Color color, Segment3 segment1, Segment3 segment2, float size, float dashLen, float minY, float maxY, bool renderLimits, bool alphaBlend);
        private static DrawCircle m_drawCircleFunc;
        private static DrawBezier m_drawBezierFunc;
        private static DrawSegment m_drawSegmentFunc;
        private static DrawElbow m_drawElbowFunc;
        private delegate void PropertyChangedEventHandler<T>(T val);
        private static event PropertyChangedEventHandler<bool> EventFenceModeChanged;
        //private static event PropertyChangedEventHandler<PLTDrawMode> EventDrawModeChanged;
        //private static event PropertyChangedEventHandler<PLTControlMode> EventControlModeChanged;
        //private static event PropertyChangedEventHandler<PLTActiveState> EventActiveStateChanged;
        //private static event PropertyChangedEventHandler<PLTObjectMode> EventObjectModeChanged;
        public enum PLTAngleMode { Dynamic, Single }
        public enum PLTSnapMode : int { Off, Objects, ZoneLines }
        public enum PLTDrawMode : int { Single, Straight, Curved, Freeform, Circle }
        public enum PLTLockingMode : int { Off, Lock }
        public enum PLTHoverState : int { Unbound, SpacingLocus, AngleLocus, ControlPointFirst, ControlPointSecond, ControlPointThird, Curve, ItemwiseItem }
        public enum PLTControlMode : int { Itemwise, Spacing }
        public enum PLTObjectMode : int { Undefined, Props, Trees }
        public enum PLTActiveState : int {
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
        [Flags]
        public enum ItemCollisionType : byte {
            None = 0x00,
            Props = 0x01,
            Trees = 0x02,
            Blocked = 0x04,
            Water = 0x08,
            GameArea = 0x10
        }
        public struct PLTControlPoint {
            public Vector3 m_position;
            public Vector3 m_direction;
            public bool m_outside;
            public void Clear() {
                m_position = Vector3.zero;
                m_direction = Vector3.zero;
                m_outside = false;
            }
        }
        public struct PLTItemPlacementInfo {
            public float m_t; //where on the curve it is located (endpoint location for fence mode)
            public Vector3 m_position;
            public Vector3 m_itemDirection;
            public Vector3 m_offsetDirection;   //for radial left/right -/+ offset, it is perpendicular to and rotated -90deg from m_itemDirection
            private Vector3 m_centerCorrection;
            public uint m_itemID;
            public float m_angle;
            public float m_brightness;
            public float m_scale;
            public Color m_color;
            public ItemCollisionType m_collisionFlags;
            public bool m_isValidPlacement;
            private PropInfo m_propInfo;
            private TreeInfo m_treeInfo;

            public Vector3 MeshPosition => PLTSettings.UseMeshCenterCorrection ? m_position + m_centerCorrection : m_position;

            public PropInfo PropPrefab {
                get => m_propInfo;
                set {
                    m_propInfo = value;
                    if (value.IsMeshCenterOffset(out Vector3 orthogonalCenterCorrection) && PLTSettings.UseMeshCenterCorrection && orthogonalCenterCorrection.sqrMagnitude != 0f) {
                        //use negative angle since Unity is left-handed / CW rotation
                        m_centerCorrection = Quaternion.AngleAxis(-m_angle * Mathf.Rad2Deg, Vector3.up) * orthogonalCenterCorrection;
                        return;
                    }
                    m_centerCorrection = Vector3.zero;
                }
            }
            public TreeInfo TreePrefab {
                get => m_treeInfo;
                set {
                    m_treeInfo = value;
                    if (value.IsMeshCenterOffset(out Vector3 orthogonalCenterCorrection) && PLTSettings.UseMeshCenterCorrection && orthogonalCenterCorrection.sqrMagnitude != 0f) {
                        //use negative angle since Unity is left-handed / CW rotation
                        m_centerCorrection = Quaternion.AngleAxis(-m_angle * Mathf.Rad2Deg, Vector3.up) * orthogonalCenterCorrection;
                        return;
                    }
                    m_centerCorrection = Vector3.zero;
                }
            }
            public void SetDirectionsXZ(Vector3 itemDirection) {
                Vector3 offsetDir;
                itemDirection.y = 0f;
                itemDirection.Normalize();
                m_itemDirection = itemDirection;
                offsetDir.x = itemDirection.z;
                offsetDir.z = -itemDirection.x;
                offsetDir.y = 0f;
                m_offsetDirection = offsetDir;
            }
        }
        public static UITextureAtlas m_sharedTextures;

        private static readonly Queue<Action> m_activeStates = new Queue<Action>();
        private object m_cacheLock;
        private int m_controlPointCount;
        private int m_cachedControlPointCount;
        private PLTControlPoint[] m_controlPoints;
        private PLTControlPoint[] m_cachedControlPoints;
        public static PLTControlMode m_controlMode;
        public static PLTSnapMode m_snapMode;
        private PLTActiveState m_activeState;
        public static PLTObjectMode m_objectMode;
        public static PLTDrawMode m_drawMode;
        public static Randomizer m_randomizer;

        public static PropInfo m_propInfo;
        public static TreeInfo m_treeInfo;
        private static PropInfo m_propPrefab;
        private static TreeInfo m_treePrefab;
        private static float m_assetModelX;
        private static float m_assetModelZ;
        private static float m_assetWidth;
        private static float m_assetLength;

        public static bool m_fenceMode;
        public static bool m_isCopyPlacing;
        private bool m_useCOBezierMethod;
        private bool m_mouseLeftDown;
        private bool m_mouseRightDown;
        private bool m_positionChanging;
        private bool m_pendingPlacementUpdate;
        private Vector3 m_mousePosition;
        private Vector3 m_cachedPosition;
        public static PLTAngleMode m_angleMode;
        public static float m_spacingSingle = 8f;
        public static readonly SegmentState m_segmentState = new SegmentState();
        //public static int m_itemCount;
        private int[] m_randInts;
        public static ToolBar m_toolBar;
        public static OptionPanel m_optionPanel;

        public static Segment3 m_mainSegment;
        public static Bezier3 m_mainBezier;
        public static Circle3XZ m_mainCircle;
        //   Secondary
        public static Segment3 m_mainArm1;
        public static Segment3 m_mainArm2;
        private Circle3XZ m_rawCircle;
        public static float m_mainElbowAngle;

        private Ray m_mouseRay;
        private float m_mouseRayLength;
        private bool m_mouseRayValid;
        private float m_lengthTimer;

        private static PLTHoverState m_hoverState = PLTHoverState.Unbound;
        //Hover Render Radii
        public static float m_hoverPointDiameter = 1.5f;
        public static float m_hoverAngleLocusDiameter = 10f;
        public static float m_hoverPointDistanceThreshold = 1.5f;
        public static float m_hoverCurveDistanceThreshold = 1f;
        public static float m_hoverItemwiseCurveDistanceThreshold = 12f;
        //Hovered Angle
        public static float m_hoverAngle = 0f;
        //Hovered Curve Position
        public static float m_hoverCurveT = 0f;
        //Hovered Curve Position for Itemwise Placement
        public static float m_hoverItemwiseT = 0f;

        private PLTControlPoint[] m_lockBackupControlPoints;
        private float m_lockBackupSpacing = 8f;
        private float m_lockBackupAngleSingle = 0f;
        private float m_lockBackupAngleOffset = 0f;
        private float m_lockBackupItemSecondAngle = 0f;
        private Vector3 m_lockBackupCachedPosition = Vector3.zero;
        private Vector3 m_lockBackupItemDirection = Vector3.right;
        private float m_lockBackupItemwiseT = 0f;

        public Color m_PLTColor_default = new Color32(39, 130, 204, 128);
        public Color m_PLTColor_defaultSnapZones = new Color32(39, 130, 204, 255);
        public Color m_PLTColor_locked = new Color32(28, 127, 64, 128);
        public Color m_PLTColor_lockedStrong = new Color32(28, 127, 64, 192);
        public Color m_PLTColor_lockedHighlight = new Color32(228, 239, 232, 160);
        public Color m_PLTColor_copyPlace = new Color32(114, 45, 186, 128);
        public Color m_PLTColor_copyPlaceHighlight = new Color32(214, 223, 234, 160);
        public Color m_PLTColor_hoverBase = new Color32(33, 142, 129, 204);
        public Color m_PLTColor_hoverCopyPlace = new Color32(196, 198, 242, 204);
        public Color m_PLTColor_undoItemOverlay = new Color32(214, 144, 81, 204);
        public Color m_PLTColor_curveWarning = new Color32(231, 155, 24, 160);
        public Color m_PLTColor_ItemwiseLock = new Color32(29, 72, 168, 128);
        public Color m_PLTColor_MaxFillContinue = new Color32(211, 193, 221, 128);

        public PropInfo PropPrefab {
            get => m_propPrefab;
            set {
                if (m_propPrefab != value) {
                    m_propPrefab = value;
                    m_objectMode = PLTObjectMode.Props;
                    if (!(value is null)) {
                        float assetModelX = 2f * value.m_mesh.bounds.extents.x;
                        float assetModelZ = 2f * value.m_mesh.bounds.extents.z;
                        if (assetModelX < assetModelZ) {
                            m_assetWidth = assetModelX;
                            m_assetLength = assetModelZ;
                        } else {
                            m_assetWidth = assetModelZ;
                            m_assetLength = assetModelX;
                        }
                    }
                    if (PLTSettings.AutoDefaultSpacing && m_objectMode == PLTObjectMode.Props) {
                        float spacingSingle = m_assetLength;
                        if (m_fenceMode) {
                            if (spacingSingle == 0) spacingSingle = 8f;
                        } else {
                            if (spacingSingle < 2f) spacingSingle = 2f;
                            else if (spacingSingle < 4f) spacingSingle *= 2.2f;
                        }
                        m_spacingSingle = spacingSingle;
                    }
                    PlacementCalculator.UpdateItemPlacementInfo(m_isCopyPlacing, m_segmentState, m_segmentState.IsContinueDrawing, m_segmentState.m_keepLastOffsets);
                }
            }
        }
        public TreeInfo TreePrefab {
            get => m_treePrefab;
            set {
                if (m_treePrefab != value) {
                    m_treePrefab = value;
                    m_objectMode = PLTObjectMode.Trees;
                    if (!(value is null)) {
                        float assetModelX = 2f * value.m_mesh.bounds.extents.x;
                        float assetModelZ = 2f * value.m_mesh.bounds.extents.z;
                        if (assetModelX < assetModelZ) {
                            m_assetWidth = assetModelX;
                            m_assetLength = assetModelZ;
                        } else {
                            m_assetWidth = assetModelZ;
                            m_assetLength = assetModelX;
                        }
                    }
                    if (PLTSettings.AutoDefaultSpacing && m_objectMode == PLTObjectMode.Trees) {
                        float spacingSingle = m_assetLength;
                        if (m_fenceMode) {
                            if (spacingSingle == 0) spacingSingle = 8f;
                        } else {
                            if (spacingSingle > 7f) spacingSingle *= 1.1f;
                        }
                        m_spacingSingle = spacingSingle;
                    }
                    PlacementCalculator.UpdateItemPlacementInfo(m_isCopyPlacing, m_segmentState, m_segmentState.IsContinueDrawing, m_segmentState.m_keepLastOffsets);
                }
            }
        }

        public static PLTAngleMode AngleMode {
            get => m_angleMode;
            set {
                if (m_angleMode != value) {
                    m_angleMode = value;
                    //OnAngleModeChanged(value);
                }
            }
        }

        public static float TotalPropertyAngleOffset => (m_assetModelZ > m_assetModelX ? Mathf.PI / 2f : 0f) + (PLTSettings.AngleFlip180 ? Mathf.PI : 0f);


        protected override void Awake() {
            base.Awake();
            m_drawCircleFunc = Singleton<RenderManager>.instance.OverlayEffect.DrawCircle;
            m_drawBezierFunc = Singleton<RenderManager>.instance.OverlayEffect.DrawBezier;
            m_drawSegmentFunc = Singleton<RenderManager>.instance.OverlayEffect.DrawSegment;
            m_drawElbowFunc = Singleton<RenderManager>.instance.OverlayEffect.DrawSegment;
            PlacementCalculator.m_itemCount = 0;
            m_controlPointCount = 0;
            PlacementCalculator.m_placementInfos = new PLTItemPlacementInfo[MAX_ITEM_ARRAY_LENGTH];
            PlacementCalculator.m_fenceEndPoints = new Vector3[MAX_ITEM_ARRAY_LENGTH];
            m_randInts = new int[MAX_ITEM_ARRAY_LENGTH];
            m_controlPoints = new PLTControlPoint[MAX_CONTROLPOINT_LENGTH];
            m_cachedControlPoints = new PLTControlPoint[MAX_CONTROLPOINT_LENGTH];
            m_cachedControlPoints = new PLTControlPoint[MAX_CONTROLPOINT_LENGTH];
            m_lockBackupControlPoints = new PLTControlPoint[MAX_CONTROLPOINT_LENGTH];
            m_cacheLock = new object();
            m_randomizer = new Randomizer((int)DateTime.Now.Ticks); //standard time-based randomizer
                                                                    //clear last continue parameters
                                                                    //m_placementCalculator.ResetLastContinueParameters();

            //check main menu settings
            //if (Settings.AnarchyPLTOnByDefault) {
            //    userSettingsControlPanel.showErrorGuides = false;
            //    userSettingsControlPanel.placeBlockedItems = true;
            //    userSettingsControlPanel.anarchyPLT = true;
            //    userSettingsControlPanel.errorChecking = false;
            // }

            //event subscriptions
            // userSettingsControlPanel.eventErrorCheckingSettingChanged += delegate () {
            //    placementCalculator.UpdatePlacementErrors();
            //};
            //userSettingsControlPanel.eventParametersTabSettingChanged += delegate () {
            //    placementCalculator.UpdateItemPlacementInfo();
            //
            //              if (userSettingsControlPanel.autoDefaultSpacing == true) {
            //                placementCalculator.SetDefaultSpacing();
            //          }
            //    };
            //  userSettingsControlPanel.eventRenderingPositioningSettingChanged += delegate () {
            //    UpdateCurves();
            //  placementCalculator.UpdateItemPlacementInfo();
            //};
#if FALSE
            EventDrawModeChanged += (m) => {
                bool straightOrCircle = m == PLTDrawMode.Straight || m_drawMode == PLTDrawMode.Circle;
                if (straightOrCircle) UpdateCurves();
                if (straightOrCircle && m_fenceMode && (m_activeState == PLTActiveState.CreatePointSecond || m_activeState == PLTActiveState.CreatePointThird)) {
                    //snap first control point to last fence endpoint
                    //when user switched from fencemode[curved/freeform -> straight] and curve is coupled to previous segment
                    Vector3 lastFenceEndpoint = m_placementCalculator.GetLastFenceEndpoint();
                    Vector3 firstControlPoint = m_controlPoints[0].m_position;
                    if (lastFenceEndpoint != Vector3.zero && lastFenceEndpoint != Vector3.down && lastFenceEndpoint != firstControlPoint) {
                        ModifyControlPoint(lastFenceEndpoint, 1);
                        m_placementCalculator.UpdateItemPlacementInfo();
                    }
                }
            };
            //auto-set active state when controlMode is changed
            EventControlModeChanged += (m) => {
                if (m == PLTControlMode.Itemwise) {
                    if (m_activeState == PLTActiveState.LockIdle) {
                        GoToActiveState(PLTActiveState.ItemwiseLock);
                    }
                } else if (m == PLTControlMode.Spacing) {
                    if (m_activeState == PLTActiveState.ItemwiseLock) {
                        GoToActiveState(PLTActiveState.LockIdle);
                    }
                }
            };
            //update perfect circle when prefab changed
            m_placementCalculator.eventSpacingSingleChanged += delegate (object sender, float spacing) {
                if (userSettingsControlPanel.perfectCircles) {
                    UpdateCurves();
                }
            };
#endif
        }

        protected override void OnEnable() {
            base.OnEnable();
        }

        protected override void OnDisable() {
            base.OnDisable();
        }

        private const int LEFTMOUSEBUTTON = 0;
        private const int RIGHTMOUSEBUTTON = 1;
        protected override void OnToolGUI(Event e) {
            if (!m_toolController.IsInsideUI) {
                switch (e.type) {
                case EventType.MouseDown:
                    if (e.button == LEFTMOUSEBUTTON) {
                        m_mouseLeftDown = true;
                        switch (m_activeState) {
                        case PLTActiveState.CreatePointFirst:
                            PlacementCalculator.m_itemCount = 0;
                            m_segmentState.FinalizeForPlacement(false);
                            if (m_mouseRayValid && AddControlPoint(m_cachedPosition)) {
                                GoToActiveState(PLTActiveState.CreatePointSecond);
                                ModifyControlPoint(m_mousePosition, 2); //update second point to mouse position
                                UpdateCachedControlPoints();
                                UpdateCurves();
                                PlacementCalculator.UpdateItemPlacementInfo(m_isCopyPlacing, m_segmentState, false, false);
                            }
                            break;
                        case PLTActiveState.CreatePointSecond:
                        case PLTActiveState.CreatePointThird:
                            if ((e.modifiers & EventModifiers.Alt) == EventModifiers.Alt) {
                                switch (m_drawMode) {
                                case PLTDrawMode.Straight:
                                case PLTDrawMode.Circle:
                                case PLTDrawMode.Curved:
                                case PLTDrawMode.Freeform:
                                    Singleton<SimulationManager>.instance.AddAction(CreateItems(true, true));
                                    return;
                                }
                            }
                            break;
                        case PLTActiveState.LockIdle:
                        case PLTActiveState.MovePointFirst:
                        case PLTActiveState.MovePointSecond:
                        case PLTActiveState.MovePointThird:
                        case PLTActiveState.MoveSegment:
                        case PLTActiveState.ChangeSpacing:
                        case PLTActiveState.ChangeAngle:
                        case PLTActiveState.ItemwiseLock:
                        case PLTActiveState.MoveItemwiseItem:
                        case PLTActiveState.MaxFillContinue:
                            if ((e.modifiers & EventModifiers.Alt) == EventModifiers.Alt) {
                                Singleton<SimulationManager>.instance.AddAction(CreateItems(true, true));
                            }
                            return;
                        }
                    } else if (e.button == RIGHTMOUSEBUTTON) {
                        m_mouseRightDown = true;
                    }
                    break;
                }
                return;
            }
            switch (e.type) {
            case EventType.MouseUp:
                switch (e.keyCode) {
                case KeyCode.Mouse0:
                    m_mouseLeftDown = false;
                    break;
                case KeyCode.Mouse1:
                    m_mouseRightDown = false;
                    //if (this.m_mode == PropTool.Mode.Single && !this.m_angleChanged) {
                    //    Singleton<SimulationManager>.instance.AddAction(this.ToggleAngle());
                    //}
                    break;
                }
                break;
            case EventType.KeyDown:
                if ((e.modifiers & EventModifiers.Control) == EventModifiers.Control && e.keyCode == KeyCode.Z) {
                    // perform undo
                } else if ((e.modifiers & EventModifiers.Alt) == EventModifiers.Alt) {
                    m_isCopyPlacing = true;
                } else {
                    switch (e.keyCode) {
                    case KeyCode.Escape:
                        ResetPLT();
                        break;
                    }
                }
                break;
            }
        }

        protected override void OnToolLateUpdate() {
            //All 3 stuff
            Vector3 _mousePosition = Input.mousePosition;
            m_mouseRay = Camera.main.ScreenPointToRay(_mousePosition);
            m_mouseRayLength = Camera.main.farClipPlane;
            m_mouseRayValid = !m_toolController.IsInsideUI && Cursor.visible;
            //Prop/Tree stuff
            UpdateCachedPosition(false); //also updates m_positionChanging
            //Net stuff
            if (m_lengthTimer > 0f) {
                m_lengthTimer = Mathf.Max(0f, m_lengthTimer - Time.deltaTime);
            }
            //check if user switched from Curved/Freeform(in CreatePointThird) -> Straight/Circle
            if ((m_drawMode == PLTDrawMode.Straight || m_drawMode == PLTDrawMode.Circle) && m_activeState == PLTActiveState.CreatePointThird) {
                bool _cancelCPResult = CancelControlPoint();
                if (_cancelCPResult) {
                    GoToActiveState(PLTActiveState.CreatePointSecond);
                    ModifyControlPoint(m_mousePosition, 2); //update second point to mouse position
                    UpdateCachedControlPoints();
                    UpdateCurves();
                } else {
                    ResetAllControlPoints();
                    GoToActiveState(PLTActiveState.CreatePointFirst);
                    ModifyControlPoint(m_mousePosition, 1);
                    UpdateCachedControlPoints();
                    UpdateCurves();
                }
                return;
            }
        }

        private void GoToActiveState(PLTActiveState state) {
            PLTActiveState oldState = m_activeState;
            m_activeState = state;
            if (state == oldState) {
                return;
            }
            //takes care of any overhead that needs to be done
            //when switching states
            switch (state) {
            case PLTActiveState.CreatePointFirst:
                m_segmentState.m_isContinueDrawing = false;
                PlacementCalculator.m_itemCount = 0;
                m_segmentState.FinalizeForPlacement(false);
                break;
            case PLTActiveState.CreatePointSecond:
            case PLTActiveState.CreatePointThird:
                break;
            case PLTActiveState.LockIdle:
                UpdateCachedControlPoints();
                UpdateCurves();
                switch (oldState) {
                case PLTActiveState.CreatePointFirst:
                case PLTActiveState.CreatePointSecond:
                case PLTActiveState.CreatePointThird:
                case PLTActiveState.MovePointFirst:
                case PLTActiveState.MovePointSecond:
                case PLTActiveState.MovePointThird:
                case PLTActiveState.MoveSegment:
                case PLTActiveState.ChangeAngle:
                default:
                    PlacementCalculator.UpdateItemPlacementInfo(m_isCopyPlacing, m_segmentState, m_segmentState.IsContinueDrawing, m_segmentState.m_keepLastOffsets);
                    break;
                case PLTActiveState.ChangeSpacing:
                case PLTActiveState.MaxFillContinue:
                    PlacementCalculator.UpdateItemPlacementInfo(m_isCopyPlacing, m_segmentState, true, false);
                    break;
                }
                int offset = m_fenceMode ? 0 : (m_controlMode == PLTControlMode.Itemwise ? 0 : 1);
                int itemAngleCenterIndex = m_controlMode == PLTControlMode.Itemwise ? ITEMWISE_INDEX : 1;
                if (PlacementCalculator.m_itemCount >= (1 + offset)) {
                    m_hoverAngle = PlacementCalculator.m_placementInfos[itemAngleCenterIndex].m_angle - TotalPropertyAngleOffset + Mathf.PI;
                }
                break;
            case PLTActiveState.MovePointFirst:
                m_lockBackupControlPoints[0] = m_cachedControlPoints[0];
                break;
            case PLTActiveState.MovePointSecond:
                m_lockBackupControlPoints[1] = m_cachedControlPoints[1];
                break;
            case PLTActiveState.MovePointThird:
                m_lockBackupControlPoints[2] = m_cachedControlPoints[2];
                break;
            case PLTActiveState.MoveSegment:
                for (int i = 0; i < MAX_CONTROLPOINT_LENGTH; i++) {
                    m_lockBackupControlPoints[i].m_position = m_controlPoints[i].m_position;
                }
                m_lockBackupCachedPosition = m_cachedPosition;
                break;
            case PLTActiveState.ChangeSpacing:
                m_lockBackupSpacing = m_spacingSingle;
                break;
            case PLTActiveState.ChangeAngle:
                itemAngleCenterIndex = m_controlMode == PLTControlMode.Itemwise ? ITEMWISE_INDEX : 1;
                m_lockBackupAngleSingle = PlacementCalculator.m_angleSingle;
                m_lockBackupAngleOffset = PlacementCalculator.m_angleOffset;
                m_lockBackupItemSecondAngle = PlacementCalculator.m_placementInfos[itemAngleCenterIndex].m_angle;
                m_lockBackupItemDirection = PlacementCalculator.m_placementInfos[itemAngleCenterIndex].m_itemDirection;
                break;
            case PLTActiveState.ItemwiseLock:
                //nothing here...
                break;
            case PLTActiveState.MoveItemwiseItem:
                m_lockBackupItemwiseT = m_hoverItemwiseT;
                break;
            case PLTActiveState.MaxFillContinue:
                PlacementCalculator.UpdateItemPlacementInfo(m_isCopyPlacing, m_segmentState, m_segmentState.IsContinueDrawing, m_segmentState.m_keepLastOffsets);
                break;
            default:
                m_activeState = PLTActiveState.Undefined;
                break;
            }
        }

        private void UpdateCachedPosition(bool ignorePosChangingCondition) {
            if (ignorePosChangingCondition || (PlacementCalculator.IsVectorXZPositionChanging(m_cachedPosition, m_mousePosition, 0.001f))) {
                m_positionChanging = true;
            } else {
                m_positionChanging = false;
            }
            m_cachedPosition = m_mousePosition; //from prop/tree tool *IMPORTANT*
        }

        private void ResetAllControlPoints() {
            m_controlPoints[0].Clear();
            m_controlPoints[1].Clear();
            m_controlPoints[2].Clear();
            m_controlPointCount = 0;
            m_positionChanging = true;
        }

        private bool CancelControlPoint() {
            bool result = false;
            //custom stuff
            if ((m_controlPointCount > 0) && (m_controlPointCount <= 3)) {
                switch (m_controlPointCount) { //placing first CP
                case 0:
                    ResetAllControlPoints();
                    ModifyControlPoint(m_cachedPosition, 1);
                    m_positionChanging = true;
                    result = true;
                    break;
                case 1: //placing second CP
                    m_controlPoints[0].Clear();
                    m_cachedControlPoints[0].Clear();
                    m_cachedControlPoints[1].Clear();
                    m_cachedControlPoints[2].Clear();
                    m_controlPointCount = 0;
                    m_positionChanging = true;
                    result = true;
                    break;
                //for straight
                //in locking mode
                //   or the instant before placement occurs
                //for curved and freeform
                //   placing/creating third CP
                case 2:
                    m_cachedControlPoints[2].Clear();
                    m_controlPointCount = 1;
                    if (m_snapMode != PLTSnapMode.Objects && m_snapMode != PLTSnapMode.ZoneLines) {
                        m_controlPoints[0].m_direction = m_controlPoints[1].m_direction;
                    }
                    ModifyControlPoint(m_mousePosition, 2);
                    m_positionChanging = true;
                    result = true;
                    break;
                //for curved and freeform
                //in locking mode
                //   or the instant before placement occurs
                case 3:
                    m_controlPoints[2].Clear();
                    m_cachedControlPoints[2].Clear();
                    m_controlPointCount = 2;
                    m_positionChanging = true;
                    ModifyControlPoint(m_mousePosition, 3);
                    result = true;
                    break;
                }
            } else {
                Debug.LogError("[PLT]: CancelControlPoint(): m_controlPointCount is out of bounds! (value = " + m_controlPointCount.ToString() + ")");
                result = false;
            }
            UpdateCachedPosition(true);
            UpdateCachedControlPoints();
            return result;
        }

        private bool TryRaycast(out Vector3 hitPos) {
            RaycastInput input = new RaycastInput(m_mouseRay, m_mouseRayLength);
            if (m_mouseRayValid && EToolBase.RayCast(input, out EToolBase.RaycastOutput raycastOutput)) {
                if (!raycastOutput.m_currentEditObject) {
                    hitPos = raycastOutput.m_hitPos;
                    return true;
                }
            }
            hitPos = Vector3.zero;
            return false;
        }

        protected override void OnToolUpdate() {
            UpdateCachedControlPoints();
            if (TryRaycast(out Vector3 hitPos)) {
                m_mousePosition = hitPos;
                //trying more stuff to have curves perfectly follow mouse
                switch (m_activeState) {
                case PLTActiveState.CreatePointFirst:
                    if (m_cachedControlPoints[0].m_position != hitPos) {
                        m_cachedPosition = hitPos;
                        PlacementCalculator.UpdateItemPlacementInfo(m_isCopyPlacing, m_segmentState, false, false);
                    }
                    break;
                case PLTActiveState.CreatePointSecond:
                    if (m_cachedControlPoints[1].m_position != hitPos) {
                        m_cachedPosition = hitPos;
                        ModifyControlPoint(hitPos, 2);
                        if (m_drawMode == PLTDrawMode.Straight || m_drawMode == PLTDrawMode.Circle) {
                            goto UpdateItemPlacementInfo;
                        }
                    }
                    break;
                case PLTActiveState.CreatePointThird:
                    if (m_cachedControlPoints[2].m_position != hitPos) {
                        m_cachedPosition = hitPos;
                        ModifyControlPoint(hitPos, 3);
                        if (m_drawMode == PLTDrawMode.Curved || m_drawMode == PLTDrawMode.Freeform) {
                            goto UpdateItemPlacementInfo;
                        }
                    }
                    break;
                case PLTActiveState.MovePointFirst:
                    if (m_cachedControlPoints[0].m_position != hitPos) {
                        m_cachedPosition = hitPos;
                        ModifyControlPoint(hitPos, 1);
                        goto UpdateItemPlacementInfo;
                    }
                    break;
                case PLTActiveState.MovePointSecond:
                    if (m_cachedControlPoints[1].m_position != hitPos) {
                        m_cachedPosition = hitPos;
                        ModifyControlPoint(hitPos, 2);
                        goto UpdateItemPlacementInfo;
                    }
                    break;
                case PLTActiveState.MovePointThird:
                    if (m_cachedControlPoints[2].m_position != hitPos) {
                        m_cachedPosition = hitPos;
                        ModifyControlPoint(hitPos, 3);
                        goto UpdateItemPlacementInfo;
                    }
                    break;
                case PLTActiveState.MaxFillContinue:
                case PLTActiveState.LockIdle:
                case PLTActiveState.MoveSegment:
                case PLTActiveState.ChangeSpacing:
                case PLTActiveState.ChangeAngle:
UpdateItemPlacementInfo:
                    PlacementCalculator.UpdateItemPlacementInfo(m_isCopyPlacing, m_segmentState, m_segmentState.IsContinueDrawing, m_segmentState.m_keepLastOffsets);
                    break;
                }
                if (m_cachedControlPointCount == 0) return;
                m_pendingPlacementUpdate = true;
                switch (m_drawMode) {
                case PLTDrawMode.Straight:
                    if (m_cachedControlPointCount >= 1) {
                        m_mainSegment.a = m_cachedControlPoints[0].m_position;
                        m_mainSegment.b = m_cachedControlPoints[1].m_position;
                    }
                    break;
                case PLTDrawMode.Curved:
                case PLTDrawMode.Freeform:
                    if (m_cachedControlPointCount >= 1) {
                        m_mainArm1.a = m_cachedControlPoints[0].m_position;
                        m_mainArm1.b = m_cachedControlPoints[1].m_position;
                    }
                    if (m_cachedControlPointCount >= 2) {
                        if (m_useCOBezierMethod == true) {
                            //uses negative of endDirection
                            m_mainBezier = PLTMath.QuadraticToCubicBezierCOMethod(m_cachedControlPoints[0].m_position, m_cachedControlPoints[1].m_direction, m_cachedControlPoints[2].m_position, (-m_cachedControlPoints[2].m_direction));
                        } else {
                            m_mainBezier = PLTMath.QuadraticToCubicBezier(m_cachedControlPoints[0].m_position, m_cachedControlPoints[1].m_position, m_cachedControlPoints[2].m_position);
                        }
                        m_mainArm2.a = m_cachedControlPoints[1].m_position;
                        m_mainArm2.b = m_cachedControlPoints[2].m_position;
                        //***SUPER-IMPORTANT (for convergence of fenceMode)***
                        PLTMath.BezierXZ(ref m_mainBezier);
                        //calculate direction here in case controlPoint direction was not set correctly
                        Vector3 dirArm1 = m_mainArm1.b - m_mainArm1.a;
                        dirArm1.y = 0f;
                        dirArm1.Normalize();
                        Vector3 dirArm2 = m_mainArm2.b - m_mainArm2.a;
                        dirArm2.y = 0f;
                        dirArm2.Normalize();
                        m_mainElbowAngle = Mathf.Abs(PLTMath.AngleSigned(-dirArm1, dirArm2, Vector3.up));
                    }
                    break;
                case PLTDrawMode.Circle:
                    if (m_cachedControlPointCount >= 1) {
                        Vector3 center = m_cachedControlPoints[0].m_position;
                        Vector3 pointOnCircle = m_cachedControlPoints[1].m_position;
                        center.y = 0f;
                        pointOnCircle.y = 0f;
                        Circle3XZ mainCircle = new Circle3XZ(center, pointOnCircle);
                        m_rawCircle = mainCircle;
                        //perfect circle radius-snapping
                        if (PLTSettings.PerfectCircles) {
                            switch (m_controlMode) {
                            case PLTControlMode.Itemwise:
                            case PLTControlMode.Spacing:
                                //snap to perfect circle
                                if (m_fenceMode) {
                                    mainCircle.m_radius = mainCircle.PerfectRadiusByChords(m_spacingSingle);
                                } else {
                                    mainCircle.m_radius = mainCircle.PerfectRadiusByArcs(m_spacingSingle);
                                }
                                break;
                            }
                        }
                        m_mainCircle = mainCircle;
                    }
                    break;
                }
            }
        }

        private bool ModifyControlPoint(Vector3 position, int pointNumber) {
            bool result = true;
            int pointCount = m_controlPointCount;
            bool flag = pointNumber <= (pointCount + 1);
            bool flag2 = ((pointCount >= 0) && (pointCount <= 3));

            if (flag && flag2) {
                switch (pointNumber - 1) {
                case 0: //first point
                    if ((pointCount == 0 || pointCount == 1) && m_activeState == PLTActiveState.CreatePointFirst) {
                        m_controlPoints[0].m_position = position;
                        if (m_snapMode != PLTSnapMode.Objects && m_snapMode != PLTSnapMode.ZoneLines) {
                            m_controlPoints[0].m_direction = Vector3.zero;
                        }
                    } else if ((pointCount == 2 || pointCount == 3) && m_activeState == PLTActiveState.MovePointFirst) {
                        //when pointcount = 2, should be adjusting line startpoint
                        //when pointcount = 3, should be adjusting curve startpoint
                        Vector3 normVector = (m_controlPoints[1].m_position - position);
                        normVector.y = 0f;
                        normVector.Normalize();
                        m_controlPoints[0].m_position = position;
                        m_controlPoints[0].m_direction = normVector;
                        m_controlPoints[1].m_direction = normVector;
                        if (m_drawMode == PLTDrawMode.Freeform) {
                            //do the freeform algorithm stuff
                            ReverseControlPoints3();
                            CalculateFreeformMiddlePoint(ref m_controlPoints);
                            ReverseControlPoints3();
                        }
                    } else if (((pointCount == 1 && m_activeState == PLTActiveState.CreatePointSecond) ||
                                (pointCount == 2 && m_activeState == PLTActiveState.CreatePointThird)) &&
                                (m_drawMode == PLTDrawMode.Straight || m_drawMode == PLTDrawMode.Circle)) {
                        m_controlPoints[0].m_position = position;
                        Vector3 normVector = (m_controlPoints[1].m_position - position);
                        normVector.y = 0f;
                        normVector.Normalize();
                        m_controlPoints[0].m_direction = normVector;
                        m_controlPoints[1].m_direction = normVector;
                        UpdateCachedControlPoints();
                        UpdateCurves();
                    } else {
                        result = false;
                    }
                    break;
                case 1: //second point (end for Straight mode)
                    //for lines, this means either placement or movement
                    //for curves, this means either placement or movement
                    if (pointCount == 1 && m_activeState == PLTActiveState.CreatePointSecond) { //placement
                        Vector3 normVector = (position - m_controlPoints[0].m_position);
                        normVector.y = 0f;
                        normVector.Normalize();
                        m_controlPoints[1].m_position = position;
                        if (m_snapMode != PLTSnapMode.Objects && m_snapMode != PLTSnapMode.ZoneLines) {
                            m_controlPoints[0].m_direction = normVector;
                        }
                        m_controlPoints[1].m_direction = normVector;
                    } else if ((pointCount == 2 || pointCount == 3) && m_activeState == PLTActiveState.MovePointSecond) { //movement
                        Vector3 normVector = position - m_controlPoints[0].m_position;
                        normVector.y = 0f;
                        normVector.Normalize();
                        m_controlPoints[1].m_position = position;
                        Vector3 normVector2 = (m_controlPoints[2].m_position - position);
                        normVector2.y = 0f;
                        normVector2.Normalize();
                        m_controlPoints[2].m_direction = normVector2;
                        if (m_snapMode != PLTSnapMode.Objects && m_snapMode != PLTSnapMode.ZoneLines) {
                            m_controlPoints[0].m_direction = normVector;
                        }
                        m_controlPoints[1].m_direction = normVector;
                        if (m_drawMode == PLTDrawMode.Freeform) {
                            CalculateFreeformMiddlePoint(ref m_controlPoints);
                        }
                    } else {
                        result = false;
                    }
                    break;
                case 2: //third point (end for Curved and Freeform modes)
                    if (pointCount == 2 && m_activeState == PLTActiveState.CreatePointThird) {
                        if (m_drawMode == PLTDrawMode.Freeform) {
                            m_controlPoints[2].m_position = position;
                            //normal Freeform algorithm from NetTool.SimulationStep
                            CalculateFreeformMiddlePoint(ref m_controlPoints);
                        } else {
                            Vector3 normVector = (position - m_controlPoints[1].m_position);
                            normVector.y = 0f;
                            normVector.Normalize();
                            m_controlPoints[2].m_position = position;
                            m_controlPoints[2].m_direction = normVector;
                        }
                    } else if ((pointCount == 2 || pointCount == 3) && m_activeState == PLTActiveState.MovePointThird) {
                        if (m_drawMode == PLTDrawMode.Freeform) {
                            m_controlPoints[2].m_position = position;
                            //normal Freeform algorithm from NetTool.SimulationStep
                            CalculateFreeformMiddlePoint(ref m_controlPoints);
                        } else {
                            Vector3 normVector = (position - m_controlPoints[1].m_position);
                            normVector.y = 0f;
                            normVector.Normalize();
                            m_controlPoints[2].m_position = position;
                            m_controlPoints[2].m_direction = normVector;
                        }
                    } else {
                        result = false;
                    }
                    break;
                }
            } else {
                if (!flag2) {
                    Debug.LogError("[PLT]: ModifyControlPoint(): m_controlPointCount is out of bounds! (value = " + this.m_controlPointCount.ToString() + ")");
                }
                result = false;
            }
            if (result) {
                UpdateCachedControlPoints();
            }
            return result;
        }

        private void ReverseControlPoints3() {
            PLTControlPoint buffer = m_controlPoints[0];
            m_controlPoints[0] = m_controlPoints[2];
            m_controlPoints[2] = buffer;
            m_controlPoints[0].m_direction *= -1f;
            m_controlPoints[2].m_direction *= -1f;
            m_controlPoints[1].m_direction = m_controlPoints[0].m_direction;
        }

        private void UpdateCurves() {
            if (m_cachedControlPointCount == 0) {
                return;
            } else {
                m_pendingPlacementUpdate = true;
                switch (m_drawMode) {
                case PLTDrawMode.Straight:
                    if (m_cachedControlPointCount >= 1) {
                        m_mainSegment.a = m_cachedControlPoints[0].m_position;
                        m_mainSegment.b = m_cachedControlPoints[1].m_position;
                    }
                    break;
                case PLTDrawMode.Curved:
                case PLTDrawMode.Freeform:
                    if (m_cachedControlPointCount >= 1) {
                        m_mainArm1.a = m_cachedControlPoints[0].m_position;
                        m_mainArm1.b = m_cachedControlPoints[1].m_position;
                    }
                    if (m_cachedControlPointCount >= 2) {
                        if (m_useCOBezierMethod) {
                            //uses negative of endDirection
                            m_mainBezier = PLTMath.QuadraticToCubicBezierCOMethod(m_cachedControlPoints[0].m_position, m_cachedControlPoints[1].m_direction, m_cachedControlPoints[2].m_position, (-m_cachedControlPoints[2].m_direction));
                        } else {
                            m_mainBezier = PLTMath.QuadraticToCubicBezier(m_cachedControlPoints[0].m_position, m_cachedControlPoints[1].m_position, m_cachedControlPoints[2].m_position);
                        }
                        m_mainArm2.a = m_cachedControlPoints[1].m_position;
                        m_mainArm2.b = m_cachedControlPoints[2].m_position;
                        //***SUPER-IMPORTANT (for convergence of fenceMode)***
                        PLTMath.BezierXZ(ref m_mainBezier);
                        //calculate direction here in case controlPoint direction was not set correctly
                        Vector3 dirArm1 = (m_mainArm1.b - m_mainArm1.a);
                        dirArm1.y = 0f;
                        dirArm1.Normalize();
                        Vector3 dirArm2 = (m_mainArm2.b - m_mainArm2.a);
                        dirArm2.y = 0f;
                        dirArm2.Normalize();
                        m_mainElbowAngle = Mathf.Abs(PLTMath.AngleSigned(-dirArm1, dirArm2, Vector3.up));
                    }
                    break;
                case PLTDrawMode.Circle:
                    if (m_cachedControlPointCount >= 1) {
                        Vector3 center = m_cachedControlPoints[0].m_position;
                        Vector3 pointOnCircle = m_cachedControlPoints[1].m_position;
                        center.y = 0f;
                        pointOnCircle.y = 0f;
                        Circle3XZ mainCircle = new Circle3XZ(center, pointOnCircle);
                        m_rawCircle = mainCircle;
                        //perfect circle radius-snapping
                        if (PLTSettings.PerfectCircles) {
                            switch (m_controlMode) {
                            case PLTControlMode.Itemwise:
                            case PLTControlMode.Spacing:
                                //snap to perfect circle
                                if (m_fenceMode) {
                                    mainCircle.m_radius = mainCircle.PerfectRadiusByChords(m_spacingSingle);
                                } else {
                                    mainCircle.m_radius = mainCircle.PerfectRadiusByArcs(m_spacingSingle);
                                }
                                break;
                            }
                        }
                        m_mainCircle = mainCircle;
                    }
                    break;
                }
            }
        }

        private void UpdateCachedControlPoints() {
            while (!Monitor.TryEnter(m_cacheLock, SimulationManager.SYNCHRONIZE_TIMEOUT)) { }
            try {
                for (int i = 0; i < MAX_CONTROLPOINT_LENGTH; i++) {
                    m_cachedControlPoints[i] = m_controlPoints[i];
                }
                m_cachedControlPointCount = m_controlPointCount;
            } finally {
                Monitor.Exit(m_cacheLock);
            }
        }

        public override void RenderGeometry(RenderManager.CameraInfo cameraInfo) {
            switch (m_activeState) {
            case PLTActiveState.CreatePointSecond: //creating second control point
                if (m_drawMode == PLTDrawMode.Straight || m_drawMode == PLTDrawMode.Circle) {
                    RenderItems(cameraInfo);
                }
                break;
            case PLTActiveState.CreatePointThird: //creating third control point
            case PLTActiveState.LockIdle: //in lock mode, awaiting user input
            case PLTActiveState.MovePointFirst: //in lock mode, moving first control point
            case PLTActiveState.MovePointSecond: //in lock mode, moving second control point
            case PLTActiveState.MovePointThird: //in lock mode, moving third control point
            case PLTActiveState.MoveSegment: //in lock mode, moving full line or curve
            case PLTActiveState.ChangeSpacing: //in lock mode, changing item-to-item spacing along the line or curve
            case PLTActiveState.ChangeAngle: //in lock mode, changing initial item (first item's) angle
            case PLTActiveState.ItemwiseLock:
            case PLTActiveState.MoveItemwiseItem:
            case PLTActiveState.MaxFillContinue: //out of bounds
                RenderItems(cameraInfo);
                break;
            }
        }

        public bool IsActiveStateAnItemRenderState() {
            switch (m_drawMode) {
            case PLTDrawMode.Straight:
            case PLTDrawMode.Circle:
                switch (m_activeState) {
                case PLTActiveState.CreatePointFirst:
                    return false;
                case PLTActiveState.CreatePointSecond:
                case PLTActiveState.CreatePointThird:
                case PLTActiveState.LockIdle:
                case PLTActiveState.MovePointFirst:
                case PLTActiveState.MovePointSecond:
                case PLTActiveState.MovePointThird:
                case PLTActiveState.MoveSegment:
                case PLTActiveState.ChangeSpacing:
                case PLTActiveState.ChangeAngle:
                case PLTActiveState.ItemwiseLock:
                case PLTActiveState.MoveItemwiseItem:
                case PLTActiveState.MaxFillContinue:
                    return true;
                }
                break;
            case PLTDrawMode.Curved:
            case PLTDrawMode.Freeform:
                switch (m_activeState) {
                case PLTActiveState.CreatePointFirst:
                case PLTActiveState.CreatePointSecond:
                    return false;
                case PLTActiveState.CreatePointThird:
                case PLTActiveState.LockIdle:
                case PLTActiveState.MovePointFirst:
                case PLTActiveState.MovePointSecond:
                case PLTActiveState.MovePointThird:
                case PLTActiveState.MoveSegment:
                case PLTActiveState.ChangeSpacing:
                case PLTActiveState.ChangeAngle:
                case PLTActiveState.MaxFillContinue:
                    return true;
                }
                break;
            }
            return false;
        }

        public void RenderMaxFillContinueMarkers(RenderManager.CameraInfo cameraInfo) {
            const float radius = 6f;
            if (m_controlMode == PLTControlMode.Itemwise) return;
            Color maxFillContinueColor = m_PLTColor_MaxFillContinue;
            //initial item
            Vector3 initialItemPosition = PlacementCalculator.InitialItemPosition;
            ref PLTItemPlacementInfo initialItem = ref PlacementCalculator.m_placementInfos[0];
            Segment3 thresholdMarkerInitial = new Segment3(initialItemPosition - (initialItem.m_offsetDirection * radius), initialItemPosition + (initialItem.m_offsetDirection * radius));
            RenderSegment(cameraInfo, thresholdMarkerInitial, 0.25f, 0f, maxFillContinueColor, false, true);
            //final item
            Vector3 finalItemPosition = PlacementCalculator.FinalItemPosition;
            ref PLTItemPlacementInfo finalItem = ref PlacementCalculator.m_placementInfos[PlacementCalculator.m_itemCount - 1];
            Segment3 thresholdMarkerFinal = new Segment3(finalItemPosition - (finalItem.m_offsetDirection * radius), finalItemPosition + (finalItem.m_offsetDirection * radius));
            RenderCircle(cameraInfo, finalItemPosition, 0.5f, maxFillContinueColor, false, true);
            RenderCircle(cameraInfo, finalItemPosition, radius, maxFillContinueColor, false, true);
            RenderSegment(cameraInfo, thresholdMarkerFinal, 0.25f, 0f, maxFillContinueColor, false, true);
            //mouse indicators
            maxFillContinueColor.a *= 0.40f;
            RenderSegment(cameraInfo, new Segment3(m_mousePosition, initialItemPosition), 0.05f, 3.00f, maxFillContinueColor, false, true);
            RenderSegment(cameraInfo, new Segment3(m_mousePosition, finalItemPosition), 0.05f, 3.00f, maxFillContinueColor, false, true);
        }


        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo) {
            base.RenderOverlay(cameraInfo);
            Event e = Event.current;
            //bool twoPointedDrawMode = m_drawMode == PLTDrawMode.Straight || m_drawMode == PLTDrawMode.Circle;
            bool renderMFC = m_segmentState.IsReadyForMaxContinue || m_segmentState.IsMaxFillContinue;

            Color mainCurveColor = m_PLTColor_locked;
            Color lockIdleColor = m_PLTColor_locked;
            Color lockIdleColorStrong = m_PLTColor_lockedStrong;
            //Color highlightColor = m_PLTColor_lockedHighlight;
            Color curveWarningColor = m_PLTColor_curveWarning;
            Color copyPlaceColor = m_PLTColor_copyPlace;
            Color itemwiseLockColor = m_PLTColor_ItemwiseLock;
            //Color maxFillContinueColor = m_PLTColor_MaxFillContinue;

            Color createPointColor = m_controlMode == PLTControlMode.Itemwise ? m_PLTColor_ItemwiseLock : m_PLTColor_default;

            if ((e.modifiers & EventModifiers.Control) == EventModifiers.Control && PLTSettings.ShowUndoPreviews) {
                //undoManager.RenderLatestEntryCircles(cameraInfo, m_PLTColor_undoItemOverlay);
            }

            if (IsActiveStateAnItemRenderState() && m_segmentState.IsReadyForMaxContinue) {
                copyPlaceColor = m_PLTColor_MaxFillContinue;
            }

            switch (m_activeState) {
            case PLTActiveState.CreatePointFirst: //creating first control point
                if (!m_toolController.IsInsideUI && Cursor.visible) {
                    //medium circle
                    RenderCircle(cameraInfo, m_cachedPosition, 1.00f, createPointColor, false, false);
                    //small pinpoint circle
                    RenderCircle(cameraInfo, m_cachedPosition, 0.10f, createPointColor, false, true);
                }
                break;
            case PLTActiveState.CreatePointSecond: //creating second control point
                bool gotoCreatePointFirst = false;
                switch (m_drawMode) {
                case PLTDrawMode.Straight:
                case PLTDrawMode.Circle:
                    if (m_cachedControlPoints[1].m_direction != Vector3.zero) {
                        if ((e.modifiers & EventModifiers.Alt) == EventModifiers.Alt) {
                            createPointColor = copyPlaceColor; ;
                        } else if ((e.modifiers & EventModifiers.Control) == EventModifiers.Control) {
                            createPointColor = m_PLTColor_locked;
                        }
                        switch (m_drawMode) {
                        case PLTDrawMode.Straight:
                            if (!m_segmentState.m_allItemsValid) {
                                RenderSegment(cameraInfo, m_mainSegment, 1.50f, 0f, curveWarningColor, false, true);
                            }
                            RenderSegment(cameraInfo, m_mainSegment, 1.00f, 0f, createPointColor, false, true);
                            break;
                        case PLTDrawMode.Circle:
                            if (!m_segmentState.m_allItemsValid) {
                                RenderMainCircle(cameraInfo, m_mainCircle, 1.50f, curveWarningColor, false, true);
                            }
                            RenderMainCircle(cameraInfo, m_mainCircle, 1.00f, createPointColor, false, true);
                            break;
                        }
                        //MaxFillContinue
                        if (renderMFC) {
                            RenderMaxFillContinueMarkers(cameraInfo);
                        }
                    } else {
                        gotoCreatePointFirst = true;
                    }
                    break;
                case PLTDrawMode.Curved:
                case PLTDrawMode.Freeform:
                    RenderLine(cameraInfo, m_mainArm1, 1.00f, 2f, createPointColor, false, false);
                    break;
                }
                if (gotoCreatePointFirst) {
                    goto case PLTActiveState.CreatePointFirst;
                }
                //small pinpoint circles
                RenderCircle(cameraInfo, m_cachedControlPoints[0].m_position, 0.10f, createPointColor, false, true);
                RenderCircle(cameraInfo, m_cachedControlPoints[1].m_position, 0.10f, createPointColor, false, true);
                break;
            case PLTActiveState.CreatePointThird: //creating third control point
                if (true || m_cachedControlPoints[2].m_direction != Vector3.zero) {
                    if ((m_drawMode == PLTDrawMode.Curved) || (m_drawMode == PLTDrawMode.Freeform)) { //not sure if this is necessary
                        if ((e.modifiers & EventModifiers.Alt) == EventModifiers.Alt) {
                            createPointColor = copyPlaceColor;
                        } else if ((e.modifiers & EventModifiers.Control) == EventModifiers.Control) {
                            createPointColor = m_PLTColor_locked;
                        }
                        if (!m_segmentState.m_allItemsValid) {
                            RenderBezier(cameraInfo, m_mainBezier, 1.50f, curveWarningColor, false, true);
                        }
                        //for the size for these it should be 1/4 the size for renderline
                        RenderElbow(cameraInfo, m_mainArm1, m_mainArm2, 1.00f, 2f, createPointColor, false, true);
                        RenderBezier(cameraInfo, m_mainBezier, 1.00f, createPointColor, false, true);
                        //MaxFillContinue
                        if (renderMFC) {
                            RenderMaxFillContinueMarkers(cameraInfo);
                        }
                    }
                } else {
                    goto case PLTActiveState.CreatePointSecond;
                }
                //small pinpoint circles
                RenderCircle(cameraInfo, m_cachedControlPoints[0].m_position, 0.10f, createPointColor, false, true);
                RenderCircle(cameraInfo, m_cachedControlPoints[1].m_position, 0.10f, createPointColor, false, true);
                RenderCircle(cameraInfo, m_cachedControlPoints[2].m_position, 0.10f, createPointColor, false, true);
                break;
            //in lock mode, moving first control point
            case PLTActiveState.MovePointFirst:
            //in lock mode, moving second control point
            case PLTActiveState.MovePointSecond:
            //in lock mode, moving third control point
            case PLTActiveState.MovePointThird:
            //in lock mode, moving full line or curve
            case PLTActiveState.MoveSegment:
            //in lock mode, changing item-to-item spacing along the line or curve
            case PLTActiveState.ChangeSpacing:
            //in lock mode, changing initial item (first item's) angle
            case PLTActiveState.ChangeAngle:
            //in lock mode, awaiting user input
            case PLTActiveState.LockIdle:
            //in itemwise lock mode, awaiting user input
            case PLTActiveState.ItemwiseLock:
            //in lock mode, moving position of single item
            case PLTActiveState.MoveItemwiseItem:
                lockIdleColor = m_hoverState == PLTHoverState.SpacingLocus ? lockIdleColorStrong : lockIdleColor;
                if ((e.modifiers & EventModifiers.Alt) == EventModifiers.Alt) {
                    mainCurveColor = copyPlaceColor;
                } else {
                    //MaxFillContinue
                    if ((m_activeState == PLTActiveState.LockIdle || m_activeState == PLTActiveState.MaxFillContinue) && renderMFC) {
                        RenderMaxFillContinueMarkers(cameraInfo);
                    }
                    if ((e.modifiers & EventModifiers.Control) == EventModifiers.Control) {
                        if (m_controlMode == PLTControlMode.Itemwise) {
                            if (m_activeState == PLTActiveState.ItemwiseLock) {
                                mainCurveColor = lockIdleColor;
                            } else if (m_activeState == PLTActiveState.LockIdle) {
                                mainCurveColor = itemwiseLockColor;
                            }
                        } else { //not in itemwise mode
                            mainCurveColor = createPointColor;
                        }
                    } else {
                        if (m_controlMode == PLTControlMode.Itemwise) {
                            if (m_activeState == PLTActiveState.ItemwiseLock) {
                                mainCurveColor = itemwiseLockColor;
                            } else if (m_activeState == PLTActiveState.LockIdle) {
                                mainCurveColor = lockIdleColor;
                            }
                        } else { //not in itemwise mode
                            mainCurveColor = lockIdleColor;
                        }
                        //show adjustment circles
                        RenderHoverObjectOverlays(cameraInfo, false);
                        if (m_hoverState == PLTHoverState.Curve && m_activeState == PLTActiveState.LockIdle) {
                            mainCurveColor = (e.modifiers & EventModifiers.Alt) == EventModifiers.Alt ? m_PLTColor_copyPlaceHighlight : m_PLTColor_lockedHighlight;
                        }
                    }
                }
                switch (m_drawMode) {
                case PLTDrawMode.Straight:
                    RenderSegment(cameraInfo, m_mainSegment, 1.00f, 0f, mainCurveColor, false, false);
                    if (!m_segmentState.m_allItemsValid) {
                        RenderSegment(cameraInfo, m_mainSegment, 1.50f, 0f, curveWarningColor, false, true);
                    }
                    break;
                case PLTDrawMode.Curved:
                case PLTDrawMode.Freeform:
                    RenderElbow(cameraInfo, m_mainArm1, m_mainArm2, 1.00f, 2f, lockIdleColor, false, true);
                    if (m_hoverState == PLTHoverState.SpacingLocus) {
                        RenderBezier(cameraInfo, m_mainBezier, 1.00f, mainCurveColor, false, true);
                    } else {
                        RenderBezier(cameraInfo, m_mainBezier, 1.00f, mainCurveColor, false, false);
                    }
                    if (!m_segmentState.m_allItemsValid) {
                        RenderBezier(cameraInfo, m_mainBezier, 1.50f, curveWarningColor, false, true);
                    }
                    break;
                case PLTDrawMode.Circle:
                    RenderMainCircle(cameraInfo, m_mainCircle, 1.00f, mainCurveColor, false, true);
                    if (!m_segmentState.m_allItemsValid) {
                        RenderMainCircle(cameraInfo, m_mainCircle, 1.50f, curveWarningColor, false, true);
                    }
                    break;
                }
                //small pinpoint circles
                RenderCircle(cameraInfo, m_cachedControlPoints[0].m_position, 0.10f, lockIdleColor, false, true);
                RenderCircle(cameraInfo, m_cachedControlPoints[1].m_position, 0.10f, lockIdleColor, false, true);
                RenderCircle(cameraInfo, m_cachedControlPoints[2].m_position, 0.10f, lockIdleColor, false, true);
                break;
            case PLTActiveState.MaxFillContinue:
                switch (m_drawMode) {
                case PLTDrawMode.Straight:
                case PLTDrawMode.Circle:
                    if (m_cachedControlPoints[1].m_direction != Vector3.zero) {
                        //MaxFillContinue
                        if (renderMFC) {
                            RenderMaxFillContinueMarkers(cameraInfo);
                        }
                        if ((e.modifiers & EventModifiers.Alt) == EventModifiers.Alt) {
                            createPointColor = copyPlaceColor;
                        } else if ((e.modifiers & EventModifiers.Control) == EventModifiers.Control) {
                            createPointColor = lockIdleColor;
                        }
                        switch (m_drawMode) {
                        case PLTDrawMode.Straight:
                            if (!m_segmentState.m_allItemsValid) {
                                RenderSegment(cameraInfo, m_mainSegment, 1.50f, 0f, curveWarningColor, false, true);
                            }
                            RenderSegment(cameraInfo, m_mainSegment, 1.00f, 0f, createPointColor, false, true);
                            break;
                        case PLTDrawMode.Circle:
                            if (!m_segmentState.m_allItemsValid) {
                                RenderMainCircle(cameraInfo, m_mainCircle, 1.50f, curveWarningColor, false, true);
                            }
                            RenderMainCircle(cameraInfo, m_mainCircle, 1.00f, createPointColor, false, true);
                            break;
                        }
                    }
                    //small pinpoint circles
                    RenderCircle(cameraInfo, m_cachedControlPoints[0].m_position, 0.10f, createPointColor, false, true);
                    RenderCircle(cameraInfo, m_cachedControlPoints[1].m_position, 0.10f, createPointColor, false, true);
                    break;
                case PLTDrawMode.Curved:
                case PLTDrawMode.Freeform:
                    if (true || m_cachedControlPoints[2].m_direction != Vector3.zero) {
                        if ((e.modifiers & EventModifiers.Alt) == EventModifiers.Alt) {
                            createPointColor = copyPlaceColor;
                        } else if ((e.modifiers & EventModifiers.Control) == EventModifiers.Control) {
                            createPointColor = lockIdleColor;
                        } else {
                            if (renderMFC) {
                                RenderMaxFillContinueMarkers(cameraInfo);
                            }
                        }
                        if (!m_segmentState.m_allItemsValid) {
                            RenderBezier(cameraInfo, m_mainBezier, 1.50f, curveWarningColor, false, true);
                        }
                        //for the size for these it should be 1/4 the size for renderline
                        RenderElbow(cameraInfo, m_mainArm1, m_mainArm2, 1.00f, 2f, createPointColor, false, true);
                        RenderBezier(cameraInfo, m_mainBezier, 1.00f, createPointColor, false, true);
                    }
                    //small pinpoint circles
                    RenderCircle(cameraInfo, m_cachedControlPoints[0].m_position, 0.10f, createPointColor, false, true);
                    RenderCircle(cameraInfo, m_cachedControlPoints[1].m_position, 0.10f, createPointColor, false, true);
                    RenderCircle(cameraInfo, m_cachedControlPoints[2].m_position, 0.10f, createPointColor, false, true);
                    break;
                }
                break;
            }
            if (PLTSettings.ShowErrorGuides) {
                RenderPlacementErrorOverlays(cameraInfo);
            }
        }

        private static int HoverItemPositionIndex => m_controlMode == PLTControlMode.Itemwise ? ITEMWISE_INDEX : 1;
        //Index for rendering angle locus around
        private static int HoverItemAngleCenterIndex => m_controlMode == PLTControlMode.Itemwise ? ITEMWISE_INDEX : 1;

        public void RenderPlacementErrorOverlays(RenderManager.CameraInfo cameraInfo) {
            bool anarchy = PLTSettings.AnarchyPLT;
            bool @override = anarchy || (!anarchy && PLTSettings.PlaceBlockedItems);
            if ((m_segmentState.m_allItemsValid && !@override) || PlacementCalculator.m_itemCount <= 0 || !IsActiveStateAnItemRenderState()) return;
            Color32 blockedColor = @override ? new Color32(219, 192, 82, 80) : new Color32(219, 192, 82, 200);
            Color32 invalidPlacementColor = anarchy ? new Color32(193, 78, 72, 50) : new Color32(193, 78, 72, 200);
            float radius;
            switch (m_objectMode) {
            case PLTObjectMode.Props:
                PropInfo propInfo = m_propInfo;
                radius = Mathf.Max(propInfo.m_generatedInfo.m_size.x, propInfo.m_generatedInfo.m_size.z) * Mathf.Max(propInfo.m_maxScale, propInfo.m_minScale);
                break;
            case PLTObjectMode.Trees:
                TreeInfo treeInfo = m_treeInfo;
                radius = Mathf.Max(treeInfo.m_generatedInfo.m_size.x, treeInfo.m_generatedInfo.m_size.z) * Mathf.Max(treeInfo.m_maxScale, treeInfo.m_minScale);
                break;
            default:
                return;
            }
            int itemCount = PlacementCalculator.m_itemCount;
            for (int i = 0; i < itemCount; i++) {
                if (!PlacementCalculator.m_placementInfos[i].m_isValidPlacement || @override) {
                    Vector3 itemPos = PlacementCalculator.m_placementInfos[i].m_position;
                    if (PlacementCalculator.m_placementInfos[i].m_collisionFlags == ItemCollisionType.Blocked) {
                        RenderCircle(cameraInfo, itemPos, 0.10f, blockedColor, false, false);
                        RenderCircle(cameraInfo, itemPos, 2f, blockedColor, false, false);
                        RenderCircle(cameraInfo, itemPos, radius, blockedColor, false, true);
                    } else {
                        RenderCircle(cameraInfo, itemPos, 0.10f, invalidPlacementColor, false, false);
                        RenderCircle(cameraInfo, itemPos, 2f, invalidPlacementColor, false, false);
                        RenderCircle(cameraInfo, itemPos, radius, invalidPlacementColor, false, true);
                    }
                }
            }
        }

        public void RenderHoverObjectOverlays(RenderManager.CameraInfo cameraInfo, bool altDown) {
            if (PlacementCalculator.m_itemCount < 1 + (m_fenceMode ? 0 : 1)) {
                if (m_controlMode != PLTControlMode.Itemwise) return;
            }
            //setup highlight colors
            Color32 baseColor = m_PLTColor_hoverBase;
            Color32 lockIdleColor = m_PLTColor_locked;
            Color32 highlightColor = m_PLTColor_lockedHighlight;
            if (altDown) {
                baseColor = m_PLTColor_hoverCopyPlace;
                highlightColor = m_PLTColor_copyPlaceHighlight;
            }
            bool angleObjectMode = m_objectMode == PLTObjectMode.Props;

            switch (m_activeState) {
            case PLTActiveState.Undefined:
            case PLTActiveState.CreatePointFirst:
            case PLTActiveState.CreatePointSecond:
            case PLTActiveState.CreatePointThird:
            case PLTActiveState.MaxFillContinue:
                return;
            case PLTActiveState.LockIdle:
                RenderCircle(cameraInfo, m_cachedControlPoints[0].m_position, m_hoverPointDiameter, m_hoverState == PLTHoverState.ControlPointFirst ? highlightColor : baseColor, false, false);
                RenderCircle(cameraInfo, m_cachedControlPoints[1].m_position, m_hoverPointDiameter, m_hoverState == PLTHoverState.ControlPointSecond ? highlightColor : baseColor, false, false);
                switch (m_drawMode) {
                case PLTDrawMode.Curved:
                case PLTDrawMode.Freeform:
                    RenderCircle(cameraInfo, m_cachedControlPoints[2].m_position, m_hoverPointDiameter, m_hoverState == PLTHoverState.ControlPointThird ? highlightColor : baseColor, false, false);
                    break;
                }
                //spacing control point
                Vector3 spacingPos = m_fenceMode ? PlacementCalculator.m_fenceEndPoints[HoverItemPositionIndex] : PlacementCalculator.m_placementInfos[HoverItemPositionIndex].m_position;
                RenderCircle(cameraInfo, spacingPos, m_hoverPointDiameter, m_hoverState == PLTHoverState.SpacingLocus || m_hoverState == PLTHoverState.ItemwiseItem ? highlightColor : baseColor, false, false);
                //spacing fill indicator
                if (m_hoverState == PLTHoverState.SpacingLocus) {
                    RenderProgressiveSpacingFill(cameraInfo, m_spacingSingle, 1.00f, 0.20f, Color.Lerp(highlightColor, lockIdleColor, 0.50f), false, true);
                }
                //ANGLE
                Vector3 angleCenter;
                Vector3 anglePos;
                Color32 blendColor;
                if (angleObjectMode) {
                    //angle indicator
                    angleCenter = PlacementCalculator.m_placementInfos[HoverItemAngleCenterIndex].m_position;
                    anglePos = Circle2.Position3FromAngleXZ(angleCenter, m_hoverAngleLocusDiameter, m_hoverAngle);
                    Color32 angleColor = m_hoverState == PLTHoverState.AngleLocus ? highlightColor : baseColor;
                    RenderCircle(cameraInfo, anglePos, m_hoverPointDiameter, angleColor, false, false);
                    //angle locus
                    blendColor = Color.Lerp(baseColor, angleColor, 0.50f);
                    blendColor.a = 88;
                    RenderCircle(cameraInfo, angleCenter, m_hoverAngleLocusDiameter * 2f, blendColor, false, true);
                    //angle indicator line
                    Segment3 _angleLine = new Segment3(angleCenter, anglePos);
                    RenderLine(cameraInfo, _angleLine, 0.05f, 0.50f, blendColor, false, true);
                }
                break;
            case PLTActiveState.MovePointFirst:
                RenderCircle(cameraInfo, m_cachedControlPoints[0].m_position, m_hoverPointDiameter, highlightColor, false, false);
                break;
            case PLTActiveState.MovePointSecond:
                RenderCircle(cameraInfo, m_cachedControlPoints[1].m_position, m_hoverPointDiameter, highlightColor, false, false);
                break;
            case PLTActiveState.MovePointThird:
                if (m_drawMode == PLTDrawMode.Curved || m_drawMode == PLTDrawMode.Freeform) {
                    RenderCircle(cameraInfo, m_cachedControlPoints[2].m_position, m_hoverPointDiameter, highlightColor, false, false);
                }
                break;
            case PLTActiveState.MoveSegment:
                //this is done in RenderOverlay()
                break;
            case PLTActiveState.ChangeSpacing:
                //item second
                spacingPos = m_fenceMode ? PlacementCalculator.m_fenceEndPoints[HoverItemPositionIndex] : PlacementCalculator.m_placementInfos[HoverItemPositionIndex].m_position;
                RenderCircle(cameraInfo, spacingPos, m_hoverPointDiameter, highlightColor, false, false);
                if (m_fenceMode) {
                    blendColor = Color.Lerp(baseColor, highlightColor, 0.50f);
                    RenderLine(cameraInfo, new Segment3(PlacementCalculator.m_fenceEndPoints[0], PlacementCalculator.m_fenceEndPoints[1]), 0.05f, 0.50f, blendColor, false, true);
                } else {
                    RenderProgressiveSpacingFill(cameraInfo, m_spacingSingle, 1.00f, 0.20f, highlightColor, false, true);
                }
                break;
            case PLTActiveState.ChangeAngle:
                //ANGLE
                angleCenter = PlacementCalculator.m_placementInfos[HoverItemAngleCenterIndex].m_position;
                anglePos = Circle2.Position3FromAngleXZ(angleCenter, m_hoverAngleLocusDiameter, m_hoverAngle);
                RenderCircle(cameraInfo, anglePos, m_hoverPointDiameter, highlightColor, false, false);
                //angle locus
                blendColor = Color.Lerp(baseColor, highlightColor, 0.50f);
                blendColor.a = 88;
                RenderCircle(cameraInfo, angleCenter, m_hoverAngleLocusDiameter * 2f, blendColor, false, true);
                //angle indicator line
                RenderLine(cameraInfo, new Segment3(angleCenter, anglePos), 0.05f, 0.50f, blendColor, false, true);
                break;
            case PLTActiveState.MoveItemwiseItem:
                Vector3 _spacingPos = m_fenceMode ? PlacementCalculator.m_fenceEndPoints[HoverItemPositionIndex] : PlacementCalculator.m_placementInfos[HoverItemPositionIndex].m_position;
                RenderCircle(cameraInfo, _spacingPos, m_hoverPointDiameter, highlightColor, false, false);
                break;
            }
        }

        public static void RenderCircle(RenderManager.CameraInfo cameraInfo, Vector3 position, float size, Color color, bool renderLimits, bool alphaBlend) {
            Singleton<ToolManager>.instance.m_drawCallData.m_overlayCalls++;
            m_drawCircleFunc(cameraInfo, color, position, size, -1f, 1280f, renderLimits, alphaBlend);
        }

        public static void RenderLine(RenderManager.CameraInfo cameraInfo, Segment3 segment, float size, float dashLength, Color color, bool renderLimits, bool alphaBlend) {
            Singleton<ToolManager>.instance.m_drawCallData.m_overlayCalls++;
            m_drawSegmentFunc(cameraInfo, color, segment, size, dashLength, -1f, 1280f, renderLimits, alphaBlend);
        }

        public static void RenderSegment(RenderManager.CameraInfo cameraInfo, Segment3 segment, float size, float dashLength, Color color, bool renderLimits, bool alphaBlend) {
            Singleton<ToolManager>.instance.m_drawCallData.m_overlayCalls++;
            m_drawSegmentFunc(cameraInfo, color, segment, size, dashLength, -1f, 1280f, renderLimits, alphaBlend);
        }

        public static void RenderElbow(RenderManager.CameraInfo cameraInfo, Segment3 segment1, Segment3 segment2, float size, float dashLength, Color color, bool renderLimits, bool alphaBlend) {
            Singleton<ToolManager>.instance.m_drawCallData.m_overlayCalls++;
            m_drawElbowFunc(cameraInfo, color, segment1, segment2, size, dashLength, -1f, 1280f, renderLimits, alphaBlend);
        }

        public static void RenderBezier(RenderManager.CameraInfo cameraInfo, Bezier3 bezier, float size, Color color, bool renderLimits, bool alphaBlend) {
            Singleton<ToolManager>.instance.m_drawCallData.m_overlayCalls++;
            m_drawBezierFunc(cameraInfo, color, bezier, size, -100000f, 100000f, -1f, 1280f, renderLimits, alphaBlend);
        }

        public static void RenderMainCircle(RenderManager.CameraInfo cameraInfo, Circle3XZ circle, float size, Color color, bool renderLimits, bool alphaBlend) {
            Singleton<ToolManager>.instance.m_drawCallData.m_overlayCalls += 2;
            m_drawCircleFunc(cameraInfo, color, circle.m_center, circle.Diameter + size, -1f, 1280f, renderLimits, alphaBlend);
            m_drawCircleFunc(cameraInfo, color, circle.m_center, circle.Diameter - size, -1f, 1280f, renderLimits, alphaBlend);
            if (circle.m_radius > 0f) {
                RenderLine(cameraInfo, new Segment3(circle.m_center, circle.Position(0f)), 0.05f, 1.00f, color, false, true);
            }
        }

        public static void RenderProgressiveSpacingFill(RenderManager.CameraInfo cameraInfo, float fillLength, float interval, float size, Color color, bool renderLimits, bool alphaBlend) {
            if (m_fenceMode) return;
            interval = Mathf.Clamp(interval, SPACING_MIN, SPACING_MAX);
            int numItems = Mathf.Clamp(Mathf.CeilToInt(fillLength / interval), 0, 262144);
            switch (m_drawMode) {
            case PLTDrawMode.Straight:
                float firstItemT = PlacementCalculator.m_placementInfos[0].m_t;
                color.a *= 0.75f;
                RenderSegment(cameraInfo, m_mainSegment.Cut(firstItemT, firstItemT + fillLength / PLTMath.LinearSpeedXZ(m_mainSegment)), size, 0f, color, renderLimits, alphaBlend);
                break;
            case PLTDrawMode.Curved:
            case PLTDrawMode.Freeform:
                firstItemT = PlacementCalculator.m_placementInfos[0].m_t;
                PLTMath.StepDistanceCurve(m_mainBezier, firstItemT, fillLength, PlacementCalculator.m_tolerance, out float tFill);
                color.a *= 0.75f;
                RenderBezier(cameraInfo, m_mainBezier.Cut(firstItemT, tFill), size, color, renderLimits, true);
                break;
            case PLTDrawMode.Circle:
                if (m_mainCircle.m_radius <= 0f) return;
                Quaternion rotation = Quaternion.AngleAxis(interval / m_mainCircle.Circumference * -360f, Vector3.up);
                Vector3 position = m_mainCircle.Position(0f);
                Vector3 center = m_mainCircle.m_center;
                Vector3 radiusVector = position - center;
                for (int i = 0; i < numItems; i++) {
                    RenderCircle(cameraInfo, position, size, color, renderLimits, alphaBlend);
                    radiusVector = rotation * radiusVector;
                    position = center + radiusVector;
                }
                break;
            }
        }

        public override void SimulationStep() {
            //TreeInfo treeInfo = m_treeInfo;
            //PropInfo propInfo = m_propInfo;
            //Prop/Tree methods
            //test if prefabs are all null then return
#if FALSE
            RaycastInput input = new RaycastInput(m_mouseRay, m_mouseRayLength);
            if (m_mouseRayValid && EToolBase.RayCast(input, out EToolBase.RaycastOutput raycastOutput)) {
                if (!raycastOutput.m_currentEditObject) {
                    m_mousePosition = raycastOutput.m_hitPos;
                }
            }
#endif
        }

        private void RenderItems(RenderManager.CameraInfo cameraInfo) {
            int itemCount = PlacementCalculator.m_itemCount;
            switch (m_objectMode) {
            case PLTObjectMode.Props:
                for (int i = 0; i < itemCount; i++) {
                    Vector3 position = PlacementCalculator.m_placementInfos[i].MeshPosition;
                    if (PlacementCalculator.m_placementInfos[i].m_isValidPlacement) {
                        PropInfo propInfo = PlacementCalculator.m_placementInfos[i].PropPrefab;
                        if (PLTSettings.RenderAndPlacePosResVanilla) {
                            position = position.QuantizeToGameShortGridXYZ();
                        }
                        InstanceID id = default;
                        if (propInfo.m_requireHeightMap) {
                            Singleton<TerrainManager>.instance.GetHeightMapping(position, out Texture heightMap,
                                                                                out Vector4 heightMapping, out Vector4 surfaceMapping);
                            EPropInstance.RenderInstance(cameraInfo, propInfo, id, position, PlacementCalculator.m_placementInfos[i].m_scale,
                                                         PlacementCalculator.m_placementInfos[i].m_angle, PlacementCalculator.m_placementInfos[i].m_color,
                                                         RenderManager.DefaultColorLocation, true, heightMap, heightMapping, surfaceMapping);
                        } else {
                            EPropInstance.RenderInstance(cameraInfo, propInfo, id, position, PlacementCalculator.m_placementInfos[i].m_scale, PlacementCalculator.m_placementInfos[i].m_angle,
                                                         PlacementCalculator.m_placementInfos[i].m_color, RenderManager.DefaultColorLocation, true);
                        }
                    }
                }
                break;
            case PLTObjectMode.Trees:
                for (int i = 0; i < itemCount; i++) {
                    Vector3 position = PlacementCalculator.m_placementInfos[i].MeshPosition;
                    if (PlacementCalculator.m_placementInfos[i].m_isValidPlacement) {
                        if (PLTSettings.RenderAndPlacePosResVanilla) {
                            position = position.QuantizeToGameShortGridXYZ();
                        }
                        TreeInstance.RenderInstance(null, PlacementCalculator.m_placementInfos[i].TreePrefab, position, PlacementCalculator.m_placementInfos[i].m_scale,
                                                    PlacementCalculator.m_placementInfos[i].m_brightness, RenderManager.DefaultColorLocation);
                    }
                }
                break;
            }
        }

        public IEnumerator CreateItems(bool continueDrawing, bool isCopyPlacing) {
            int placedItems = 0;
            int itemCount = PlacementCalculator.m_itemCount;
            PropInfo m_propPrefab = null;
            TreeInfo m_treePrefab = null;
            if (itemCount > 0) {
                Randomizer randomizer = m_randomizer;
                //new as of 170623
                //placementCalculator.UpdateItemPlacementInfo();
                switch (m_objectMode) {
                case PLTObjectMode.Props:
                    if (!(m_propPrefab is null)) {
                        PropManager pmInstance = Singleton<PropManager>.instance;
                        for (int i = 0; i < itemCount; i++) {
                            PropInfo propInfo = PlacementCalculator.m_placementInfos[i].PropPrefab;
                            Vector3 position = PlacementCalculator.m_placementInfos[i].m_position;
                            //new as of 161102 for Prop Precision
                            if (PLTSettings.RenderAndPlacePosResVanilla) {
                                position = position.QuantizeToGameShortGridXYZ();
                            }
                            //for correct variation of itemwise placement
                            if (m_controlMode == PLTControlMode.Itemwise && i == ITEMWISE_INDEX) {
                                propInfo.GetVariation(ref randomizer);
                            }
                            if (PlacementCalculator.m_placementInfos[i].m_isValidPlacement) {
                                if (pmInstance.CreateProp(out uint propID, ref randomizer, propInfo, position, PlacementCalculator.m_placementInfos[i].m_angle, true)) {
                                    placedItems++;
                                    PlacementCalculator.m_placementInfos[i].m_itemID = propID;
                                    InstanceID id = default;
                                    EffectInfo effectInfo = pmInstance.m_properties.m_placementEffect;
                                    Singleton<EffectManager>.instance.DispatchEffect(effectInfo, id, new EffectInfo.SpawnArea(position, Vector3.up, 1f),
                                                                                     Vector3.zero, 0f, 1f, Singleton<AudioManager>.instance.DefaultGroup);
                                }
                            }
                        }
                        if (placedItems > 0) {
                            //undoManager.AddEntry(_itemCount, m_placementInfo, ObjectMode.Props, fenceMode, placementCalculator.segmentState);
                        }
                    }
                    break;
                case PLTObjectMode.Trees:
                    if (!(m_treePrefab is null)) {
                        TreeManager tmInstance = Singleton<TreeManager>.instance;
                        for (int i = 0; i < itemCount; i++) {
                            Vector3 position = PlacementCalculator.m_placementInfos[i].m_position;
                            TreeInfo treeInfo = PlacementCalculator.m_placementInfos[i].TreePrefab;
                            //for correct variation of itemwise placement
                            if (m_controlMode == PLTControlMode.Itemwise && i == ITEMWISE_INDEX) {
                                treeInfo.GetVariation(ref randomizer);
                            }
                            if (PlacementCalculator.m_placementInfos[i].m_isValidPlacement) {
                                if (tmInstance.CreateTree(out uint treeID, ref randomizer, treeInfo, position, true)) {
                                    placedItems++;
                                    PlacementCalculator.m_placementInfos[i].m_itemID = treeID;
                                    InstanceID id = default;
                                    EffectInfo effectInfo = tmInstance.m_properties.m_placementEffect;
                                    Singleton<EffectManager>.instance.DispatchEffect(effectInfo, id, new EffectInfo.SpawnArea(position, Vector3.up, 1f),
                                                                                     Vector3.zero, 0f, 1f, Singleton<AudioManager>.instance.DefaultGroup);
                                }
                            }
                        }
                        if (placedItems > 0) {
                            //undoManager.AddEntry(_itemCount, m_placementInfo, ObjectMode.Trees, fenceMode, placementCalculator.segmentState);
                        }
                    }
                    break;
                }
                if (!isCopyPlacing) {
                    PlacementCalculator.m_itemCount = 0;
                    m_segmentState.FinalizeForPlacement(continueDrawing);
                }
            }
            yield return 0;
        }

        private bool AddControlPoint(Vector3 position) {
            switch (m_controlPointCount) {
            case 0:
                m_controlPoints[0].m_position = position;
                if (m_snapMode != PLTSnapMode.Objects && m_snapMode != PLTSnapMode.ZoneLines) {
                    m_controlPoints[0].m_direction = Vector3.zero;
                }
                m_controlPointCount = 1;
                m_controlPoints[1].Clear();
                m_controlPoints[2].Clear();
                m_positionChanging = true;
                return true;
            case 1:
                m_controlPoints[1].m_position = position;
                Vector3 normVector = position - m_controlPoints[0].m_position;
                normVector.y = 0f;
                normVector.Normalize();
                if (m_snapMode != PLTSnapMode.Objects && m_snapMode != PLTSnapMode.ZoneLines) {
                    m_controlPoints[0].m_direction = normVector;
                }
                m_controlPoints[1].m_direction = normVector;
                m_controlPointCount = 2;
                m_positionChanging = true;
                return true;
            case 2:
                if (m_drawMode == PLTDrawMode.Freeform) {
                    m_controlPoints[2].m_position = position;
                    CalculateFreeformMiddlePoint(ref m_controlPoints);
                } else { //must be curved
                    Vector3 p1 = m_controlPoints[1].m_position;
                    if (p1 == Vector3.zero) {
                        Debug.LogError("[PLT]: AddControlPoint(): Middle control point not found!");
                        return false;
                    }
                    m_controlPoints[2].m_position = position;
                    m_controlPoints[2].m_direction = (position - p1).NormalizeXZ();
                }
                m_controlPointCount = 3;
                m_positionChanging = true;
                return true;
            }
            return false;
        }

        public void CalculateFreeformMiddlePoint(ref PLTControlPoint[] controlPoints) {
            float Max(float a, float b) => (a <= b) ? b : a;
            float Min(float a, float b) => (a >= b) ? b : a;
            Vector3 cpVector0 = controlPoints[0].m_position;
            Vector3 cpVector1 = controlPoints[1].m_position;
            Vector3 cpVector2 = controlPoints[2].m_position;
            Vector3 p2_p0 = cpVector2 - cpVector0;
            Vector3 dir_p1 = controlPoints[1].m_direction;
            p2_p0.y = 0f;
            dir_p1.y = 0f;
            p2_p0 = Vector3.Normalize(p2_p0);
            controlPoints[1].m_position = cpVector0 + dir_p1 *
                                          (float)Math.Sqrt(0.5f * p2_p0.sqrMagnitude /
                                          Max(0.001f, 1f - (float)Math.Cos(3.14159274f - 2f *
                                          Min(1.17809725f, (float)Math.Acos(Vector3.Dot(p2_p0, dir_p1))))));
            Vector3 dir_p2 = cpVector2 - cpVector1;
            dir_p2.y = 0f;
            dir_p2.Normalize();
            controlPoints[2].m_direction = dir_p2;

            //sometimes things don't work corrently
            if (float.IsNaN(cpVector1.x) || float.IsNaN(cpVector1.y) || float.IsNaN(cpVector1.z)) {
                controlPoints[1].m_position = cpVector0 + 0.01f * controlPoints[0].m_direction;
                controlPoints[1].m_direction = controlPoints[0].m_direction;
            }
        }

        private void ResetPLT() {
            m_activeState = PLTActiveState.Undefined;
        }

        public static void InitializedPLT() {
            string[] m_spriteNamesPLT = {
                "PLT_MultiStateZero", "PLT_MultiStateZeroFocused", "PLT_MultiStateZeroHovered", "PLT_MultiStateZeroPressed", "PLT_MultiStateZeroDisabled",
                "PLT_MultiStateOne", "PLT_MultiStateOneFocused", "PLT_MultiStateOneHovered", "PLT_MultiStateOnePressed", "PLT_MultiStateOneDisabled",
                "PLT_MultiStateTwo", "PLT_MultiStateTwoFocused", "PLT_MultiStateTwoHovered", "PLT_MultiStateTwoPressed", "PLT_MultiStateTwoDisabled",
                "PLT_ToggleCPZero", "PLT_ToggleCPZeroFocused", "PLT_ToggleCPZeroHovered", "PLT_ToggleCPZeroPressed", "PLT_ToggleCPZeroDisabled",
                "PLT_ToggleCPOne", "PLT_ToggleCPOneFocused", "PLT_ToggleCPOneHovered", "PLT_ToggleCPOnePressed", "PLT_ToggleCPOneDisabled",
                "PLT_FenceModeZero", "PLT_FenceModeZeroFocused", "PLT_FenceModeZeroHovered", "PLT_FenceModeZeroPressed", "PLT_FenceModeZeroDisabled",
                "PLT_FenceModeOne", "PLT_FenceModeOneFocused", "PLT_FenceModeOneHovered", "PLT_FenceModeOnePressed", "PLT_FenceModeOneDisabled",
                "PLT_FenceModeTwo", "PLT_FenceModeTwoFocused", "PLT_FenceModeTwoHovered", "PLT_FenceModeTwoPressed", "PLT_FenceModeTwoDisabled",
                "PLT_ItemwiseZero", "PLT_ItemwiseZeroFocused", "PLT_ItemwiseZeroHovered", "PLT_ItemwiseZeroPressed", "PLT_ItemwiseZeroDisabled",
                "PLT_ItemwiseOne", "PLT_ItemwiseOneFocused", "PLT_ItemwiseOneHovered", "PLT_ItemwiseOnePressed", "PLT_ItemwiseOneDisabled",
                "PLT_SpacingwiseZero", "PLT_SpacingwiseZeroFocused", "PLT_SpacingwiseZeroHovered", "PLT_SpacingwiseZeroPressed", "PLT_SpacingwiseZeroDisabled",
                "PLT_SpacingwiseOne", "PLT_SpacingwiseOneFocused", "PLT_SpacingwiseOneHovered", "PLT_SpacingwiseOnePressed", "PLT_SpacingwiseOneDisabled",
                "PLT_BasicDividerTile02x02"
            };
            ToolController toolController = ToolsModifierControl.toolController;
            try {
                PropLineTool propLineTool = toolController.gameObject.GetComponent<PropLineTool>();
                if (propLineTool is null) {
                    propLineTool = toolController.gameObject.AddComponent<PropLineTool>();
                    m_sharedTextures = PAUtils.CreateTextureAtlas(@"PLTAtlas", @"PropAnarchy.PLT.Icons.", m_spriteNamesPLT, 1024);
                    m_toolBar = UIView.GetAView().AddUIComponent(typeof(ToolBar)) as ToolBar;
                    m_optionPanel = UIView.GetAView().AddUIComponent(typeof(OptionPanel)) as OptionPanel;
                }
            } catch (Exception e) {
                Debug.LogException(e);
            }
        }

        public static void UnloadPLT() {
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
