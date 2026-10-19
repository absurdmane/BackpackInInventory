using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.Mono;
using HarmonyLib;
using UnityEngine;

namespace BackpackInInventory
{
    [BepInPlugin(Guid, Name, Version)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string Guid = "obenseuer.absurdmane.backpackininventory";
        public const string Name = "Backpack In Inventory";
        public const string Version = "1.0.0";

        internal static ManualLogSource Log;

        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<string> SlotColor;
        internal static ConfigEntry<float> SortButtonScale;

        internal static bool On => Enabled != null && Enabled.Value;

        /// <summary>Background of the backpack's slots, parsed from <see cref="SlotColor"/>.</summary>
        internal static Color Tint { get; private set; } = DefaultTint;

        private static readonly Color DefaultTint = new Color32(205, 220, 255, 255);

        private Harmony harmony;

        private void Awake()
        {
            Log = Logger;
            Enabled = Config.Bind("General", "Enabled", true,
                "Show the worn backpack's slots under the inventory slots; the separate backpack window can't be opened. " +
                "Off = vanilla backpack. Works without a restart.");
            SlotColor = Config.Bind("General", "SlotColor", "#CDDCFF",
                "Background colour of the backpack's slots (#RRGGBB). #FFFFFF = same as the inventory slots.");
            SortButtonScale = Config.Bind("General", "SortButtonScale", 0.75f,
                new ConfigDescription("Size of the backpack's Sort button (on the backpack slot) relative to the inventory's Sort button.",
                    new AcceptableValueRange<float>(0.1f, 1f)));
            SortButtonScale.SettingChanged += (_, __) => SortButton.Rescale();
            ParseTint();
            Enabled.SettingChanged += (_, __) => BackpackSlots.RequestSync();
            SlotColor.SettingChanged += (_, __) =>
            {
                ParseTint();
                BackpackSlots.Repaint();
            };

            harmony = new Harmony(Guid);
            foreach (Type patches in new[] { typeof(VanillaBackpack), typeof(BackpackSlots), typeof(InventoryExtension), typeof(DropOnBackpack), typeof(QuickMove) })
            {
                // A game update breaking one patch class shouldn't take the rest with it.
                try
                {
                    harmony.CreateClassProcessor(patches).Patch();
                }
                catch (Exception e)
                {
                    Log.LogError($"{patches.Name} could not be applied: {e}");
                }
            }

            // Loaded into a running game (hot reload): take over the inventory now.
            BackpackSlots.RequestSync();
            Log.LogInfo($"{Name} {Version} ready");
        }

        private void Update() => BackpackSlots.Tick();

        private void OnDestroy()
        {
            harmony?.UnpatchSelf();
            BackpackSlots.Shutdown();
        }

        private static void ParseTint()
        {
            if (ColorUtility.TryParseHtmlString(SlotColor.Value.Trim(), out Color color))
            {
                color.a = 1f;
                Tint = color;
            }
            else
            {
                Tint = DefaultTint;
                Log.LogWarning($"SlotColor '{SlotColor.Value}' is not a #RRGGBB colour; using the default.");
            }
        }
    }
}
