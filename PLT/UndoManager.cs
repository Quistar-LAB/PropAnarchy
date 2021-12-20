using ColossalFramework;
using EManagersLib;
using System.Collections;
using UnityEngine;
using static PropAnarchy.PLT.PropLineTool;

namespace PropAnarchy.PLT {
    public static class UndoManager {
        private const int MAX_UNDO_COUNT = 64;
        private class CyclicStack<T> {
            private const int m_capacity = MAX_UNDO_COUNT;
            private readonly T[] m_stack;
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
        public readonly struct UndoEntry {
            public readonly struct ItemSubEntry {
                private readonly uint m_itemID;
                private readonly ItemType m_itemType;
                public Vector3 Position { get; }

                public ItemSubEntry(uint itemID, ItemType itemType, Vector3 position) {
                    m_itemID = itemID;
                    m_itemType = itemType;
                    Position = position;
                }
                public IEnumerator ReleaseItem(bool dispatchPlacementEffect) {
                    uint itemID = m_itemID;
                    switch (m_itemType) {
                    case ItemType.TREE:
                        TreeManager tmInstance = Singleton<TreeManager>.instance;
                        if (tmInstance.m_trees.m_buffer[itemID].m_flags != 0) {
                            tmInstance.ReleaseTree(itemID);
                            if (dispatchPlacementEffect) {
                                DispatchPlacementEffect(Position, true);
                            }
                        }
                        break;
                    case ItemType.PROP:
                        if (EPropManager.m_props.m_buffer[itemID].m_flags != 0) {
                            Singleton<PropManager>.instance.ReleaseProp(itemID);
                            if (dispatchPlacementEffect) {
                                DispatchPlacementEffect(Position, true);
                            }
                        }
                        break;
                    }
                    yield return null;
                }
            }
            public readonly bool m_fenceMode;
            public readonly int m_itemCount;
            public readonly ItemSubEntry[] m_items;
            public UndoEntry(ItemInfo[] items, int itemCount, bool fenceMode) {
                m_itemCount = itemCount;
                m_fenceMode = fenceMode;
                ItemSubEntry[] subItems = new ItemSubEntry[itemCount];
                for (int i = 0; i < itemCount; i++) {
                    subItems[i] = new ItemSubEntry(items[i].m_itemID, ItemInfo.m_itemType, items[i].Position);
                }
                m_items = subItems;
            }
        }
        private static readonly CyclicStack<UndoEntry> m_undoStack = new CyclicStack<UndoEntry>();

        public static bool AddEntry(int itemCount, ItemInfo[] items, bool fenceMode) {
            UndoEntry newUndoEntry = new UndoEntry(items, itemCount, fenceMode);
            m_undoStack.Push(newUndoEntry);
            return true;
        }

        public static bool UndoLatestEntry() {
            if (m_undoStack.Count > 0) {
                UndoEntry entry = m_undoStack.Pop();
                int itemCount = entry.m_itemCount;
                UndoEntry.ItemSubEntry[] subEntry = entry.m_items;
                SimulationManager smInstance = Singleton<SimulationManager>.instance;
                for (int i = 0; i < itemCount; i++) {
                    smInstance.AddAction(subEntry[i].ReleaseItem(true));
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
