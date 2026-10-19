using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BackpackInInventory
{
    /// <summary>
    /// The game's inventory navigation looks for the first slot among everything under the pointer, even under a panel or
    /// a button. Slots scrolled out of our viewport are hidden by the mask but still take raycasts, and they lie under the
    /// item info panel: pressing Use/Eat deselected the item (closing the info) and could make a hidden slot the current one.
    /// Both lookups skip the hidden slots.
    /// </summary>
    [HarmonyPatch]
    internal static class HiddenSlotClicks
    {
        // Mouse button down: deselects when the slot under the pointer isn't the selected one.
        [HarmonyPrefix]
        [HarmonyPatch(typeof(InventoryNavigationHandler), nameof(InventoryNavigationHandler.DeselectSlotIfNecessary))]
        private static bool DeselectSlotIfNecessary(InventoryNavigationHandler __instance)
        {
            if (!Plugin.On)
                return true;
            Slot slot = FirstVisibleSlot(__instance.GetRaycastResults());
            if (slot != null && slot.isActiveAndEnabled && !slot.GetSelectedStatus())
                __instance.UndoSelection();
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(InventoryNavigationHandler), nameof(InventoryNavigationHandler.GetSlotUnderMouse))]
        private static bool GetSlotUnderMouse(InventoryNavigationHandler __instance, ref Slot __result)
        {
            if (!Plugin.On)
                return true;
            __result = FirstVisibleSlot(__instance.GetRaycastResults());
            return false;
        }

        private static Slot FirstVisibleSlot(List<RaycastResult> hits)
        {
            Vector2 pointer = Input.mousePosition;
            foreach (RaycastResult hit in hits)
            {
                Slot slot = hit.gameObject != null ? hit.gameObject.GetComponent<Slot>() : null;
                if (slot != null && !InventoryLayout.IsHiddenSlot(slot, pointer))
                    return slot;
            }
            return null;
        }
    }
}
