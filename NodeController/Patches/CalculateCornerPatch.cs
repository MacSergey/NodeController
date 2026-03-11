namespace NodeController.Patches {
    using HarmonyLib;
    using JetBrains.Annotations;
    using KianCommons;
    using NodeController.Util;
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Emit;
    using UnityEngine;
    using static KianCommons.Patches.TranspilerUtils;

    [UsedImplicitly]
    [HarmonyPatch]
    static class CalculateCornerPatch {
        /// <param name="leftSide">left side going away from the junction</param>
        static float GetMinCornerOffset(float cornerOffset0, ushort nodeID, ushort segmentID, bool leftSide) {
            var segmentData = SegmentEndManager.Instance.
                GetAt(segmentID: segmentID, nodeID: nodeID);
            if (segmentData == null)
                return cornerOffset0;
            return segmentData.Corner(leftSide).Offset;
        }

        [UsedImplicitly]
        static MethodBase TargetMethod() {
            return typeof(NetSegment).GetMethod(
                    nameof(NetSegment.CalculateCorner), [typeof(NetInfo) , typeof(Vector3) , typeof(Vector3) , typeof(Vector3) , typeof(Vector3) , typeof(NetInfo) , typeof(Vector3) , typeof(Vector3) , typeof(Vector3) , typeof(NetInfo) , typeof(Vector3) , typeof(Vector3) , typeof(Vector3) , typeof(ushort) , typeof(ushort) , typeof(bool) , typeof(bool) , typeof(Vector3).MakeByRefType() , typeof(Vector3).MakeByRefType() , typeof(bool).MakeByRefType() , typeof(float)],
                    BindingFlags.Public | BindingFlags.Static) ??
                    throw new System.Exception("CalculateCornerPatch Could not find target method.");
        }

        static MethodInfo mGetMinCornerOffsetOriginal = ReflectionHelpers.GetMethod(
            typeof(NetAI), nameof(NetAI.GetMinCornerOffset));

        static MethodInfo mGetMinCornerOffset = ReflectionHelpers.GetMethod(
            typeof(CalculateCornerPatch), nameof(GetMinCornerOffset));

        [HarmonyBefore(CSURUtil.HARMONY_ID)]
        public static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions, MethodBase original) {
            // apply the flat junctions traspiler
            instructions = FlatJunctionsCommons.ModifyFlatJunctionsTranspiler(instructions, original);

            CodeInstruction ldarg_startNodeID = GetLDArg(original, "startNodeID"); // push startNodeID into stack,
            CodeInstruction ldarg_segmentID = GetLDArg(original, "ignoreSegmentID");
            CodeInstruction ldarg_leftSide = GetLDArg(original, "leftSide");
            CodeInstruction call_GetMinCornerOffset = new CodeInstruction(OpCodes.Call, mGetMinCornerOffset);

            int n = 0;
            foreach (var instruction in instructions) {
                yield return instruction;
                bool is_Callvirt_GetMinCornerOffsetOriginal =
                    instruction.opcode == OpCodes.Callvirt && instruction.operand == mGetMinCornerOffsetOriginal;
                if (is_Callvirt_GetMinCornerOffsetOriginal) {
                    n++;
                    yield return ldarg_startNodeID;
                    yield return ldarg_segmentID;
                    yield return ldarg_leftSide;
                    yield return call_GetMinCornerOffset;
                }
            }

            Log.Debug($"TRANSPILER CalculateCornerPatch: Successfully patched NetSegment.CalculateCorner(). " +
                $"found {n} instances of Callvirt NetAI.GetMinCornerOffset()");
            yield break;
        }
    }
}
