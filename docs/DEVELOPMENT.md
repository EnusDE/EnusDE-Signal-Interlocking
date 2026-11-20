# Development status and continuation — 0.7.59

[Deutsch](#deutsch) · [Project overview](../README.md) · [Build instructions](BUILD.md)

## Source checkpoint

`main` in `EnusDE/EnusDE-Signal-Interlocking` is the canonical current source. The manifest and `Entry.Version` are **0.7.59**, with the exact dependency **Railroader 0.6.3**. This is an Early Alpha source checkpoint; creating a source commit does not create a release, tag, binary package or runtime installation.

The checkpoint includes all 273 production C# files, build/check tooling and synthetic test fixtures, both language catalogs and generated English fallback code, mod definitions/Relay script, component notices and public guides. Historical versioned notes retain the status recorded when written. Private discussion drafts, handoffs, live-world data, local dependencies and build outputs are excluded.

## Last recorded executable checks

The full `Test.ps1` run completed on **19 November 2026, 13:45:58 UTC** (14:45:58 Europe/Berlin). Its saved report records exit code 0. `build/checks/RESULTS.txt` contains **1,792 PASS and 0 FAIL**. The console log contains 1,791 PASS lines because one manifest/package check is collected in the results file without being printed.

This run includes **26 production-window callback checks** and **three serialize/reload/reopen cycles**, including the four-section, fourth-section-selected, 180-degree-rotated block-view case. The build's compiler response lists all 273 current production files; those files were present and no newer production sources were found during the source audit.

The existing install archives with and without `TEST` in their names were separately checked for ZIP integrity and identical DLL content. Their shared SHA256 is:

```text
4474baa4a977d6a2accd9a1ba745ba2e402f6d1a61a9640b0824edef9e8bd2bb
```

The compiled mod DLL SHA256 is:

```text
53450e2b2ab30b1e2f73af2f6b72006ba0bf7427daef1b9e5832cb46c998abb1
```

These are historical local verification results, not a claim that a new test run or GitHub CI occurred during source publication. Raw logs, DLLs and packages stay outside Git. Source-checkpoint preparation changes documentation/ignore rules only; production sources, test code, language packs and version numbers remain byte-for-byte unchanged. Unchanged large runtime suites need not be rerun solely for this documentation/Git checkpoint.

## Open validation

- Local compilation used the available older game libraries and the adapter for `Game.HomeDirectory.UserPaths` (with the legacy fallback). The new API contract is covered by simulated checks. **Compilation against verified original Railroader 0.6.3 libraries remains to be recorded.**
- The author has reported working in-game operation, including GWB. That report does not establish complete acceptance of every UI interaction, map, occupancy/reload case or multiplayer combination.
- Full in-game/UI and multiplayer regression checks remain open. Use save copies and suitable game libraries for future acceptance work; never redistribute those libraries.
- Tutorials and manuals remain in progress. Historical guides may contain older game versions and earlier pending-test statements.

## Continue work safely

Read [AGENTS.md](../AGENTS.md), inspect the live remote `main` and local pending work, then make only the requested change. Do not apply an older checkout over this checkpoint. Review any concurrent remote changes before integrating; never force-push or resolve conflicts by blindly choosing one side.

After a completed requested change, run appropriate checks, explicitly stage and inspect the intended source/tests/public documentation, commit and upload, then verify the remote commit. Routine source uploads are authorized by the owner. A clearly marked intermediate checkpoint is permitted, with honest test and validation status. Report any failed or unconfirmed upload as unsaved remotely.

This is an agent workflow at task checkpoints, not a continuously running sync service. Releases, tags, installation and game launches require separate requests. Preserve existing prepared packages unchanged. Select the next development task with the owner; this checkpoint does not authorize additional runtime features.

## Deutsch

`main` ist der aktuelle maßgebliche Quellstand **0.7.59 für Railroader 0.6.3**, weiterhin Early Alpha. Quellcode-Sicherung und Veröffentlichung eines installierbaren Releases sind getrennte Schritte.

Die letzte belegte vollständige Prüfung vom 19.11.2026 ergab **1.792 PASS, 0 FAIL**, einschließlich 26 Fenster-Callback-Prüfungen und drei Speicher-/Lade-/Wiederöffnungszyklen. Diese Ergebnisse stammen vom vorhandenen Lauf; bei der Quellcode-Sicherung wird kein neuer großer Testlauf behauptet. Produktionsquellen, Testcode, Sprachpakete und Versionsnummern bleiben bytegenau unverändert.

Der lokale Build verwendete ältere Spielbibliotheken mit 0.6.3-Pfadadapter. Ein nachgewiesener Original-0.6.3-Build sowie vollständige Spiel-/UI-/Mehrspielertests stehen noch aus. Gemeldeter funktionierender Spielbetrieb ist keine vollständige Abnahme aller Kombinationen.

Vor weiterer Arbeit Live-Remote und lokale Änderungen prüfen. Nach abgeschlossenen beauftragten Schritten passende Prüfungen ausführen, ausgewählten Quellcode committen/übertragen und den Remote-Commit bestätigen. Bei Fehlern offen berichten. Keine Force-Pushs, keine blinde Konfliktauflösung und keine dauerhaft laufende Hintergrundsynchronisierung. Releases, Tags, Installation und Spielstart bleiben gesondert beauftragt. Private Notizen, Spiel-/Anlagendaten, Bibliotheken, Pakete und Protokolle bleiben lokal.
