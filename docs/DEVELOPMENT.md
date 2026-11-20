# Development status and continuation — 0.7.60

[Deutsch](#deutsch) · [Project overview](../README.md) · [Build instructions](BUILD.md)

## Current change

`main` is the canonical source. Version **0.7.60**, Early Alpha for **Railroader 0.6.3**, moves all mod settings into an independent window opened by the launcher gear. The three existing workspace icons remain. Settings stay reachable before milestone unlock and for clients; shared-settings and operating permissions retain their host checks.

The former Company mod-list replacement and preferences-tab registrations are removed. The launcher cannot hide its only settings entrance through the former visibility option. Existing local settings and world/installation storage keep their paths and formats. No object, route, diagram, link or save migration is performed.

See [English instructions](../mod/EnusDE-Signaltechnik/Guides/SETTINGS-0.7.60.en.md) and [German instructions](../mod/EnusDE-Signaltechnik/Guides/SETTINGS-0.7.60.de.md).

## Checks and limits

The current production code compiles against the locally installed original game libraries. The installation's Unity player metadata identifies **0.6.3**. Production window/picker callbacks have passed 18 focused offline checks, including access before milestone unlock, client access, unchanged workspace restrictions, repeated open/close, pending edits, manager replacement, unload and ownership cleanup.

The first complete check attempt with the actual local 0.6.3 libraries exposed a pre-existing vehicle-tab hook assumption: the callback is now generated from BuildContent rather than PopulatePanel. The resolver now accepts these two known owners with the exact UITabbedPanelBuilder/void signature, requires one unique match, and rejects unknown/ambiguous targets. Three targeted regression checks cover both contracts and rejection. Vehicle controls and automation commands are unchanged.

The complete Build.ps1/Test.ps1 run on **20 November 2026** passed: **1,794 PASS, 0 FAIL**, including the 18 standalone picker/window checks, three callback-resolution compatibility checks, localization, production callback/IL and native game API checks. The console has 1,793 PASS lines because one package assertion is recorded only in RESULTS.txt. Package integrity and byte comparison against the staged runtime files are checked separately before delivery. The previous historical full run for 0.7.59 on 19 November 2026 recorded 1,792 PASS, 0 FAIL using the then available older libraries.

No Unity/game session has been started for this change. Complete in-game/UI and multiplayer acceptance remains open. In particular, verify the visible gear/layout, language changes after Apply & Save, the native Company mod list, host/client settings restrictions and repeated world load/unload. Offline UI stand-ins do not prove the final Unity rendering or network behavior.

## Continue work

Read [AGENTS.md](../AGENTS.md), inspect the live remote and local pending work, and preserve any concurrent changes. After requested work, run the appropriate checks, explicitly stage the intended source/tests/public guides, commit and upload, then verify the remote commit. Routine source uploads are authorized. Never force-push or silently overwrite another working copy.

Game libraries, binaries/packages, live-world data, settings, diagnoses/logs and private notes stay outside Git. Runtime installation, releases, tags and game launches remain separately requested actions. Preserve previously prepared archives. This is a task-checkpoint workflow, not a continuously running sync service.

## Deutsch

**0.7.60 für Railroader 0.6.3, weiterhin Early Alpha.** Einstellungen über das Zahnrad im Auswahlfenster an der Uhrleiste; eigenes Fenster, auch vor der Meilensteinfreischaltung und für Mitspieler. Die bisherigen Eingriffe in Company-Modliste und Spieloptionen entfallen. Gespeicherte Einstellungen und Anlagen bleiben erhalten; Host-Rechte werden nicht erweitert.

Der Code kompiliert gegen die lokalen originalen Spielbibliotheken; die Unity-Player-Metadaten nennen 0.6.3. 18 gezielte Offline-Prüfungen der produktiven Fenster-/Tastenrückrufe sind bestanden. Der vollständige Build.ps1/Test.ps1-Lauf vom **20.11.2026** ist mit **1.794 PASS, 0 FAIL** bestanden, einschließlich Sprachkatalogen, produktiven Rückrufen und nativen API-Prüfungen. Zusätzlich wird das Installationsarchiv auf Integrität und bytegenaue Übereinstimmung mit den Laufzeitdateien geprüft. Die Fahrzeugreiter-Erkennung wurde für den umbenannten 0.6.3-Rückruf korrigiert; unbekannte oder mehrdeutige Treffer bleiben abgewiesen. Die frühere vollständige 0.7.59-Prüfung vom 19.11.2026 ergab 1.792 PASS, 0 FAIL und nutzte damals ältere Bibliotheken.

Spiel-/Darstellungs-/Mehrspielerabnahme bleiben offen. Kein Spielstart und keine Installation durch den Assistenten. Nach abgeschlossenen Aufgaben ausgewählten Quellcode auf GitHub sichern und den Remote-Commit bestätigen; ungesicherte Änderungen offen melden.
