using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ColossalFramework;
using ColossalFramework.IO;
using ICities;


namespace PropAnarchy {
    public class PASerializeDataExtension : ISerializableDataExtension {
        private enum Format : uint {
            Version1 = 1,
            Version4 = 4,
            Version5,
            Version6,
            Version7,
        }
        private const string PROP_ANARCHY_KEY = @"Quistar/PropAnarchy";

        public class Data : IDataContainer {
            public void AfterDeserialize(DataSerializer s) {}

            public void Deserialize(DataSerializer s) {
            }

            public void Serialize(DataSerializer s) {
            }
        }

        public static void IntegratedDeserialize() {

        }

        public void OnCreated(ISerializableData serializedData) {}

        public void OnLoadData() {}

        public void OnReleased() {}

        public void OnSaveData() {}
    }
}
