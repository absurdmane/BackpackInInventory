# Testing

Rounds 1 and 2 verified in game by the user on 2026-10-18 (items marked [-] skipped).

## Round 1 (skeleton, vanilla window off, slots under the inventory)

- [x] The mod loads (BepInEx log: "Backpack In Inventory 0.1.0 ready"), DevMenu shows its section.
- [x] DevMenu → "Log the three backpack storages": Backpack 24 slots, Rucksack 40, Cooler Bag 24 (refrigeration 0.5); all forbid "Backpack".
- [x] No "OPEN" tab next to the backpack slot, no "3" badge on it, key 3 does nothing, equipping doesn't open a window.
- [x] Backpack worn: its slots right under the inventory slots, same grid, bluish; the area scrolls with the wheel; thin bar on the right.
- [x] Nothing visible outside the area when scrolled (clipping), the inventory looks vanilla without a backpack.
- [x] Drag items main ↔ backpack; the backpack slot item switches Empty ↔ full variant (take everything out → "Empty ...").
- [x] An empty backpack can't be dropped into backpack slots.
- [x] With a fridge / station / trade open the backpack slots stay and work (drag to and from the container).
- [x] Take off / swap backpack (Backpack → Rucksack → Cooler Bag): the slots change, contents kept.
- [x] Close and reopen the menu several times: slots come back, no duplicates.
- [x] DevMenu "Mod on / off": off = vanilla layout and window back, on = slots back.
- [x] Save and load with items in the backpack.
- [x] Small Sort button in the top right corner of the backpack slot while a backpack is worn (gone without one, gone with the mod off).
- [x] It sorts only the backpack's slots (items stay in the backpack), plays the sort sound; the backpack item stays "full".
- [x] Clicking it doesn't pick up / drag the backpack itself.

## Round 2 (backpack as part of the inventory)

- [x] Main inventory full, backpack has room: picking up an item from the world works and it lands in the backpack.
- [x] Same with an item giver (e.g. a dispenser/container that gives an item on use): no "inventory full", the item lands
      in the backpack (first try: it dropped on the ground; fixed).
- [-] Money dragged or added into a backpack slot turns into money (not a money item in the slot). Couldn't be tested; skipped.
- [x] A clock / calendar clock only in the backpack shows the time.
- [x] Auto-consume (food/drink) takes from the backpack when the main slots have none; also after closing the menu.
- [-] Main slots empty, items only in the backpack: building menu and sleep robbery see a non-empty inventory. Code only, hard to set up; skipped.
- [x] Taking a stolen item from someone's container straight into a backpack slot counts as theft, like into a main slot.
- [x] Sorting the backpack (its Sort button) doesn't move anything into hands or main slots.
- [x] Nothing changes with the mod off (DevMenu toggle): pickups ignore the backpack again.
- [x] Drag an item onto the backpack slot icon: it goes into the backpack (merged into a matching stack first), the slot
      shows green while hovering; a whole stack that doesn't fit: the rest goes back; full backpack: red flash, item back.
- [x] Same from an open container / station into the backpack icon; during trade the trader's items can't be dropped there.
- [x] Another backpack dropped on the worn one (or "equipped"): swaps only while the worn one is empty; otherwise red, a
      notification, nothing moves (vanilla dropped the full one on the ground). An empty backpack item turns "full" after a drop.

## Round 3 (quick move main ↔ backpack)

Verified 2026-10-19: the user reported "works well" for the round as a whole (not item by item).

With only the inventory open (no container, station, trade):
- [x] Shift-click (quick move) an item in a main slot: it goes into the backpack, merging into a matching stack first.
- [x] Shift-click an item in the backpack: it goes into the main slots, not back into the backpack and not into hands.
- [x] A stack only partly fits: the rest stays where it was; the backpack item stays "full" / turns "Empty" correctly.
- [x] Target full: nothing moves, the inventory flashes "full".
- [x] The Move button with an amount (slider) moves that amount, in both directions.
- [x] Equipable items (clothes, a backpack, tools for hands): shift-click equips them as before.
- [x] A stolen item moved this way is not a new theft; no money / "New item" notification.
- [x] With a container / station open: shift-click still goes main → container, backpack → container, container → main.
- [x] Mod off (DevMenu): shift-click with nothing open behaves as vanilla.

## Every release: cold start and vanilla regression

- [ ] Cold start: restart the game, load a save with a backpack worn, no Reload changed. Slots, tint, Sort button there.
- [ ] Load a second save without restarting: same.
- [ ] With a backpack worn and the mod on, plain vanilla actions on main-slot and backpack items: Use/Eat, secondary action,
      drag, split (amount slider), drop to ground, equip/unequip, close and reopen the menu, open a container and a station.

## Fixed

- Hot reload hid the inventory item icons every other time: the layout restore swapped the slot grid and its placeholder grid,
  so the placeholders covered the slots. Fixed (order saved before moving, slot grid always drawn last). Check: several
  Reload changed in a row, icons always visible. [verified 2026-10-18]
- Sort button on the backpack slot missing after loading a save (appeared only after a hot reload): loading rebuilds the
  character slots and destroyed the button with the old backpack slot; a "searched once" flag kept it from being made again.
  Now it is re-created whenever the backpack slot object changes. [verified 2026-10-19: the user's in-game check before
  the release]
- With a backpack worn, Use/Eat on any item only closed the item info (1.0.0 bug). Every backpack sync re-captured all
  backpack slots, rebuilding their item icons, and destroying an item icon closes the info panel; using an item triggers
  such a sync. Now only slots that are gone are captured again. Not re-checked in game yet: Use/Eat with a backpack worn,
  on items in the main slots and in the backpack.
