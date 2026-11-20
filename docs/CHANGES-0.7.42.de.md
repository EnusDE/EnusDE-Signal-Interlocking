# 0.7.42-P0 — Fahrstraßen schrittweise vorbereiten

- Manuelle Start-Ziel-Anforderungen, gespeicherte Fahrstraßen/Folgen und vorhandene Kontakt-Automatik verwenden eine begrenzte FIFO-Warteschlange statt vollständiger Berechnung im Klick oder Kontaktaufruf.
- Verarbeitung pro Frame mit weichem 2-ms-Zeitbudget und höchstens 512 kleinen Schritten. Zwischen nativen Schutzberechnungen einzelner Teilfahrstraßen wird abgegeben. Einzelne native Aufrufe und die abschließende atomare Reservierung können dieses Budget überschreiten; keine harte Framedauer-Garantie.
- Reine Wegsuche auf getrennter Datenmomentaufnahme im Hintergrund. Unity-, Gleisnetz- und Stellbefehle bleiben im Hauptthread. Jeweils nur eine Vorbereitung aktiv.
- Fortschritt und Anzahl wartender Anforderungen, Zusammenführung gleicher Anforderungen, Grenze von 32 sowie Abbruch. HALT, Spielstandwechsel und Wiederherstellung verwerfen ausstehende Arbeit; vorhandene Verschlüsse bleiben bestehen.
- Konfigurations-/Anlagenänderungen verwerfen veraltete Vorbereitung. Das native Gleisnetz wird erneut aufgenommen und verglichen. Fahrwege und aktuelle Belegung werden vor dem Stellen erneut geprüft.
- Die gesamte Folge wird weiterhin gemeinsam reserviert oder zurückgerollt. Während der Suche keine Teilfestlegung; GFM, Weichen-, Flanken-, Block- und GWB-Prüfungen sowie bestehende Aktivgrenzen bleiben erhalten.
- Kontakt-Automatik verwendet vorbereitete Fahrwege erst nach erneuter Prüfung von Zug, Verband, Auftrag, Richtung und Betriebszustand; höchstens eine vorbereitete Kontakt-Anforderung je Automatiktakt. Wiederholung und Ausweichziel bleiben eingerichtet.
- Flankenschutz-Gleisnetzzuordnung mit linearem Aufbau statt vollständiger Netzsuche für jeden Knoten. Wiederkehrende Vorschlagsteile werden innerhalb einer Vorbereitung wiederverwendet.
- Englische/deutsche Warteschlangenhilfe; veraltete Hilfstasten-/GWB-Angaben in F1 korrigiert. Kommunikation und allgemeiner Automatik-Ausbau bleiben Konzepte.

Konkreter Spielhänger/Beinaheabsturz, Framedauer und Mehrspieler sind noch im tatsächlichen Spielstand abzunehmen. Synthetische Messungen sind kein Spiel-FPS-Nachweis.
