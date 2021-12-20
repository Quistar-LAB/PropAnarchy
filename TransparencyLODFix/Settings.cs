using System.Threading;

namespace PropAnarchy.TransparencyLODFix {
    internal static class Settings {
        internal const float RenderDistanceThreshold = 100000f;
        internal const float RenderDistanceThresholdEffects = 100000f;
        internal static bool m_hideClouds = false;
        internal static float m_lodFactorMultiplierProps = 1000f; /* RANGE: 1f, 1000f */
        internal static float m_distanceOffsetProps = 100f; /* RANGE: 1f, 1000f */
        internal static float m_lodDistanceMultiplierProps = 0.25f; /* RANGE: 0.05f 1f */
        internal static float m_fallbackRenderDistanceProps = 1000f; /* RANGE: 1000f, 100000f */
        internal static float m_lodFactorMultiplierBuildings = 1000f; /* RANGE: 1f, 1000f */
        internal static float m_distanceOffsetBuildings = 100f; /* RANGE: 1f, 1000f */
        internal static float m_lodDistanceMultiplierBuildings = 0.25f; /* RANGE: 0.05f 1f */
        internal static float m_fallbackRenderDistanceBuildings = 1000f; /* RANGE: 1000f, 100000f */

        internal static void Reset() {
            m_hideClouds = false;
            m_lodFactorMultiplierProps = 1000f; /* RANGE: 1f, 1000f */
            m_distanceOffsetProps = 100f; /* RANGE: 1f, 1000f */
            m_lodDistanceMultiplierProps = 0.25f; /* RANGE: 0.05f 1f */
            m_fallbackRenderDistanceProps = 1000f; /* RANGE: 1000f, 100000f */
            m_lodFactorMultiplierBuildings = 1000f; /* RANGE: 1f, 1000f */
            m_distanceOffsetBuildings = 100f; /* RANGE: 1f, 1000f */
            m_lodDistanceMultiplierBuildings = 0.25f; /* RANGE: 0.05f 1f */
            m_fallbackRenderDistanceBuildings = 1000f; /* RANGE: 1000f, 100000f */
        }

        internal static bool HideClouds {
            get => m_hideClouds;
            set {
                if (m_hideClouds != value) {
                    m_hideClouds = value;
                    ThreadPool.QueueUserWorkItem(PAModule.SaveSettings);
                }
            }
        }

        internal static float LodFactorMultiplierProps {
            get => m_lodFactorMultiplierProps;
            set {
                if (m_lodFactorMultiplierProps != value) {
                    m_lodFactorMultiplierProps = value;
                    ThreadPool.QueueUserWorkItem(PAModule.SaveSettings);
                }
            }
        }
        internal static float DistanceOffsetProps {
            get => m_distanceOffsetProps;
            set {
                if (m_distanceOffsetProps != value) {
                    m_distanceOffsetProps = value;
                    ThreadPool.QueueUserWorkItem(PAModule.SaveSettings);
                }
            }
        }

        internal static float LodDistanceMultiplierProps {
            get => m_lodDistanceMultiplierProps;
            set {
                if (m_lodDistanceMultiplierProps != value) {
                    m_lodDistanceMultiplierProps = value;
                    ThreadPool.QueueUserWorkItem(PAModule.SaveSettings);
                }
            }
        }

        internal static float FallbackRenderDistanceProps {
            get => m_fallbackRenderDistanceProps;
            set {
                if (m_fallbackRenderDistanceProps != value) {
                    m_fallbackRenderDistanceProps = value;
                    ThreadPool.QueueUserWorkItem(PAModule.SaveSettings);
                }
            }
        }

        internal static float LodFactorMultiplierBuildings {
            get => m_lodFactorMultiplierBuildings;
            set {
                if (m_lodFactorMultiplierBuildings != value) {
                    m_lodFactorMultiplierBuildings = value;
                    ThreadPool.QueueUserWorkItem(PAModule.SaveSettings);
                }
            }
        }

        internal static float DistanceOffsetBuildings {
            get => m_distanceOffsetBuildings;
            set {
                if (m_distanceOffsetBuildings != value) {
                    m_distanceOffsetBuildings = value;
                    ThreadPool.QueueUserWorkItem(PAModule.SaveSettings);
                }
            }
        }

        internal static float LodDistanceMultiplierBuildings {
            get => m_lodDistanceMultiplierBuildings;
            set {
                if (m_lodDistanceMultiplierBuildings != value) {
                    m_lodDistanceMultiplierBuildings = value;
                    ThreadPool.QueueUserWorkItem(PAModule.SaveSettings);
                }
            }
        }

        internal static float FallbackRenderDistanceBuildings {
            get => m_fallbackRenderDistanceBuildings;
            set {
                if (m_fallbackRenderDistanceBuildings != value) {
                    m_fallbackRenderDistanceBuildings = value;
                    ThreadPool.QueueUserWorkItem(PAModule.SaveSettings);
                }
            }
        }
    }
}
