using ColossalFramework;
using EManagersLib;
using UnityEngine;
using static PropAnarchy.PLT.PropLineTool;

namespace PropAnarchy.PLT {
    public static class UndoManager {
        private const int MAX_UNDO_COUNT = 64;
        public enum ObjectMode {
            Undefined = 0,
            Props = 1,
            Trees = 2
        }
        class CyclicStack<T> {
            private const int m_capacity = MAX_UNDO_COUNT;
            private T[] m_stack;
            private int m_curIndex = 0;
            public int Count { get; private set; }
            public CyclicStack() {
                m_stack = new T[m_capacity];
                Count = 0;
            }
            public void Push(T item) {
                m_curIndex = (m_curIndex + m_capacity - 1) % m_capacity;
                m_stack[m_curIndex] = item;
                Count++;
            }
            public T Pop() {
                int oldIndex = m_curIndex;
                m_curIndex = (m_curIndex + m_capacity + 1) % m_capacity;
                Count--;
                return m_stack[oldIndex];
            }
            public T Peek() => m_stack[m_curIndex];
        }
        public struct UndoEntry {
            public struct ItemSubEntry {
                private uint m_itemID;
                private float m_angle;
                private Vector3 m_position;
                public ObjectMode m_itemType;
                public uint TreeID {
                    get => m_itemID;
                    set {
                        m_itemID = value;
                        m_itemType = ObjectMode.Trees;
                    }
                }
                public uint PropID {
                    get => m_itemID;
                    set {
                        m_itemID = value;
                        m_itemType = ObjectMode.Props;
                    }
                }
                public Vector3 Position {
                    get => m_position;
                    set => m_position = value;
                }
                public float AssetLength { get; set; }
                public float Angle {
                    get => m_angle;
                    set => m_angle = value % 360f;
                }
                public bool ReleaseItem(bool dispatchPlacementEffect) {
                    uint itemID = m_itemID;
                    switch (m_itemType) {
                    case ObjectMode.Trees:
                        if (Singleton<TreeManager>.instance.m_trees.m_buffer[itemID].m_flags != 0) {
                            Singleton<TreeManager>.instance.ReleaseTree(itemID);
                            if (dispatchPlacementEffect) {

                            }
                            return true;
                        }
                        break;
                    case ObjectMode.Props:
                        if (EPropManager.m_props.m_buffer[itemID].m_flags != 0) {
                            Singleton<PropManager>.instance.ReleaseProp(itemID);
                            if (dispatchPlacementEffect) {

                            }
                            return true;
                        }
                        break;
                    }
                    return false;
                }
            }
            public bool m_fenceMode;
            public int m_itemCount;
            public ItemSubEntry[] m_items;
        }
        private static readonly CyclicStack<UndoEntry> m_undoStack = new CyclicStack<UndoEntry>();

        public static bool AddEntry(int itemCount, ItemInfo[] items) {
            UndoEntry entry = default;
            PrefabInfo prefab = ItemInfo.Prefab;
            if (prefab is PropInfo) {
                UndoEntry.ItemSubEntry[] subEntries = new UndoEntry.ItemSubEntry[itemCount];
                for (int i = 0; i < itemCount; i++) {
                    subEntries[i].PropID = items[i].m_itemID;
                    subEntries[i].Position = items[i].Position;
                    subEntries[i].Angle = items[i].m_angle;
                }
                entry.m_itemCount = itemCount;
                entry.m_items = subEntries;
                m_undoStack.Push(entry);
                return true;
            } else if (prefab is TreeInfo) {
                UndoEntry.ItemSubEntry[] subEntries = new UndoEntry.ItemSubEntry[itemCount];
                for (int i = 0; i < itemCount; i++) {
                    subEntries[i].TreeID = items[i].m_itemID;
                    subEntries[i].Position = items[i].Position;
                }
                entry.m_itemCount = itemCount;
                entry.m_items = subEntries;
                m_undoStack.Push(entry);
                return true;
            }
            return false;
        }

        public static bool AddEntry(int itemCount, ItemInfo[] items, bool fenceMode) {
            UndoEntry entry = default;
            PrefabInfo prefab = ItemInfo.Prefab;
            if (prefab is PropInfo) {
                UndoEntry.ItemSubEntry[] subEntries = new UndoEntry.ItemSubEntry[itemCount];
                for (int i = 0; i < itemCount; i++) {
                    subEntries[i].PropID = items[i].m_itemID;
                    subEntries[i].Position = items[i].Position;
                    subEntries[i].Angle = items[i].m_angle;
                }
                entry.m_itemCount = itemCount;
                entry.m_fenceMode = fenceMode;
                //entry.m_segmentState = segmentState;
                entry.m_items = subEntries;
                m_undoStack.Push(entry);
                return true;
            } else if (prefab is TreeInfo) {
                UndoEntry.ItemSubEntry[] subEntries = new UndoEntry.ItemSubEntry[itemCount];
                for (int i = 0; i < itemCount; i++) {
                    subEntries[i].TreeID = items[i].m_itemID;
                    subEntries[i].Position = items[i].Position;
                }
                entry.m_itemCount = itemCount;
                entry.m_fenceMode = fenceMode;
                entry.m_items = subEntries;
                m_undoStack.Push(entry);
                return true;
            }
            return false;
        }

        public static bool UndoLatestEntry() {
            if (m_undoStack.Count > 0) {
                UndoEntry entry = m_undoStack.Pop();
                int itemCount = entry.m_itemCount;
                UndoEntry.ItemSubEntry[] subEntry = entry.m_items;
                for (int i = 0; i < itemCount; i++) {
                    subEntry[i].ReleaseItem(true);
                }
                return true;
            }
            return false;
        }

        public static bool RenderLatestEntryCircles(RenderManager.CameraInfo cameraInfo, Color32 renderColor) {
            if (m_undoStack.Count > 0) {
                Color32 pinpointColor = new Color32(renderColor.r, renderColor.g, renderColor.b, 225);
                Color32 pointColor = new Color32(renderColor.r, renderColor.g, renderColor.b, 204);
                Color32 boundsColor = new Color32(renderColor.r, renderColor.g, renderColor.b, 153);
                UndoEntry entry = m_undoStack.Peek();
                UndoEntry.ItemSubEntry[] items = entry.m_items;
                int itemCount = entry.m_itemCount;
                for (int i = 0; i < itemCount; i++) {
                    Vector3 position = items[i].Position;
                    RenderCircle(cameraInfo, position, DOTSIZE, pinpointColor, false, false);
                    RenderCircle(cameraInfo, position, 2f, pointColor, false, false);
                    RenderCircle(cameraInfo, position, 8f, boundsColor, false, true);
                }
                return true;
            }
            return false;
        }
    }
}
