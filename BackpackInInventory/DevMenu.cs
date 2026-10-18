using System.ComponentModel;
using System.Text;

namespace BackpackInInventory
{
    /// <summary>
    /// Buttons for the dev-only DevMenu tool, which finds this class by name. Nothing here runs without it,
    /// and neither mod references the other.
    /// </summary>
    internal static class DevMenu
    {
        public static string Status()
        {
            Storage shown = BackpackSlots.Shown;
            string worn = shown == null ? "none shown" : $"{shown.name}: {shown.Slots?.Length ?? 0} slots";
            return $"Mod: {(Plugin.On ? "ON" : "OFF")}   Backpack: {worn}\n{InventoryLayout.Describe()}";
        }

        [Description("Mod on / off")]
        public static void ToggleMod() => Plugin.Enabled.Value = !Plugin.Enabled.Value;

        [Description("Log the three backpack storages (slots, categories, cooling) and the layout")]
        public static void DumpBackpacks()
        {
            BackpackStorage backpack = BackpackStorage.instance;
            if (backpack == null)
            {
                Plugin.Log.LogInfo("[Dump] no BackpackStorage yet");
                return;
            }
            var text = new StringBuilder("[Dump] backpack storages\n");
            Describe(text, "Backpack", backpack.backpackStorage);
            Describe(text, "Rucksack", backpack.rucksackStorage);
            Describe(text, "Cooler Bag", backpack.coolerBagStorage);
            text.Append("Worn: ").Append(backpack.GetStorage()?.name ?? "none").Append(", equipped flag ").Append(backpack.Equipped).Append('\n');
            text.Append(InventoryLayout.Describe());
            Plugin.Log.LogInfo(text.ToString());
        }

        private static void Describe(StringBuilder text, string label, Storage storage)
        {
            if (storage == null)
            {
                text.Append(label).Append(": null\n");
                return;
            }
            int used = 0;
            if (storage.Slots != null)
                foreach (SlotController slot in storage.Slots)
                    if (slot != null && !slot.IsEmpty())
                        used++;
            text.Append(label).Append(": '").Append(storage.name).Append("' slots ").Append(storage.storageSlots?.Length ?? -1)
                .Append(" (controllers ").Append(storage.Slots?.Length ?? -1).Append(", used ").Append(used).Append(')')
                .Append(", settings '").Append(storage.StorageSettings).Append('\'')
                .Append(", allowed [").Append(string.Join(", ", storage.AllowedCategories ?? new string[0])).Append(']')
                .Append(", forbidden [").Append(string.Join(", ", storage.ForbiddenCategories ?? new string[0])).Append(']')
                .Append(", refrigeration ").Append(storage.RefigerationFactor)
                .Append(", disableSpoil ").Append(storage.disableSpoil)
                .Append(", maxStack ").Append(storage.maxSlotStackSize)
                .Append('\n');
        }
    }
}
