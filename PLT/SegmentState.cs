using UnityEngine;

namespace PropAnarchy.PLT {
    public class SegmentState {
        //used in non-fence mode
        public float m_lastFinalOffset;
        public float m_newFinalOffset;
        //used in fence mode
        public Vector3 m_lastFenceEndpoint;
        public Vector3 m_newFenceEndpoint;
        //used in both
        public bool m_isContinueDrawing;
        public bool m_keepLastOffsets;
        public bool m_maxItemCountExceeded;
        public bool m_isMaxFillContinue;
        //error checking
        public bool m_allItemsValid;

        public SegmentState() {
            m_lastFinalOffset = 0f;
            m_newFinalOffset = 0f;
            m_lastFenceEndpoint = Vector3.down;
            m_newFenceEndpoint = Vector3.down;
            m_isContinueDrawing = false;
            m_keepLastOffsets = false;
            m_maxItemCountExceeded = false;
            m_isMaxFillContinue = false;
            m_allItemsValid = true;
        }

        public delegate void EventHandler();
        private event EventHandler EventLastContinueParameterChanged;

        // === non-fence === 
        public float LastFinalOffset {
            set {
                if (m_lastFinalOffset != value) {
                    m_lastFinalOffset = value;
                    EventLastContinueParameterChanged?.Invoke();
                }
            }
        }
        // === fence === 
        public Vector3 LastFenceEndpoint {
            set {
                if (m_lastFenceEndpoint != value) {
                    m_lastFenceEndpoint = value;
                    EventLastContinueParameterChanged?.Invoke();
                }
            }
        }
        /// <summary>
        /// Returns true if the curve is currently continue-drawing to fill the same curve because max item count threshold was exceeded.
        /// </summary>
        public bool IsMaxFillContinue {
            get => m_isMaxFillContinue;
            set {
                if (m_isMaxFillContinue != value) {
                    m_isMaxFillContinue = value;
                    EventLastContinueParameterChanged?.Invoke();
                }
            }
        }

        public bool IsContinueDrawing => m_isContinueDrawing;

        public bool IsReadyForMaxContinue => m_maxItemCountExceeded;

        public void FinalizeForPlacement(bool continueDrawing) {
            if (continueDrawing) {
                if (!m_keepLastOffsets) {
                    LastFenceEndpoint = m_newFenceEndpoint;
                    LastFinalOffset = m_newFinalOffset;
                }
                IsMaxFillContinue = m_maxItemCountExceeded;
            } else {
                LastFenceEndpoint = Vector3.down;
                LastFinalOffset = 0f;
                IsMaxFillContinue = false;
                m_keepLastOffsets = false;
            }
            m_newFenceEndpoint = Vector3.down;
            m_newFinalOffset = 0f;
        }

        public void ResetLastContinueParameters() {
            LastFenceEndpoint = Vector3.down;
            LastFinalOffset = 0f;
        }
        /// <summary>
        /// Used for Undo max-fill-continue.
        /// </summary>
        /// <param name="lastFinalOffsetValue"></param>
        /// <param name="lastFenceEndpointVector"></param>
        /// <returns></returns>
        public void RevertLastContinueParameters(float lastFinalOffsetValue, Vector3 lastFenceEndpointVector) {
            m_keepLastOffsets = false;
            LastFinalOffset = lastFinalOffsetValue;
            LastFenceEndpoint = lastFenceEndpointVector;
        }

        public bool AreLastContinueParametersZero() {
            if ((m_lastFenceEndpoint == Vector3.down || m_lastFenceEndpoint == Vector3.zero) && m_lastFinalOffset == 0f) {
                return true;
            }
            return false;
        }
        public bool AreNewContinueParametersEmpty() {
            if ((m_newFenceEndpoint == Vector3.down || m_newFenceEndpoint == Vector3.zero) && m_newFinalOffset == 0f) {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Checks whether an input *position* is close to the last fence endpoint
        /// </summary>
        /// <param name="position"></param>
        /// <returns>true if the input position is within 2mm of the last fence endpoint</returns>
        public bool IsPositionEqualToLastFenceEndpoint(Vector3 position) {
            if (Vector3.Distance(position, m_lastFenceEndpoint) <= 0.002f) {
                return true;
            }
            return false;
        }
    }
}
