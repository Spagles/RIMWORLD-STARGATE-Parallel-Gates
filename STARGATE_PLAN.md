# StarGate Parallel Gates - Development Plan

This file tracks the intended direction of the StarGate mechanics so we can iterate without losing the larger design.

## Current Goal

Build a playable StarGate planet system where:

- every 7-symbol address represents a stable destination;
- the same address always returns to the same saved destination;
- the home planet has its own permanent address;
- players manually dial addresses through the control panel UI;
- travel feels like moving to another planet, even while RimWorld technically keeps everything inside one save.

## Current State

Implemented:

- StarGate building with offline/online graphics and animation.
- Control panel building required for activation.
- Manual dial panel with 3 symbol rings.
- Guided address highlighting.
- Home address button inside the dial panel.
- Address book with home, recent addresses, and known planets.
- Address database stored in the save.
- Planet records stored by address.
- Site records stored per planet.
- First gate site generated per planet.
- Travel to generated map through an active gate.
- Return path to home address.
- New destination maps use vanilla-style map generation instead of the old artificial tiled test surface.

Known limitations:

- RimWorld still has one internal world map.
- The StarGate "planet" system is currently a custom database over normal RimWorld maps.
- Vanilla world-map UI does not yet show a separate StarGate galaxy/planet map.
- Civilizations, settlements, and planet-level exploration are represented in data, but not fully playable yet.

## Design Direction

We will not rely on RimWorld having multiple native worlds at once.

Instead, the mod owns a separate StarGate travel layer:

1. A 7-symbol address maps to a deterministic seed.
2. That seed creates or loads a `StarGatePlanetRecord`.
3. Each planet owns multiple `StarGateSiteRecord` entries.
4. The first site is always the primary gate site.
5. Later sites can become settlements, ruins, enemy camps, resource zones, quests, or hidden gates.
6. Generated maps are saved and reused.
7. Returning home uses the saved home address and home map record.

Core invariant: the address is the planet identity. The same 7-symbol address must always resolve to the same planet seed, the same planet record, and the same saved destination data inside the current save.

Site invariant: a site ID is only an optional sub-destination inside a planet. If a site target is missing, stale, or not yet known, travel must fall back to the planet's primary gate site instead of blocking travel to the planet.

## Next Milestone: Planet System V1

Goal: make the current system feel reliable and understandable in gameplay.

Tasks:

- Make home dialing completely manual but guided.
- Confirm same address always returns to same destination.
- Confirm different addresses create different destinations.
- Confirm return home works from generated planet maps.
- Add debug/dev tools for inspecting the StarGate database.
- Add clear in-game messages when a new planet is discovered vs an old planet is reconnected.
- Prevent duplicate or broken hidden world objects.
- Ensure save/load keeps planet records, site records, generated maps, and gate addresses intact.

## Next Milestone: Planet Content V1

Goal: make generated planets worth visiting.

Tasks:

- Add planet types: forest, desert, ice, toxic, ruins, ancient, hostile.
- Derive planet type from the address seed.
- Generate richer first-site maps based on planet type.
- Add guaranteed safe arrival zone around the gate.
- Add ruins near some gates.
- Add resources based on planet type.
- Add hostile/event chance after arrival.
- Add a simple "planet discovered" letter.

## Next Milestone: Settlements And Sites

Goal: each planet should have multiple persistent places.

Tasks:

- Generate virtual settlements and ruins in the planet database.
- Add UI to view known sites on the current planet.
- Allow dialing or choosing known sites on the same planet later.
- Create hostile/friendly settlement site records.
- Generate maps for sites only when first visited.
- Store whether each site has been visited, cleared, destroyed, or allied.

## Next Milestone: StarGate UI

Goal: make the system readable without relying on debug logs.

Tasks:

- Improve address book layout.
- Show home address, current planet, current site, and known planets.
- Show last 5 visited addresses.
- Add planet status: unknown, discovered, visited, hostile, home.
- Replace temporary UI icons with proper assets.
- Add optional "copy/show address" tools for testing.

## Next Milestone: Galaxy / Planet Map Prototype

Goal: stop relying on the vanilla world map for StarGate destinations.

Tasks:

- Build a custom StarGate map window.
- Show discovered planets as nodes.
- Show sites under each planet.
- Let players select a known address from the custom map.
- Keep vanilla world map untouched for normal RimWorld behavior.

## Save Data Rules

Every persistent destination needs:

- address;
- deterministic seed;
- display name;
- planet type;
- known sites;
- primary gate site;
- map ID if generated;
- world object ID if a hidden map parent exists;
- tile if a vanilla map needs one;
- visited/discovered state.

## Testing Checklist

For every major change:

- Start a clean test save.
- Spawn or use a StarGate and control panel.
- Open dial panel.
- Dial a random 7-symbol address.
- Confirm a new planet record is created.
- Travel through the gate.
- Confirm the map is playable and natural-looking.
- Use the home button to guide manual home dialing.
- Dial home manually.
- Travel back home.
- Dial the same offworld address again.
- Confirm it returns to the same saved destination.
- Save and reload.
- Confirm addresses and destinations still work.

## Current Priority

1. Test the new same-address persistence in game.
2. Test return home from a generated StarGate planet map.
3. Use the new StarGate DB panel to verify stored planet/site IDs.
4. Start Planet Content V1: richer arrival area, ruins, resources, and simple danger.
5. Start Galaxy / Planet Map Prototype after travel persistence is stable.

