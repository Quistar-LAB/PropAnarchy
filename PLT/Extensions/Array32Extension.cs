using System;
using System.Collections.Generic;
using System.Reflection;

namespace PropAnarchy.PLT.Extensions {
    internal static class Array32Extension {
        internal static IEnumerable<uint> NextFreeItems<T>(this Array32<T> array, int numItems) {
            Func<Array32<T>, uint[]> getUnusedItems = PAUtils.CreateGetter<Array32<T>, uint[]>(typeof(Array32<T>).GetField("m_unusedItems", BindingFlags.Instance | BindingFlags.NonPublic));
            Func<Array32<T>, int> getUnusedCount = PAUtils.CreateGetter<Array32<T>, int>(typeof(Array32<T>).GetField("m_unusedCount", BindingFlags.Instance | BindingFlags.NonPublic));
            uint[] unusedItems = getUnusedItems(array);
            int unusedCount = getUnusedCount(array);
            if (unusedCount >= numItems) {
                for (int i = 0; i < numItems; i++) {
                    yield return unusedItems[unusedCount - i];
                }
            }
        }
    }
}
