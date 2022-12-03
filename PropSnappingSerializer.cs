using System.IO;
using ColossalFramework.IO;
using EManagersLib;
using ICities;

namespace PropAnarchy
{
    // Mimics original Prop Snapping to store snapping data for non-expanded prop arrays.
    public class SerializableDataExtension : SerializableDataExtensionBase {
        private const string ID = "PropSnapping";
        private const int VERSION = 1;

        public override void OnSaveData() {
            base.OnSaveData();
            if (ToolManager.instance.m_properties.m_mode != ItemClass.Availability.Game) {
                return;
            }
            using (var ms = new MemoryStream()) {
                DataSerializer.Serialize(ms, DataSerializer.Mode.Memory, VERSION, new Data());
                var data = ms.ToArray();
                serializableDataManager.SaveData(ID, data);
            }
        }
    }

    public class Data : IDataContainer {
        public void Serialize(DataSerializer s) {
            var items = EPropManager.m_props.m_buffer;
            if (items.Length == 65536) {
                s.WriteInt32(items.Length);
                var @ushort = EncodedArray.UShort.BeginWrite(s);
                for (var index = 0; index < items.Length; index++)
                    @ushort.Write(items[index].m_posY);
                @ushort.EndWrite();
            }
        }

        public void Deserialize(DataSerializer s) {
        }

        public void AfterDeserialize(DataSerializer s) {
        }
    }
}