## Progress Update - 2026-07-05

Implemented in the current working build:

- Home map lookup now uses the saved home map ID first.
- Known offworld maps are reused by saved map ID.
- Saved hidden world objects are reopened instead of always creating duplicates.
- Each address now receives a deterministic planet type.
- Planet type influences the distant tile/biome search when creating a new destination.
- In-game messages distinguish new planet creation from reconnecting to a known planet.
- Control panels now expose a temporary StarGate DB debug window.
- The mod builds successfully after these changes.

## Progress Update - Planet Content V1

Implemented in the current working build:

- Newly discovered StarGate planets now send a discovery letter.
- Each generated gate site gets content only once and stores that state in the save.
- The gate arrival area is cleared and paved into a safer ancient platform.
- Broken ruins are placed near the gate.
- Planet type now adds light flavor and resources:
  - desert worlds can reveal gold;
  - ice worlds can reveal uranium and stone debris;
  - toxic worlds can reveal plasteel and marshy patches;
  - forest worlds add extra trees;
  - ancient ruin worlds add more ruins, components, and ship chunks.
- StarGate DB now shows planet discovery state and whether site content has already generated.

## Progress Update - One-Way Wormhole Feel

Implemented in the current working build:

- Destination gates opened by travel are now marked as incoming-only while the wormhole is active.
- Entering an incoming-only wormhole is treated as fatal instead of a safe return trip.
- Normal gate travel now uses a pawn job: the colonist walks into the gate before the teleport happens.
- The StarGate right-click menu now labels incoming gates as dangerous.

## Progress Update - Kawoosh Burst

Implemented in the current working build:

- Opening a StarGate now triggers a short blue kawoosh/wormhole burst in front of the gate.
- The burst vaporizes pawns, animals, plants, items, and buildings in its path.
- StarGate buildings and StarGate control panels are protected from the burst for gameplay stability.
- Burst duration, length, and width are tunable in the StarGate XML comp values.
- The temporary drawn placeholder visual is disabled until proper sprite frames are available.
- Current burst damage area is tuned shorter so the nearby control panel operator should survive.

## Progress Update - Planet Travel Records

Implemented in the current working build:

- The StarGate address book can now guide the player to a fresh unknown planet address.
- The player still dials the new address manually through the symbol panel.
- Planet and site records now store visit count and last visit tick.
- Successful StarGate travel records a real visit only after pawns arrive.
- The address book now shows the current location, current address, known planet type, and visit count.
- StarGate DB now exposes visit state for planets and sites.

## Progress Update - Clean Discovery And Galaxy Prototype

Implemented in the current working build:

- New games no longer auto-create the old prototype P3X-001 offworld planet.
- Legacy prototype address records are removed from the StarGate database on load.
- Recent addresses are filtered to valid 7-symbol StarGate addresses only.
- Known planets shown to the player are limited to discovered or visited planets.
- Empty/invalid gate addresses no longer send the player to a hidden prototype destination.
- A first StarGate Galaxy window now exists outside the vanilla RimWorld world map.
- The Galaxy window shows Home, unknown address generation, and discovered planets with type and visit count.

## Progress Update - Planet Profiles And Sites

Implemented in the current working build:

- Every StarGate planet now has a deterministic profile derived from its address seed.
- Planet profile includes atmosphere, civilization trace, threat level, and resource richness.
- Discovery letters now include the planet profile.
- Planet site records now track known/hidden state and threat level.
- The Galaxy window now has a planet detail panel with profile data and site records.
- StarGate DB now exposes planet profile fields and site threat/known state.

## Progress Update - Planet Scanning

Implemented in the current working build:

- The Galaxy window can scan the current StarGate planet.
- Each scan reveals one hidden site record on that planet.
- Hidden sites no longer reveal their name/type/threat before scanning.
- Revealed sites are saved as known.
- Planets now track scan count and last scan tick.
- Scanning sends a discovery letter with the new site's type and threat.

## Progress Update - Strict Planet Map Travel

Implemented in the current working build:

- StarGate travel now rejects a destination if it resolves to the same map as the source.
- Stale site map links pointing at the current map are cleared before generation.
- Normal planet travel no longer falls back to the vanilla world-site path.
- If the detached StarGate planet map cannot be generated, the connection fails visibly instead of silently sending the player to a fake destination.
- Successful travel now reports the source map ID and destination map ID for testing.

## Progress Update - Address Seed Visibility

Implemented in the current working build:

- Discovery letters now show the deterministic planet seed.
- Galaxy planet details now show the deterministic planet seed.
- StarGate DB now shows the deterministic planet seed.
- This makes it easy to verify that the same address still resolves to the same planet after time passes or after save/load.

## Progress Update - Galaxy Site Travel

Implemented in the current working build:

- StarGate targets can now include a planet address plus an optional site ID.
- Manual dialing still uses the 7-symbol planet address.
- If Galaxy opens the dial panel for a known site, the gate remembers that site target after the correct address is dialed.
- Known sites in Galaxy now have a Dial site button.
- Each site can generate or reopen its own saved map.
- Unknown or stale site targets now fall back to the planet's primary gate site instead of blocking planet travel.
- This is the first concrete step in path A: the custom StarGate Galaxy layer is becoming the authoritative travel layer, while vanilla world tiles remain technical map holders.
