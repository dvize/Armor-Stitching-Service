using System;
using System.Diagnostics;
using System.Reflection;
using Aki.Reflection.Patching;
using BepInEx;
using BepInEx.Configuration;
using EFT;
using VersionChecker;

namespace ASS
{
    [BepInPlugin("com.dvize.ASS", "dvize.ASS", "1.1.0")]
    public class Plugin : BaseUnityPlugin
    {
        internal static ConfigEntry<Boolean> ArmorServiceMode { get; set; }
        internal static ConfigEntry<float> TimeDelayRepairInSec { get; set; }
        internal static ConfigEntry<float> ArmorRepairRateOverTime { get; set; }

        internal void Awake()
        {
            CheckEftVersion();

            ArmorServiceMode = Config.Bind(
                "Armor Repair Settings",
                "Enable/Disable Mod",
                true,
                "Enables the Armor Repairing Options Below"
            );
            TimeDelayRepairInSec = Config.Bind(
                "Armor Repair Settings",
                "Time Delay Repair in Sec",
                60f,
                "How Long Before you were last hit that it repairs armor"
            );
            ArmorRepairRateOverTime = Config.Bind(
                "Armor Repair Settings",
                "Armor Repair Rate",
                0.5f,
                "How much durability per second is repaired"
            );
        }

        private void CheckEftVersion()
        {
            // Make sure the version of EFT being run is the correct version
            int currentVersion = FileVersionInfo
                .GetVersionInfo(BepInEx.Paths.ExecutablePath)
                .FilePrivatePart;
            int buildVersion = TarkovVersion.BuildVersion;
            if (currentVersion != buildVersion)
            {
                Logger.LogError(
                    $"ERROR: This version of {Info.Metadata.Name} v{Info.Metadata.Version} was built for Tarkov {buildVersion}, but you are running {currentVersion}. Please download the correct plugin version."
                );
                EFT.UI.ConsoleScreen.LogError(
                    $"ERROR: This version of {Info.Metadata.Name} v{Info.Metadata.Version} was built for Tarkov {buildVersion}, but you are running {currentVersion}. Please download the correct plugin version."
                );
                throw new Exception($"Invalid EFT Version ({currentVersion} != {buildVersion})");
            }
        }
    }

    //re-initializes each new game
    internal class NewGamePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() =>
            typeof(GameWorld).GetMethod(nameof(GameWorld.OnGameStarted));

        [PatchPrefix]
        public static void PatchPrefix()
        {
            ASS.ArmorRegenComponent.Enable();
        }
    }
}
