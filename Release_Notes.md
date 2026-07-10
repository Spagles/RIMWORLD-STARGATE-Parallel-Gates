# Release Notes

This file is the player-facing release summary for each public version of `RIMWORLD-STARGATE: Parallel Gates`.

Primary language: English  
Secondary language: Czech

---

## Version 0.1.0

### ENG

**Persistent Planetary System Release**

Added:
- Every new seven-symbol Stargate address can create its own persistent RimWorld planet layer.
- Each planet has its own world map with terrain, biomes, roads, factions, settlements, and discoverable sites.
- A persistent gateway location is created on the foreign planet's world map.
- Pawns can form caravans and explore the foreign planet.
- Undiscovered settlements and locations remain hidden until exploration or scanning reveals them.
- Planet generation is deterministic: the same address produces the same initial layout.
- Forty percent of new planet profiles are deterministically selected as Jaffa-controlled worlds.
- Jaffa-controlled worlds contain native low-tech settlements and Jaffa military outposts using one consistent Jaffa faction.
- Existing legacy pocket maps remain supported.

Requirements:
- RimWorld 1.6
- Odyssey DLC
- Harmony
- Humanoid Alien Races

Important:
- Odyssey is required for the independent-globe system.
- Restart RimWorld after installing or updating the mod.
- Dial a new address to generate a new full planet. Legacy pocket maps are not automatically converted.

### CZ

**Persistent Planetary System Release**

Verze 0.1.0 přidává vlastní trvalou planetu pro nové adresy, mapu světa, cestování karavanou, skryté lokality a 40% šanci na planetu pod nadvládou Jaffů.

Požadavky: RimWorld 1.6, DLC Odyssey, Harmony a Humanoid Alien Races.

---

## Version 0.0.5

### ENG

**Full Planet Globes Release**

Added:
- Every new seven-symbol Stargate address creates its own persistent RimWorld globe
- Standard world generation with terrain, rivers, roads, factions, settlements, and discoverable sites
- A normal world-map gateway location on each generated planet
- Deterministic planet generation from the address seed

Requirements:
- RimWorld 1.6
- Odyssey
- Harmony
- Humanoid Alien Races

Legacy pocket maps from versions 0.0.3 and 0.0.4 remain loadable.

### CZ

**Vydani plnych planetarnich globu**

Pridano:
- Kazda nova sedmisymbolova adresa vytvori vlastni trvaly RimWorld globus
- Bezna generace terenu, rek, cest, frakci, osad a odhalitelnych lokalit
- Normalni lokace brany na mape sveta kazde planety
- Deterministicka generace podle seedu adresy

Pro tuto funkci je povinny DLC Odyssey. Stare pocket mapy verzi 0.0.3 a 0.0.4 zustavaji nacitatelne.

---

## Version 0.0.4

### ENG

**Persistent Planets Release**

Added:
- Persistent 150x150 pocket planets stored directly in the savegame
- Deterministic temperate, forest, desert, ice, toxic, and ancient-ruins destinations
- Real settlements and outposts populated by eligible Stargate, vanilla, and mod factions
- Persistent secondary sites discovered through the Galaxy scanner
- Atomic group travel with rollback on transfer failure
- Compatibility handling for existing v0.0.3 destination maps

Changed:
- Off-world destinations no longer occupy random tiles on the home planet
- Returning to the same address restores the exact saved map state

### CZ

**Vydani Trvale planety**

Pridano:
- Trvale planetarni mapy 150x150 ukladane primo do savegame
- Deterministicke mirne, lesni, poustni, ledove, toxicke a ruinove destinace
- Skutecne osady a zakladny obyvane vhodnymi Stargate, vanilla a modovanymi frakcemi
- Trvale vedlejsi lokality odhalovane Galaxy skenerem
- Atomicky presun skupiny s navratem pri chybe
- Kompatibilita s existujicimi cilovymi mapami verze v0.0.3

