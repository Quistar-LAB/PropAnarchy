namespace PropAnarchy.AdaptivePropVisibility {
    public static class Patcher {
#if FALSE
		public static void RefreshLevelOfDetail() {
			float num = RenderManager.LevelOfDetailFactor * 100f;
			if (this.m_generatedInfo.m_triangleArea == 0f) {
				this.m_maxRenderDistance = 1000f;
			} else {
				this.m_maxRenderDistance = Mathf.Sqrt(this.m_generatedInfo.m_triangleArea) * num + 100f;
				this.m_maxRenderDistance = Mathf.Min(1000f, this.m_maxRenderDistance);
			}
			if (this.m_isDecal || this.m_isMarker) {
				this.m_lodRenderDistance = 0f;
			} else if (this.m_lodMesh != null) {
				this.m_lodRenderDistance = this.m_maxRenderDistance * 0.25f;
			} else {
				this.m_lodRenderDistance = this.m_maxRenderDistance;
			}
			if (this.m_effects != null) {
				for (int i = 0; i < this.m_effects.Length; i++) {
					if (this.m_effects[i].m_effect != null) {
						this.m_maxRenderDistance = Mathf.Max(this.m_maxRenderDistance, this.m_effects[i].m_effect.RenderDistance());
					}
				}
				this.m_maxRenderDistance = Mathf.Min(1000f, this.m_maxRenderDistance);
			}
		}

 
        public static void RefreshLevelOfDetail() {
            if (m_generatedInfo.m_triangleArea == 0.0f || float.IsNaN(m_generatedInfo.m_triangleArea)) {
                m_maxRenderDistance = OptionsWrapper<Options>.Options.FallbackRenderDistanceProps;
            } else {
                float num = RenderManager.LevelOfDetailFactor * OptionsWrapper<Options>.Options.LodFactorMultiplierProps;
                m_maxRenderDistance = (float)(Mathf.Sqrt(m_generatedInfo.m_triangleArea) * (double)num + OptionsWrapper<Options>.Options.DistanceOffsetProps);
                this.m_maxRenderDistance = Mathf.Min(Options.RenderDistanceThreshold, this.m_maxRenderDistance);
            }
            m_lodRenderDistance = m_isDecal || m_isMarker ? 0.0f : (!(m_lodMesh != null) ? m_maxRenderDistance : m_maxRenderDistance * OptionsWrapper<Options>.Options.LodDistanceMultiplierProps);
            if (m_effects != null) {
                for (int index = 0; index < m_effects.Length; ++index) {
                    if (m_effects[index].m_effect != null)
                        m_maxRenderDistance = Mathf.Max(m_maxRenderDistance, m_effects[index].m_effect.RenderDistance());
                }
                this.m_maxRenderDistance = Mathf.Min(Options.RenderDistanceThresholdEffects, this.m_maxRenderDistance);
            }
        }
#endif
    }
}
