using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ColossalFramework;
using ColossalFramework.Math;
using UnityEngine;

namespace PropAnarchy {
    public static class PAPropInstanceExtension {
		public const ushort PROP_CREATED_FLAG = 0x0001;
		public static void UpdateProp(this PropInstance instance, uint propID) {
			if ((instance.m_flags & PROP_CREATED_FLAG) == 0) return;
			PropInfo info = instance.Info;
			if (info is not null && info.m_createRuining) {
				Vector3 position = instance.Position;
				float offset = 4.5f;
				if (info.m_isDecal) {
					Randomizer randomizer = new(propID);
					float scale = info.m_minScale + randomizer.Int32(10000u) * (info.m_maxScale - info.m_minScale) * 0.0001f;
					offset = Mathf.Max(info.m_generatedInfo.m_size.x, info.m_generatedInfo.m_size.z) * scale * 0.5f + 2.5f;
				}
				float minX = position.x - offset;
				float minZ = position.z - offset;
				float maxX = position.x + offset;
				float maxZ = position.z + offset;
				TerrainModify.UpdateArea(minX, minZ, maxX, maxZ, false, true, false);
			}
		}
	}
}
