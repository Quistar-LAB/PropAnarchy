using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using static PropAnarchy.PAModule;

namespace PropAnarchy {
    internal partial class PAPatcher {
        private static IEnumerable<CodeInstruction> ReplaceLDCI4_MaxTreeLimit(IEnumerable<CodeInstruction> instructions) {
            foreach (var instruction in instructions) {
                if (instruction.LoadsConstant(LastMaxTreeLimit))
                    yield return new CodeInstruction(OpCodes.Ldc_I4, MaxTreeLimit);
                else
                    yield return instruction;
            }
        }

        private void EnableLimitPatches(Harmony harmony) {

        }

        private void DisableLimitPatches(Harmony harmony) {

        }
    }
}
