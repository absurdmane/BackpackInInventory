using System;
using System.Linq;
using HarmonyLib;

namespace BackpackInInventory
{
    /// <summary>
    /// An item dropped on the worn backpack (the backpack character slot) goes into the backpack's slots: merged into a
    /// matching stack first, then free slots; what doesn't fit goes back. Backpacks themselves keep the vanilla swap.
    /// </summary>
    [HarmonyPatch]
    internal static class DropOnBackpack
    {
        private const string BackpackCategory = "Backpack";

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Slot), nameof(Slot.OnDrop), typeof(ItemData))]
        private static bool OnDrop(Slot __instance, ItemData itemData)
        {
            if (BlocksSwap(__instance, itemData))
            {
                itemData.Return(0);
                TellEmptyFirst();
                return false;
            }
            SlotController[] slots = Target(__instance, itemData);
            if (slots == null)
                return true;

            // As Slot.OnDrop does for a normal slot, with the first fitting backpack slot as the target; otherStorage
            // makes DropItem spill the rest into the other backpack slots (its siblings).
            int amount = itemData.amount;
            SlotController target = FirstFitting(slots, itemData);
            if (!target.DropItem(itemData, out int remaining, isSibling: false, allowItemSwap: false, otherStorage: true))
                itemData.Return(amount - remaining);
            itemData.item.PlaySound();
            if (remaining >= amount)
                Inventory.instance.inventoryPanelUI.InventoryFull();
            return false;
        }

        // Green instead of red while dragging over the backpack slot when the item can go in; red for a backpack that
        // can't replace the worn one.
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Slot), nameof(Slot.CheckIfAccepted))]
        private static void CheckIfAccepted(Slot __instance, ItemData itm, ref bool __result)
        {
            if (__result && BlocksSwap(__instance, itm))
                __result = false;
            else if (!__result && Target(__instance, itm) != null)
                __result = true;
        }

        // "Equip" on a backpack item (right click / quick move) swaps the same way.
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Inventory), nameof(Inventory.EquipBackpack))]
        private static bool EquipBackpack()
        {
            if (!Plugin.On || WornIsEmpty())
                return true;
            TellEmptyFirst();
            return false;
        }

        /// <summary>
        /// Another backpack can't replace a worn one that still holds items: vanilla puts the old one back into the
        /// dragged item's slot, where a full backpack isn't allowed, so it dropped on the ground.
        /// </summary>
        private static bool BlocksSwap(Slot slot, ItemData itemData)
        {
            if (!Plugin.On || itemData == null || itemData.item == null || !itemData.isDragging)
                return false;
            SlotController[] character = Inventory.instance != null ? Inventory.instance.CharacterSlots : null;
            if (character == null || character.Length <= Inventory.backpack || character[Inventory.backpack].slot != slot)
                return false;
            if (itemData.slotController == character[Inventory.backpack])
                return false; // the worn backpack itself
            return itemData.item.Categories != null && itemData.item.Categories.Contains(BackpackCategory) && !WornIsEmpty();
        }

        private static bool WornIsEmpty()
        {
            Storage storage = BackpackStorage.instance != null ? BackpackStorage.instance.GetStorage() : null;
            return storage == null || storage.Slots == null || storage.IsStorageEmpty();
        }

        private static void TellEmptyFirst()
        {
            Inventory.instance.inventoryPanelUI.InventoryFull();
            Notifications.instance?.CreateNotification("Backpack", "Empty the worn backpack before switching to another one");
        }

        /// <summary>The backpack's slots when this drop is ours to handle, otherwise null (vanilla).</summary>
        private static SlotController[] Target(Slot slot, ItemData itemData)
        {
            if (!Plugin.On || itemData == null || itemData.item == null || !itemData.isDragging || slot.slotDisabled || slot.noDrop)
                return null;
            SlotController[] character = Inventory.instance != null ? Inventory.instance.CharacterSlots : null;
            if (character == null || character.Length <= Inventory.backpack || character[Inventory.backpack].slot != slot)
                return null;
            SlotController[] slots = BackpackStorage.instance != null ? BackpackStorage.instance.GetStorage()?.Slots : null;
            if (slots == null || slots.Length == 0)
                return null;
            if (itemData.item.Categories != null && itemData.item.Categories.Contains(BackpackCategory))
                return null; // swapping backpacks stays vanilla
            if (Array.IndexOf(slots, itemData.slotController) >= 0)
                return null; // already in the backpack
            // During trade only the player's own items; the trader's go through buying.
            if (TradePanel.instance != null && TradePanel.instance.activeTrade != null &&
                (itemData.slotController?.slot == null || !itemData.slotController.slot.playerSlot))
                return null;
            return slots;
        }

        private static SlotController FirstFitting(SlotController[] slots, ItemData itemData)
        {
            SlotController empty = null;
            foreach (SlotController controller in slots)
            {
                if (controller == null)
                    continue;
                if (!controller.SlotHasSpaceForItem(itemData.item, out int left, itemData.owner, itemData.amount, 0, itemData.meta) &&
                    left >= itemData.amount)
                    continue;
                if (!controller.itemStack.IsEmpty())
                    return controller; // a stack it merges into
                if (empty == null)
                    empty = controller;
            }
            return empty ?? slots[0];
        }
    }
}
