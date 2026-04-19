using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using VanillaFurnitureExpandedFactory;
using Verse;

namespace VFEFactoryBuffsNTweaks
{
    public static class Patch_FishfarmAllowsPlacing
    {
        public static MethodInfo OriginalAnyFishMethod = null;

        public static MethodInfo ResolveAnyFishMethod()
        {
            bool debug = VFEFactoryBuffsNTweaksSettings.DebugLog;

            if (debug)
                Log.Message("[VFEFactoryBuffsNTweaks] Fish farm: resolving AnyFishPopulationAt...");

            FieldInfo trackerField = AccessTools.Field(typeof(Map), "waterBodyTracker");
            if (trackerField == null)
            {
                Log.Error("[VFEFactoryBuffsNTweaks] Could not find Map.waterBodyTracker field. " +
                          "Fish farm patch will not be applied. " +
                          "This likely means RimWorld or VFE Factory has been updated.");
                return null;
            }

            System.Type trackerType = trackerField.FieldType;

            if (debug)
                Log.Message("[VFEFactoryBuffsNTweaks] Fish farm: Map.waterBodyTracker field found, " +
                            "type = " + trackerType.FullName);

            MethodInfo method = AccessTools.Method(trackerType, "AnyFishPopulationAt",
                                                   new[] { typeof(IntVec3) });
            if (method == null)
            {
                Log.Error("[VFEFactoryBuffsNTweaks] Could not find AnyFishPopulationAt(IntVec3) on " +
                          trackerType.FullName + ". Fish farm patch will not be applied.");
                return null;
            }

            if (debug)
                Log.Message("[VFEFactoryBuffsNTweaks] Fish farm: resolved AnyFishPopulationAt on " +
                            trackerType.FullName);

            return method;
        }

        public static bool AnyFishPopulationBypass(object trackerInstance, IntVec3 c)
        {
            bool debug = VFEFactoryBuffsNTweaksSettings.DebugLog;

            if (VFEFactoryBuffsNTweaksMod.Settings?.fishFarmIgnoreFishPopulation == true)
            {
                if (debug)
                    Log.Message("[VFEFactoryBuffsNTweaks] Fish farm bypass: " +
                                "fishFarmIgnoreFishPopulation is ON — returning true for " + c);
                return true;
            }

            bool result = (bool)OriginalAnyFishMethod.Invoke(trackerInstance, new object[] { c });

            if (debug)
                Log.Message("[VFEFactoryBuffsNTweaks] Fish farm bypass: " +
                            "AnyFishPopulationAt(" + c + ") = " + result);

            return result;
        }

        public static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions)
        {
            bool debug = VFEFactoryBuffsNTweaksSettings.DebugLog;

            if (OriginalAnyFishMethod == null)
            {
                Log.Warning("[VFEFactoryBuffsNTweaks] Fish farm transpiler: " +
                            "OriginalAnyFishMethod is null, emitting unmodified IL.");
                foreach (var i in instructions) yield return i;
                yield break;
            }

            if (debug)
                Log.Message("[VFEFactoryBuffsNTweaks] Fish farm transpiler: " +
                            "starting IL scan for AnyFishPopulationAt call.");

            var bypassMethod = AccessTools.Method(
                typeof(Patch_FishfarmAllowsPlacing), nameof(AnyFishPopulationBypass));

            bool patched = false;

            foreach (var instruction in instructions)
            {
                if (!patched && instruction.Calls(OriginalAnyFishMethod))
                {
                    if (debug)
                        Log.Message("[VFEFactoryBuffsNTweaks] Fish farm transpiler: " +
                                    "found AnyFishPopulationAt call — redirecting to bypass.");

                    yield return new CodeInstruction(OpCodes.Call, bypassMethod);
                    patched = true;
                    continue;
                }
                yield return instruction;
            }

            if (patched)
            {
                Log.Message("[VFEFactoryBuffsNTweaks] Fish farm transpiler: " +
                            "AnyFishPopulationAt successfully redirected to bypass.");
            }
            else
            {
                Log.Error("[VFEFactoryBuffsNTweaks] Fish farm transpiler: " +
                          "AnyFishPopulationAt call not found in AllowsPlacing IL. " +
                          "The VFE Factory mod may have been updated. " +
                          "Fish population bypass not applied.");
            }
        }
    }
}
