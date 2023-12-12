# Description

## AzuEPI is Extended Player Inventory

`Version checks with itself. If installed on the server, it will kick clients who do not have it installed.`

`This mod uses ServerSync, if installed on the server and all clients, it will sync all configs to client`

`This mod uses a file watcher. If the configuration file is not changed with BepInEx Configuration manager, but changed in the file directly on the server, upon file save, it will sync the changes to all clients.`

What is the difference in this version of ExtendedPlayerInventory compared to others posted?

* This version comes with more compatibility in mind, as well as a few more features.
* One core feature is making it very very hard to lose your items, even when your inventory bugs out.
* It also comes with an API for mods/mod authors to add custom slots to the inventory. This is used by Jewelcrafting to
  add dedicated slots for the utility items.
* Seamless compatibility with Equipment And Quickslots, through automatic disabling of conflicting features. Also,
  seamless migration from it. Please see the **_Migration from Equipment And Quickslots_** section for more information.

---

Recommended to install via [r2modman](https://valheim.thunderstore.io/package/ebkr/r2modman/)
or [Thundersore Mod Manager](https://www.overwolf.com/app/Thunderstore-Thunderstore_Mod_Manager)

If you are installing this manually, you will need to make
sure [BepInEx for Valheim](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/) is installed
correctly. Then, just make sure the dll ends up in your BepInEx/plugins folder.

![](https://i.imgur.com/vkGrXTg.png "Example Image")

## Other mods adding slots via the API (Extreme example)

![](https://i.imgur.com/t0upUgs.png)
_**Mods that added slots in the image above:**_

[Jewelcrafting](https://valheim.thunderstore.io/package/Smoothbrain/Jewelcrafting/),  [BowsBeforeHoes](https://valheim.thunderstore.io/package/Azumatt/BowsBeforeHoes/), [Backpacks](https://valheim.thunderstore.io/package/Smoothbrain/Backpacks/), [MagicPlugin](https://valheim.thunderstore.io/package/blacks7ar/MagicPlugin/),
and Fenrir's Curse

**_Mods that can be seen in the image above:_**

[Jewelcrafting](https://valheim.thunderstore.io/package/Smoothbrain/Jewelcrafting/), [BowsBeforeHoes](https://valheim.thunderstore.io/package/Azumatt/BowsBeforeHoes/), [Backpacks](https://valheim.thunderstore.io/package/Smoothbrain/Backpacks/), [MagicPlugin](https://valheim.thunderstore.io/package/blacks7ar/MagicPlugin/),
[Minimal_UI](https://valheim.thunderstore.io/package/Azumatt/Minimal_UI/), [RapidLoadouts](https://valheim.thunderstore.io/package/Azumatt/RapidLoadouts/),
Fenrir's
Curse,
and of course, AzuEPI.

## API Information/Wiki

https://github.com/AzumattDev/AzuEPI/wiki/API-Home

# Migration from Equipment And Quickslots

* More information on migrating from Equipment And Quickslots:
    * If you remove Equipment And Quickslots, you only need to boot the game up with this mod installed. Upon player
      spawn (Don't freak out about being naked at the main menu!) your items will be migrated to the new system. You
      will either drop the items and thus be found on the ground in front of you, or find them in your inventory (
      Gear slots should auto equip!).
    * `Once your player saves` your Equipment And Quickslot gear will be erased. Migration back to his mod might not
      be as smooth. `You have been warned!`

---

`Feel free to reach out to me on discord if you need manual download assistance.`

# Author Information

### Azumatt

`DISCORD:` Azumatt#2625

`STEAM:` https://steamcommunity.com/id/azumatt/

For Questions or Comments, find me in the Odin Plus Team Discord or in mine:

[![https://i.imgur.com/XXP6HCU.png](https://i.imgur.com/XXP6HCU.png)](https://discord.gg/Pb6bVMnFb2)
<a href="https://discord.gg/pdHgy6Bsng"><img src="https://i.imgur.com/Xlcbmm9.png" href="https://discord.gg/pdHgy6Bsng" width="175" height="175"></a>
***