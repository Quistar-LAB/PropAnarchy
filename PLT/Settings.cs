using ColossalFramework;
using UnityEngine;

namespace PropAnarchy.PLT {
    public static class Settings {
        private const string FILENAME = "PropLineTool";
        private static bool m_autoDefaultSpacing = true;
        private static bool m_errorChecking = true;
        private static bool m_showErrorGuides = true;
        private static bool m_anarchyPLT = false;
        private static bool m_placeBlockedItems = false;
        private static bool m_renderPosResVanilla = false;
        private static bool m_useMeshCenterCorrection = true;
        private static bool m_perfectCircles = false;
        private static bool m_linearFenceFill = false;
        private static bool m_angleFlip180 = false;

        public static Color m_PLTColor_default = new Color32(39, 130, 204, 128);
        public static Color m_PLTColor_defaultSnapZones = new Color32(39, 130, 204, 255);
        public static Color m_PLTColor_locked = new Color32(28, 127, 64, 128);
        public static Color m_PLTColor_lockedStrong = new Color32(28, 127, 64, 192);
        public static Color m_PLTColor_lockedHighlight = new Color32(228, 239, 232, 160);
        public static Color m_PLTColor_copyPlace = new Color32(114, 45, 186, 128);
        public static Color m_PLTColor_copyPlaceHighlight = new Color32(214, 223, 234, 160);
        public static Color m_PLTColor_hoverBase = new Color32(33, 142, 129, 204);
        public static Color m_PLTColor_hoverCopyPlace = new Color32(196, 198, 242, 204);
        public static Color m_PLTColor_undoItemOverlay = new Color32(214, 144, 81, 204);
        public static Color m_PLTColor_curveWarning = new Color32(231, 155, 24, 160);
        public static Color m_PLTColor_ItemwiseLock = new Color32(29, 72, 168, 128);
        public static Color m_PLTColor_MaxFillContinue = new Color32(211, 193, 221, 128);

        public static bool AutoDefaultSpacing {
            get => m_autoDefaultSpacing;
            set {
                if (m_autoDefaultSpacing != value) {
                    m_autoDefaultSpacing = value;
                    SaveSettings();
                }
            }
        }
        public static bool AngleFlip180 {
            get => m_angleFlip180;
            set {
                if (m_angleFlip180 != value) {
                    m_angleFlip180 = value;
                    SaveSettings();
                }
            }
        }
        public static bool ShowUndoPreviews { get; set; } = true;
        public static bool ErrorChecking {
            get => m_errorChecking;
            set {
                if (m_errorChecking != value) {
                    m_errorChecking = value;
                    SaveSettings();
                }
            }
        }
        public static bool ShowErrorGuides {
            get => m_showErrorGuides;
            set {
                if (m_showErrorGuides != value) {
                    m_showErrorGuides = value;
                    SaveSettings();
                }
            }
        }
        public static bool AnarchyPLT {
            get => m_anarchyPLT;
            set {
                if (m_anarchyPLT != value) {
                    m_anarchyPLT = value;
                    SaveSettings();
                }
            }
        }
        public static bool PlaceBlockedItems {
            get => m_placeBlockedItems;
            set {
                if (m_placeBlockedItems != value) {
                    m_placeBlockedItems = value;
                    SaveSettings();
                }
            }
        }
        public static bool RenderAndPlacePosResVanilla {
            get => m_renderPosResVanilla;
            set {
                if (m_renderPosResVanilla != value) {
                    m_renderPosResVanilla = value;
                    SaveSettings();
                }
            }
        }
        public static bool UseMeshCenterCorrection {
            get => m_useMeshCenterCorrection;
            set {
                if (m_useMeshCenterCorrection != value) {
                    m_useMeshCenterCorrection = value;
                    SaveSettings();
                }
            }
        }
        public static bool PerfectCircles {
            get => m_perfectCircles;
            set {
                if (m_perfectCircles != value) {
                    m_perfectCircles = value;
                    SaveSettings();
                }
            }
        }
        public static bool LinearFenceFill {
            get => m_linearFenceFill;
            set {
                if (m_linearFenceFill != value) {
                    m_linearFenceFill = value;
                    SaveSettings();
                }
            }
        }

        public static SavedBool AnarchyPLTOnByDefault = new SavedBool("anarchyPLTOnByDefault", FILENAME, false, true);
        public static void SaveSettings() {

        }

        public static void LoadSettings() {

        }
    }
}