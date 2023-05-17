using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using UnityEngine;

namespace ASS
{
    internal class ArmorRegenComponent : MonoBehaviour
    {
        private static float newRepairRate;
        private static ArmorComponent armor;
        private static float timeSinceLastHit = 0f;
        private static Player player;
        private static Slot slotContents;
        private static bool isRegenerating = false;
        private static InventoryControllerClass inventoryController;

        private readonly Dictionary<EquipmentSlot, List<Item>> equipmentSlotDictionary =
            new Dictionary<EquipmentSlot, List<Item>>
            {
                { EquipmentSlot.ArmorVest, new List<Item>() },
                { EquipmentSlot.TacticalVest, new List<Item>() },
                { EquipmentSlot.Eyewear, new List<Item>() },
                { EquipmentSlot.FaceCover, new List<Item>() },
                { EquipmentSlot.Headwear, new List<Item>() },
            };

        protected static ManualLogSource Logger
        {
            get; private set;
        }

        private ArmorRegenComponent()
        {
            if (Logger == null)
            {
                Logger = BepInEx.Logging.Logger.CreateLogSource(nameof(ArmorRegenComponent));
            }
        }

        internal static void Enable()
        {
            if (Singleton<GameWorld>.Instantiated)
            {
                var gameWorld = Singleton<GameWorld>.Instance;
                gameWorld.GetOrAddComponent<ArmorRegenComponent>();

                Logger.LogDebug("ASS: Attaching events");
            }
        }

        private void Start()
        {
            player = Singleton<GameWorld>.Instance.MainPlayer;

            player.OnPlayerDeadOrUnspawn += Player_OnPlayerDeadOrUnspawn;
            player.BeingHitAction += Player_BeingHitAction;
        }


        private void Update()
        {
            if (ASS.Plugin.ArmorServiceMode.Value)
            {
                timeSinceLastHit += Time.unscaledDeltaTime;
                if (timeSinceLastHit >= Plugin.TimeDelayRepairInSec.Value)
                {
                    if (!isRegenerating)
                    {
                        isRegenerating = true;
                        StartCoroutine(RepairArmor());
                    }
                }
            }
        }


        private IEnumerator RepairArmor()
        {
            //if the time since we were last hit exceeds TimeDelayRepairInSec.Value then repair all armor
            while (isRegenerating && Plugin.ArmorServiceMode.Value)
            {
                //Logger.LogInfo($"Repairing Armor Block Reached Because TimeSinceLastHitReached: " + timeSinceLastHit);
                //repair the armor divided by the time.unfixed rate
                newRepairRate = Plugin.ArmorRepairRateOverTime.Value * Time.deltaTime;

                foreach (EquipmentSlot slot in equipmentSlotDictionary.Keys.ToArray())
                {
                    //Logger.LogInfo("ASS: Checking EquipmentSlot: " +  slot);
                    Slot tempSlot = getEquipSlot(slot);

                    if (tempSlot == null || tempSlot.ContainedItem == null)
                    {
                        continue;
                    }

                    foreach (var item in tempSlot.ContainedItem.GetAllItems())
                    {
                        //get the armorcomponent of each item in items and check to see if all item componenets (even helmet side ears) are max durability
                        armor = item.GetItemComponent<ArmorComponent>();

                        //check if it needs repair for the current item in loop of all items for the slot
                        if (
                            armor != null
                            && (armor.Repairable.Durability < armor.Repairable.MaxDurability)
                        )
                        {
                            //increase armor durability by newRepairRate until maximum then set as maximum durability
                            if (
                                armor.Repairable.Durability + newRepairRate
                                >= armor.Repairable.MaxDurability
                            )
                            {
                                armor.Repairable.Durability = armor.Repairable.MaxDurability;
                                //Logger.LogInfo("ASS: Setting MaxDurability for " + item.LocalizedName());
                            }
                            else
                            {
                                armor.Repairable.Durability += newRepairRate;
                                Logger.LogInfo("ASS: Repairing " + item.LocalizedName() + " : " + armor.Repairable.Durability + "/" + armor.Repairable.MaxDurability);
                            }
                        }
                    }
                }

                // Wait for the next frame before continuing
                yield return null;
            }

        }

        private void Player_BeingHitAction(DamageInfo arg1, EBodyPart arg2, float arg3)
        {
            timeSinceLastHit = 0f;
            isRegenerating = false;
            StopCoroutine(RepairArmor());
        }

        private void Player_OnPlayerDeadOrUnspawn(Player player)
        {
            Disable();
        }

        private void Disable()
        {
            if (player != null)
            {
                player.OnPlayerDeadOrUnspawn -= Player_OnPlayerDeadOrUnspawn;
                player.BeingHitAction -= Player_BeingHitAction;
            }
        }

        private Slot getEquipSlot(EquipmentSlot slot)
        {
            var player = Singleton<GameWorld>.Instance.MainPlayer;

            // Use AccessTools to get the protected field _inventoryController
            inventoryController = (InventoryControllerClass)
                AccessTools.Field(typeof(Player), "_inventoryController").GetValue(player);

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

        
    

