using System;
using HarmonyLib;
using OS.Items;

namespace BackpackInInventory
{
    /// <summary>
    /// Quick move (shift-click, the Move button) with no container or station open: main slots → backpack, backpack → main
    /// slots. Vanilla has no target then and the move does nothing. Equipable items keep the vanilla behaviour (they get
    /// equipped). With a container or station open nothing changes: backpack slots are player slots, so they already move there.
    /// </summary>
    [HarmonyPatch]
    internal static class QuickMove
    {
        // Both ItemData.QuickMove and AmountSliderUI (Move with an amount) go through this overload.
        [HarmonyPrefix]
        [HarmonyPatch(typeof(SlotController), nameof(SlotController.MoveToOtherInventory),
            new[] { typeof(int), typeof(int) }, new[] { ArgumentType.Out, ArgumentType.Normal })]
        private static bool MoveToOtherInventory(SlotController __instance, ref int movedAmount, int amount, ref bool __result)
        {
            Storage backpack = BackpackSlots.Shown;
            Inventory inventory = Inventory.instance;
            if (!Plugin.On || backpack?.Slots == null || inventory == null || inventory.ForeignSlots != null)
                return true;
            ItemStack stack = __instance.itemStack;
            Item item = stack?.itemReference?.Item;
            if (item == null || stack.itemId == -1 || stack.itemAmount <= 0 || IsEquipable(item))
                return true;

            SlotController[] targets;
            if (Array.IndexOf(inventory.Slots, __instance) >= 0)
                targets = backpack.Slots;
            else if (Array.IndexOf(backpack.Slots, __instance) >= 0)
                targets = inventory.Slots;
            else
                return true; // character slots

            amount = Math.Min(amount, stack.itemAmount);
            // Straight into the other slots, as SlotController.MoveToOtherInventory does but without
            // ItemOperations.AddItemAndReturnRemaining: the item stays in the inventory, so no money, item triggers or
            // stolen-item checks, and no overflow back into the slots it came from.
            int remaining = amount;
            foreach (SlotController target in targets)
            {
                if (target == null || target == __instance)
                    continue;
                remaining = target.AddItemAndReturnRemaining(item, remaining, stack.ownerId, stack.Meta, stackAmount: stack.itemAmount);
                if (remaining <= 0)
                    break;
            }

            movedAmount = amount - remaining;
            __result = true;
            if (movedAmount <= 0)
            {
                // The storage panel is hidden with nothing open, so both directions flash the inventory.
                inventory.inventoryPanelUI.InventoryFull();
                return false;
            }
            if (targets == inventory.Slots)
                BackpackSlots.MarkChanged(backpack); // a partial move changes the amount without OnTaken
            stack.itemAmount -= movedAmount;
            if (stack.itemAmount <= 0)
                __instance.RemoveItem();
            else
                __instance.slot?.UpdateSlot();
            return false;
        }

        private static bool IsEquipable(Item item)
        {
            if (item.Actions == null)
                return false;
            foreach (Item.ItemAction action in item.Actions)
                if (action is ItemEquipable)
                    return true;
            return false;
        }
    }
}
