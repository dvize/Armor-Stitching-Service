using System.Collections.Generic;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using UnityEngine;

namespace armorMod
{
    internal class AssComponent : MonoBehaviour
    {
        internal static ManualLogSource Logger;
        internal static GameWorld gameWorld;
        internal static Player player;
        internal static float timeSinceLastHit = 0f;
        internal static float timeSinceLastRepair = 0f; // New variable to control repair frequency
        internal static Slot slotContents;
        internal static InventoryControllerClass inventoryController;

        internal static List<EquipmentSlot> equipmentSlotDictionary = new List<EquipmentSlot>
        {
            { EquipmentSlot.ArmorVest},
            { EquipmentSlot.TacticalVest},
            { EquipmentSlot.Eyewear},
            { EquipmentSlot.FaceCover},
            { EquipmentSlot.Headwear},
        };

        internal static List<EquipmentSlot> weaponSlotDictionary = new List<EquipmentSlot>
        {
            { EquipmentSlot.FirstPrimaryWeapon },
            { EquipmentSlot.SecondPrimaryWeapon },
            { EquipmentSlot.Holster },
        };

        private void Awake()
        {
            if (Logger == null)
                Logger = BepInEx.Logging.Logger.CreateLogSource(nameof(AssComponent));
        }

        private void Start()
        {
            player = gameWorld.MainPlayer;
            inventoryController = (InventoryControllerClass)AccessTools.Field(typeof(Player), "_inventoryController").GetValue(player);
            player.BeingHitAction += ResetTimeSinceLastHit;
            Logger.LogDebug("AssComponent enabled successfully.");
        }

        private void Update()
        {
            timeSinceLastHit += Time.deltaTime;
            timeSinceLastRepair += Time.deltaTime;

            if (timeSinceLastRepair >= 1.0f)
            {
                if (timeSinceLastHit >= AssPlugin.TimeDelayRepairInSec.Value && AssPlugin.ArmorServiceMode.Value)
                {
                    RepairItems(equipmentSlotDictionary, true);
                }

                if (timeSinceLastHit >= AssPlugin.weaponTimeDelayRepairInSec.Value && AssPlugin.WeaponServiceMode.Value)
                {
                    RepairItems(weaponSlotDictionary, false);
                }

                timeSinceLastRepair = 0f; 
            }
        }

        private static void RepairItems(List<EquipmentSlot> slots, bool isArmor)
        {
            float repairRate = isArmor ? AssPlugin.ArmorRepairRateOverTime.Value : AssPlugin.weaponRepairRateOverTime.Value;
            float maxDurabilityDrainRate = isArmor ? AssPlugin.MaxDurabilityDegradationRateOverTime.Value : AssPlugin.weaponMaxDurabilityDegradationRateOverTime.Value;

            foreach (var slot in slots)
            {
                var slotContents = GetEquipSlot(slot);
                if (slotContents?.ContainedItem == null) continue;

                foreach (Item item in slotContents.ContainedItem.GetAllItems())
                {
                    if (isArmor)
                    {
                        // Specifically for face shields, if the item has a FaceShieldComponent.
                        if (item.TryGetItemComponent<FaceShieldComponent>(out var faceShield) && AssPlugin.fixFaceShieldBullets.Value)
                        {
                            // Check if face shield has been hit and reset hits if so.
                            if (faceShield.Hits > 0)
                            {
                                faceShield.Hits = 0;
                                faceShield.HitsChanged?.Invoke();
                            }
                        }
                    }

                    if (item.TryGetItemComponent<RepairableComponent>(out var component))
                    {
                        float maxCap = isArmor ? AssPlugin.MaxDurabilityCap.Value : AssPlugin.weaponMaxDurabilityCap.Value;
                        float maxRepairableDurability = (maxCap / 100) * component.MaxDurability;

                        if (component.Durability < maxRepairableDurability)
                        {
#if DEBUG
                            Logger.LogWarning($"Repairing {item.Name.Localized()} in {slot} with {component.Durability} / {component.MaxDurability} durability");
#endif
                            component.Durability = Mathf.Min(component.Durability + repairRate, component.MaxDurability);
                            component.MaxDurability = Mathf.Max(component.MaxDurability - maxDurabilityDrainRate, 0);
                        }
                    }
                }
            }
        }

        private static void ResetTimeSinceLastHit(DamageInfo dmgInfo, EBodyPart bodyPart, float hitEffectId)
        {
            timeSinceLastHit = 0f;
        }

        private static Slot GetEquipSlot(EquipmentSlot slot)
        {
            if (inventoryController != null)
            {
                slotContents = inventoryController.Inventory.Equipment.GetSlot(slot);
                return slotContents.ContainedItem == null ? null : slotContents;
            }
            return null;
        }

        internal static void Enable()
        {
            if (Singleton<IBotGame>.Instantiated)
            {
                gameWorld = Singleton<GameWorld>.Instance;
                gameWorld.GetOrAddComponent<AssComponent>();
            }
        }
    }
}