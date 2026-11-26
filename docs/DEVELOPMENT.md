# Development status and continuation — 0.7.74

The GFM path validator now accepts a detector anchored on the adjoining rail at the same ordinary two-way node where a saved path ends. The diagram bindings already identify the full circuit in this case. The live reader, inspection, manual search and automatic catalogue apply the same endpoint rule. The endpoint must match a shared native node. Switch nodes, three-way joins, detached rails and displaced boundaries remain invalid. One-sided C limits use the direction on the detector's rail.

The full build and offline suite passed **2,218 checks, zero failures** against local Railroader 0.6.3 libraries. Regression checks cover a turnout guard ending at a segment boundary, both travel directions, automatic and manual detection, and the rejection conditions. The existing section ID and path remain usable. Game acceptance remains open. Check the existing W198–W212 connection in Ela when clear and when a vehicle enters that section.

## Deutsch

0.7.74 behebt eine falsche GFM-Ablehnung am Übergang zweier durchgehend verbundener Gleissegmente. Der vorhandene Abschnitt bleibt mit seiner Kennung und Gleisbildzuordnung erhalten. In Ela steht der Spieltest am Knick zwischen W198 und W212 noch aus.

# Previous development status — 0.7.73

The GFM world overlay prepares a copy of the saved sections and their identity colours on a worker. Colours are cached for the loaded section catalogue. The indexed colour calculation produced identical results to 0.7.72 on 908 sections in an offline comparison. It took about 1.3–2.1 ms with about 0.49 MiB allocated per run. These figures measure only colour preparation.

The camera and native track are read on the Unity thread. The overlay queues geometry and object work across frames with a 1 ms time target per slice. The selected section and the nearest pieces have priority within the visible range. Existing pieces remain during refresh and are reused. The overlay can draw up to 3,000 visible pieces, including end posts. Closing Build, hiding the overlay, unloading and losing build permission cancel pending work.

The full build and offline suite passed **2,208 checks, zero failures** against local Railroader 0.6.3 libraries. Regression checks cover colour parity, cancellation, deferred geometry work, empty views and draw priority.

With signal diagnostics enabled, `gfm_overlay_state` records enable and disable times. `gfm_overlay_refresh` records reused and newly allocated pieces, wall-clock duration and the slowest main-thread slice. The next in-game comparison uses the same camera movement with the overlay off and on. Verify selection, curve and point alignment, end posts, floating-origin movement, repeated toggles and frame times. Operational track detection and route protection continue independently of the overlay.

## Deutsch

0.7.73 bereitet die Abschnittsfarben im Hintergrund vor. Im Baumodus entstehen Linien und Klickflächen schrittweise. Bereits vorhandene Abschnitte bleiben beim Kameraschwenk sichtbar. Die betriebliche Freimeldung läuft unabhängig weiter. Im Spiel steht der Vergleich derselben Kamerafahrt mit und ohne Anzeige noch aus.

# Previous development status — 0.7.72

[English guide](../mod/EnusDE-Signaltechnik/Guides/GFM-WORLD-0.7.72.en.md) · [Deutsche Anleitung](../mod/EnusDE-Signaltechnik/Guides/GFM-WORLD-0.7.72.de.md)

26 November 2026: host-local Build overlay displays saved GFM intervals as automatically coloured identity lines with main-path endpoint posts and clickable trigger targets. Neighbouring sections prefer different colours; colours can repeat and never grant authority. Selection reports length, circuit boundaries and diagram owners, opens existing checked GFM/assignment tools, and inspection can reopen the same section in world Build mode. An unassigned selected section remains visible in inspection. Camera range 400 m / 3,000 pieces; hidden/unavailable intervals are not clear-track reports. Overlay cleanup covers exit, world/data/authority changes and unloading. No saved paths are changed by rendering.

Completed full Test.ps1 against local original Railroader 0.6.3 libraries: **2,202 PASS / 0 FAIL**; both main and Runenwacht compiled. Added tests cover deterministic identity colours, shared/physical adjacency, overlaps, parallel tracks, long chains with repeated palette, no mutation and empty installations. Existing UI/localization/lifecycle/route protection tests passed. Documentation diff and package contents checked separately. Main 0.7.72; optional Runenwacht stays 0.2.2/minimum main 0.7.64/API v1.

