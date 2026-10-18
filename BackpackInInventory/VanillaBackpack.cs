using HarmonyLib;

namespace BackpackInInventory
{
    /// <summary>
    /// The vanilla backpack window is off while the mod is on: the "OPEN" tab, key 3 and opening on equip do nothing,
    /// and the backpack slot loses its "3" badge.
    /// </summary>
    [HarmonyPatch]
    internal static class VanillaBackpack
    {
        // Key 3 and the OPEN tab go through ToggleBackpack; equipping with the inventory open calls OpenBackpack.
        [HarmonyPrefix]
        [HarmonyPatch(typeof(BackpackStorage), nameof(BackpackStorage.ToggleBackpack))]
        [HarmonyPatch(typeof(BackpackStorage), nameof(BackpackStorage.OpenBackpack))]
        private static bool BlockOpen() => !Plugin.On;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(BackpackButton), nameof(BackpackButton.ChangeButtonStatus))]
        private static void HideOpenTab(BackpackButton __instance)
        {
            if (Plugin.On && __instance.buttonEnabled != null)
                __instance.buttonEnabled.SetActive(false);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Inventory), nameof(Inventory.StartInit))]
        private static void AfterInventoryInit() => BackpackSlots.RequestSync();

        /// <summary>Brings the tab and the badge in line with the Enabled setting.</summary>
        internal static void Apply(bool on)
        {
            BackpackStorage backpack = BackpackStorage.instance;
            if (backpack == null)
                return;
            if (on && backpack.BackpackIsOpen())
                backpack.CloseBackpack();
            if (backpack.backpackButton != null && backpack.backpackButton.buttonEnabled != null)
                backpack.backpackButton.buttonEnabled.SetActive(!on && backpack.Equipped);

            Slot slot = BackpackSlot();
            if (slot == null)
                return;
            if (on)
                slot.HideHotKeyNumber();
            else if (!slot.isHotKeySlot)
                slot.SetHotKeyNumber("3");
        }

        private static Slot BackpackSlot()
        {
            SlotController[] slots = Inventory.instance != null ? Inventory.instance.CharacterSlots : null;
            if (slots == null || slots.Length <= Inventory.backpack || slots[Inventory.backpack] == null)
                return null;
            return slots[Inventory.backpack].slot;
        }
    }
}
