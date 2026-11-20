# Map-dependent signalling unlock – 0.7.31-P0

## Defaults

When the loaded progression contains the native **Whittier–Bryson signalling milestone** (`sctc`), **Signal & Interlocking construction** inherits its prerequisites. On the standard map this follows the corresponding Ela bridge/track phases. The actual prerequisite objects are reused; no fixed bridge ID or translated title is assumed.

Buying native CTC is **not required**. The independent signalling programme retains its **$7,500 base price** and the game's normal discount calculation. An existing unlock is never charged again.

Without native `sctc`, signalling is immediately available without an unlock purchase by default. Detection uses the loaded progression, not installed mod counts. Vehicle mods do not change the rule; a map extension retaining the native signalling milestone initially retains its prerequisites too.

Sandbox remains immediately available. Existing unlocked saves stay unlocked even if newer map/host rules are stricter. Material policy is separate: a free unlock does not automatically make construction materials free in economy mode.

Until unlocked, **Build, Editor and Dispatcher** are blocked. The IXL/STW menu explains the milestone requirement. Diagram and route writes check the unlock too. Settings, handbook and external backup recovery remain accessible.

## Host settings

Open **Company → Options → Mods → Signaltechnik → Materials and construction costs** to inspect the policy source, base price and required milestones.

- **Use map defaults:** automatic detection or the map creator's file.
- **Immediately available:** no unlock purchase.
- **Purchase for $7,500 base price:** optionally select a milestone on this map.

Confirm with **Apply unlock policy**. The prerequisite selector applies only to the host purchase override. Previously saved explicit prerequisites remain host purchase rules. Host policy, prerequisite and permanent unlock are saved, synchronized and backed up with the installation.

## Map creator configuration

An optional **EnusDE-Signaltechnik.progression.json** can be shipped beside the active map data file, normally `Data/Map.json`:

```json
{
  "Schema": 1,
  "Mode": "purchase",
  "Prerequisite": "my-map-bridge"
}
```

Replace `my-map-bridge` with the actual milestone ID. A missing or unfinished prerequisite never grants the unlock.

Other examples:

```json
{ "Schema": 1, "Mode": "free" }
```

```json
{ "Schema": 1, "Mode": "free", "Prerequisite": "my-map-bridge" }
```

The second grants signalling automatically for free after completing the prerequisite. `Mode: "auto"`, without an explicit prerequisite, keeps the defaults. An explicit host override takes precedence over the map file.

The file belongs to the map, not the signalling mod folder or a global directory of installed mods. No additional dependency is needed. Files are re-read on world load, capped at 16 KiB, and reject malformed content or unknown fields. Errors block new default unlocks while existing unlocks remain usable. A typo therefore cannot silently grant free signalling.

## Gameplay checks

1. Fresh locked economy save on the standard map: no purchase before the native prerequisites; Build, Editor and Dispatcher blocked. Settings and recovery accessible.
2. Complete the native prerequisites without buying native CTC. Purchase only the independent signalling programme and verify access.
3. Select an existing unlock again: no duplicate debit. Save/reload: unlock retained.
4. Custom map without `sctc`: immediately available with no unlock debit. Material policy stays unchanged.
5. Custom map with author rule: locked before its milestone, then purchasable or automatically free according to `Mode`. Also check a missing milestone ID and malformed file.
6. Existing unlocked test save: remains usable. Use a separate fresh economy save for the locked-before-unlock test; do not rebuild the existing installation.
7. Matching client version: follows host unlock with no separate purchase. Native milestones and their costs remain unchanged.

Offline checks cover policy decisions, file/save round-trips, retained unlocks, access locks and the native prerequisite check before charging. Actual live map/milestone objects and multiplayer still need gameplay validation.
