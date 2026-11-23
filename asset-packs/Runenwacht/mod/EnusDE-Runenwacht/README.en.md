# Runenwacht 0.2.0 / Signal & Interlocking 0.7.64

Experimental test build for Railroader 0.6.3. Both packages must match on host/clients. No game installation, launch or finished gameplay/multiplayer validation is implied.

## New model and material pass

The first 0.1.0 in-game screenshots exposed overly dark metal, flat emissive crystals, primitive rectangular supports and floating captions. This pass replaces those shortcuts: layered riveted circular housing, a centred three-core group (red in the centre; fixed amber and green neighbours), diagonally mounted fixed ring guides, individual sealed spire backs, curved crescent support, rounded copper pipes, bevelled clamps, ceramic insulators and a chamfered stone foundation. Shunting crystals are horizontal cut stones with separate mounting collars.

Every crystal is permanently present and retains one colour. Faceted bodies now have non-emissive, textured lit surfaces; polished facet edges and thin veins are separate animated submeshes. Emission no longer paints over the entire body. Shared generated albedo and roughness/metallic masks provide steel grain, copper scratches/patina, ceramic and stone variation without downloaded images or game textures. Active-pipeline shader selection replaces choosing the first shader merely found in memory. Exact daylight/night exposure and shader behaviour still need a Railroader test.

Technical code and display name are separate immutable frame fields. They are displayed on framed enamel plates; equal name/code does not create duplicate plates. A small RUNENWACHT maker plate identifies the system. Caption-size controls and pick bounds are retained. The concept pictures remain an art direction, not a claim of a pixel-identical model.

## Individual lamp test without routes

1. As host/single-player administrator, enter **Build** and select a completed signal. Open its configuration; choose the front/back direction for a main mast.
2. Open **Individual signal lamp test · host** and choose a state for the main/standalone head: Stop, Clear, Slow, Dark, Invalid or identification light. Shunting heads also offer Shunting permission. No route needs to be created or the signal linked to a panel first.
3. Installed attached distant/shunting heads have their own controls. Testing an attached distant head deliberately shows a synthetic Clear at the main head so combined Hl and H/V models can display a valid advance combination. This is labelled in the editor. The shunting permission test keeps the main red.
4. The test is local visual output for the host. The operating API never returns the chosen permissive test aspect: both mast directions are held at native Stop, and admission rejects routes using a test-held entry, destination, intermediate or flank signal. Other clients receive operating Stop, not the synthetic host view. Existing routes and direction locks must be released before starting. A lamp test does not enable disabled native AI integration or give a player permission to drive.
5. Choose **End lamp test · restore operating indication**, leave Build, close its window or select another physical signal. Tests also end after 120 seconds and during world/data reload or authority loss. Actual operating indications return, including invalidity or identification light. Nothing from a test is saved as an aspect, route, license or operating-state change.

This test control works with built-in H/V, Hl and installed presentation packs. Actual Unity editor callbacks, all visual states, native AI approach/braking, proxy restoration, network/late join and lifecycle behaviour still need game acceptance. Offline domain checks exercise gates, stale requests, two-direction holds, route roles, separate main/distant/shunting plans, expiry and cleanup.

## Install / activate / continue

Use the existing optional-pack activation: host console `/enus_assets enable`, then `/enus_assets unlock:enusde.runenwacht` once per test world; normal signal construction must be unlocked. Activation starts OFF after world load; free world test licenses and model selection persist. The main works alone and missing/faulty providers still use a visible built-in fallback. Keep a full save/configuration backup before trying or rolling back; older mods can discard unfamiliar optional fields when saving.

The public reference is `asset-packs/Runenwacht`: `RunenwachtDesign` contains the shared production geometry, `RunenwachtPalette` shared PBR resources, `RunenwachtModel` the renderer and `CrystalLogic` exclusive fixed-colour animation. `Test-RunenwachtGeometry.ps1` exports the actual design for `Preview-Runenwacht.py`. Blender renders are **offline model studies**, not Railroader screenshots or proof of game-shader fidelity. The design deliberately keeps opaque directional backs. No audio, wagon replacements, automatic asset transfer or paid license purchase is added.

Game acceptance priorities: daylight and night crystal facets/emission, plate readability and user display names, rear/oblique visibility, assembled heads and ground clearance, all lamp tests including stop/restore, locked-route refusal, two-minute expiry, selection/window/world transitions, AI Stop, host/client views, original 0.6.3 pipeline and frame time with many high-detail signals. No LOD is included yet.
