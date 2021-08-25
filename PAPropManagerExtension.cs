using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using ColossalFramework;
using ColossalFramework.Math;
using UnityEngine;
using System.Reflection;
using System.Reflection.Emit;
using System.ComponentModel;
using static PropAnarchy.PAModule;

namespace PropAnarchy {
    public static class PAPropManagerExtension {
		public static Array32<PropInstance> m_props;
		public static uint[] m_propNextGrid; /* uint holder for nextGrid in PropInstance */
		public static uint[] m_propGrid;

		public static int MAX_MAP_PROP;
		public static int MAX_PROP_COUNT;

		private static int m_propLayer;

		private static Func<S, T> CreateGetter<S, T>(FieldInfo field) {
			string methodName = field.ReflectedType.FullName + ".get_" + field.Name;
			DynamicMethod setterMethod = new DynamicMethod(methodName, typeof(T), new Type[1] { typeof(S) }, true);
			ILGenerator gen = setterMethod.GetILGenerator();
			if (field.IsStatic) {
				gen.Emit(OpCodes.Ldsfld, field);
			} else {
				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(OpCodes.Ldfld, field);
			}
			gen.Emit(OpCodes.Ret);
			return (Func<S, T>)setterMethod.CreateDelegate(typeof(Func<S, T>));
		}

		private static Action<S, T> CreateSetter<S, T>(FieldInfo field) {
			string methodName = field.ReflectedType.FullName + ".set_" + field.Name;
			DynamicMethod setterMethod = new DynamicMethod(methodName, null, new Type[2] { typeof(S), typeof(T) }, true);
			ILGenerator gen = setterMethod.GetILGenerator();
			if (field.IsStatic) {
				gen.Emit(OpCodes.Ldarg_1);
				gen.Emit(OpCodes.Stsfld, field);
			} else {
				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(OpCodes.Ldarg_1);
				gen.Emit(OpCodes.Stfld, field);
			}
			gen.Emit(OpCodes.Ret);
			return (Action<S, T>)setterMethod.CreateDelegate(typeof(Action<S, T>));
		}

		#region InstanceIDExtension
		private const uint OBJECT_TYPE = 0xff000000u;
		private const uint OBJECT_PROP = 0x0a000000u;
		private const uint OBJECT_INDEX = 0x00ffffffu;
		private static Func<InstanceID, uint> get_m_id;
		private static Action<InstanceID, uint> set_m_id;
        public static uint GetProp32(this InstanceID instance) {
			uint id = get_m_id(instance);
			return (id & OBJECT_TYPE) != OBJECT_PROP ? 0u : (id & OBJECT_INDEX);
		}
		public static void SetProp32(this InstanceID instance, uint id) => set_m_id(instance, id);
        #endregion InstanceIDExtension

        public static void Init() {
			FieldInfo m_idField = typeof(InstanceID).GetField("m_id", BindingFlags.NonPublic | BindingFlags.Instance);
			get_m_id = CreateGetter<InstanceID, uint>(m_idField);
			set_m_id = CreateSetter<InstanceID, uint>(m_idField);
			m_propLayer = LayerMask.NameToLayer("Props");
			m_props = new Array32<PropInstance>(MaxPropLimit);
			m_props.CreateItem(out uint _);
		}

		private static void InitializeProp(uint prop, ref PropInstance data, bool assetEditor) {
			int posX;
			int posY;
			if (assetEditor) {
				posX = Mathf.Clamp(((data.m_posX / 16) + 32768) * 270 / 65536, 0, 269);
				posY = Mathf.Clamp(((data.m_posZ / 16) + 32768) * 270 / 65536, 0, 269);
			} else {
				posX = Mathf.Clamp((data.m_posX + 32768) * 270 / 65536, 0, 269);
				posY = Mathf.Clamp((data.m_posZ + 32768) * 270 / 65536, 0, 269);
			}
			int grid = posY * 270 + posX;
			while (!Monitor.TryEnter(m_propGrid, SimulationManager.SYNCHRONIZE_TIMEOUT)) {
			}
			try {
				m_propNextGrid[prop] = m_propGrid[grid];
				m_propGrid[grid] = prop;
			} finally {
				Monitor.Exit(m_propGrid);
			}
		}

		private static void FinalizeProp(uint prop, ref PropInstance data) {
			int posX;
			int posY;
			if (Singleton<ToolManager>.instance.m_properties.m_mode == ItemClass.Availability.AssetEditor) {
				posX = Mathf.Clamp(((int)(data.m_posX / 16) + 32768) * 270 / 65536, 0, 269);
				posY = Mathf.Clamp(((int)(data.m_posZ / 16) + 32768) * 270 / 65536, 0, 269);
			} else {
				posX = Mathf.Clamp(((int)data.m_posX + 32768) * 270 / 65536, 0, 269);
				posY = Mathf.Clamp(((int)data.m_posZ + 32768) * 270 / 65536, 0, 269);
			}
			int grid = posY * 270 + posX;
			while (!Monitor.TryEnter(m_propGrid, SimulationManager.SYNCHRONIZE_TIMEOUT)) {}
			try {
				for(uint i = m_propGrid[grid], nextProp = 0; i != 0; i = nextProp) {
					if(i == prop) {
						if(nextProp == 0) {
							m_propGrid[grid] = data.m_nextGridProp;
                        } else {
							m_props.m_buffer[nextProp].m_nextGridProp = data.m_nextGridProp;
                        }
                    }
                }
				data.m_nextGridProp = 0;
			} finally {
				Monitor.Exit(m_propGrid);
			}
			int x = posX * 45 / 270;
			int z = posY * 45 / 270;
			Singleton<RenderManager>.instance.UpdateGroup(x, z, m_propLayer);
			PropInfo info = data.Info;
			if (info != null && info.m_effectLayer != -1) {
				Singleton<RenderManager>.instance.UpdateGroup(x, z, info.m_effectLayer);
			}
		}

