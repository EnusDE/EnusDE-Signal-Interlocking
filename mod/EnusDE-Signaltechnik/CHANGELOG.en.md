# 0.7.68-P0 · Waypoint destinations and following routes

- A cleared intermediate shunt signal stays associated with the same train during entry detection.
- If a waypoint lies just before a signal on another segment, destination discovery follows up to 300 m of unambiguous track. It stops at turnouts and ambiguous connections.

# 0.7.67-P0 · Advance waypoint requests and turnbacks

- Set up to three verified route legs ahead for the same train. Stop extending the request at an occupied or unresolved leg.
- Separate legs at a planned reversal. Verify that the whole consist has stopped clear of the points at the turnback location.
- Admit direct train departures along configured lines through the existing permission and block checks.

# 0.7.66-P0 · Signal controls and waypoint requests

- Left-click a world signal to request a route as host. Right-click for object actions.
- Enable automatic requests for fixed waypoints from the launcher gear. The locomotive and game log show the reason for waiting.
- The Editor groups setup by task. The background catalog connects working detection boundaries through signals without their own detector.
- Gameplay and multiplayer acceptance remain pending.

[Operating guide](Guides/WAYPOINT-0.7.66.en.md)

# 0.7.65-P0 · Local waypoint automation

- Enable or pause waypoint route requests from locomotive information, or request the next route once.
- Match the directed native path, movement type and identified consist. Prepare one local route at a time and preserve ordinary locking when the driver takes over.
- Dispatcher status shows locomotive, movement, endpoints and waiting reason. The host can withdraw a pending request and STOP pauses further automatic requests.
- Local fixed waypoints, clear and fully monitored routes. Real Auto Engineer, UI and multiplayer testing pending.
- Runenwacht 0.2.1 is rebuilt with the updated main dependency.

[Waypoint guide](Guides/WAYPOINT-0.7.65.en.md)

## 0.7.64 — Runenwacht quality pass and individual lamp testing

- Runenwacht 0.2.0: centred red/amber/green crystal cluster, curved crescent and detailed housings/conduits/ceramics, UV/PBR textures selected for the active pipeline, non-emissive faceted bodies plus fine animated light veins.
- Framed technical-code and display-name plaques; maker identification, retained stable IDs and local brightness controls.
- Host Build editor: separate installed head tests without routes, native Stop for both mast directions and exclusion of related entry/destination/via/flank routes. No permissive test aspect reaches the operating API.
- End/selection change/Build exit/120-second timeout/world-data transition cleanup restores actual indications. Local host test; clients see operating Stop. No saved test state.
- Both assemblies and full offline suite pass; real Unity, shader, AI, multiplayer and performance acceptance remains open.

## 0.7.63 — optional presentation packs / Runenwacht experiment

- Main-owned immutable signal state API; separate optional Runenwacht 0.1.0 mod supplies original procedural models, directional shields, fixed-colour crystals and exclusive light fades.
- Persisted model choices and free world test licenses; host-only console activation resets OFF each world load. No paid purchase or automatic asset transfer.
- Main/distant/shunting and combined heads retain existing signal IDs, routes, material workflow and operating authority. Missing/faulty providers fall back visibly; route-locked conversions remain blocked.
- Existing label/emission controls, model click bounds and idempotent Unity resource cleanup; bilingual setup and public pack-author reference.
- Offline build/tests only; renderer, camera visibility, performance, save/reload and multiplayer acceptance remain open.

# 0.7.62-P0 – Native interchange purchases

- Added the missing two native interchange suppliers for signal building supplies and signal equipment; existing Equipment wagon selection and MOW storage remain.
- Prepare an isolated industry DTO before native component creation/indexing. Reuse original interchange tracks; preserve other suppliers and cached map data, skip ambiguous/empty interchange definitions.
- Buying uses native wagon destinations, prices/handling, payment, off-map service and return. No instant-fill command or second purchasing menu.
- Added English/German purchase hints and a step-by-step guide. No chat command needed. Explained expected generic FM crates, FL skeleton limits and closed XM appearance; no dedicated textures shipped.
- Added 11 production preparation/prefix/native-contract regressions. Game purchase/payment, delivery/return, visible loads and MP acceptance remain open. User confirmed previous panel/wagon selection; this does not prove the new purchase path.

