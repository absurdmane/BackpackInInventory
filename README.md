# Backpack In Inventory for Obenseuer

A BepInEx 6 (Unity Mono) plugin for **Obenseuer**. The worn backpack becomes part of your inventory: its slots sit right
under the inventory slots, always there, and it works together with an open container, station or trade. No separate
backpack window, which closed whatever you had open.

## What it does

- The worn backpack's slots are shown under the inventory slots, with a bluish tint. Scroll the inventory with the mouse wheel.
- The backpack window is gone: no "OPEN" tab, key 3 and equipping a backpack don't open it.
- With a container, station or trade open, the backpack slots stay and work like the inventory.
- Quick move with only the inventory open moves items from the inventory into the backpack and back. Clothes and other
  equipable items are still equipped.
- Drop an item on the backpack slot to put it into the backpack.
- A small **Sort** button on the backpack slot sorts the backpack; its items stay in it.
- The backpack counts as inventory everywhere: pickups and item givers put items into it when the inventory is full,
  a clock in it shows the time, auto-consume takes food and drinks from it.
- A backpack that still has items in it can't be swapped for another one: empty it first (the game dropped it on the ground).

## Install

1. Install [BepInEx 6 (Unity Mono)](https://builds.bepinex.dev/projects/bepinex_be) into the game folder.
2. Put `BackpackInInventory.dll` into `BepInEx/plugins/BackpackInInventory/`.
3. Start the game once; settings appear in `BepInEx/config/obenseuer.absurdmane.backpackininventory.cfg`.

## Configuration

| Section | Key | Default | Meaning |
|---|---|---|---|
| General | Enabled | true | Off = the vanilla backpack. Works without a restart. |
| General | SlotColor | #CDDCFF | Background of the backpack slots (#RRGGBB); #FFFFFF = like the inventory slots. |
| General | SortButtonScale | 0.75 | Size of the backpack's Sort button relative to the inventory's. |

## Reporting problems

When reporting a problem, please attach `BepInEx/LogOutput.log`.

## Building

Requirements: .NET SDK 6+ and the game with BepInEx 6 installed.

1. Copy `local.props.example` to `local.props` and set `GameDir` to your Obenseuer folder.
2. `dotnet build BackpackInInventory -c Release` (add `-p:Deploy=true` to copy the DLL into the game's plugins folder).

## License

MIT © absurdmane, see [LICENSE](LICENSE).