Not included: arbitrary new world-click cuts, automatic merging/splitting or signal-placement detection, C-direction arrows or flank-protection exceptions, shared client selection, camera teleport and automatic repairs. Actual Unity picking/rendering, near-signal interaction, floating-origin alignment, wide/curved/invalid intervals, day/night shader visibility, performance, navigation, save/reload and MP acceptance remain open. No runtime installation, game launch, tag or public release. Existing archives remain unchanged. Earlier 0.8.0 acceptance items below remain open.

## Deutsch

0.7.72: Im Host-Baumodus gespeicherte GFM-Abschnitte farbig anzeigen und in der Welt auswählen; Grenzen des Hauptpfads markieren; zur bestehenden geprüften GFM-/Gleisbildbearbeitung und zurück wechseln. Farben sind automatische Abschnittskennzeichnungen, keine Belegungs-/Erlaubnisfarben. **2.202 PASS / 0 FAIL**, beide Assemblies kompiliert. Bearbeitung weiter im vorhandenen Editor, kein freies Welt-Zeichnen neuer Grenzen und keine Schutz-Ausnahme. Spiel-/Darstellungs-/KI-/Mehrspielerabnahme noch offen; kein Spielstart oder Installation. Vorhandene ZIPs und Runenwacht bleiben unverändert.

# Previous development status — 0.7.71

[English guide](../mod/EnusDE-Signaltechnik/Guides/MODULE-PROBLEMS-0.7.71.en.md) · [Deutsche Anleitung](../mod/EnusDE-Signaltechnik/Guides/MODULE-PROBLEMS-0.7.71.de.md)

Host/editor diagnostics now group findings by interlocking and functional module, with plain-language cause, effect and action, optional unchanged technical detail and direct diagram navigation. Saved GFM path validation exposes typed causes without changing its rejection/acceptance rules. Checks additionally cover saved diagram/point/route bindings, flank calculation, retained route faults, known line faults, failed game/waypoint integration and optional-model problems. Unknown check exceptions stay unverified. No automatic repairs or protection changes.

25 November 2026: completed full Test.ps1 against the local original Railroader 0.6.3 libraries: **2,194 PASS / 0 FAIL** in build/checks/RESULTS.txt; main and Runenwacht compiled. New tests exercise module-failure isolation/location, native detail separation, stale world/configuration/install revisions, normal route states versus fault, bilingual distinct GFM actions and actual typed path rejection fixtures. Source/documentation diff and new package contents checked separately. Main 0.7.71; Runenwacht remains 0.2.2, minimum main 0.7.64/API v1 unchanged.

This is a manual host/editor snapshot, not full health coverage: not every automation request, unconfigured GFM gap or current occupancy is inspected. Runtime line errors are the last known state; provider rendering errors can be historical. No world migration, installation, game launch, tag or public release. Existing archives retained.

Still open for 0.8.0: in-game readability/navigation and UI behaviour, actual malformed/moved references, incomplete checks, intentional paused/occupied states, missing-model fallback, retained-fault protection, save/reload and host/client tests; earlier route-colour, AI/braking/turnback/block/following-train tests below; actual W157/stub-track GFM extent and rejection diagnosis. The community layout has not been changed or proven repaired.

## Deutsch

0.7.71 ergänzt **Modulprobleme und nächste Schritte**: betroffenes Stellwerk und Funktionsmodul, Ursache im Klartext, Auswirkung und Behebungshinweis. GFM-Ursachen werden strukturiert unterschieden. Originalkennungen/Fehlermeldungen sind zuschaltbar; unbekannte Prüfprobleme bleiben ungeklärt. **2.194 PASS / 0 FAIL** im vollständigen Test.ps1 am 25.11.2026, beide Assemblies kompiliert. Kein Laufzeitschutz verändert und keine automatische Reparatur. Manueller Host-/Editor-Schnappschuss mit begrenzter Abdeckung; Spiel-/KI-/Mehrspielerabnahme bleibt offen. Hauptmod 0.7.71, Runenwacht unverändert 0.2.2. Nachfolgend frühere Entwicklungsstände erhalten.

# Previous development status — 0.7.70