Zmeneno:
- Cizi planety uz nezabiraji nahodne tily domovskeho sveta
- Navrat na stejnou adresu obnovi presny ulozeny stav mapy

---

## Version 0.0.3

### ENG

**Parallel Gates Preview Release**

Added:
- Automatic home StarGate spawn on a new game
- Automatic StarGate control panel support
- Manual 7-symbol dialing system
- Address Book and Galaxy browser
- Generated offworld destination maps
- Return travel back to the home world
- Incoming StarGate events:
  - raids
  - traders
  - ally / visitor arrivals
- DevTools support for Stargate testing
- Improved installation documentation

Changed:
- Stargate travel now behaves more like a real gameplay system instead of a two-gate test setup
- UI readability for the Address Book and Galaxy screens was improved
- Gate and control panel visuals now include more natural shadowing

For players:
- You can now start a new game and immediately play with a home Stargate setup
- Destination maps are much closer to a real playable preview experience
- The mod is now in a much better state for community testing

### CZ

**Preview release Parallel Gates**

Pridano:
- Automaticky spawn domovske StarGate pri nove hre
- Automaticka podpora ovladaciho panelu ke StarGate
- Manualni vytaceni pomoci 7 symbolu
- Address Book a Galaxy prohlizec
- Generovane mimo-domovske cilove mapy
- Navrat zpatky na domovsky svet
- Prichozi StarGate udalosti:
  - raidy
  - obchodnici
  - spojenci / navstevy
- DevTools podpora pro testovani Stargate funkci
- Lepsi instalacni dokumentace

Zmeneno:
- Stargate cestovani uz funguje vic jako skutecny gameplay system a ne jen jako test dvou bran
- Citelnost UI pro Address Book a Galaxy byla zlepsena
- Brana i panel maji prirozenejsi stin

Pro hrace:
- Nyni lze zacit novou hru a hned mit doma funkcni Stargate setup
- Cilove mapy jsou mnohem bliz skutecne hratelne preview verzi
- Mod je ted ve vyrazne lepsim stavu pro komunitni testovani

---

## Version 0.0.2

### ENG

**StarGate Travel MVP**

Added:
- New StarGate art
- Animated online gate state
- Control panel activation flow
- Longer active gate time
- Generated remote Stargate destination travel
- Group pawn travel
- Wormhole travel loading flavor text

Changed:
- Gate activation became more structured and visual
- Travel moved from simple local testing toward generated destination maps

For players:
- This version introduced the first real Stargate travel loop

### CZ

**StarGate Travel MVP**

Pridano:
- Novy vzhled StarGate
- Animovany online stav brany
- Aktivace pres ovladaci panel
- Delsi doba aktivni brany
- Cestovani na generovane vzdalené lokace
- Skupinovy pruchod kolonistu
- Flavor text pri cestovani cervi dirou

Zmeneno:
- Aktivace brany dostala lepsi strukturu i vizualni stav
- Cestovani se posunulo od lokalniho testu ke generovanym cilum

Pro hrace:
- Tahle verze prinesla prvni skutecnou hratelnou smycku Stargate cestovani

---

## Version 0.0.1

### ENG

**Initial Parallel Gates MVP**

Added:
- RimWorld 1.6 compatibility update
- Jaffa content update and preservation
- Czech localization updates
- Spawnable StarGate building
- Offline / online gate states
- Basic loaded-map gate-to-gate pawn travel

For players:
- This was the first working public MVP for the Parallel Gates direction

### CZ

**Initial Parallel Gates MVP**

Pridano:
- Kompatibilita s RimWorld 1.6
- Zachovani a aktualizace Jaffa obsahu
- Aktualizace ceske lokalizace
- Spawnovatelna StarGate budova
- Offline / online stavy brany
- Zakladni cestovani mezi dvema branami na nactenych mapach

Pro hrace:
- Slo o prvni funkcni verejne MVP pro smer Parallel Gates
