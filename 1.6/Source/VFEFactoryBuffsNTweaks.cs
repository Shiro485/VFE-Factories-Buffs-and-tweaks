using HarmonyLib;
using VanillaFurnitureExpandedFactory;
using RimWorld;
using UnityEngine;
using Verse;

namespace VFEFactoryBuffsNTweaks
{
    [StaticConstructorOnStartup]
    public static class VFEFactoryBuffsNTweaksInit
    {
        static VFEFactoryBuffsNTweaksInit()
        {
            var harmony = new Harmony("TPSCO.VFEFactoryBuffsNTweaks");
            ApplyAutofanerPatch(harmony);
            ApplyFishfarmPatch(harmony);
        }

        private static void ApplyAutofanerPatch(Harmony harmony)
        {
            var target = AccessTools.Method(
                typeof(Command_SetPlantToGrowAutofarmer), "ProcessInput");

            if (target == null)
            {
                Log.Error("[VFEFactoryBuffsNTweaks] Could not find " +
                          "Command_SetPlantToGrowAutofarmer.ProcessInput — autofarmer patch skipped.");
                return;
            }

            var transpiler = new HarmonyMethod(
                AccessTools.Method(typeof(Patch_AutofarmerProcessInput),
                                   nameof(Patch_AutofarmerProcessInput.Transpiler)));

            harmony.Patch(target, transpiler: transpiler);
            Log.Message("[VFEFactoryBuffsNTweaks] Autofarmer sow-skill patch applied successfully.");
        }

        private static void ApplyFishfarmPatch(Harmony harmony)
        {
            var target = AccessTools.Method(
                typeof(PlaceWorker_Fishfarm), "AllowsPlacing");

            if (target == null)
            {
                Log.Error("[VFEFactoryBuffsNTweaks] Could not find " +
                          "PlaceWorker_Fishfarm.AllowsPlacing — fish farm patch skipped.");
                return;
            }

            var anyFishMethod = Patch_FishfarmAllowsPlacing.ResolveAnyFishMethod();
            if (anyFishMethod == null) return;

            Patch_FishfarmAllowsPlacing.OriginalAnyFishMethod = anyFishMethod;

            var transpiler = new HarmonyMethod(
                AccessTools.Method(typeof(Patch_FishfarmAllowsPlacing),
                                   nameof(Patch_FishfarmAllowsPlacing.Transpiler)));

            harmony.Patch(target, transpiler: transpiler);
            Log.Message("[VFEFactoryBuffsNTweaks] Fish farm fish-population patch applied successfully.");
        }
    }

    public class VFEFactoryBuffsNTweaksMod : Mod
    {
        public static VFEFactoryBuffsNTweaksSettings Settings { get; private set; }

        public VFEFactoryBuffsNTweaksMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<VFEFactoryBuffsNTweaksSettings>();
        }

        public override string SettingsCategory() => "VFE Factory Buffs and Tweaks";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);

            // --- Autofarmer skill threshold ---
            listing.Label(
                "Autofarmer max sowing skill: " + Settings.autofarmerMaxSkill +
                "\n<color=#888888><size=11>Plants requiring a sowing skill higher than this " +
                "value will be hidden from the autofarmer plant picker. " +
                "Vanilla value is 0 (only skill-less plants).</size></color>");
            Settings.autofarmerMaxSkill = (int)listing.Slider(Settings.autofarmerMaxSkill, 0f, 20f);

            listing.Gap();

            // --- Fish farm no-fish bypass ---
            listing.CheckboxLabeled(
                "Fish farms work in water without natural fish population",
                ref Settings.fishFarmIgnoreFishPopulation,
                "When enabled, fish farms can be placed in any valid water tile even if " +
                "no natural fish population exists there.");

            listing.Gap();

            // --- Debug logging ---
            listing.CheckboxLabeled(
                "Enable debug logging",
                ref Settings.debugLogging,
                "When enabled, detailed logs are printed to the dev console for both patches. " +
                "Useful for diagnosing issues. Disable in normal play.");

            listing.End();
            base.DoSettingsWindowContents(inRect);
        }
    }

    public class VFEFactoryBuffsNTweaksSettings : ModSettings
    {
        public int autofarmerMaxSkill = 0;
        public bool fishFarmIgnoreFishPopulation = false;
        public bool debugLogging = false;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref autofarmerMaxSkill,            "autofarmerMaxSkill",            0);
            Scribe_Values.Look(ref fishFarmIgnoreFishPopulation,  "fishFarmIgnoreFishPopulation",  false);
            Scribe_Values.Look(ref debugLogging,                  "debugLogging",                  false);
            base.ExposeData();
        }

        public static int GetAutofarmerMaxSkill() =>
            VFEFactoryBuffsNTweaksMod.Settings?.autofarmerMaxSkill ?? 0;

        public static bool DebugLog =>
            VFEFactoryBuffsNTweaksMod.Settings?.debugLogging ?? false;
    }
}