[English instructions](../mod/EnusDE-Signaltechnik/Guides/ROUTE-TYPES-0.7.70.en.md) · [Deutsche Anleitung](../mod/EnusDE-Signaltechnik/Guides/ROUTE-TYPES-0.7.70.de.md)

Route rendering carries the admitted route's Shunting flag into green train / white shunting track colours. Occupied remains red and unknown remains amber across base rails, independent half-rails, active switch arms and inline number mirrors. Preparing and retained fault indications stay separate. No authority, occupancy admission or native signal aspect is derived from the display colour.

Automatic waypoint movement keeps explicit choices. In automatic detection, a structural train destination wins even if later admission fails. Only no train destination plus a supported same-station shunting endpoint on the proven current path permits the shunting alternative. Configured free-line intervals (also disabled) and block signals are excluded. The normal route planner and all protective checks still run afterwards. Query failures are reported, not reinterpreted as a shunting request.

25 November 2026: full Test.ps1 **2,170 PASS, 0 FAIL** against the local original 0.6.3 libraries, with both assemblies compiled. New checks cover route metadata/state/occupancy priority, released intervals, display aggregation, explicit choices, lazy preference/error propagation and directed local/free-line/block exclusions. UI texts and both language packs/generated fallback updated. Main version is 0.7.70; optional Runenwacht stays 0.2.2 and API v1/minimum main 0.7.64 remain unchanged. No schema/data migration, installation, game launch, tag or public release.

## Toward 0.8.0 / Richtung 0.8.0

This is a development checkpoint, not a claim of full 0.8.0 acceptance. Next real-game tests: colour/occupancy at normal and mirrored panels, shunting into occupied track outside protected points, refusal at occupied/unknown protection, train preference versus local shunting-only goals, explicit roles, missing line permission, both directions, native waypoint/braking/turnback and 0.7.69 following trains/block continuation, save/reload and host/client views. The public 0.7.59 user's W157/stub-track case still needs the actual GFM extents and rejection message; this change does not guess or rewrite that installation. Direct editor palette/drag placement remains a separate usability request.

**Deutsch:** 0.7.70 trennt grüne Zugfahrstraßen und weiße Rangierstraßen; Rot/Unbekannt behalten Vorrang. Automatische Fahrartwahl bevorzugt Zugfahrt und wechselt nur ohne Zugfahrziel auf eine nachgewiesene örtliche Rangieralternative. Explizite Fahrarten, Schutzprüfungen und native Signalbegriffe bleiben erhalten. **2.170 PASS, 0 FAIL** am 25.11.2026. Für 0.8.0 stehen die genannten Spiel-/KI-/Mehrspielerprüfungen und die konkrete GFM-Fallprüfung noch aus. Nachfolgend erhaltene vorherige Entwicklungsstände.

---

# Previous checkpoint — 0.7.69

## Block continuation and following trains

The host test of 0.7.68 set and used three consecutive routes from HS-007 via HS-014 and HS-017 to HS-020. The train stopped at HS-020. STR-001 was active and HS-020 was its last forward block signal. The waypoint check expected a station departure at that point and rejected the request to continue to the next interlocking. A separate dispatcher request to the linked line destination tried to set the complete chain and was rejected by a later occupied GFM section.

0.7.69 matches consecutive block signals and the configured arrival in each direction. It proves only the corresponding directed line slice for each leg, including the distance from a staggered signal to its detector. A dispatcher request to a linked destination can stop at an available block signal if GFM proves occupancy in a later leg. It shows the reduced limit and the occupied section. The remaining manual legs must be requested separately. All admitted legs still use ordinary occupancy, flank, point, locking and permission checks.

`Build.ps1` and its full `Test.ps1` run passed **2,142 checks, zero failures** against local Railroader 0.6.3 libraries. Tests cover both line directions, block ownership at either end, direction conflicts, missing track proof, an occupied first block, unknown detection and a following train up to a free block signal. Retest STR-001 from a world state with the line initially clear, in both directions. Put a second train behind the first after the first block becomes clear. Verify the following request stops short of the occupied block and opposing traffic remains held. In-game and host/client acceptance of 0.7.69 remain pending.

## Deutsch zu 0.7.69