See Guides/INTERCHANGE-0.7.62.en.md. Manually install; all participants need 0.7.62 and a full restart.

# 0.7.61-P0 – Build mode and native MOW logistics

- Explicit Build mode with a right-hand icon palette, selected-tool feedback and End action. Native X and workspace switches leave Build; confirmed plans survive.
- Cyan virtual plans for the local builder; original cones/barrier for confirmed unfinished objects outside Build. Private cursor previews remain local; completed infrastructure retains its model.
- Normal captive-wagon orders for both stable signal cargos, plus loading/unloading at compatible native MOW warehouses. FL/FM building supplies and XM signal equipment feed active owned MOW work-train construction.
- Replaced manual purchasing/depot controls with native order access, warehouse stock and open-job/status overview, also in the construction milestone.
- Explicit partial legacy-stock transfer with capacity checks, rollback and save receipts. Excess remains. Existing installations, routes and cargo IDs are retained.
- First point wiring can debit nearby native warehouse stock; its existing one-time charge remains, without a work-car requirement at the point.
- All settings remain in the launcher gear. English/German texts and focused material/API/mode checks included.
- Compiles against local 0.6.3 libraries; no new Unity, delivery or multiplayer acceptance yet.

See Guides/BUILD-MOW-0.7.61.en.md. All multiplayer participants need 0.7.61 and a complete restart.

# 0.7.60-P0 – Settings in the launcher

- Added a fourth gear button opening a separate resizable settings window.
- Settings remain reachable before milestone unlock, for clients and when an old file hid the toolbar button.
- Removed the former Company mod-list selector and dedicated preferences tab; native mod management remains native.
- Preserved settings storage, world data, host authority and the three existing workspace symbols.
- Updated active menu/help references and both language packs.
- Added production picker/window callback and lifecycle regressions.
- Fixed the existing vehicle-tab hook lookup for the renamed 0.6.3 BuildContent callback. Exact signatures remain required; unknown/ambiguous callbacks are rejected.
- Compiles against the local game libraries with 0.6.3 player metadata; in-game/UI/multiplayer acceptance remains pending.

See Guides/SETTINGS-0.7.60.en.md. All multiplayer participants require 0.7.60 and a complete restart.

# 0.7.59-P0 – Continue editing saved block views

Open with a prepared preview; option changes refresh it while retaining placement. Apply rebases the open editor to the saved state, allowing another edit/save. Widening views can grow the panel instead of resetting position. Old native-render callbacks are rejected. Added four-section rotated-view and repeated save/reload/reopen tests. See Guides/BLOCK-VIEW-REEDIT-0.7.59.en.md. Gameplay/UI acceptance remains pending.

# 0.7.58-P0 – Block view actions during live number updates

Separate construction snapshots from live train-number traffic so ordinary number forwarding no longer silently disables block-view actions. Added visible status/errors and a persistent Apply button, no-op event guards and current shared configuration loading. Block construction retains actual-change checks and current live numbers. Added production-window callback lifecycle tests. See Guides/BLOCK-VIEW-ACTIONS-0.7.58.en.md. Gameplay/UI acceptance remains pending.

# 0.7.57-P0 – Stable preview placement

Placement clicks update the candidate in place without rebuilding/refitting the view. Current placement remains visible; temporary pointer previews clear on exit. Added inline status and exact one-cell move buttons. Preview and block-build placement can grow the panel while retaining collision/size checks and source/permission bindings. See Guides/BLOCK-PLACEMENT-0.7.57.en.md. Gameplay/UI acceptance remains pending.

