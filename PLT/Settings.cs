using System.Threading;
using UnityEngine;

namespace PropAnarchy.PLT {
    internal static class Settings {
        internal static bool m_autoDefaultSpacing = true;
        internal static bool m_errorChecking = true;
        internal static bool m_showErrorGuides = true;
        internal static bool m_useMeshCenterCorrection = false;
        internal static bool m_perfectCircles = false;
        internal static bool m_linearFenceFill = false;
        internal static bool m_angleFlip180 = false;
        internal static bool m_showUndoPreview = true;

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

        internal static bool AutoDefaultSpacing {
            get => m_autoDefaultSpacing;
            set {
                m_autoDefaultSpacing = value;
                ThreadPool.QueueUserWorkItem(PAModule.SaveSettings);
            }
        }
        internal static bool AngleFlip180 {
            get => m_angleFlip180;
            set {
                m_angleFlip180 = value;
                ThreadPool.QueueUserWorkItem(PAModule.SaveSettings);
            }
        }
        internal static bool ShowUndoPreviews {
            get => m_showUndoPreview;
            set {
                m_showUndoPreview = value;
                ThreadPool.QueueUserWorkItem(PAModule.SaveSettings);
            }
        }
        internal static bool ErrorChecking {
            get => m_errorChecking;
            set {
                m_errorChecking = value;
                ThreadPool.QueueUserWorkItem(PAModule.SaveSettings);
            }
        }
        internal static bool ShowErrorGuides {
            get => m_showErrorGuides;
            set {
                m_showErrorGuides = value;
                ThreadPool.QueueUserWorkItem(PAModule.SaveSettings);
            }
        }
        internal static bool AnarchyPLT => PAModule.UsePropAnarchy;

        internal static bool UseMeshCenterCorrection {
            get => m_useMeshCenterCorrection;
            set {
                m_useMeshCenterCorrection = value;
                ThreadPool.QueueUserWorkItem(PAModule.SaveSettings);
            }
        }
        internal static bool PerfectCircles {
            get => m_perfectCircles;
            set {
                m_perfectCircles = value;
                ThreadPool.QueueUserWorkItem(PAModule.SaveSettings);
            }
        }
        internal static bool LinearFenceFill {
            get => m_linearFenceFill;
            set {
                m_linearFenceFill = value;
                ThreadPool.QueueUserWorkItem(PAModule.SaveSettings);
            }
        }
    }
}