Die Automatik prüft den letzten Blockabschnitt bis zum Einfahrsignal des Nachbarstellwerks. Bei einer manuellen Anforderung an ein verknüpftes Streckenziel kann der Fdl den freien Teil bis zu einem Blocksignal stellen. Der belegte Abschnitt wird angezeigt und bleibt gesperrt. Die Weiterfahrt danach separat anfordern. Im Testspielstand STR-001 anfangs frei lassen. Danach die Weiterfahrt von HS-020 nach HS-009 und einen zweiten Zug im freigewordenen Block prüfen.

## Waypoint follow-up corrections

In the 0.7.67 host test, a train entered its first route while the following routes at HS-043 and HS-062 were already set. The automation briefly treated RS-037 as a separate start and withdrew both routes. A permissive intermediate shunt indication now remains associated with the exact reserved consist while entry detection catches up.

The route from HS-015 waited without a destination. Its fixed waypoint was on an ordinary track segment without a signal. Beyond the waypoint, destination discovery now checks up to 300 metres of uniquely connected track. It stops at a turnout, ambiguous junction or unavailable segment. The train's stop remains the native waypoint. Existing route, block and occupancy checks continue to apply.

The full offline suite passed 2,127 checks, including the verified intermediate signal before entry recognition and an unambiguous continuation across two segments. Retest the same waypoint from HS-015 and the consecutive train routes via RS-037 in Railroader.

## Waypoint lookahead and turnbacks

Waypoint automation can set up to three consecutive train or shunt routes for the same verified consist. It matches each route against the directed native path and checks the approach for other vehicles. All route, point, flank and occupancy checks continue to apply. A route conflict stops the lookahead at the affected signal.

Automatic movement selection uses the current signal and independent waypoint selection. A local shunt stays local. A direct train departure across an interlocking boundary needs a configured line between the selected departure and arrival signals. Its direction permission and block checks still apply. The test build does not repair a damaged line catalogue.

When the native waypoint contains a direction change, the planner reads its current leg separately. At the end of a verified turnback leg, the host waits until the whole consist has stopped beyond the points before releasing the occupied destination. Train coupling orders remain outside this workflow.

The full build and offline suite passed 2,124 checks against Railroader 0.6.3, including guarded advance reservations, foreign and unreadable occupancy, and turnout clearance at a stopped turnback. In-game tests are pending. Start with HS-044, then check three clear consecutive signals in both movements, a blocked middle route, a shunt via the sawmill with a long consist and a valid WHB–ELA departure from HS-006.

## World signal controls, waypoint requests and GFM

- The normal world interaction opens a route request for a finished main or shunt signal. The host chooses the movement where needed and a reachable destination. The existing dispatcher and interlocking check the request. The object context menu provides inspection and the permission-checked build editor. Remote players see the signal state through the existing replication.
- The launcher gear offers one world setting for automatic waypoint requests, which activates the host signal connection and block coordination. The standard map picker creates an oriented waypoint in Railroader 0.6.3. The reader accepts this format when the game's own path proves one continuous direction. Orders request one eligible local leg at a time. The native locomotive Signalling tab shows the current reason and offers movement selection, pause and a single request. Gameplay diagnostics log state changes, including failures before an actual request.
- The background catalog uses physical detector cuts even when ordinary signals split the drawn track. Automatic fields prefer a unique physical section. Existing explicit diagram bindings remain authoritative. The inspection shows the exact uncovered portions of a test path.
- The next in-game tests are a normal fixed map waypoint through a shunt-capable location, a protected point initially in the other position, a manual signal request, a GFM join near a point and host/client visibility. Remote driver requests, interlocking handover and waypoint orders involving coupling or reversals are future work.

The full build and offline suite passed **2,113 checks with zero failures** against local Railroader 0.6.3 assemblies. The checks include native map-waypoint values, path reachability, locomotive tab callbacks, physical track detection and Runenwacht 0.2.2 registration against this build. The game reached its title screen. This build still needs installation through the mod manager and acceptance in a world.

[English instructions](../mod/EnusDE-Signaltechnik/Guides/WAYPOINT-0.7.66.en.md) · [Deutsche Anleitung](../mod/EnusDE-Signaltechnik/Guides/WAYPOINT-0.7.66.de.md).

## Runenwacht 0.2.2 compatibility update

