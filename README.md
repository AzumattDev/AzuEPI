# AzuEPI - Extended Player Inventory


- **Vanity System** - Change armor appearance without losing stats
- **Loadout System** - Save & swap 10 equipment sets with custom names
- **Player Stats** - 40+ customizable stats with smart formatting, in multiplayer you can view others stats also!
- **Player Preview** - Real-time 3D character view matching your vanity
- **Quick Slots Expansion** - Now 0-8 slots (up from 3) with full customization

![v2.0 Overview Vanity](https://i.imgur.com/UV6N3gl.png)

![v2.0 Overview Loadouts](https://i.imgur.com/YpgJODf.png)

![v2.0 Overview Stats](https://i.imgur.com/IahuMoB.png)

---

## Features

**AzuEPI** (Extended Player Inventory) expands Valheim's inventory with equipment slots, quick slots, vanity
customization, and loadout management.

Mods that add slots using the API will either create the slot, or extend the slot if it already exists. Meaning, AdventureBackpacks will share the Backpack slot with Smoothbrain's Backpacks. Hunter Legacy will share the quiver slot with BowsBeforeHoes & Rusty Bags.

## Disclaimer, if you don't like the new layout you can use the configuration file to use the old layout (with improvements added)!
![](https://i.imgur.com/cAHSdmP.png)

### Inventory & Equipment

- **0-6 extra inventory rows** (configurable)
- **6 standard equipment slots**: Helmet, Chest, Legs, Back, Utility, Trinket
- **2 special slots**: Wishbone, Demister (toggleable)
- **API for mod slots**: Other mods can add custom equipment slots
- **Auto-equip**: Items equip automatically when picked up or dragged into an equipment slot. Upgrading an item that was equipped will re-equip the upgraded version automatically.

### Quick Slots (v2.0: 0-8 Slots!)

- **Expanded from 3 to 8 slots** with individual hotkeys
- **Default hotkeys**: Alt+Z/X/C/V/B/N/1/2 (fully customizable)
- **HUD customization**: Resize, reposition, horizontal/vertical layout
- **Drag to move**: Ctrl+LeftClick to reposition on screen

![Quick Slots HUD](https://i.imgur.com/mZPISNl.png)

![Quick Slots HUD2](https://i.imgur.com/2DowZWz.png)

### Vanity System

- **Change appearance** of 6 equipment types without losing stats
- **Hide equipment** while keeping benefits
- **Real-time preview** - character updates instantly
- **Multiplayer sync** - appearance syncs across network
- **Auto-disables** when Armoire mod is detected

Left click to set, right click to clear. Controller is A and X respectively.

![Vanity System](https://i.imgur.com/UV6N3gl.png)

### Loadout System

- **10 saveable loadouts** with custom names (20 char max)
- **Rename feature**: Click "R" button on any loadout
- **One-click swapping** preserves current gear
- **Visual previews** and item tooltips
- **Loadout Inventory Grid**: View all items in selected loadout in a dedicated grid
  - Drag items directly from player inventory into loadouts
  - Ctrl+Click or Right-Click to remove items from loadout
  - Full tooltip support showing item details
- **Persistent**: Saves across sessions

![Loadout System](https://i.imgur.com/YpgJODf.png)

### Player Stats & Preview

- **Player Preview**: Real-time 3D character in inventory - Rotate and zoom.
- **Stats Panel**: 40+ stats with smart formatting
    - Distances: "5.2km" or "500m"
    - Time: "2d 5h" or "45m 30s"
    - Numbers: "1.5M" or "500k"
    - in multiplayer you can view others stats also!
- **Access**: Click 📋 button or hover over character name
- **Fully customizable**: Choose which stats to display

Right stick to scroll the stats window with controller.

![Player Preview & Stats](https://i.imgur.com/IahuMoB.png)

### Gamepad Support (where possible!)

- Complete controller navigation for all panels
- D-Pad/Stick for loadouts and vanity selection
- Button hints and visual overlays
- Seamless keyboard/mouse + controller switching

### Console Commands (7 Total)

Access via F5 console:

- `azuepi.repairall` - Instant full repair (most useful!)
- `azuepi.dropall` - Drop all inventory items
- `azuepi.quickfix` - Fix stuck items/inventory issues
- `azuepi.invlistall` - List all items to console
- `azuepi.slotsfree` - Show free space info
- `azuepi.breakall` - Break all equipped items
- `azuepi.removeall` - ⚠️ Permanently delete all items

---

## Installation

**Recommended**: Install
via [Gale](https://thunderstore.io/c/valheim/p/Kesomannen/GaleModManager/), [r2modman](https://valheim.thunderstore.io/package/ebkr/r2modman/)
or [Thunderstore Mod Manager](https://www.overwolf.com/app/Thunderstore-Thunderstore_Mod_Manager)

**Manual**: Install [BepInEx for Valheim](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/), then
place the DLL in `BepInEx/plugins` folder.

### Server Setup

- **Version check**: Kicks clients without the mod installed
- **ServerSync**: Configs sync from server to all clients automatically (the ones tagged with `[Synced From Server]`)
- **File watcher**: Config changes sync in real-time

---

## Quick Start Guide

### Vanity System

1. Open inventory (Tab) → Click Vanity button (👔)
2. Select equipment slot (Helmet, Chest, Legs, etc.)
3. Choose vanity item or hide the slot
4. Appearance changes, stats unchanged!

*Note: Auto-disables if Armoire mod is installed*

### Loadout System

1. Equip desired gear → Click Loadout button (🎯)
2. Select empty slot (1-10) - saves automatically
3. **Rename**: Click "R" button (max 20 characters)
4. **Switch**: Click different loadout to swap instantly
5. **Manage Items**: Use the inventory grid to the right of loadouts
   - Drag items from your inventory to add to loadout
   - Ctrl+Click or Right-Click items in grid to remove
   - Grid shows all items currently in selected loadout

*Tip: Create loadouts for combat, building, farming, etc.*

### Player Stats

1. Open inventory → Click 📋 button OR hover over character name
2. **Customize stats**: Press F1 (
   a [Configuration Manager](https://thunderstore.io/c/valheim/p/Azumatt/Azus_UnOfficial_ConfigManager/) is
   required) → "5 - UI Features" → "Player Stats to Display"
3. Use checkboxes to select from 40+ stats

**Categories**: Combat, Resources, Travel, Crafting, Misc

### Quick Slots

1. **Setup**: Config → Set slots (0-8)
2. **Place items**: Open inventory, drag to quick slots
3. **Use**: Alt+Z/X/C/V/B/N/1/2 (or custom hotkeys)
4. **Customize HUD**: Ctrl+LeftClick to drag, config for size/layout

---

## Configuration

Access via BepInEx [Configuration Manager](https://thunderstore.io/c/valheim/p/Azumatt/Azus_UnOfficial_ConfigManager/) (
F1). Most settings apply instantly!

**Key Settings:**

- **Section 2 - Inventory**: Extra rows (0-6), equipment row, auto-equip
- **Section 3 - Quick Slots**: Number (0-8), HUD visibility, layout
- **Section 4 - Special Slots**: Wishbone, Demister toggle
- **Section 5 - UI Features**: Vanity/Loadout buttons, stats selection, legacy layout
- **Section 6 - Labels**: Customize equipment slot text
- **Section 7 - HUD**: Quick slots size, position, drag keys
- **Section 8 - Hotkeys**: Individual hotkeys for slots 1-8

⚠️ **Important**: Turn OFF "Enable Equipment Row" if using Randy Knapp's Equipment and Quick Slots mod. This mod will
automatically do this on boot, but keep it off.


---

## Compatibility

**Compatible:**

- Equipment And Quickslots (automatic migration)
- Armoire (vanity auto-disables)
- ValheimPlus, Adventure Backpacks, Better Archery
- Jewelcrafting, BowsBeforeHoes, Backpacks, MagicPlugin
- Epic Loot, Fenrir's Curse, Wizardry, RustyBags
- Minimal_UI, JudesEquipment


  ![Mod Integration](https://i.imgur.com/cXKlgEF.png)

**Incompatible:**

- ExtraSlots, ExtraSlotsCustomSlots (use AzuEPI instead)

---

## Troubleshooting

**Items stuck or equipment not working?**

- Run `azuepi.quickfix` in console (F5)
- Auto-fix runs when closing inventory

**Button disappeared (👔/🎯)?**

- Vanity: Check if Armoire is installed (auto-disables)
- Verify buttons enabled in config (Section 5)

**Can't rename loadouts?**

- Click small "R" button on saved loadout
- Max 20 characters

**Stats panel missing?**

- Click 📋 button near character preview
- Or hover over character name

**Quick slots not showing?**

- Enable in config: Section 3 → "Show Quick Slots on HUD"
- Set slots > 0

**Loadout won't swap?**

- Need free inventory space for swapped items
- Doesn't delete - preserves gear by swapping back

**Vanity not syncing?**

- Ensure all players have mod installed (shouldn't be required, but just in case)
- Try: Close/reopen inventory, re-equip item

**Using existing save?**

- Yes! Works perfectly with existing characters

**Multiplayer?**

- Install on server + all clients for full functionality
- Configs sync automatically via ServerSync

---

## Migration from Equipment And Quickslots

1. Remove Equipment And Quickslots mod
2. Install AzuEPI
3. Load game - items auto-migrate on spawn
4. Save character

⚠️ **One-way migration** - reverting may cause item loss!

---

## API for Mod Developers

AzuEPI provides a comprehensive API for adding custom equipment slots.

**Key Features:**

- Add/remove slots with validation
- Quick slot additions (accept any item)
- Visual prefab registration
- Vanity API integration
- Event subscriptions

**Full Documentation**: https://github.com/AzumattDev/AzuEPI/wiki/API-Home

**Mods Using API**: 
* [Jewelcrafting](https://thunderstore.io/c/valheim/p/Smoothbrain/Jewelcrafting/) - Neck & Finger slots.
* [BowsBeforeHoes](https://thunderstore.io/c/valheim/p/Azumatt/BowsBeforeHoes/) - Quiver slot.
* [Backpacks](https://thunderstore.io/c/valheim/p/Smoothbrain/Backpacks/) - Backpack slot.
* [MagicPlugin](https://thunderstore.io/c/valheim/p/blacks7ar/MagicPlugin/) - Tome & Earring slots.
* [Fenrir's Curse](https://thunderstore.io/c/valheim/p/Azumatt/FenrirsCurse_PTR/) - 
* [Rusty Bags](https://thunderstore.io/c/valheim/p/RustyMods/RustyBags/) - Quiver & Bag slots.
* [Vikings Summoner](https://thunderstore.io/c/valheim/p/Radamanto/Vikings_Summoner/) - Grimoire slot.

**Mods supported natively**
* [Adventure Backpacks](https://thunderstore.io/c/valheim/p/Vapok/AdventureBackpacks/) - Backpack slot
* [Wizardry](https://thunderstore.io/c/valheim/p/Therzie/Wizardry/) - Ring slot
* [Epic Loot](https://thunderstore.io/c/valheim/p/RandyKnapp/EpicLoot/) - Finger Slot
* [Judes Equipment](https://thunderstore.io/c/valheim/p/GoldenJude/Judes_Equipment/) - Backpack slot
* [Hunter Legacy](https://thunderstore.io/c/valheim/p/Dreanegade/Hunter_Legacy/) - Quiver slot

![Mod Integration](https://i.imgur.com/cXKlgEF.png)

---

## Support & Community

**Need Help?**

- [GitHub Issues](https://github.com/AzumattDev/AzuEPI/issues)
- Discord servers (see below)
- Include `LogOutput.log` from BepInEx folder when reporting bugs

### Author: Azumatt

**Discord**: Azumatt#2625
**Steam**: https://steamcommunity.com/id/azumatt/

[![Odin Plus Discord](https://i.imgur.com/XXP6HCU.png)](https://discord.gg/Pb6bVMnFb2)
[![Azumatt's Discord](https://i.imgur.com/Xlcbmm9.png)](https://discord.gg/pdHgy6Bsng)
