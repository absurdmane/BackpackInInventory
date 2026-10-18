using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace BackpackInInventory
{
    /// <summary>
    /// The worn backpack counts as part of the inventory wherever the game looks only at the main slots or skips the
    /// backpack on purpose: money and item triggers on add, room checks of pickups and item givers, the clock, auto-consume,
    /// depression, "inventory is empty". What already counts the backpack (removal, lookups, quests, ammo, theft) is left alone.
    /// </summary>
    [HarmonyPatch]
    internal static class InventoryExtension
    {
        private static SlotController[] BackpackSlots()
        {
            if (!Plugin.On || BackpackStorage.instance == null)
                return null;
            return BackpackStorage.instance.GetStorage()?.Slots;
        }

        // Money items become money, item triggers and stolen-item checks run, as for the main slots.
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Inventory), nameof(Inventory.InPlayerSlot))]
        private static void InPlayerSlot(SlotController slotid, ref bool __result)
        {
            if (__result || slotid == null)
                return;
            SlotController[] slots = BackpackSlots();
            if (slots != null && Array.IndexOf(slots, slotid) >= 0)
                __result = true;
        }

        // Callers asking for the slots without the backpack: pickup and item-giver room checks ("inventory full" while
        // the backpack has room) and the clock check.
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Inventory), nameof(Inventory.AllSlots))]
        private static void AllSlots(ref bool ignoreBackpack)
        {
            if (Plugin.On)
                ignoreBackpack = false;
        }

        // Pickups add with ignoreBackpack, so a full main inventory refused them even with room in the backpack.
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ItemOperations), nameof(ItemOperations.AddItemAndReturnRemaining))]
        private static void AddItemAndReturnRemaining(ItemOperations.ItemOperationSettings settings)
        {
            if (Plugin.On && settings != null)
                settings.ignoreBackpack = false;
        }

        // Item givers (and anything else adding through a main slot with "drop the rest") filled only the main slots and
        // dropped the rest on the ground. The rest now goes into the backpack first; only what still doesn't fit drops.
        [HarmonyPrefix]
        [HarmonyPatch(typeof(SlotController), nameof(SlotController.AddItemAndReturnRemaining))]
        private static void HoldDrop(SlotController __instance, ref bool dropItems, out bool __state)
        {
            __state = dropItems && Plugin.On && Inventory.instance != null && __instance.Siblings == Inventory.instance.Slots &&
                      BackpackSlots() != null;
            if (__state)
                dropItems = false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(SlotController), nameof(SlotController.AddItemAndReturnRemaining))]
        private static void OverflowToBackpack(Item item, int owner, object[] meta, GameObject dropper,
            ItemOperations.DropItemValues dropItemValues, ItemOperations.ItemOperationSettings settings, bool __state, ref int __result)
        {
            if (!__state || __result <= 0)
                return;
            int remaining = __result;
            foreach (SlotController controller in BackpackSlots() ?? new SlotController[0])
            {
                if (controller == null)
                    continue;
                remaining = controller.AddItemAndReturnRemaining(item, remaining, owner, meta, dropItems: false, null, notify: false,
                    0, null, settings);
                if (remaining <= 0)
                    break;
            }
            if (remaining > 0)
                ItemOperations.DropItem(item, remaining, owner, meta, dropper, dropItemValues);
            __result = 0; // as the game does after dropping
        }

        // Auto-consume and depression (alcohol, mushrooms) pick from these items.
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Inventory), nameof(Inventory.GetInventoryItemData))]
        private static void GetInventoryItemData(List<ItemData> __result)
        {
            SlotController[] slots = BackpackSlots();
            if (slots == null || __result == null)
                return;
            foreach (SlotController controller in slots)
            {
                // The item data lives on the slot UI; the slots are captured again right after the menu closes.
                ItemData data = controller?.slot != null ? controller.slot.GetItemItemData() : null;
                if (data != null && data.amount > 0)
                    __result.Add(data);
            }
        }

        // Building menu and sleep robbery checks.
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Inventory), nameof(Inventory.IsInventoryEmpty))]
        private static void IsInventoryEmpty(ref bool __result)
        {
            if (!__result)
                return;
            SlotController[] slots = BackpackSlots();
            if (slots == null)
                return;
            foreach (SlotController controller in slots)
            {
                if (controller != null && controller.itemStack.itemId != -1)
                {
                    __result = false;
                    return;
                }
            }
        }
    }
}