Runenwacht requires EnusDE Signal & Interlocking **0.7.64 or later** with a compatible presentation API v1. The minimum supplies the signal-code field used by the models. The game checks the required dependency and loads the main mod first. The presentation registry checks compatibility. A registration failure is logged and the main mod can display saved signals with its built-in models. Update Runenwacht once and re-enable it in Mods if Railroader disabled it after the earlier version mismatch.

The full suite completed with **2,102 checks and zero failures**. Railroader's native dependency parser and load ordering were checked against missing, older and later main versions. The same Runenwacht 0.2.2 DLL registered offline with main 0.7.64 and 0.7.65. The main 0.7.65 DLL matches the previously delivered ZIP byte for byte. One optional pack ZIP is prepared. [Runenwacht guide](../asset-packs/Runenwacht/README.md).

## Waypoint automation in 0.7.65

The locomotive panel enables, pauses or requests a single local Train or Shunt route for a fixed Auto Engineer waypoint. It uses the current directed native path and leading consist end. Preparation begins within 300 track metres. Admission within 25 metres requires a positively identified train, complete track detection, a clear destination, verified point protection, standard locking and a free route. Active conflicting routes are left to ordinary dispatcher handling. The normal route engine continues monitoring a cleared route when the driver takes over. The host can withdraw requests from the dispatcher status panel and clients can read the replicated request status.

Local interlocking only in this test build. Coupling orders, reversals, interlocking handover between players and automatic cross-station services remain follow-up work. Unknown paths or signal ambiguity keep requests waiting. Native route availability behind a protected point in the wrong position, actual driving and multiplayer need game verification. The optional Runenwacht 0.2.1 package pins the updated main dependency. [English instructions](../mod/EnusDE-Signaltechnik/Guides/WAYPOINT-0.7.65.en.md) · [Deutsche Anleitung](../mod/EnusDE-Signaltechnik/Guides/WAYPOINT-0.7.65.de.md).

The complete Build.ps1/Test.ps1 run on 24 November 2026 passed **2,097 checks, 0 failures** with local Railroader 0.6.3 libraries. Twenty-seven new checks cover approach selection, directed route matching and guarded interlocking admission. No game session was launched. The train and shunt UI, real route availability and multiplayer still need acceptance.

## Earlier Runenwacht checkpoints

Optional-pack checkpoint **Runenwacht 0.2.1**, main remains **0.7.64**. User game feedback confirms opaque rear housing but identifies weak distant light, bright inactive crystals and disconnected hardware. Pack-only changes address these with dark non-emitting resting surfaces, camera-distance gain on the selected full crystal body and fine detail near the camera, physical plaque supports and routed insulated connections. No native/route/UI/main-source changes. Full Test.ps1: **2,070 PASS, 0 FAIL**; main DLL hash byte-identical before/after. Numeric/geometry checks are offline; new near/far day/night readability, rear occlusion and attachment views need user game acceptance. [English](../asset-packs/Runenwacht/CHANGES-0.2.1.en.md) · [Deutsch](../asset-packs/Runenwacht/CHANGES-0.2.1.de.md).

**Nur Runenwacht auf 0.2.1 aktualisieren; Hauptmod 0.7.64 bleibt unverändert.** 2.070 Prüfungen bestanden. Inaktive Kristalle dunkler, aktive Körper aus der Entfernung stärker sichtbar, Schildhalter und Rohranschlüsse verbunden. Diese neue Fassung muss noch bei Tag/Nacht und seitlich im Spiel geprüft werden.

Main 0.7.64 / Runenwacht 0.2.0 / Railroader 0.6.3, Early Alpha. [English workflow](../mod/EnusDE-Signaltechnik/Guides/RUNENWACHT-0.7.64.en.md) · [Deutsche Anleitung](../mod/EnusDE-Signaltechnik/Guides/RUNENWACHT-0.7.64.de.md).

