# Development status and continuation — 0.7.61

[Deutsch](#deutsch) · [Project overview](../README.md) · [Build instructions](BUILD.md)

## Current change

`main` is the canonical source. Version **0.7.61**, Early Alpha for **Railroader 0.6.3**, adds explicit local Build sessions and native MOW material logistics. The compact right-hand icon palette stays open during placement. End/X/workspace switch cancels unconfirmed cursor work, retaining accepted plans. The local builder sees virtual models; ordinary viewers see original cone/barrier worksites for confirmed unfinished objects. Completed models and host editing restrictions remain.

Both stable signal cargo IDs now use native captive-wagon ordering and matched existing MOW material loader/unloader pairs. Native inventory, prices and wagon capacity remain native. FL and legacy FM carry building supplies; XM carries equipment. Active MOW work-train completion consumes these cargos from coupled owned supply cars. Material overview and milestone show pending requirements/status. Legacy balances are retained with explicit capacity-limited transfer, rollback and saved receipts against duplicate restoration. Point wiring retains its one-time nearby-stock charge and is not yet work-car-driven.

Settings remain in the launcher gear introduced in 0.7.60. No object/route/cargo identifier migration or automatic stock transfer occurs. Native inventory belongs to the full game save, not a configuration-only backup. Optional diagnostics remain separate and passive.

See [English workflow and acceptance steps](../mod/EnusDE-Signaltechnik/Guides/BUILD-MOW-0.7.61.en.md) and [German workflow](../mod/EnusDE-Signaltechnik/Guides/BUILD-MOW-0.7.61.de.md).

## Checks and limits

The full **Test.ps1** run on **20 November 2026** passed **1,828 PASS, 0 FAIL**. Production compiles against the local original game libraries; Unity player metadata identifies **0.6.3**. Results include:

- 22 checks compiling the actual native-material adapter, transfer and point-payment code against controlled stand-ins: registration, targeted wagon filters, real-unit capacity, no duplicate stock, host checks, fractional/partial transfer, rollback, receipts, point deduplication and ownership cleanup.
- Seven local build policy, site geometry and distinct icon checks, plus five installed-game material API/definition checks.
- Existing production UI callbacks, settings lifecycle, localization, route/occupancy/network and IL/API checks. The console has one fewer PASS line than RESULTS.txt because one package assertion is recorded only in the latter.

The real map definitions were read: Bryson and East Whittier have paired MOW material storage components. The real storage-service IL multiplies carload storage/rates by each load's nominal quantity. This is static evidence, not successful delivery in a running game. Fixture transactions use controlled stand-ins; they do not prove Unity or network rollback.

No game has been launched or installation performed. New ordering/payment, warehouse loading/unloading, active work-train completion, terrain/transparent rendering, repeated mode switches, save/reload, late join and host/client visibility need copied-save acceptance tests. No blanket performance or multiplayer acceptance is claimed. Package CRC, manifest, own-DLL-only content, both language catalogs and byte comparison to the tested runtime/template are checked separately before delivery. Earlier 0.7.60 packages remain unchanged; that historical complete run recorded 1,794 PASS, 0 FAIL.

## Continue work

Read [AGENTS.md](../AGENTS.md), inspect live remote and pending local work, and preserve concurrent changes. After requested work, run appropriate checks, explicitly stage intended source/tests/public guides, commit and upload, then verify remote. Routine source uploads are authorized; releases and tags are separate actions. Never force-push or silently overwrite another working copy.

Game libraries, binaries/packages, live-world data, settings, diagnoses/logs and private notes stay outside Git. Installation is manual through the user's mod manager; do not write game/Vortex/preferences files or start the game/Steam. Keep existing archives. This is a task-checkpoint workflow, not continuous background synchronization.

## Deutsch

**0.7.61 für Railroader 0.6.3, Early-Alpha-Testfassung.** Ausdrücklicher Baumodus mit rechter Symbolleiste und Beenden-Taste; bestätigte Pläne bleiben erhalten. Der lokale Erbauer sieht virtuelle Modelle, andere normale Ansichten zeigen Baustellen. Native Materialwagenbestellung, MOW-Lager und aktive MOW-Arbeitszugfertigstellung sind verbunden. Altbestände werden ausdrücklich und kapazitätsbegrenzt übernommen; Restbestände bleiben. Weichenverdrahtung verwendet weiterhin einmalig nahe Lagerbestände, noch keinen Arbeitswagen an der Weiche.

**1.828 PASS, 0 FAIL** im vollständigen Test.ps1-Lauf am 20.11.2026 gegen lokale Spielbibliotheken mit 0.6.3-Player-Metadaten. Darunter 22 produktive Materialadapter-/Übernahmeprüfungen mit kontrollierten Spiel-Stellvertretern, sieben Modus-/Geometrie-/Symbolprüfungen und fünf native Material-API-Prüfungen. Statische Prüfung echter MOW-Lager und Speicher-IL ersetzt keine Lieferung im Spiel.

Spiel-/Darstellungs-/Liefer-/Mehrspielerabnahme bleibt offen; kein Spielstart und keine Installation durch den Assistenten. Die Anleitung enthält konkrete Tests mit kopiertem Spielstand. Native Lagerbestände benötigen eine passende vollständige Spielstandsicherung. Einstellungen bleiben am Zahnrad. Alte Pakete unverändert behalten; abgeschlossene Quellen auf GitHub sichern und den Remote-Commit prüfen.
