# Runenwacht 0.1.0 — presentation API v1 example

[Deutsch](#deutsch) · [English installation and acceptance guide](../../mod/EnusDE-Signaltechnik/Guides/RUNENWACHT-0.7.63.en.md) · [Deutsche Anleitung](../../mod/EnusDE-Signaltechnik/Guides/RUNENWACHT-0.7.63.de.md)

Fictional manufacturer: **Zwergische Kristallwerke Kupfergrund**. System: **Runenwacht**. Original EnusDE experimental designs; the name does not identify real railway equipment. This optional mod supplies presentation, not operating rules or AI. Main 0.7.63 and Railroader 0.6.3 are required. There is no automatic asset transfer or public-release assertion.

## Reference layout

- `src/RunenwachtEntry.cs`: entry point, registration lifetime and provider metadata.
- `src/RunenwachtModel.cs`: Unity presentation instance, directional modular heads, materials, pick bounds, caption/brightness application, animation and disposal.
- `src/RunenwachtGeometry.cs`: original procedural mesh generator for housings, runes and permanent crystals.
- `src/CrystalLogic.cs`: fixed-colour channel mapping and exclusive animated transitions.
- `mod/EnusDE-Runenwacht/Info.json`: separate manifest; own assembly only, exact required main/game versions.
- `Build.ps1`: builds the pack after the main DLL exists; nothing installs or runs the game.

From the repository root, `Test.ps1 -GameDirectory '<game>'` builds both assemblies and runs the normal suite plus geometry fixture. To build only the pack after the main, use `asset-packs/Runenwacht/Build.ps1`. `tools/Package-Runenwacht.ps1` packages previously tested outputs into two new archives; it refuses existing output filenames and does not run tests for you. Build artifacts stay outside tracked source.

## Contract for another pack

Implement `ISignalPresentationProvider` from `src/SignalPresentationPacks.cs` in the main project. Register once in `OnModLoad` and dispose the registration on unload. Metadata must be stable: API 1, a unique lowercase ID using letters/digits/dots/hyphens, a parsable version, and a display name. Do not bundle the main assembly or game libraries. A pack is an ordinary trusted installed mod; the API is an architectural boundary, not a sandbox against arbitrary third-party executable code.

`Create(parent, kind, preview)` owns only children/resources under the supplied parent and returns `ISignalPresentation`. Supply finite positive `LocalBounds` (maximum dimension 20 m) for the world click target. Local units are metres; Y is up, the front faces **negative Z**, the back face rotates 180 degrees. In this reference, combined main ring centre is 4.45 m and maximum height about 5.02 m; preview and shunting centres are 3.0 m and 2.05 m. Standalone variants use shorter masts.

The main calls `Apply(SignalViewFrame)` with immutable front/back snapshots. They contain enabled modules, caption, label scale, preview flag, bounded local emission strength and already-resolved main/shunting/distant aspects. Main retains stable IDs, persistence, permissions, route/occupancy checks, AI authority, construction and host/world-license gates. The provider receives no mutable route/marker/runtime object through this contract and must never determine movement authority from lighting.

Build physical components once. Aspect changes update fixed-colour channel intensity; they do not replace meshes or recolour crystals. `LateUpdate` advances visual animation; a red indication immediately clears conflicting permissions. Respect local emission controls and do not animate a permissive light after it is withdrawn. Module visibility follows configured front/back/module flags. Separate animation timing does not delay operating changes in the main mod.

Make `Dispose` idempotent and also release owned Unity meshes/materials in `OnDestroy`. Registration/unregistration invalidates visual caches; factory/update exceptions quarantine the provider and select a visible built-in fallback without deleting saved identifiers. Existing saved pack models render when installed even with new-placement activation off. New selections require compatible installed provider, session activation and persisted world test license; old worlds get neither an automatic license nor automatic conversions.

This example has no external model/texture/audio downloads. Detailed meshes are generated locally. Baked textures, LODs, extra particle effects, asset streaming, wagon cargo replacements, paid licenses and a finished multiplayer acceptance test remain future work. The project has no blanket open-source license; included third-party notices apply only to their named components. See the repository notices before redistribution.

## Deutsch

Eigenes optionales Modpaket und API-v1-Beispiel für Runenwacht der fiktiven **Zwergischen Kristallwerke Kupfergrund**. Das Hauptmod liefert den Betriebszustand; das Paket zeichnet Gehäuse, feste Kristallfarben und Lichtanimationen. Freischaltung nur experimentell per Konsole plus kostenloser Welt-Testlizenz. Beide Pakete müssen zunächst auf Host und Clients manuell installiert werden. Es gibt keine automatische Übertragung und keine Zahlungsfunktion. Vollständige deutsche Einrichtung und noch offene Spielprüfungen stehen in der oben verlinkten Anleitung.

Für weitere Pakete: eigener Modordner, eigenes Manifest und eigene Assembly; `ISignalPresentationProvider` registrieren, immutable Zustände darstellen und alle selbst erzeugten Meshes/Materialien zuverlässig freigeben. Interne Betriebslogik bleibt im Hauptmod. Lokales Koordinatensystem: Meter, Y nach oben, Vorderseite nach −Z. Quellcode und Referenz sind öffentlich lesbar; eine allgemeine Weiterverteilungslizenz wird dadurch nicht erteilt.
