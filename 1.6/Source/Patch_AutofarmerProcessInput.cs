using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection.Emit;
using VanillaFurnitureExpandedFactory;
using Verse;

namespace VFEFactoryBuffsNTweaks
{
    public static class Patch_AutofarmerProcessInput
    {
        public static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions,
            ILGenerator generator)
        {
            bool debug = VFEFactoryBuffsNTweaksSettings.DebugLog;

            var sowMinSkillField = AccessTools.Field(
                typeof(PlantProperties),
                nameof(PlantProperties.sowMinSkill));

            var getSkillMethod = AccessTools.Method(
                typeof(VFEFactoryBuffsNTweaksSettings),
                nameof(VFEFactoryBuffsNTweaksSettings.GetAutofarmerMaxSkill));

            if (debug)
                Log.Message("[VFEFactoryBuffsNTweaks] Autofarmer transpiler: starting IL scan.");

            bool awaitingBranch = false;
            bool patched        = false;

            foreach (var instruction in instructions)
            {
                if (!patched && instruction.LoadsField(sowMinSkillField))
                {
                    if (debug)
                        Log.Message("[VFEFactoryBuffsNTweaks] Autofarmer transpiler: " +
                                    "found sowMinSkill ldfld, watching next opcode.");
                    yield return instruction;
                    awaitingBranch = true;
                    continue;
                }

                if (awaitingBranch)
                {
                    awaitingBranch = false;

                    if (instruction.opcode == OpCodes.Brtrue_S || instruction.opcode == OpCodes.Brtrue)
                    {
                        if (debug)
                            Log.Message("[VFEFactoryBuffsNTweaks] Autofarmer transpiler: " +
                                        "matched brtrue.s pattern — replacing with " +
                                        "call+bgt.s. Threshold will be " +
                                        VFEFactoryBuffsNTweaksSettings.GetAutofarmerMaxSkill());
                        yield return new CodeInstruction(OpCodes.Call, getSkillMethod);
                        yield return new CodeInstruction(OpCodes.Bgt_S, instruction.operand);
                        patched = true;
                        continue;
                    }

                    if (instruction.opcode == OpCodes.Ldc_I4_0)
                    {
                        if (debug)
                            Log.Message("[VFEFactoryBuffsNTweaks] Autofarmer transpiler: " +
                                        "matched ldc.i4.0 pattern — replacing constant 0 " +
                                        "with call to GetAutofarmerMaxSkill.");
                        yield return new CodeInstruction(OpCodes.Call, getSkillMethod);
                        patched = true;
                        continue;
                    }

                    Log.Warning(
                        "[VFEFactoryBuffsNTweaks] Autofarmer transpiler: unexpected opcode '" +
                        instruction.opcode + "' after sowMinSkill field load. " +
                        "Patch not applied for this occurrence.");
                }

                yield return instruction;
            }

            if (patched)
            {
                Log.Message("[VFEFactoryBuffsNTweaks] Autofarmer transpiler: " +
                            "IL patch applied successfully.");
            }
            else
            {
                Log.Error(
                    "[VFEFactoryBuffsNTweaks] Autofarmer transpiler: patch target not found in " +
                    "ProcessInput IL. The VFE Factory mod may have been updated. " +
                    "Autofarmer skill filter not applied.");
            }
        }
    }
}
