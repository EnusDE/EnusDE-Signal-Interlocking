# Kartenabhängige Freischaltung – 0.7.31-P0

## Standardverhalten

Auf einer Karte mit dem nativen Signal-Meilenstein **Whittier–Bryson** (interne Kennung `sctc`) übernimmt der eigene Meilenstein **Signaltechnik – Ausbau** dessen Voraussetzungen. Damit wird unser Ausbau zum selben Fortschrittspunkt kaufbar wie der native Signalausbau, auf der Standardkarte also nach den entsprechenden Ela-Brücken-/Streckenphasen. Die tatsächliche Liste wird aus der geladenen Karte übernommen; eine feste Brücken-ID oder ein übersetzter Anzeigename wird nicht vorausgesetzt.

Der native CTC-Ausbau muss **nicht gekauft werden**. Der eigene Signaltechnik-Ausbau hat weiterhin einen Grundpreis von **7.500 $**; die normale Rabattberechnung des Spiels bleibt zuständig. Eine bereits vorhandene Freischaltung wird nicht nochmals berechnet.

Fehlt der native `sctc`-Meilenstein, ist die Signaltechnik standardmäßig sofort und ohne Freischaltkauf verfügbar. Es wird die geladene Fortschrittsstruktur geprüft, nicht die Anzahl installierter Mods. Eine Fahrzeugmod verändert diese Regel nicht. Eine Kartenerweiterung, welche den nativen Signalausbau beibehält, behält zunächst auch dessen Voraussetzungen.

Der Sandkasten bleibt sofort verfügbar. Bereits freigeschaltete Spielstände bleiben freigeschaltet. Eine nachträglich strengere Karten- oder Host-Vorgabe zieht einen früheren Kauf nicht zurück. Die vorhandene Materialwirtschaft bleibt davon getrennt: Eine kostenlose Freischaltung bedeutet im Wirtschaftsmodus nicht automatisch kostenloses Baumaterial.

Bis zur Freischaltung sind **Baumodus, Editor und Fahrdienstleiter** gesperrt. Der STW-/IXL-Schalter erklärt die Meilensteinvoraussetzung. Zusätzlich prüfen die Schreibzugriffe für Gleisbilder und Fahrstraßen die Freischaltung. Einstellungen, Handbuch und externe Sicherungen bleiben erreichbar.

## Host-Einstellungen

**Company → Optionen → Mods → Signaltechnik → Material und Baukosten** zeigt Herkunft, Grundpreis und benötigte Meilensteine.

Der Host kann dort auswählen:

- **Kartenvorgabe verwenden:** automatische Erkennung beziehungsweise Herstellerdatei.
- **Sofort verfügbar:** Freischaltung ohne Kauf.
- **Kauf zum Grundpreis von 7.500 $:** optional einen Meilenstein dieser Karte als Voraussetzung wählen.

Mit **Freischaltung übernehmen** bestätigen. Die Voraussetzungsauswahl gilt nur für die Host-Kaufvorgabe. Frühere ausdrücklich gespeicherte Voraussetzungen bleiben beim Update als Kaufvorgabe erhalten. Host-Vorgabe, Voraussetzung und dauerhafte Freischaltung werden gemeinsam mit dem Anlagenbestand gespeichert, synchronisiert und gesichert.

## Vorgabe für Kartenhersteller

Neben der tatsächlich geladenen Kartendatei, üblicherweise `Data/Map.json`, kann die Karte diese optionale Datei enthalten:

```text
Data/EnusDE-Signaltechnik.progression.json
```

Beispiel: Kauf erst nach einem eigenen Kartenmeilenstein:

```json
{
  "Schema": 1,
  "Mode": "purchase",
  "Prerequisite": "my-map-bridge"
}
```

`Prerequisite` muss die echte interne Meilensteinkennung der Karte sein. `my-map-bridge` ist nur ein Platzhalter. Ein fehlender oder unvollständiger Meilenstein gibt die Signaltechnik nicht frei.

Weitere Vorgaben:

```json
{ "Schema": 1, "Mode": "free" }
```

```json
{ "Schema": 1, "Mode": "free", "Prerequisite": "my-map-bridge" }
```

Die zweite Variante schaltet nach Abschluss des angegebenen Meilensteins automatisch und ohne Kauf frei. Mit `Mode: "auto"` ohne explizite Voraussetzung gilt das Standardverhalten. Eine explizite Host-Vorgabe hat Vorrang vor der Herstellerdatei.

Die Datei gehört zur Karte, nicht in den Signaltechnik-Modordner oder in einen allgemeinen Ordner aller installierten Mods. Eine zusätzliche Pflichtmod ist nicht erforderlich. Änderungen an der Herstellerdatei werden beim nächsten Laden der Welt eingelesen. Die Dateigröße ist auf 16 KiB begrenzt. Fehlerhafte Dateien oder unbekannte Felder werden gemeldet und blockieren neue automatische Freischaltungen; bestehende Freischaltungen bleiben erhalten. So wird ein Tippfehler nicht stillschweigend zu einer kostenlosen Freischaltung.

## Spieltests

1. Neuer, noch nicht freigeschalteter **Wirtschaftsspielstand** auf der Standardkarte: Signaltechnik-Ausbau vor seinen nativen Voraussetzungen nicht kaufbar; Baumodus, Editor und Fahrdienstleiter gesperrt. Einstellungen und Sicherungsmenü erreichbar.
2. Native Voraussetzungen abschließen: Signaltechnik-Ausbau kaufbar. Nativen CTC-Ausbau zunächst **nicht** kaufen. Nur den eigenen Ausbau kaufen und Zugriff auf Bau-/Stellwerksfunktionen prüfen.
3. Eigene Freischaltung erneut anwählen: keine zweite Abbuchung. Speichern und neu laden: weiterhin freigeschaltet.
4. Andere Karte ohne `sctc`: nach dem Laden sofort verfügbar, kein Freischaltbetrag abgezogen. Materialbedarf bleibt entsprechend den bisherigen Wirtschaftseinstellungen aktiv.
5. Andere Karte mit Herstellerdatei und eigenem Meilenstein: vorher gesperrt, danach je nach `Mode` kaufbar oder kostenlos freigeschaltet. Fehlende Meilenstein-ID und fehlerhafte Datei ebenfalls prüfen.
6. Vorhandene Testwelt mit bereits freigeschalteter Signaltechnik: bleibt nutzbar. Für den Vorher-Test eine separate, noch nicht freigeschaltete Wirtschaftswelt verwenden; die bestehende Anlage muss nicht neu gebaut werden.
7. Client mit derselben Modversion: Freischaltungsstatus folgt dem Host, ohne eigenen Kauf. Vorgaben ändern und Welt neu laden; native fremde Meilensteine und deren Kaufpreise dürfen unverändert bleiben.

Offline werden Entscheidungsregeln, Datei-/Save-Rundläufe, vorhandene Freischaltungen, Zugriffssperren und der native Prüfschritt vor der Geldabbuchung geprüft. Die tatsächlichen Karten-/Meilensteinobjekte der laufenden Welt und der Mehrspielerablauf benötigen noch diese Spieltests.
