# Development status and continuation — 0.7.64

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

## Current change

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