# 0.7.56-P0 – Block view window layout

The block-view preview reserves its own row in a scrollable form instead of overlapping controls. Compact dropdowns and fitting to the actual viewport keep settings reachable at smaller window sizes. View and operating data remain unchanged. See Guides/BLOCK-VIEW-LAYOUT-0.7.56.en.md. Gameplay/UI acceptance remains pending.

# 0.7.55-P0 – Editable block views and destination links

Per-panel display detail, selected sections, horizontal reflection and 180° rotation, with staged preview and group placement. Repeated train numbers are inside the rail. Original sources and sole signal ownership are retained. Tutorial setup links every selected departure to its destination and orients the destination from the local entry; permission modules support schematic track ends. See Guides/BLOCK-VIEWS-0.7.55.en.md. Gameplay acceptance remains pending.

# 0.7.54-P0 – Multiple signals per tutorial role

GWB tutorial signal clicks now add/remove individual signals instead of replacing the selection. All selected fields stay green, including the repeated-selection diagram picker. Counts, names and individual deselect controls are shown. Every selected signal is validated; shared line geometry and direction checks remain required. See Guides/GWB-MULTISELECT-0.7.54.en.md. Gameplay acceptance remains pending.

# 0.7.53-P0 – GWB setup tutorial

One selection at a time: entry A, departure A, entry B, departure B. Per-step explanation and checks with actionable feedback. Physical path setup is independent of complete operational route design, target buttons and GFM. Missing linked destination/permission controls are proposed; suitable existing fields are reused. Prepare point detection on the chosen path and activate only at checked tutorial completion. Simple object assignment remains optional. See Guides/GWB-TUTORIAL-0.7.53.en.md.

# 0.7.52-P0 – Diagram first, world click assigns directly

Open signal/point fields are green; the active target has a bright border and can be brought into view. Clicking its real world object saves the assignment without another confirmation, restores the same local diagram and marks the next field. Removed the global object list and empty map area from this workflow. New-signal construction is collapsed until requested. Selection tooltip identifies the green target; single enabled mast heads can be picked from either side. See Guides/DIAGRAM-FIRST-0.7.52.en.md.

# 0.7.51-P0 – Delete GWB connections

Deletion now clears dependent permission fields and auxiliary bindings together with the line entry. Preview and confirmation are available at every assistant step. Group bindings are released and associated block automation/contacts disabled. World objects, GFM and drawings remain. Affected occupancy, routes and direction holds are rechecked before deletion. See Guides/LINE-REMOVAL-0.7.51.en.md.

# 0.7.50-P0 – Setup assistant test build

Stepwise world-object assignment with an orientation map; existing/new signal setup from a diagram field. Four-step line/GWB setup with existing-connection checks; confirmed replacement of block divisions and complete compact number views. Optional receiving-track capacity and incoming-route reservations. Fixed direct detection assignment between adjacent points.

New workflows still require gameplay and multiplayer acceptance. See Guides/SETUP-ASSISTANT-0.7.50.en.md for scope and limitations.

# Changes since 0.7.22 — English release notes for 0.7.40

Updated from 0.7.22 to 0.7.40. Railroader 0.6.2 is required. Host and clients must use the same mod version.

IMPORTANT: Recalculate older saved routes containing points once to update their physical protection boundaries.

Tutorial / user guide: WORK IN PROGRESS. Version-specific setup and test notes are included.

## 0.7.40

- Train and shunting routes now use the same physical point-protection boundaries as GFM detection, replacing separately generated 25-metre route guards.
- Diverting flank-protection points use the same shared boundary definitions; other point arms and threat corridors remain protected.
- Route protection signatures detect obsolete point boundaries after detector or binding changes.
- Rejection messages identify train versus shunting movement and distinguish track, point-protection and flank areas.

## 0.7.39

