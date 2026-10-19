using System;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace BackpackInInventory
{
    /// <summary>
    /// Shows the worn backpack's storage slots under the inventory slots, as player slots. The contents stay in the
    /// backpack's own <see cref="Storage"/>; only where its slots are drawn changes. The storage is never entered
    /// (<see cref="Storage.EnterExternally"/> would close an open container or station).
    /// </summary>
    [HarmonyPatch]
    internal static class BackpackSlots
    {
        /// <summary>Items with this category can't go into the backpack's slots (an empty backpack would vanish into itself).</summary>
        private const string BackpackCategory = "Backpack";

        private static Storage shown;
        private static bool syncPending;
        private static bool emptyStateDirty;

        internal static Storage Shown => shown;

        internal static void RequestSync() => syncPending = true;

        /// <summary>Called every frame by the plugin.</summary>
        internal static void Tick()
        {
            if (syncPending)
            {
                syncPending = false;
                Sync();
            }
            if (emptyStateDirty)
            {
                emptyStateDirty = false;
                // Swaps the item in the backpack slot between its empty and full variant. The storage's own update
                // relay only runs while it is the open container, which ours never is.
                BackpackStorage.instance?.UpdateEmptyState();
            }
        }

        // Equip, take off, swap (the callbacks of the backpack character slot). The slot may still hold the old item
        // while they run, so the sync waits for the next frame.
        [HarmonyPostfix]
        [HarmonyPatch(typeof(BackpackStorage), nameof(BackpackStorage.OnDropped))]
        [HarmonyPatch(typeof(BackpackStorage), nameof(BackpackStorage.OnTaken))]
        [HarmonyPatch(typeof(BackpackStorage), nameof(BackpackStorage.OnReturn))]
        private static void BackpackSlotChanged() => syncPending = true;

        // Closing the menu destroys the slots (ExitExternally on every backpack storage). They are captured again right
        // away, so code that reads the slot UI while the menu is closed (auto-consume, depression) still sees the backpack.
        [HarmonyPostfix]
        [HarmonyPatch(typeof(BackpackStorage), nameof(BackpackStorage.ExitStorages))]
        private static void StoragesExited() => syncPending = true;

        // Every time the game menu shows a panel. Closing the menu destroys the slots (ResetBackpack → ExitStorages →
        // ExitExternally on every backpack storage), so they are captured again here.
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameUIController), nameof(GameUIController.OpenPanel))]
        private static void PanelOpened()
        {
            try
            {
                Sync();
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Showing the backpack slots failed: {e}");
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Slot), nameof(Slot.SetColorToNormal))]
        private static void TintBackpackSlot(Slot __instance)
        {
            if (Plugin.On && !__instance.selected && !__instance.noDrop && __instance.slotImage != null &&
                InventoryLayout.IsBackpackSlot(__instance))
                __instance.slotImage.color = Plugin.Tint;
        }

        private static bool GameReady =>
            Inventory.instance != null && Inventory.instance.inventoryPanelUI != null &&
            Inventory.instance.CharacterSlots != null && Inventory.instance.CharacterSlots.Length > Inventory.backpack &&
            BackpackStorage.instance != null;

        /// <summary>Brings the inventory screen in line with the setting and the worn backpack.</summary>
        internal static void Sync()
        {
            if (!GameReady)
                return;
            bool on = Plugin.On;
            VanillaBackpack.Apply(on);

            Storage wanted = on ? BackpackStorage.instance.GetStorage() : null;
            if (wanted != null && wanted.Slots == null)
            {
                wanted = null; // not started yet
                syncPending = true;
            }
            if (shown != null && shown != wanted)
                Release(shown);
            shown = null;

            if (!on)
            {
                SortButton.Show(false);
                InventoryLayout.Remove();
                return;
            }
            if (!InventoryLayout.Install())
                return;
            if (wanted != null)
            {
                Capture(wanted);
                shown = wanted;
            }
            SortButton.Show(shown != null);
            InventoryLayout.Relayout();
        }

        internal static void Shutdown()
        {
            if (shown != null)
                Release(shown);
            shown = null;
            SortButton.Destroy();
            InventoryLayout.Remove();
            if (GameReady)
                VanillaBackpack.Apply(false);
        }

        /// <summary>Re-applies the slot colour.</summary>
        internal static void Repaint()
        {
            if (shown == null)
                return;
            foreach (SlotController controller in shown.Slots)
                if (controller?.slot != null)
                    controller.slot.SetColorToNormal();
        }

        private static void Capture(Storage storage)
        {
            Transform grid = InventoryLayout.BackpackGrid;
            string[] forbidden = storage.ForbiddenCategories.Contains(BackpackCategory)
                ? storage.ForbiddenCategories
                : storage.ForbiddenCategories.Concat(new[] { BackpackCategory }).ToArray();
            foreach (SlotController controller in storage.Slots)
            {
                if (controller == null)
                    continue;
                // What Storage.GetStorage does when the storage is opened, plus the extra forbidden category.
                controller.SetAllowedCategories(storage.AllowedCategories);
                controller.SetForbiddenCategories(forbidden);
                controller.OnDropped = (i, a, o, m) => Changed(storage);
                controller.OnTaken = (i, a, o, m) => Changed(storage);
                controller.OnReturn = (i, a, o, m) => Changed(storage);
                // Re-capturing a slot that is already shown rebuilds its item icon, and destroying any item icon closes
                // the item info panel. Syncs also run while the player uses an item (Inventory.ForceCharacterItemsUpdate re-fires the backpack
                // slot's OnDropped), so only slots that are gone or elsewhere are captured again.
                bool shownHere = controller.slot != null && controller.slot.transform.parent == grid &&
                                 controller.slot.slotcontrol == controller;
                if (!shownHere)
                {
                    controller.CaptureSlot(grid);
                    if (controller.slot.transform.parent != grid)
                        controller.slot.transform.SetParent(grid, false);
                    controller.slot.SetColorToNormal();
                }
                controller.slot.playerSlot = true;
            }
        }

        private static void Release(Storage storage)
        {
            if (storage.Slots == null)
                return;
            foreach (SlotController controller in storage.Slots)
            {
                if (controller == null)
                    continue;
                // Back to what Storage.StorageLeave leaves behind; the vanilla callbacks are set again on the next open.
                controller.ResetAllowedCategories();
                controller.ResetForbiddenCategories();
                controller.OnDropped = storage.OnDroppedToSlot;
                controller.OnTaken = storage.OnTakenFromSlot;
                controller.OnReturn = storage.OnReturnToSlot;
                controller.RelaseSlot(destroy: true);
                controller.slot = null;
            }
        }

        internal static void MarkChanged(Storage storage) => Changed(storage);

        private static void Changed(Storage storage)
        {
            storage.storageChanged = true;
            emptyStateDirty = true;
        }
    }
}
