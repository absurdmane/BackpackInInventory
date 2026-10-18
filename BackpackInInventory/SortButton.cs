using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UiButton = UnityEngine.UI.Button; // the game has its own global Button

namespace BackpackInInventory
{
    /// <summary>
    /// A small copy of the inventory's Sort button in the top right corner of the backpack slot, shown while a backpack
    /// is worn. Sorts the backpack's own slots; items stay in the backpack.
    /// </summary>
    internal static class SortButton
    {
        private const string VanillaSortMethod = nameof(InventoryPanelUI.SortInventory);

        private static GameObject button;
        private static bool notFound;

        /// <summary>Shows or hides the button; created on first show.</summary>
        internal static void Show(bool visible)
        {
            // Loading a save rebuilds the character slots, destroying the button with the old backpack slot.
            Slot backpackSlot = Inventory.instance.CharacterSlots[Inventory.backpack].slot;
            if (button != null && (backpackSlot == null || button.transform.parent != backpackSlot.transform))
                Destroy();
            if (button == null && visible)
                Create(backpackSlot);
            if (button != null && button.activeSelf != visible)
                button.SetActive(visible);
        }

        internal static void Destroy()
        {
            if (button != null)
                Object.Destroy(button);
            button = null;
        }

        private static void Create(Slot backpackSlot)
        {
            if (notFound || backpackSlot == null)
                return;
            UiButton vanilla = FindVanilla(Inventory.instance.inventoryPanelUI);
            if (vanilla == null)
            {
                notFound = true; // don't search on every sync
                Plugin.Log.LogWarning("The inventory's Sort button was not found; the backpack has no Sort button.");
                return;
            }

            button = Object.Instantiate(vanilla.gameObject, backpackSlot.transform, false);
            button.name = "BackpackInInventory Sort";
            var rect = (RectTransform)button.transform;
            rect.anchorMin = Vector2.one;
            rect.anchorMax = Vector2.one;
            rect.pivot = Vector2.one;
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = new Vector3(Plugin.SortButtonScale.Value, Plugin.SortButtonScale.Value, 1f);
            rect.SetAsLastSibling();
            LayoutElement layout = button.GetComponent<LayoutElement>();
            if (layout != null)
                layout.ignoreLayout = true;

            UiButton copy = button.GetComponent<UiButton>();
            copy.onClick = new UiButton.ButtonClickedEvent(); // drops the persistent SortInventory call
            copy.onClick.AddListener(new UnityAction(SortBackpack));
            copy.navigation = new Navigation { mode = Navigation.Mode.None };
        }

        internal static void Rescale()
        {
            if (button != null)
                button.transform.localScale = new Vector3(Plugin.SortButtonScale.Value, Plugin.SortButtonScale.Value, 1f);
        }

        private static UiButton FindVanilla(InventoryPanelUI panel)
        {
            foreach (UiButton candidate in panel.GetComponentsInChildren<UiButton>(true))
            {
                for (int i = 0; i < candidate.onClick.GetPersistentEventCount(); i++)
                    if (candidate.onClick.GetPersistentMethodName(i) == VanillaSortMethod)
                        return candidate;
            }
            return null;
        }

        private static void SortBackpack()
        {
            Storage storage = BackpackSlots.Shown;
            if (storage == null || storage.Slots == null)
                return;
            InventoryPanelUI panel = Inventory.instance.inventoryPanelUI;
            Inventory.instance.SortSlots(storage.Slots);
            MiscAudioPlayer.PlayRandomSoundEverywhere(panel.sortSound, panel.gameObject);
        }
    }
}
