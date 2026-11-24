# Project instructions

## Interface language

- English is the default interface language. German must be selectable without another mod.
- Every new or changed player-visible text must be provided in English and German in the same change. Use LocalText.Select(english, german), WindowText(english, german), or a LocalText.Get key present in both language packs.
- Maintain mod/EnusDE-Signaltechnik/Languages/en.json and de.json together. Regenerate src/BuiltInLanguage.cs with tools/update-language-catalog.py when the packs change.
- Do not add new untranslated UI literals. The localization check only exempts the project name and the language-neutral X/Y coordinate labels; do not extend that list with interface prose.
- Keep technical identifiers, user-provided names and native game error messages intact. Do not translate them into different identifiers.
- Run the localization checks through Test.ps1 after UI text changes. Keep prepared release archives unchanged unless a new package version is explicitly being built.

## Source checkpoints (standing user authorization)

- GitHub `EnusDE/EnusDE-Signal-Interlocking`, branch `main`, is the canonical current source. At the beginning of each task, inspect the live remote state and the local worktree; preserve pending work and coordinate any concurrent changes before editing.
- After each completed requested development/fix step, run the checks appropriate to that change, review and explicitly stage the intended source/tests/public documentation, commit, upload to GitHub and verify the remote commit. Routine source uploads are already authorized; do not ask again.
- Use normal forward commits. Never force-push, reset away work, overwrite others' changes or resolve conflicts blindly. If remote changes appear, read and integrate them safely; report any unresolved conflict or upload failure explicitly.
- A clearly labelled intermediate source checkpoint is allowed when useful. Report tests actually run and remaining limitations; do not claim a checkpoint is a tested release or that an unconfirmed upload is backed up.
- Exclude game libraries, binaries/install ZIPs, SDKs, saves/world or installation data, personal settings, diagnostics/logs, secrets, caches and private working notes/chat handoffs. Review the staged/new files, not just ignore patterns.
- This workflow runs at task checkpoints, not as a continuously running background synchronization service. Releases, tags, runtime installation and game launches require a separate request. Keep existing prepared archives unchanged.

## Settings entry point (owner decision, 20 November 2026)

- Open all mod settings through the gear in the clock-toolbar launcher, in an independent window. This fully replaces the previous Company/Preferences settings entry points.
- Keep this access available before milestone unlock and for clients editing local preferences. Retain host authority for shared settings and all existing operating/build permissions.
- Do not replace, wrap or add a selector to the game's installed-mod list, or register another preferences/Mods tab. Preserve other mods' menu entries.
- Keep the launcher reachable even if an older settings file disabled its former visibility option. Retain saved settings and installation data; moving the UI does not require data migration.

## Build and materials (owner decision, 20 November 2026)

- Build is an explicit local mode with a right-hand tool panel and an End build mode action. Keep unconfirmed cursor previews local; exiting cancels the cursor but retains confirmed plans. Ordinary viewers see unfinished confirmed objects as construction sites; the local builder sees virtual models. Do not grant operating authority from rendering state.
- Use the native captive-wagon ordering and compatible existing MOW warehouse loading/unloading for the two stable signal cargo IDs. Keep active native MOW work-train completion and host authority. Do not recreate a parallel purchasing/depot interface.
- Retain legacy balances and support explicit capacity-limited transfer with receipts. Native warehouse inventory belongs to the full game save, not an installation-configuration-only backup. Preserve stable object, route and cargo IDs.
- Current point wiring retains the one-time nearby-stock charge; it is not yet work-car-driven. Document this boundary and require actual game/MP acceptance before claiming the new logistics or views tested in Unity.

## Optional presentation packs (owner decision, 22 November 2026)

- The main mod owns signal/route/AI authority. Optional installed packs implement the immutable presentation API and own their models, materials, light animations and cleanup. Keep the main usable without any pack and preserve saved IDs with a visible built-in fallback.
- Runenwacht under `asset-packs/Runenwacht` is the public reference implementation. Require main 0.7.64 or later, the first build with every presentation API v1 member used by the pack. Keep the separately tested game dependency. Preserve the API v1 binary contract for existing packs. Use the presentation API registration check for compatibility, and leave the minimum main version fixed during compatible main updates. Run `Test.ps1` to build and check both assemblies. Do not bundle main/game DLLs into the optional pack.
- Keep each crystal's colour fixed, all crystal geometry physically installed, and conflicting light channels exclusive. Opaque backs/hoods and correct front/back orientation are part of the design. Rendering never grants movement authority.
- Experiment activation is console-only and OFF per session; new selections additionally require an installed compatible pack and a separate saved world test license. Current licenses are free test grants, not paid purchases. No automatic asset transfer or completed in-game/MP validation may be claimed. Preserve host authority and bilingual UI/docs.

## Model quality and individual lamp tests (23 November 2026)

- Runenwacht 0.1.0 game screenshots showed dark crude housings and flat emissive crystals. Preserve the revised layered/riveted shapes, crescent support, visible faceted crystal bodies with dark non-emitting inactive surfaces, separate fine animated emission, active-pipeline PBR materials and framed technical-code/display-name plates. Generated concept art is a design reference, not proof of in-game fidelity.
- Individual lamp tests are temporary host Build-editor presentation overrides. Never publish a permissive test indication as operating/native authority: hold both mast directions at Stop and exclude affected route entry/destination/via/flank admission. Respect existing route/direction locks and clean up on selection/Build/timeout/world/data/authority transitions. Do not persist test states or automatically enable disabled AI integration.
- Keep code and display name separate, and preserve the original API-v1 frame constructor alongside additive metadata. Offline geometry renders/checks do not replace actual shader, AI, UI, multiplayer or performance acceptance.

- Runenwacht 0.2.1 follows owner feedback: selected active bodies may emit gently near and more strongly far away; distance gain never activates another channel or changes authority. Preserve opaque rear occlusion, fixed crystal colours, actual plaque-to-mast supports and continuous head/insulator/bus connections. Pack-only updates do not advance/repackage unchanged main code.
