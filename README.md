# RIMWORLD-STARGATE: Parallel Gates

![Uploading preview.png…]()


`RIMWORLD-STARGATE: Parallel Gates` is a RimWorld 1.6 Stargate prototype mod based on the RimGate Jaffa/Kree mod base.

It keeps the original Jaffa content alive on RimWorld 1.6 and expands it with a playable Stargate system focused on dialing, travel, generated destinations, and incoming gate events.

## Current Features

From the original mod base and earlier Parallel Gates work:

- RimWorld 1.6 support
- Humanoid Alien Races support
- Jaffa race content
- Jaffa factions:
  - Apophis
  - Anubis
  - Ra
- Jaffa pawn kinds
- Jaffa armor, helmets, weapons, sounds, and naming content
- Czech localization support for updated mod content

From the current Parallel Gates implementation:

- Automatic home StarGate spawn on a new game
- Automatic StarGate control panel support
- Manual 7-symbol dialing system
- Address Book
- Galaxy browser
- Generated offworld destination maps
- A full independent RimWorld globe for every seven-symbol address
- Independent terrain, roads, factions, settlements, sites, and world-map exploration per address
- Stable saved addresses and generated destinations
- Persistent 150x150 planet maps that preserve terrain, buildings, items, and inhabitants
- Deterministic planet biomes and civilization profiles
- Jaffa, vanilla, and compatible mod-faction settlements and outposts
- Scannable secondary sites with separate persistent maps
- Return travel to the home world
- Incoming Stargate events:
  - raids
  - traders
  - ally / visitor arrivals
- DevTools support for Stargate testing

## Release v0.1.0

### Added

- Full independent RimWorld planet layers for new Stargate addresses
- Standard world generation with terrain, biomes, roads, factions, settlements, and sites
- Persistent gateway map objects located on each planet's own world map
- Caravan travel and exploration across foreign planets
- Hidden undiscovered locations revealed through exploration
- A 40% deterministic chance for a Jaffa-controlled world with native settlements and Jaffa military outposts
- Odyssey is required for the full planet-layer experience

Existing v0.0.3 and v0.0.4 pocket maps remain supported as legacy save content.

## Requirements

- RimWorld 1.6
- Odyssey DLC
- Harmony
- Humanoid Alien Races

Odyssey is a hard requirement for independent planet layers and world-map travel.

## Installation

1. Install the required dependencies and the Odyssey DLC.
2. Place this repository in the RimWorld `Mods` folder.
3. Enable Harmony, Odyssey, Humanoid Alien Races, and this mod.
4. Restart RimWorld after installing or updating the mod.

For the full planet system, dial a new address. Existing legacy pocket maps remain loadable but are not automatically converted.

## Release v0.0.4

### Added

- Persistent pocket planets saved directly in the RimWorld save
- Address and site metadata stored on every generated planet map
- Temperate, forest, desert, ice, toxic, and ancient-ruins planet generators
- Real settlements and outposts populated by eligible active factions
- Deterministic site content and legacy v0.0.3 map compatibility
- Atomic group transfer with rollback if the destination cannot accept everyone

### Changed

- New planets no longer consume random tiles on the home world's globe
- The same address now always reopens the exact saved map
- Galaxy scan sites now generate as their own persistent off-world locations
- Static gate materials are initialized safely on the main thread

## Release v0.0.3

### Added

- Automatic home StarGate spawn on a new game
- Control panel based gate workflow
- Manual 7-symbol dialing
- Address Book and Galaxy browser
- Generated offworld Stargate destinations
- Return-home travel loop
- Incoming Stargate events:
  - raids
  - traders
  - ally / visitor groups
- DevTools actions for Stargate testing
- Better installation documentation

### Changed

- Stargate travel now behaves more like a real gameplay system instead of a simple two-gate local test
- UI readability for Address Book and Galaxy was improved
- Gate and control panel visuals now use more natural shadows

### For Players

- You can start a new game and immediately get a usable home Stargate setup
- Destination maps are now much closer to a real playable preview experience
- The mod is in a much better state for public community testing

Full player-facing version notes are in [Release_Notes.md](</C:/Program Files (x86)/Steam/steamapps/common/RimWorld/Mods/Rimworld@Gate/Release_Notes.md>).

## Tutorial

### What to download

1. Download this repository from GitHub as ZIP, or clone it with Git.
2. Extract the downloaded archive if needed.
3. You should end up with the full mod folder: `Rimworld@Gate`

### Where to install it

Copy the full mod folder into:

`C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods`

After copying, the result should look like:

`C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\Rimworld@Gate`

### What else you need enabled in RimWorld

Enable these mods in this order:

1. `Harmony`
2. `Odyssey`
3. `Humanoid Alien Races`
4. `RIMWORLD-STARGATE: Parallel Gates`

Then restart RimWorld when the game asks for it.

### Important notes

- A full game restart is recommended after every manual update
- DevTools are useful for testing Stargate features, but not required for normal play
- For a bilingual installation guide, see [HOWTOINSTALL.md](</C:/Program Files (x86)/Steam/steamapps/common/RimWorld/Mods/Rimworld@Gate/HOWTOINSTALL.md>)

## Community

- Discord: [https://discord.gg/SjSH9Tkpf](https://discord.gg/SjSH9Tkpf)
- Website: [https://stargatemod.page.gd/](https://stargatemod.page.gd/)
- YouTube: [https://www.youtube.com/@panzmoravylab](https://www.youtube.com/@panzmoravylab)
- Workshop description: [STEAM_WORKSHOP_DESCRIPTION.txt](STEAM_WORKSHOP_DESCRIPTION.txt)

## Credits

This project started from the continued RimGate Jaffa/Kree mod maintained by Mlie, based on work by Helixien, Xen, Carnov, CPT.OHU, and Erdelf / Humanoid Alien Races.

The current Parallel Gates development is maintained by panzmoravylab.