- Added a shared directional passage point for signal return to Stop and train-number advancement: 5 metres by default, adjustable from 0 to 25 metres.
- Occupancy alone no longer counts as a verified passage of the start signal when directional vehicle tracking is available.
- Occupancy readers now use actual vehicle track-segment and metre intervals rather than projected world-position overlap.
- Local route checks read only the requested portion of an extended A/B detection section.
- Shunting routes can use known occupied intermediate track outside point-protection intervals.
- Added installation refresh and configuration inspection while preserving saved settings and stable IDs.
- Added direct section-manager access and more readable occupancy errors with signal, point, GFM and metre context.

## 0.7.38

- Reduced excessive flank reservations beyond diverting protection points while retaining the actual threat corridor.
- Improved linked auxiliary-destination handling independently of the last selected diagram cell.
- Corrected train-number transfer completion, retry handling, stale duplicate entries and ordinary display ownership across overlapping sections.
- Added GFM boundary-origin details, including source objects, track positions and diagram assignments.
- Added manual diagnostic ZIP export with configuration, route attempts, occupancy readings, vehicle positions and number bindings.
- Added an optional local export folder suitable for user-managed synchronization. No automatic upload or remote control is performed.

## 0.7.37

- Separated local station movements from unrelated extended GFM direction chains.
- Added an explicit host option for manual routes with incomplete configured GFM coverage; unmonitored areas remain marked accordingly.
- Restricted block-topology invalidation to relevant track, detection and boundary changes.
- Added clear-only GFM reset with background-catalogue pause and cancellation of outdated detection results.
- Added world-object boundary-to-boundary section setup and explicit detector links to native point arms.
- Added inspection links for detection problems and visible recording-folder information.
- Native point-guard reconstruction no longer shortens at detector-free signals or perpetuates an old generated B boundary as its own location reference.

## 0.7.36

- Added full GFM rescan previews for one interlocking or the complete installation.
- Added point-arm calibration, manual correction and separate occupancy indication for individual point arms.
- Added read-only section inspection, boundary highlighting and route-coverage tests showing exact gaps.
- Detection resets preserve world objects and drawing geometry; previews and the required installation checkpoint guard replacement of assignments.

## 0.7.35

- Added continuous signal placement without opening properties after every placement.
- Signal type, model and construction defaults remain selected during placement; Escape or a mode change ends the series.

## 0.7.34

- Improved saved GFM-path validation for native position rounding, equivalent path fragments and long sections.
- Genuine gaps, skipped boundaries and point crossings remain rejected with more specific diagnostics.

## 0.7.33

- Added one-click world assignment for signals, points and GFM boxes without an extra confirmation or unintended point movement.
- Added assignment and dispatcher display of independent, directional C boundaries.
- Quick actions now save directly; world-signal context menus provide Edit.
- Improved detection through virtual destinations, detector-free intermediate signals, separate crossing rails and independently configured mast directions.
- Connected shared A/B diagram boundaries to actual point protection.
- Added a separate route-choice window with short/long labels and distinguishing signal or point names.
- Added multiple departure/arrival signals per line direction, auxiliary arrival destinations and read-only neighbouring-signal displays.
- Detection search length is a proposal filter rather than a limit on the physical occupancy section.

## 0.7.32

- Added usable local line/permission-indicator placement and guided shared block/line setup.
- Added explicit Test opt-in for Hl construction.
- Automation contacts can be configured before construction, with initial direction and a first-main-signal proposal.
- Added world selection for contact start and target signals, plus linked-contact information at signals.
- Added a construction default for new signals with GFM boxes and GFM detection within an existing drawing.
- Restricted bulk electric-point conversion to correctly assigned local points.
- Improved target-directed route search, release countdowns and blocking feedback.
- Shortened temporary backup filenames to address the reported Windows path-length failure.
- Auxiliary-release waiting time and blocking reasons are visible to clients; normal cancellation does not silently start a timed auxiliary release.

## 0.7.31

