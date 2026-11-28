# Browser interlocking preview

The game host enables browser access for the current world from the gear in the clock launcher. Choose **Browser interlocking → Start on this PC** to open it on the game computer. The host can also select a private IPv4 address under **Start on home network**, copy the access link and open it on a second computer. The link grants operating and game editor access for this session. Share it only with the intended operator on a trusted network. Home network traffic uses plain HTTP on TCP port 47327. Stopping access, changing worlds or losing host authority closes the listener and revokes the link.

In **Dispatcher**, select a start signal and destination, then choose **Preview path**. A dashed mark shows matched sections of the calculated path. The game checks occupancy, route protection and permissions when you choose **Set route**. Unmonitored or stale sections remain amber. Select an active route's start signal to request its normal safe release. If two paths remain possible, choose a closer destination or select a path in the game.

In **Editor**, select a field to open that field in Railroader. **Assign from world** starts the game's signal picker for a signal field. **Build in game** opens the existing native build tools when no field is selected. Save or cancel changes in Railroader. At a master diagram, open the owning interlocking to edit a projected field.

This preview needs acceptance in Railroader 0.6.3. Start with loopback, then test an explicit home network link and verify that the old link stops working after access is turned off. Try a train route, a shunting route, an occupied destination, a protected cancellation, a world assignment and a world change. Check signal halves, point indications and scrolling on a large diagram. Browser occupancy draws at diagram field or mapped half-rail granularity.

## Deutsch

Das Zahnrad neben der Spieluhr öffnet **Stellwerk im Browser**. **Auf diesem PC starten** öffnet den Browser. Für einen zweiten Rechner wählt der Host unter **Im Heimnetz starten** eine private IPv4-Adresse und kopiert den Zugangslink. Wer den Link hat, kann in dieser Sitzung bedienen und die Werkzeuge im Spiel öffnen. Teile ihn nur mit der bedienenden Person in einem vertrauten Netz. Das Heimnetz nutzt unverschlüsseltes HTTP über TCP-Port 47327. Ausschalten, Weltwechsel oder der Verlust der Hostrolle beendet den Zugang.

Im **Fahrdienst** zuerst Start und Ziel anklicken, dann **Fahrweg prüfen**. Die gestrichelte Vorschau markiert zugeordnete Abschnitte des Fahrwegs. Bei **Fahrstraße stellen** prüft Railroader Belegung, Schutz und Bedienrechte. Unbekannte Abschnitte bleiben gelb. Über das Startsignal einer aktiven Fahrstraße lässt sich die sichere Auflösung anfordern. Bei mehreren möglichen Wegen wähle ein näheres Ziel oder den gewünschten Weg im Spiel.

Im **Editor** öffnet ein gewähltes Feld die passenden Eigenschaften in Railroader. **In der Welt zuordnen** startet bei Signalfeldern die Signalauswahl im Spiel. Ohne Feldauswahl öffnet **Im Spiel bauen** den nativen Baumodus. Speichern und Abbrechen erfolgen im Spiel. Projizierte Felder im ESTW lassen sich auf ihrer Ursprungstafel bearbeiten.

Für den Praxistest zuerst am Spielrechner starten, dann den zweiten Rechner testen. Prüfe eine Zugfahrt, eine Rangierfahrt, ein befahrenes Zielgleis, die sichere Auflösung, die Signalzuordnung und einen Weltwechsel. Prüfe auch, wie Signalhälften, Weichen und Besetztmeldungen bei dir im Gleisbild erscheinen. Die Browseranzeige nutzt Felder und zugeordnete Halbschienen.
