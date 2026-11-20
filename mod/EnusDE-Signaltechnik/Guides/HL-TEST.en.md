# Hl test package 0.7.30-P0

To enable new Hl construction: open **Company → Options → Mods → Signaltechnik → Signal operation and display → Enable Hl signal system (Test)** and apply the settings. Disabled by default. Existing Hl signals remain operational when disabled and can be converted back to H/V.

Additional original procedural models inspired by German Hl signals. Aspect reference: **Ril 301, 301.0103 and 301.0103A01**, edition effective 15 December 2024. This is a model and aspect test, not a complete simulation of every Hl installation type. No DB pictures, textures or models are included.

## Setup

1. Install the ZIP with the mod manager and restart. Keep the existing test save. Host and clients require matching versions; no extra mods are needed.
2. Signaltechnik → Build → **Hl main signal · test** or **Hl distant signal · test**. Place on track and assign to the interlocking as usual.
3. Alternatively select an existing high main mast or distant signal → **Use Hl test model**. IDs, codes, placement, detection boundaries and interlocking assignments are retained. Convert low main signals to high masts first. Locked signals cannot be converted.
4. Main signal: configure direction A/B and train, reduced or **Ra 12 · shunting permission**. Enable **Combined distant function in Hl head** and assign the main signal to announce. Without this option no advance indication is displayed. Save as usual.
5. Standalone distant: assign its main signal and save.

## Scope and limitations

Live existing authorities map to Hp 0, Hl 1, Hl 3a, Hl 7, Hl 9a, Hl 10, Hl 12a and mast Ra 12. A standalone distant displays Hl 1, Hl 7 or Hl 10. Advance indications follow the saved target binding; new route-dependent multi-target recognition is not part of this package.

Hl combines main and advance functions in one head. The upper green/yellow light announces the next condition. Only the announcing light flashes for Hl 4/Hl 7 and their combinations; lower yellow and speed strips stay steady. The modern rulebook calls the stop indication **Hp 0**, rather than Hl 13.

**Speed limitation:** Existing reduced authorities are displayed as the 40 km/h tier. This package does not change native AI speed calculation, braking curves, turnout speeds or mph conversion. It does not guarantee that AI will enforce exactly 40 km/h; more conservative native restrictions may still apply. Full 60/100 km/h combinations are provided in **Hl-Signalbilder.html** as isolated preview only, not new selectable movement authorities. The preview cannot control game signals. No browser is required to use the mod.

Unbound signals retain invalidity crosses; out-of-service signals retain identification lights. This is the existing mod operating-mode policy; releasing electrical points for AI in that state is a game simplification. Selecting an Hl model does not alter route protection or interlocking ownership. Existing signals default to H/V and can be converted back.

## Short gameplay checks

1. New unbound Hl signal: dark with cross. Bound signal with no route: red Hp 0 only.
2. Combined main: clear own route while target stays at stop → Hl 10; clear target → Hl 1. Reduced own route → Hl 12a or Hl 3a respectively.
3. Reduced target → Hl 7 or Hl 9a. Only upper yellow flashes; lower yellow stays on for Hl 9a.
4. Ra 12: two white lights, no green proceed light or yellow advance indication.
5. Standalone distant: target stop/clear/reduced → Hl 10/Hl 1/Hl 7. No red stop authority at the distant.
6. H/V → Hl → H/V and save/reload: IDs, names, detection and bindings retained. Check both mast directions; conversion under an active route must be rejected.
7. Check cross/identification, day/night visibility and identical client aspects including flashing. Separately observe AI stopping at Hp 0 and actual movement speeds.

Offline checks do not replace gameplay and multiplayer validation.

Reference: [DB InfraGO, Ril 301 signal book](https://www.dbinfrago.com/resource/blob/13174940/cb13a4d1ee7bc79e64ecad219bbd91c4/Ril-301-INB-2026-data.pdf), sections 301.0103 and 301.0103A01.