- Added map-dependent signalling unlock rules using native progression prerequisites without requiring a native CTC purchase.
- Added optional map-author progression rules and host overrides.
- Preserved existing unlocks and Sandbox access while enforcing construction access in locked saves.
- The independent signalling programme retains its $7,500 base price with native discount calculation where a purchase is required; host policy is shared and backed up.

## 0.7.30

- Added optional original Hl main/combined and distant signal test models.
- Existing compatible signals can be converted between H/V and Hl while retaining IDs, positions and detection assignments.
- Added selected live Hl aspects and a separate aspect preview. The 60/100 km/h combinations remain preview-only; native AI speed handling is unchanged.

## 0.7.29

- Added invalidity crosses for unbound signals and identification lights for signals or interlockings taken out of service.
- Added guarded signal/interlocking operating-state changes and selective native-AI bypass.
- Added direct access to the clicked world's point configuration.

## 0.7.28

- Added one shared A/B box at an exactly coincident signal/point detection boundary while preserving existing identities.
- Shared boundaries are captured once; nearby separate boxes and directional C boundaries remain distinct.
- Shared signal/point users can be inspected from the signal and point-GFM menus without manufacturing a separate zero-length section.

## 0.7.27

- Added direct main-signal role selection: entry, intermediate, departure or block.
- Roles can be configured separately for each mast direction without replacing signal identity.
- Signal-role changes retain target-automation settings and respect active route/direction holds and configured line endpoint roles.

## 0.7.26

- Added mouse movement and context editing of master-view modules.
- Added direct point-GFM activation and station signal/boundary searches for existing diagrams.
- Improved refresh of bound signal captions, main/shunting pairs and GFM references after signal conversion.
- Improved the directional C marker's appearance.

## 0.7.25

- Added schematic strand-based recognition, configurable drawing scope and a visible master-module workspace.
- Expanded A/B/C detection tools, persistent boundary quick actions and attached-box placement checks.
- Separated editable display names from stable technical signal identities.
- Extended electric-point protection into native AI look-ahead and improved waypoint-compatible route selection.
- Signal-attached A, independent/point B and one-sided C boundaries have separate roles; new signals default to A, while deleted technical numbers are not automatically recycled.

## 0.7.24

- Added management of additional master views with independent module layouts over the shared installation.
- Added explicit recognition previews, guarded application and archiving of replaced diagrams.
- Moved settings to Company > Options > Mods while retaining existing settings.
- Extended protection of electric points against native waypoint-AI commands.
- Improved contact interaction and added an optional experimental axle-test display.
- Contact route requests respect intermediate main signals and the current waypoint limit.
- Master-view management includes guarded opening, renaming and removal with installation checkpoints; shared physical objects remain independent of their views.

## 0.7.23

- Added route calculation on request from Dispatcher start/destination selection without growing the permanent route catalogue.
- Retained the saved-route workflow as a selectable operating mode.
- Added separate movable/resizable editor and vehicle-information windows.
- Improved field-specific context menus and world/diagram point assignment.
- Right-mouse camera control no longer cancels placement; Escape ends placement.
- Incomplete start selections expire without cancelling established routes or pending automation.
- Active calculated routes retain their own live protection data; clients receive bounded route snapshots, and connection/material costs remain enforced.
- Master connection lists use configured line definitions independently of the saved route catalogue. Incomplete start selections expire after five seconds.

## Alpha status and documentation

Experimental features and known limitations remain: automatic recognition, Hl models, axle-test displays and parts of automation still require gameplay validation.

Auxiliary destinations currently accept one configured approach direction per target.

Automatic renumbering, the newly proposed GWB permission-module overhaul and complete AI shunting reversal beyond protecting signals are not included.

Signals intentionally start at Stop after loading; previous live movement authorities are not restored.

1,421 automated checks passed for 0.7.40. Actual gameplay and multiplayer validation remain ongoing.

[Tutorial / User Guide — Work in progress](Guides/TUTORIAL.en.md).
