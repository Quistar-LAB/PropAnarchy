using System.Threading;
using UnityEngine;

namespace PropAnarchy.PLT {
    internal static class Settings {
        internal static PropLineTool.AngleMode m_angleMode = PropLineTool.AngleMode.Single;
        internal static PropLineTool.ControlMode m_controlMode = PropLineTool.ControlMode.Spacing;
        internal static bool m_useMeshCenterCorrection = false;
        internal static bool m_perfectCircles = false;
        internal static bool m_linearFenceFill = false;
        internal static bool m_angleFlip180 = false;
        internal static bool m_showUndoPreview = true;
        internal static bool m_verticalLayout = false;
        internal static float m_optionXPos = 0f;
        internal static float m_optionYPos = 0f;

        internal static Color m_PLTColor_default = new Color32(39, 130, 204, 128);
        internal static Color m_PLTColor_defaultSnapZones = new Color32(39, 130, 204, 255);
        internal static Color m_PLTColor_locked = new Color32(28, 127, 64, 128);
        internal static Color m_PLTColor_lockedStrong = new Color32(28, 127, 64, 192);
        internal static Color m_PLTColor_lockedHighlight = new Color32(228, 239, 232, 160);
        internal static Color m_PLTColor_copyPlace = new Color32(114, 45, 186, 128);
        internal static Color m_PLTColor_copyPlaceHighlight = new Color32(214, 223, 234, 160);
        internal static Color m_PLTColor_hoverBase = new Color32(33, 142, 129, 204);
        internal static Color m_PLTColor_hoverCopyPlace = new Color32(196, 198, 242, 204);
        internal static Color m_PLTColor_undoItemOverlay = new Color32(214, 144, 81, 204);
        internal static Color m_PLTColor_curveWarning = new Color32(231, 155, 24, 160);
        internal static Color m_PLTColor_ItemwiseLock = new Color32(29, 72, 168, 128);
        internal static Color m_PLTColor_MaxFillContinue = new Color32(211, 193, 221, 128);

        internal static bool FenceMode { get; set; }

        internal static bool AutoDefaultSpacing { get; set; } = true;

        internal static bool VerticalLayout {
            get => m_verticalLayout;
            set {
                if (m_verticalLayout != value) {
                    m_verticalLayout = value;
                    ThreadPool.QueueUserWorkItem(PAModule.SaveSettings);
                }
            }
        }

        internal static Vector2 OptionPosition {
            get => new Vector2(m_optionXPos, m_optionYPos);
            set {
                if (value.x != m_optionXPos && value.y != m_optionYPos) {
                    m_optionXPos = value.x;
                    m_optionYPos = value.y;
                    ThreadPool.QueueUserWorkItem(PAModule.SaveSettings);
                }
            }
        }

        internal static PropLineTool.AngleMode AngleMode {
            get => m_angleMode;
            set {
                if (m_angleMode != value) {
                    m_angleMode = value;
                    ThreadPool.QueueUserWorkItem(PAModule.SaveSettings);
                }
            }
        }

        internal static PropLineTool.ControlMode ControlMode {
            get => m_controlMode;
            set {
                if (m_controlMode != value) {
                    m_controlMode = value;
                    ThreadPool.QueueUserWorkItem(PAModule.SaveSettings);
                }
            }
        }

        internal static bool AngleFlip180 {
            get => m_angleFlip180;
            set {
                if (m_angleFlip180 != value) {
                    m_angleFlip180 = value;
                    ThreadPool.QueueUserWorkItem(PAModule.SaveSettings);
                }
            }
        }

        internal static bool AngleFlip90 { get; set; }

        internal static bool ShowUndoPreviews {
            get => m_showUndoPreview;
            set {
                m_showUndoPreview = value;
                ThreadPool.QueueUserWorkItem(PAModule.SaveSettings);
            }
        }

        internal static bool UseMeshCenterCorrection {
            get => m_useMeshCenterCorrection;
            set {
                if (m_useMeshCenterCorrection != value) {
                    m_useMeshCenterCorrection = value;
                    ThreadPool.QueueUserWorkItem(PAModule.SaveSettings);
                }
            }
        }

        internal static bool PerfectCircles {
            get => m_perfectCircles;
            set {
                if (m_perfectCircles != value) {
                    m_perfectCircles = value;
                    ThreadPool.QueueUserWorkItem(PAModule.SaveSettings);
                }
            }
        }

        internal static bool LinearFenceFill {
            get => m_linearFenceFill;
            set {
                if (m_linearFenceFill != value) {
                    m_linearFenceFill = value;
                    ThreadPool.QueueUserWorkItem(PAModule.SaveSettings);
                }
            }
        }
    }
}