**23 November 2026: full Test.ps1 completed with 2,024 PASS, 0 FAIL.** Main (285 production C# files) and separate pack (7 C# files) compile against the available local original 0.6.3 game libraries. Checks now include detailed production geometry/UV exports, bevelled solids, conduits/crescent, fixed body/edge/vein channels, centred stop crystal, separate technical code/name, active-pipeline shader choice and temporary lamp-test authorization, stale requests, held directions, route roles, head combinations, expiry and cleanup. Existing route/automation/native materials/settings/localization checks remain part of the same suite. No GPU/gameplay or full native AI/network execution is claimed from these checks.

User screenshots of 0.1.0 establish that the original look was too coarse, dark and flat compared with the concept. Updated procedural models, shared PBR resources and framed plates address those reported issues. An offline Blender study uses the exported production geometry with independent studio lighting/materials; it is not Railroader and does not prove matching game exposure. The user has not yet tested this new build. Keep the explicit game acceptance checklist open: day/night/oblique appearance, plates, all head states and test controls, native Stop/route refusal, restoration on every exit path, camera/proxy placement, memory/frame time, clients and late joins. No LOD or custom wagon/audio/streaming content is added.

The host lamp test is a local visual override in the Build editor. Its permissive indication never enters the operating API or shared live frame. Native proxies use Stop and relevant route admission is blocked; disabled native AI integration is not enabled by a lamp test. Existing route/direction locks prevent test start. Both directions are held while only the selected face/head displays a test. It resets on selection/Build/timeout/world/data/authority transitions and is never persisted. The normal operating invalid/Kennlicht state returns afterwards.

## Deutsch – aktueller Stand

0.7.64 / Runenwacht 0.2.0: Formen und Materialien anhand der gemeldeten Spielbilder überarbeitet, echte Kennungs-/Namensschilder ergänzt. Einzelne vorhandene Köpfe im Host-Baumodus ohne Fahrstraße testen; native Ausgabe bleibt Halt und betroffene Fahrstraßen sind gesperrt. Ende spätestens nach zwei Minuten beziehungsweise beim Verlassen/Wechsel. Keine gespeicherten Teststellungen. Vollständiger Lauf am 23.11.2026: **2.024 PASS, 0 FAIL**, beide Assemblies gegen lokale originale 0.6.3-Bibliotheken gebaut. Blender-Ansichten sind externe Modellstudien. Das neue Ergebnis muss noch im Spiel bei Tag/Nacht sowie mit KI und im Mehrspieler geprüft werden. Keine Installation oder Spielstarts durch den Assistenten. Nachfolgend erhaltene ältere Prüfstände.

---

# Previous checkpoint — 0.7.63

[Deutsch](#aktueller-stand-deutsch) · [Pack reference](../asset-packs/Runenwacht/README.md) · [English game checklist](../mod/EnusDE-Signaltechnik/Guides/RUNENWACHT-0.7.63.en.md)

## Current checkpoint

Main **0.7.63**, Runenwacht **0.1.0**, Railroader **0.6.3**, Early Alpha. `main` remains canonical. Optional presentation API v1 sends immutable resolved head states to a separately compiled mod; original interlocking/AI/route/material authority stays in the main mod. Runenwacht generates detailed original meshes with opaque directional housings, fixed-colour crystals, exclusive animated ignition/extinction and local brightness/label controls. Creation/update failures quarantine the renderer and restore visible built-in signals while keeping saved IDs. Model choice and per-world free test licenses persist; session activation defaults OFF and new selections are centrally host/licence/provider guarded.

The **22 November 2026 full Test.ps1 run passed 1,930 PASS, 0 FAIL**. Both production assemblies compile against the available original local game libraries; player metadata identifies Railroader 0.6.3. An earlier original-library availability gap was closed before this task in the 0.7.62 baseline. The new checks cover registration/unload/version mismatch, immutable snapshots, fixed channels, exclusive transitions including immediate restrictive changes, bounded visual controls, saved licenses/model selection, legacy documents, central placement policy, Relay commands and manifest dependencies. Eleven geometry checks compile the actual procedural geometry against controlled math/mesh stand-ins, checking closed caps/crystals, ring walls, material indices and finite triangles. This is not a Unity renderer test.

Existing complete suite includes native material/API, construction, route, block, settings and English/German checks. Both language catalogs and generated fallback were updated. Old archives are preserved; separately named test packages contain only their own mod assembly/docs/notices. No runtime installation, game launch, tag or public release was performed. Source commits are not release assets.

## Continue here

Use the Runenwacht English/German guide's acceptance checklist next: front/rear/oblique day/night visibility, combined head states, shader/emission behaviour, pick/ground clearances, native construction/material completion, save/reload/move/conversion, missing-pack fallback, reload cleanup, frame time and host/client/late-join behaviour. All remain unverified in the running game. No automatic asset streaming, paid license purchase, wagon load replacements, audio/video, baked textures, LODs or new train automation is included. Public provider API is an architectural contract for installed trusted mods, not a security sandbox.

## Aktueller Stand Deutsch

**Hauptmod 0.7.63 / Runenwacht 0.1.0 / Railroader 0.6.3.** Optionales getrenntes Darstellungspaket mit eigenen 3D-Modellen, festen Kristallfarben, Lichtanimationen und Blenden. Hauptmod behält Betriebslogik und IDs. Welt-Testlizenz kostenlos und gespeichert; Aktivierung nach jedem Laden AUS, Freischaltung nur als Host. Fehlende oder defekte Pakete erhalten sichtbare eingebaute Ersatzsignale. Vollständiger Testlauf am 22.11.2026: **1.930 PASS, 0 FAIL**, beide Assemblies gebaut, beide Sprachkataloge geprüft. Elf Geometrieprüfungen verwenden kontrollierte Stellvertreter; kein Unity-Bildtest. Die ursprüngliche Bibliothekslücke war bereits im übernommenen 0.7.62-Stand geschlossen.

Noch offen: tatsächliche Sichtbarkeit/Shader, alle Signalbilder, Boden-/Klickflächen, Materialablauf, Speichern/Verschieben/Umrüsten, Paketverlust, Ressourcen und Leistung sowie Mehrspielerbetrieb im Spiel. Keine Installation und kein Spielstart erfolgt. [Deutsche Testanleitung](../mod/EnusDE-Signaltechnik/Guides/RUNENWACHT-0.7.63.de.md). Folgendes dokumentiert den unverändert erhaltenen vorherigen Stand.

---

# Previous checkpoint — 0.7.62

[Deutsch](#deutsch) · [Project overview](../README.md) · [Build instructions](BUILD.md)

## Earlier change

`main` is the canonical source. Version **0.7.62** fixes missing native interchange suppliers: a copied interchange IndustryDTO gains the two stable signal cargos before the native factory registers component IDs and schedules. Buy destinations, price/payment, handling, off-map service and return remain native. Existing supplier entries and cached map data remain unchanged. Ambiguous/empty interchange definitions are skipped. See [English buying/model guide](../mod/EnusDE-Signaltechnik/Guides/INTERCHANGE-0.7.62.en.md) or [Deutsch](../mod/EnusDE-Signaltechnik/Guides/INTERCHANGE-0.7.62.de.md).

Previous version **0.7.61**, Early Alpha for **Railroader 0.6.3**, adds explicit local Build sessions and native MOW material logistics. The compact right-hand icon palette stays open during placement. End/X/workspace switch cancels unconfirmed cursor work, retaining accepted plans. The local builder sees virtual models; ordinary viewers see original cone/barrier worksites for confirmed unfinished objects. Completed models and host editing restrictions remain.

Both stable signal cargo IDs now use native captive-wagon ordering and matched existing MOW material loader/unloader pairs. Native inventory, prices and wagon capacity remain native. FL and legacy FM carry building supplies; XM carries equipment. Active MOW work-train completion consumes these cargos from coupled owned supply cars. Material overview and milestone show pending requirements/status. Legacy balances are retained with explicit capacity-limited transfer, rollback and saved receipts against duplicate restoration. Point wiring retains its one-time nearby-stock charge and is not yet work-car-driven.

Settings remain in the launcher gear introduced in 0.7.60. No object/route/cargo identifier migration or automatic stock transfer occurs. Native inventory belongs to the full game save, not a configuration-only backup. Optional diagnostics remain separate and passive.

See [English workflow and acceptance steps](../mod/EnusDE-Signaltechnik/Guides/BUILD-MOW-0.7.61.en.md) and [German workflow](../mod/EnusDE-Signaltechnik/Guides/BUILD-MOW-0.7.61.de.md).

## Checks and limits

The full **Test.ps1** run on **20 November 2026** passed **1,839 PASS, 0 FAIL**. Production compiles against the local original game libraries; Unity player metadata identifies **0.6.3**. Results include:

- 11 additional production import-preparation/prefix checks, using actual game DTO types, cover unchanged original data, native tracks/filters/accounting, existing suppliers, repetition, key collisions and ambiguous/empty interchanges.
- 22 checks compiling the actual native-material adapter, transfer and point-payment code against controlled stand-ins: registration, targeted wagon filters, real-unit capacity, no duplicate stock, host checks, fractional/partial transfer, rollback, receipts, point deduplication and ownership cleanup.
- Seven local build policy, site geometry and distinct icon checks, plus five installed-game material API/definition checks.
- Existing production UI callbacks, settings lifecycle, localization, route/occupancy/network and IL/API checks. The console has one fewer PASS line than RESULTS.txt because one package assertion is recorded only in the latter.

The real map definitions were read: Bryson and East Whittier have paired MOW material storage components. The real storage-service IL multiplies carload storage/rates by each load's nominal quantity. This is static evidence, not successful delivery in a running game. Fixture transactions use controlled stand-ins; they do not prove Unity or network rollback.

The user confirmed the previous build panel and compatible wagon selection work sufficiently for further testing. This is not a completed purchase/delivery test. Native FM volume/wildcard-crate definitions, FL skeleton log-specific model and closed XM exterior were inspected read-only; no dedicated cargo textures were added.

No game has been launched or installation performed by the assistant. New ordering/payment, warehouse loading/unloading, active work-train completion, terrain/transparent rendering, repeated mode switches, save/reload, late join and host/client visibility need copied-save acceptance tests. No blanket performance or multiplayer acceptance is claimed. Package CRC, manifest, own-DLL-only content, both language catalogs and byte comparison to the tested runtime/template are checked separately before delivery. Earlier 0.7.60 packages remain unchanged; that historical complete run recorded 1,794 PASS, 0 FAIL.

## Continue work

Read [AGENTS.md](../AGENTS.md), inspect live remote and pending local work, and preserve concurrent changes. After requested work, run appropriate checks, explicitly stage intended source/tests/public guides, commit and upload, then verify remote. Routine source uploads are authorized; releases and tags are separate actions. Never force-push or silently overwrite another working copy.

Game libraries, binaries/packages, live-world data, settings, diagnoses/logs and private notes stay outside Git. Installation is manual through the user's mod manager; do not write game/Vortex/preferences files or start the game/Steam. Keep existing archives. This is a task-checkpoint workflow, not continuous background synchronization.

## Deutsch

**0.7.62 für Railroader 0.6.3, Early-Alpha-Testfassung.** Fehlende native Interchange-Lieferanten für den Materialkauf ergänzt. Originale Karten-/Lieferantendaten bleiben erhalten; normale Kaufen-/Bezahl-/Bedien-/Rücklieferungslogik bleibt zuständig. Ausdrücklicher Baumodus mit rechter Symbolleiste und Beenden-Taste; bestätigte Pläne bleiben erhalten. Der lokale Erbauer sieht virtuelle Modelle, andere normale Ansichten zeigen Baustellen. Native Materialwagenbestellung, MOW-Lager und aktive MOW-Arbeitszugfertigstellung sind verbunden. Altbestände werden ausdrücklich und kapazitätsbegrenzt übernommen; Restbestände bleiben. Weichenverdrahtung verwendet weiterhin einmalig nahe Lagerbestände, noch keinen Arbeitswagen an der Weiche.

**1.839 PASS, 0 FAIL** im vollständigen Test.ps1-Lauf am 20.11.2026 gegen lokale Spielbibliotheken mit 0.6.3-Player-Metadaten. Darunter elf neue produktive Lieferanten-/Prefixprüfungen mit echten Spiel-DTOs, 22 produktive Materialadapter-/Übernahmeprüfungen mit kontrollierten Spiel-Stellvertretern, sieben Modus-/Geometrie-/Symbolprüfungen und fünf native Material-API-Prüfungen. Statische Prüfung echter MOW-Lager und Speicher-IL ersetzt keine Lieferung im Spiel.

Spiel-/Darstellungs-/Liefer-/Mehrspielerabnahme bleibt offen; kein Spielstart und keine Installation durch den Assistenten. Die Anleitung enthält konkrete Tests mit kopiertem Spielstand. Native Lagerbestände benötigen eine passende vollständige Spielstandsicherung. Einstellungen bleiben am Zahnrad. Alte Pakete unverändert behalten; abgeschlossene Quellen auf GitHub sichern und den Remote-Commit prüfen.
