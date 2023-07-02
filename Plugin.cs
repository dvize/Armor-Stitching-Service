using System;
using System.Reflection;
using Aki.Reflection.Patching;
using BepInEx;
using BepInEx.Configuration;
using EFT;

namespace armorMod
{
    [BepInPlugin("com.dvize.ASS", "dvize.ASS", "1.3.1")]

    public class AssPlugin : BaseUnityPlugin
    {
        internal static ConfigEntry<Boolean> ArmorServiceMode
        {
            get; set;
        }
        internal static ConfigEntry<Boolean> WeaponServiceMode
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

        internal static ConfigEntry<float> weaponTimeDelayRepairInSec
        {
            get; set;
        }
        internal static ConfigEntry<float> weaponRepairRateOverTime
        {
            get; set;
        }
        internal static ConfigEntry<float> weaponMaxDurabilityDegradationRateOverTime
        {
            get; set;
        }
        internal static ConfigEntry<float> weaponMaxDurabilityCap
        {
            get; set;
        }

        internal static ConfigEntry<Boolean> fixFaceShieldBullets
        {
            get; set;
        }

        internal void Awake()
        {
            ArmorServiceMode = Config.Bind("Main Settings", "Enable/Disable Armor Repair", true, new ConfigDescription("Enables the Armor Repairing Options Below",
                null, new ConfigurationManagerAttributes { IsAdvanced = false, Order = 2 }));
            WeaponServiceMode = Config.Bind("Main Settings", "Enable/Disable Weapon Repair", true, new ConfigDescription("Enables the Weapon Repairing Options Below",
                null, new ConfigurationManagerAttributes { IsAdvanced = false, Order = 1 }));

            /*LoseInsuranceOnRepair = Config.Bind("Armor Repair Settings", "Lose Insurance On Repair", true, "If Enabled, you will lose insurance on whenever the armor is repaired in-raid");*/
            TimeDelayRepairInSec = Config.Bind("Armor Repair Settings", "Time Delay Repair in Sec", 60f, new ConfigDescription("How Long Before you were last hit that it repairs armor",
                new AcceptableValueRange<float>(0f, 1200f), new ConfigurationManagerAttributes { IsAdvanced = false, Order = 5 }));
            ArmorRepairRateOverTime = Config.Bind("Armor Repair Settings", "Armor Repair Rate", 0.5f, new ConfigDescription("How much durability per second is repaired",
                new AcceptableValueRange<float>(0f, 10f), new ConfigurationManagerAttributes { IsAdvanced = false, Order = 4 }));
            MaxDurabilityDegradationRateOverTime = Config.Bind("Armor Repair Settings", "Max Durability Drain Rate", 0.025f, new ConfigDescription("How much max durability per second of repairs is drained",
                new AcceptableValueRange<float>(0f, 100f), new ConfigurationManagerAttributes { IsAdvanced = false, Order = 3 }));
            MaxDurabilityCap = Config.Bind("Armor Repair Settings", "Max Durability Cap", 100f, new ConfigDescription("Maximum durability percentage to which armor will be able to repair to. For example, setting to 80 would repair your armor to maximum of 80% of it's max durability",
                new AcceptableValueRange<float>(0f, 100f), new ConfigurationManagerAttributes { IsAdvanced = false, Order = 2 }));

            weaponTimeDelayRepairInSec = Config.Bind("Weapon Repair Settings", "Time Delay Repair in Sec", 60f, new ConfigDescription("How Long Before you were last hit that it repairs weapon. Doesn't Make sense but i'm too lazy to change.",
                new AcceptableValueRange<float>(0f, 1200f), new ConfigurationManagerAttributes { IsAdvanced = false, Order = 5 }));
            weaponRepairRateOverTime = Config.Bind("Weapon Repair Settings", "Weapon Repair Rate", 0.5f, new ConfigDescription("How much durability per second is repaired",
                new AcceptableValueRange<float>(0f, 10f), new ConfigurationManagerAttributes { IsAdvanced = false, Order = 4 }));
            weaponMaxDurabilityDegradationRateOverTime = Config.Bind("Weapon Repair Settings", "Max Durability Drain Rate", 0f, new ConfigDescription("How much max durability per second of repairs is drained (set really low if using)",
                new AcceptableValueRange<float>(0f, 100f), new ConfigurationManagerAttributes { IsAdvanced = false, Order = 3 }));
            weaponMaxDurabilityCap = Config.Bind("Weapon Repair Settings", "Max Durability Cap", 100f, new ConfigDescription("Maximum durability percentage to which weapon will be able to repair to. For example, setting to 80 would repair your armor to maximum of 80% of it's max durability",
                new AcceptableValueRange<float>(0f, 100f), new ConfigurationManagerAttributes { IsAdvanced = false, Order = 2 }));


            fixFaceShieldBullets = Config.Bind("Face Shield", "Fix Bullet Cracks", true, new ConfigDescription("Enables Repairing Bullet Cracks in FaceShield",
                null, new ConfigurationManagerAttributes { IsAdvanced = false, Order = 2 }));

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

