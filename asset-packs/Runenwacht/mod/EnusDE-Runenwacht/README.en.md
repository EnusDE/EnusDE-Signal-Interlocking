# Runenwacht 0.2.1 — visibility and mounted hardware

Requires main mod **0.7.65** and Railroader **0.6.3**. This package contains the Runenwacht 0.2.1 models with the new main dependency. Replace it through your mod manager and restart the entire game. Host and clients need matching packages.

## Light at near and far distances

Inactive crystals now have dark, non-emitting reflective surfaces. For the active fixed-colour channel only, body/edge/vein emission is computed from the existing animated level and local brightness control. Near the camera the body has a restrained glow so cut surfaces remain readable. Between 12 and 80 metres a smooth increase gives the full body a stronger luminous area; above 80 metres the gain stays constant with no distance cutoff. Fine edges alone previously became subpixel lines, causing the reported loss of indication at distance. Body materials are private per head to prevent another signal receiving its state.

This does not change aspects, permissions, timing, geometry visibility or crystal colour. Dark/withdrawn channels and emission disabled in local settings remain non-emitting at every distance. The rear housings remain opaque. There are no camera-facing sprites, extra point lights or always-visible halos around the opaque housings. Actual day/night exposure, distant pixel coverage and rear/oblique visibility still require Railroader camera tests; passing the numeric curve checks does not prove game readability.

The white identification crystal has a larger aperture placed ahead of its retainer and extra far-distance gain. A solid mounting socket reaches the frame and covers the complete emitting projection from behind; no floating rear-visible light sprite is added.

## Physical connections

Framed code/name/maker plates now have two solid carrying arms and a clamp reaching the actual mast-rail plane. Front and rear mounting depth differ; the plates no longer stop in free space. Head clamping flanges reach the rails as well.

Copper runs now remain continuous along the mast, with rounded elbows, ferrules at both ends of the ceramic insulators and routed branches between the exact head outlet and mast bus coordinates. The short shunting assembly uses shorter insulators so its feed clears the stone foundation. A central pedestal joins the crescent frame to its lower beam. These are visible mechanical geometry changes, not a new electrical simulation.

## Validation / remaining game checks

Full **Test.ps1: 2,070 PASS, 0 FAIL** on 23 November 2026, including finite/monotonic near/far gain, dark channels at all distances, animation scaling, invalid inputs, closed outward-facing elbows, front/rear plaque depths, exact head-to-bus endpoints and production-mesh/UV validity. The main DLL hash was compared before/after and is identical. Both languages are documented; no new main UI strings or controls were introduced.

Still test: each active colour at roughly 3/12/30/80/150 metres, daylight/night and local brightness/emission off; inactive crystals; rear/oblique views; close side views of all plate supports, ceramic terminals, branches and the low shunting base. Use the existing host Build-editor individual lamp test; test views remain local and grant no native movement permission. No game was launched or mod installed by the assistant. These are test packages, not a public release.

Reference source includes `CrystalAppearance.cs` (pure visual curve), `RunenwachtDesign.cs` (mounts/ports), `RunenwachtPalette.cs` and `RunenwachtModel.cs`. Build with `Build.ps1` after the main DLL exists; run the repository `Test.ps1`; then `asset-packs/Runenwacht/Package.ps1` creates only the optional ZIP and refuses an existing filename. Existing prepared archives are preserved.
