using System;
using Aki.Reflection.Patching;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using EFT;

namespace armorMod
{
    [BepInPlugin("com.armorMod.ASS", "armorMod.ASS", "1.3.0")]

    public class AssPlugin : BaseUnityPlugin
    {
        internal static ConfigEntry<Boolean> ArmorServiceMode
        {
            get; set;
        }
        /*private ConfigEntry<Boolean> LoseInsuranceOnRepair
        {
            get; set;
        }*/
        internal static ConfigEntry<float> TimeDelayRepairInSec
        {
            get; set;
        }
        internal static ConfigEntry<float> ArmorRepairRateOverTime
        {
            get; set;
        }
        internal static ConfigEntry<float> MaxDurabilityDegradationRateOverTime
        {
            get; set;
        }
        internal static ConfigEntry<float> MaxDurabilityCap
        {
            get; set;
        }
        internal void Awake()
        {
            ArmorServiceMode = Config.Bind("Armor Repair Settings", "Enable/Disable Mod", true, "Enables the Armor Repairing Options Below");
            /*LoseInsuranceOnRepair = Config.Bind("Armor Repair Settings", "Lose Insurance On Repair", true, "If Enabled, you will lose insurance on whenever the armor is repaired in-raid");*/
            TimeDelayRepairInSec = Config.Bind("Armor Repair Settings", "Time Delay Repair in Sec", 60f, "How Long Before you were last hit that it repairs armor");
            ArmorRepairRateOverTime = Config.Bind("Armor Repair Settings", "Armor Repair Rate", 0.5f, "How much durability per second is repaired");
            MaxDurabilityDegradationRateOverTime = Config.Bind("Armor Repair Settings", "Max Durability Drain Rate", 0.025f, "How much max durability per second of repairs is drained");
            MaxDurabilityCap = Config.Bind("Armor Repair Settings", "Max Durability Cap", 100f, "Maximum durability percentage to which armor will be able to repair to. For example, setting to 80 would repair your armor to maximum of 80% of it's max durability");
            
            new NewGamePatch().Enable();
        }

        internal class NewGamePatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod() => typeof(GameWorld).GetMethod(nameof(GameWorld.OnGameStarted));

            [PatchPrefix]
            private static void PatchPrefix()
            {
                AssComponent.Enable();
            }
        }
    }

}