		public static void MoveProp(this PropManager propManager, uint prop, Vector3 position) {
			if (!UseCustomPropLimit) {
				propManager.MoveProp((ushort)prop, position);
				return;
			}
			PropInstance data = m_props.m_buffer[prop];
			if (data.m_flags != 0) {
				if (!data.Blocked) {
					DistrictManager dm = Singleton<DistrictManager>.instance;
					byte origPark = dm.GetPark(data.Position);
					byte newPark = dm.GetPark(position);
					DistrictPark[] districts = dm.m_parks.m_buffer;
					districts[origPark].m_propCount--;
					districts[newPark].m_propCount++;
				}
				ItemClass.Availability mode = Singleton<ToolManager>.instance.m_properties.m_mode;
				FinalizeProp(prop, ref data);
				data.Position = position;
				InitializeProp(prop, ref data, (mode & ItemClass.Availability.AssetEditor) != ItemClass.Availability.None);
				propManager.UpdateProp(prop);
			}
		}

		public static void UpdateProp(this PropManager propManager, uint prop) {
			if (!UseCustomPropLimit) {
				propManager.UpdateProp((ushort)prop);
				return;
            }
			propManager.m_updatedProps[prop >> 6] |= 1uL << (int)prop;
			propManager.m_propsUpdated = true;
		}

		public static void UpdatePropRenderer(this PropManager propManager, uint prop, bool updateGroup) {
			if (!UseCustomPropLimit) {
				propManager.UpdatePropRenderer((ushort)prop, updateGroup);
				return;
            }
			PropInstance[] propBuffer = m_props.m_buffer;
			if (propBuffer[prop].m_flags == 0) return;
			if (updateGroup) {
				int posX;
				int posZ;
				if (Singleton<ToolManager>.instance.m_properties.m_mode == ItemClass.Availability.AssetEditor) {
					posX = Mathf.Clamp(((propBuffer[prop].m_posX / 16) + 32768) * 270 / 65536, 0, 269);
					posZ = Mathf.Clamp(((propBuffer[prop].m_posZ / 16) + 32768) * 270 / 65536, 0, 269);
				} else {
					posX = Mathf.Clamp((propBuffer[prop].m_posX + 32768) * 270 / 65536, 0, 269);
					posZ = Mathf.Clamp((propBuffer[prop].m_posZ + 32768) * 270 / 65536, 0, 269);
				}
				int x = posX * 45 / 270;
				int z = posZ * 45 / 270;
				PropInfo info = propBuffer[prop].Info;
				if (info is not null && info.m_prefabDataLayer != -1) {
					Singleton<RenderManager>.instance.UpdateGroup(x, z, info.m_prefabDataLayer);
				}
				if (info is not null && info.m_effectLayer != -1) {
					Singleton<RenderManager>.instance.UpdateGroup(x, z, info.m_effectLayer);
				}
			}
		}

		public static bool CreateProp(this PropManager propManager, out uint propID, ref Randomizer randomizer, PropInfo info, Vector3 position, float angle, bool single) {
			if (!UseCustomPropLimit) {
				if(propManager.CreateProp(out ushort shortPropID, ref randomizer, info, position, angle, single)) {
					propID = shortPropID;
					return true;
                }
				propID = 0;
				return false;
            }
			if (propManager.CheckLimits() && m_props.CreateItem(out uint newPropID, ref randomizer)) {
				PropInstance[] propBuffer = m_props.m_buffer;
				propID = newPropID;
				propBuffer[newPropID].m_flags = 1;
				propBuffer[newPropID].Info = info;
				propBuffer[newPropID].Single = single;
				propBuffer[newPropID].Blocked = false;
				propBuffer[newPropID].Position = position;
				propBuffer[newPropID].Angle = angle;
				DistrictManager dm = Singleton<DistrictManager>.instance;
				dm.m_parks.m_buffer[dm.GetPark(position)].m_propCount++;
				ItemClass.Availability mode = Singleton<ToolManager>.instance.m_properties.m_mode;
				InitializeProp(newPropID, ref propBuffer[newPropID], (mode & ItemClass.Availability.AssetEditor) != ItemClass.Availability.None);
				propManager.UpdateProp(newPropID);
				propManager.m_propCount = (int)(m_props.ItemCount() - 1u);
				return true;
			}
			propID = 0;
			return false;
        }

		public static void ReleaseProp(this PropManager propManager, uint prop) {
			if (!UseCustomPropLimit) {
				propManager.ReleaseProp((ushort)prop);
				return;
            }
			PropInstance data = m_props.m_buffer[prop];
			if (data.m_flags != 0) {
				InstanceID id = default;
				id.SetProp32(prop);
				Singleton<InstanceManager>.instance.ReleaseInstance(id);
				data.m_flags |= 2;
				data.UpdateProp(prop);
				propManager.UpdatePropRenderer(prop, true);
				if (!data.Blocked) {
					DistrictManager dm = Singleton<DistrictManager>.instance;
					dm.m_parks.m_buffer[dm.GetPark(data.Position)].m_propCount--;
				}
				data.m_flags = 0;
				FinalizeProp(prop, ref data);
				m_props.ReleaseItem(prop);
				propManager.m_propCount--;
			}
		}
	}
}
