﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Configuration;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using UnityEngine;

namespace armorMod
{
    [BepInPlugin("com.armorMod.ASS", "armorMod.ASS", "1.0.0")]

    public class ASS : BaseUnityPlugin
    {
        private ConfigEntry<Boolean> ArmorServiceMode
        {
            get; set;
        }
        private static ConfigEntry<float> TimeDelayRepairInSec
        {
            get; set;
        }
        private static ConfigEntry<float> ArmorRepairRateOverTime
        {
            get; set;
        }

        private AbstractGame game;
        private bool runOnceAlready = false;
        private bool newGame = true;
        private float newRepairRate;
        private ArmorComponent armor;
        private static float timeSinceLastHit = 0f;

        private readonly Dictionary<EquipmentSlot, List<Item>> equipmentSlotDictionary = new Dictionary<EquipmentSlot, List<Item>>
        {
            { EquipmentSlot.ArmorVest, new List<Item>() },
            { EquipmentSlot.TacticalVest, new List<Item>() },
            { EquipmentSlot.Eyewear, new List<Item>() },
            { EquipmentSlot.FaceCover, new List<Item>() },
            { EquipmentSlot.Headwear, new List<Item>() }
        };

        internal void Awake()
        {
            ArmorServiceMode = Config.Bind("Armor Repair Settings", "Enable/Disable Mod", true, "Enables the Armor Repairing Options Below");
            TimeDelayRepairInSec = Config.Bind("Armor Repair Settings", "Time Delay Repair in Sec", 60f, "How Long Before you were last hit that it repairs armor");
            ArmorRepairRateOverTime = Config.Bind("Armor Repair Settings", "Armor Repair Rate", 0.5f, "How much durability per second is repaired");
        }
        private void Update()
        {
            try
            {
                game = Singleton<AbstractGame>.Instance;

                if (game.InRaid && Camera.main.transform.position != null && newGame && ArmorServiceMode.Value)
                {
                    var player = Singleton<GameWorld>.Instance.MainPlayer;
                    timeSinceLastHit += Time.deltaTime;

                    if (!runOnceAlready && game.Status == GameStatus.Started)
                    {
                        Logger.LogDebug("ASS: Attaching events");
                        player.BeingHitAction += Player_BeingHitAction;
                        player.OnPlayerDeadOrUnspawn += Player_OnPlayerDeadOrUnspawn;
                        runOnceAlready = true;
                    }

                    RepairArmor();
                }
            }
            catch { }
        }

        private void RepairArmor()
        {
            //if the time since we were last hit exceeds TimeDelayRepairInSec.Value then repair all armor
            if (timeSinceLastHit >= TimeDelayRepairInSec.Value)
            {
                //Logger.LogInfo($"Repairing Armor Block Reached Because TimeSinceLastHitReached: " + timeSinceLastHit);
                //repair the armor divided by the time.unfixed rate
                newRepairRate = ArmorRepairRateOverTime.Value * Time.deltaTime;

                foreach (EquipmentSlot slot in equipmentSlotDictionary.Keys.ToArray())
                {
                    Logger.LogInfo("ASS: Checking EquipmentSlot: " +  slot);
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
                        if (armor != null && (armor.Repairable.Durability < armor.Repairable.MaxDurability))
                        {
                            //increase armor durability by newRepairRate until maximum then set as maximum durability
                            if (armor.Repairable.Durability + newRepairRate >= armor.Repairable.MaxDurability)
                            {
                                armor.Repairable.Durability = armor.Repairable.MaxDurability;
                                //Logger.LogInfo("ASS: Setting MaxDurability for " + item.LocalizedName());
                            }
                            else
                            {
                                armor.Repairable.Durability += newRepairRate;
                                //Logger.LogInfo("ASS: Repairing " + item.LocalizedName() + " : " + armor.Repairable.Durability + "/" + armor.Repairable.MaxDurability);
                            }
                        }

                    }

                }
            }
        }

        private void Player_BeingHitAction(DamageInfo dmgInfo, EBodyPart bodyPart, float hitEffectId) => timeSinceLastHit = 0f;

        private void Player_OnPlayerDeadOrUnspawn(Player player)
        {
            Logger.LogDebug("ASS: Undo all events");
            player.BeingHitAction -= Player_BeingHitAction;
            player.OnPlayerDeadOrUnspawn -= Player_OnPlayerDeadOrUnspawn;
            runOnceAlready = false;
            newGame = false;

            Task.Delay(TimeSpan.FromSeconds(15)).ContinueWith(_ =>
            {
                // Set newGame = true after the timer is finished so it doesn't execute the events right away
                newGame = true;
            });
        }

        private Slot slotContents;
        private InventoryControllerClass inventoryController;
        private Slot getEquipSlot(EquipmentSlot slot)
        {
            var player = Singleton<GameWorld>.Instance.MainPlayer;

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
