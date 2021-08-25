using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using ColossalFramework;
using UnityEngine;

namespace PropAnarchy {
    internal partial class PAPatcher {
        private void EnablePropVariation(Harmony harmony) {
            harmony.Patch(AccessTools.Method(typeof(PropInstance), nameof(PropInstance.RenderInstance), new Type[] { typeof(RenderManager.CameraInfo), typeof(ushort), typeof(int) }),
                transpiler: new HarmonyMethod(AccessTools.Method(typeof(PAPatcher), nameof(PropInstanceRenderInstanceTranspiler))));
            harmony.Patch(AccessTools.Method(typeof(PropInstance), nameof(PropInstance.PopulateGroupData),
                new Type[] { typeof(ushort), typeof(int), typeof(int).MakeByRefType(), typeof(int).MakeByRefType(), typeof(Vector3), typeof(RenderGroup.MeshData),
                            typeof(Vector3).MakeByRefType(), typeof(Vector3).MakeByRefType(), typeof(float).MakeByRefType(), typeof(float).MakeByRefType() }),
                transpiler: new HarmonyMethod(AccessTools.Method(typeof(PAPatcher), nameof(PropInstancePopulateGroupDataTranspiler))));
            harmony.Patch(AccessTools.Method(typeof(PropTool), nameof(PropTool.RenderGeometry)),
                transpiler: new HarmonyMethod(AccessTools.Method(typeof(PAPatcher), nameof(PropToolRenderGeometryTranspiler))));
            harmony.Patch(AccessTools.Method(typeof(PropTool), nameof(PropTool.RenderOverlay), new Type[] { typeof(RenderManager.CameraInfo) }),
                transpiler: new HarmonyMethod(AccessTools.Method(typeof(PAPatcher), nameof(PropToolRenderOverlayTranspiler))));
        }

        private void DisablePropVariation(Harmony harmony) {
            harmony.Unpatch(AccessTools.Method(typeof(PropInstance), nameof(PropInstance.RenderInstance), new Type[] { typeof(RenderManager.CameraInfo), typeof(ushort), typeof(int) }),
                HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(PropInstance), nameof(PropInstance.PopulateGroupData),
                new Type[] { typeof(ushort), typeof(int), typeof(int).MakeByRefType(), typeof(int).MakeByRefType(), typeof(Vector3), typeof(RenderGroup.MeshData),
                            typeof(Vector3).MakeByRefType(), typeof(Vector3).MakeByRefType(), typeof(float).MakeByRefType(), typeof(float).MakeByRefType() }),
                HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(PropTool), nameof(PropTool.RenderGeometry)), HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(PropTool), nameof(PropTool.RenderOverlay), new Type[] { typeof(RenderManager.CameraInfo) }),
                HarmonyPatchType.Transpiler, HARMONYID);
        }

        private static IEnumerable<CodeInstruction> PropInstancePopulateGroupDataTranspiler(IEnumerable<CodeInstruction> instructions) {
            var codes = instructions.ToList();

            return codes.AsEnumerable();
        }

        private static IEnumerable<CodeInstruction> PropInstanceRenderInstanceTranspiler(IEnumerable<CodeInstruction> instructions) {
            var codes = instructions.ToList();

            return codes.AsEnumerable();
        }

        private static IEnumerable<CodeInstruction> PropToolRenderGeometryTranspiler(IEnumerable<CodeInstruction> instructions) {
            var codes = instructions.ToList();

            return codes.AsEnumerable();
        }

        private static IEnumerable<CodeInstruction> PropToolRenderOverlayTranspiler(IEnumerable<CodeInstruction> instructions) {
            var codes = instructions.ToList();

            return codes.AsEnumerable();
        }
    }
}