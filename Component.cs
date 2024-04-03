using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using UnityEngine;

#pragma warning disable IDE0044

namespace armorMod
{
    internal class AssComponent : MonoBehaviour
    {
        private static GameWorld gameWorld = new GameWorld();
        private static Player player = new Player();
        private static InventoryControllerClass _cachedInventoryController;

        private static float newRepairRate;
        private static float newMaxDurabilityDrainRate;
        private static float newWeaponRepairRate;
        private static float newWeaponMaxDurabilityDrainRate;
        private static RepairableComponent armor;
        private static FaceShieldComponent faceShield;
        private static RepairableComponent weapon;
        private static float maxRepairableDurabilityBasedOnCap;
        private static float maxWeaponRepairableDurabilityBasedOnCap;
        private static float timeSinceLastHit = 0f;
        private static Slot tempSlot;

        private int frameCount = 0;

        private static Dictionary<EquipmentSlot, List<Item>> equipmentSlotDictionary = new Dictionary<EquipmentSlot, List<Item>>
        {
            { EquipmentSlot.ArmorVest, new List<Item>() },
            { EquipmentSlot.TacticalVest, new List<Item>() },
            { EquipmentSlot.Eyewear, new List<Item>() },
            { EquipmentSlot.FaceCover, new List<Item>() },
            { EquipmentSlot.Headwear, new List<Item>() },
        };

        private static Dictionary<EquipmentSlot, List<Item>> weaponSlotDictionary = new Dictionary<EquipmentSlot, List<Item>>
        {
            { EquipmentSlot.FirstPrimaryWeapon, new List<Item>() },
            { EquipmentSlot.SecondPrimaryWeapon, new List<Item>() },
            { EquipmentSlot.Holster, new List<Item>() },
        };
        protected static ManualLogSource Logger
        {
            get; private set;
        }
        private AssComponent()
        {
            if (Logger == null)
            {
                Logger = BepInEx.Logging.Logger.CreateLogSource(nameof(AssComponent));
            }
        }

        private void Start()
        {
            player = Singleton<GameWorld>.Instance.MainPlayer;
            player.BeingHitAction += Player_BeingHitAction;
            timeSinceLastHit = 0;
            _cachedInventoryController = (InventoryControllerClass)AccessTools.Field(typeof(Player), "_inventoryController").GetValue(player);
        }
        internal static void Enable()
        {
            if (Singleton<IBotGame>.Instantiated)
            {
                gameWorld = Singleton<GameWorld>.Instance;
                gameWorld.GetOrAddComponent<AssComponent>();

                Logger.LogDebug("AssComponent enabled");
            }
        }

        private void Update()
        {
            frameCount++;

            // Perform actions every 60 frames
            if (frameCount >= 60)
            {
                frameCount = 0; // Reset frame count

                // Assuming Time.deltaTime accumulates over 60 frames for calculations
                float accumulatedDeltaTime = Time.deltaTime * 60;

                timeSinceLastHit += accumulatedDeltaTime;

                if (AssPlugin.ArmorServiceMode.Value && timeSinceLastHit >= AssPlugin.TimeDelayRepairInSec.Value)
                {
                    var armorItems = equipmentSlotDictionary.Values.SelectMany(slot => slot).SelectMany(slot => slot.GetAllItems());
                    RepairItems(armorItems, isWeapon: false, accumulatedDeltaTime);
                }

                if (AssPlugin.WeaponServiceMode.Value && timeSinceLastHit >= AssPlugin.weaponTimeDelayRepairInSec.Value)
                {
                    var weaponItems = weaponSlotDictionary.Values.SelectMany(slot => slot).SelectMany(slot => slot.GetAllItems());
                    RepairItems(weaponItems, isWeapon: true, accumulatedDeltaTime);
                }
            }
        }

        private void RepairItems(IEnumerable<Item> items, bool isWeapon, float accumulatedDeltaTime)
        {
            float repairRate = isWeapon ? AssPlugin.weaponRepairRateOverTime.Value * accumulatedDeltaTime : AssPlugin.ArmorRepairRateOverTime.Value * accumulatedDeltaTime;
            float maxDurabilityDrainRate = isWeapon ? AssPlugin.weaponMaxDurabilityDegradationRateOverTime.Value * accumulatedDeltaTime : AssPlugin.MaxDurabilityDegradationRateOverTime.Value * accumulatedDeltaTime;

            foreach (var item in items)
            {
                if (!isWeapon)
                {
                    if (item.TryGetItemComponent<FaceShieldComponent>(out var faceShield) && AssPlugin.fixFaceShieldBullets.Value)
                    {
                        if (faceShield.Hits > 0)
                        {
                            faceShield.Hits = 0;
                            faceShield.HitsChanged?.Invoke();
                        }
                    }
                }

                if (item.TryGetItemComponent<RepairableComponent>(out var repairable))
                {
                    float maxRepairableDurabilityBasedOnCap = ((isWeapon ? AssPlugin.weaponMaxDurabilityCap.Value : AssPlugin.MaxDurabilityCap.Value) / 100) * repairable.MaxDurability;

                    if (repairable.Durability < maxRepairableDurabilityBasedOnCap)
                    {
                        repairable.Durability = Mathf.Min(repairable.Durability + repairRate, repairable.MaxDurability);
                        repairable.MaxDurability = Mathf.Max(repairable.MaxDurability - maxDurabilityDrainRate, 0);
                    }
                }
            }
        }
        private void Player_BeingHitAction(DamageInfo dmgInfo, EBodyPart bodyPart, float hitEffectId) => timeSinceLastHit = 0f;



        private static Slot slotContents;
        private static InventoryControllerClass inventoryController;
        private Slot getEquipSlot(EquipmentSlot slot)
        {
            if (_cachedInventoryController != null)
            {
                var slotContents = _cachedInventoryController.Inventory.Equipment.GetSlot(slot);
                return slotContents.ContainedItem == null ? null : slotContents;
            }
            return null;
        }


    }
}

