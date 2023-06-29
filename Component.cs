using System.Collections.Generic;
using System.Linq;
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
        private static GameWorld gameWorld = new GameWorld();
        private static Player player = new Player();

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

            timeSinceLastHit += Time.deltaTime;

            if ((timeSinceLastHit >= AssPlugin.TimeDelayRepairInSec.Value) && AssPlugin.ArmorServiceMode.Value)
            {
                RepairArmor();
            }

            if ((timeSinceLastHit >= AssPlugin.weaponTimeDelayRepairInSec.Value) && AssPlugin.WeaponServiceMode.Value)
            {
                RepairWeapon();
            }

        }


        private void RepairArmor()
        {
            newRepairRate = AssPlugin.ArmorRepairRateOverTime.Value * Time.deltaTime;
            newMaxDurabilityDrainRate = AssPlugin.MaxDurabilityDegradationRateOverTime.Value * Time.deltaTime;

            foreach (EquipmentSlot slot in equipmentSlotDictionary.Keys.ToArray())
            {
                Slot tempSlot = getEquipSlot(slot);

                if (tempSlot == null || tempSlot.ContainedItem == null)
                {
                    continue;
                }

                foreach (var item in tempSlot.ContainedItem.GetAllItems())
                {
                    //get the armorcomponent of each item in items and check to see if all item componenets (even helmet side ears) are max durability

                    //Logger.LogDebug("Examining the item: " + item.Name.Localized() + " in slot: " + slot.ToString() + " for repairable component");

                    item.TryGetItemComponent<RepairableComponent>(out armor);


                    if (armor == null)
                    {
                        //Logger.LogDebug("Item: " + item.Name.Localized() + " in slot: " + slot.ToString() + " does not have a repairable component");
                        continue;
                    }

                    if (slot == EquipmentSlot.Headwear)
                    {

                        item.TryGetItemComponent<FaceShieldComponent>(out faceShield);

                        if (faceShield != null)
                        {
                            if (faceShield.Hits > 0 && AssPlugin.fixFaceShieldBullets.Value)
                            {
                                faceShield.Hits = 0;
                                faceShield.HitsChanged.Invoke();
                            }
                        }
                    }
                
                
                    maxRepairableDurabilityBasedOnCap = ((AssPlugin.MaxDurabilityCap.Value / 100) * armor.MaxDurability);

                    //check if it needs repair for the current item in loop of all items for the slot
                    if (armor.Durability < maxRepairableDurabilityBasedOnCap)
                    {
                        //increase armor durability by newRepairRate until maximum then set as maximum durability
                        if (armor.Durability + newRepairRate >= armor.MaxDurability)
                        {
                            armor.Durability = armor.MaxDurability;
                        }
                        else
                        {
                            armor.Durability += newRepairRate;
                            armor.MaxDurability -= newMaxDurabilityDrainRate;
                        }
                    }
                }

            }

        }

        private void RepairWeapon()
        {

            newWeaponRepairRate = AssPlugin.weaponRepairRateOverTime.Value * Time.deltaTime;
            newWeaponMaxDurabilityDrainRate = AssPlugin.weaponMaxDurabilityDegradationRateOverTime.Value * Time.deltaTime;

            foreach (EquipmentSlot slot in weaponSlotDictionary.Keys.ToArray())
            {
                Slot tempSlot = getEquipSlot(slot);

                if (tempSlot == null || tempSlot.ContainedItem == null)
                {
                    continue;
                }

                foreach (var item in tempSlot.ContainedItem.GetAllItems())
                {
                    item.TryGetItemComponent<RepairableComponent>(out weapon);

                    if (weapon == null)
                    {
                        //Logger.LogDebug("Item: " + item.Name.Localized() + " in slot: " + slot.ToString() + " does not have a repairable component");
                        continue;
                    }

                    //Logger.LogDebug("Item: " + item.Name.Localized() + " in slot: " + slot.ToString() + " has a repairable component");
                    maxWeaponRepairableDurabilityBasedOnCap = ((AssPlugin.weaponMaxDurabilityCap.Value / 100) * weapon.MaxDurability);

                    //check if it needs repair for the current item in loop of all items for the slot
                    if (weapon.Durability < maxWeaponRepairableDurabilityBasedOnCap)
                    {
                        //increase weapon durability by newWeaponRepairRate until maximum then set as maximum durability
                        if (weapon.Durability + newWeaponRepairRate >= weapon.MaxDurability)
                        {
                            weapon.Durability = weapon.MaxDurability;
                        }
                        else
                        {
                            weapon.Durability += newWeaponRepairRate;
                            weapon.MaxDurability -= newWeaponMaxDurabilityDrainRate;
                        }
                    }
                }
            }

        }
        private void Player_BeingHitAction(DamageInfo dmgInfo, EBodyPart bodyPart, float hitEffectId) => timeSinceLastHit = 0f;



        private static Slot slotContents;
        private static InventoryControllerClass inventoryController;
        private Slot getEquipSlot(EquipmentSlot slot)
        {
            // Use AccessTools to get the protected field _inventoryController
            inventoryController = (InventoryControllerClass)AccessTools.Field(typeof(Player), "_inventoryController").GetValue(player);

            if (inventoryController != null)
            {
                slotContents = inventoryController.Inventory.Equipment.GetSlot(slot);

                if (slotContents.ContainedItem == null)
                {
                    return null;
                }

                return slotContents;
            }

            return null;
        }


    }
}

