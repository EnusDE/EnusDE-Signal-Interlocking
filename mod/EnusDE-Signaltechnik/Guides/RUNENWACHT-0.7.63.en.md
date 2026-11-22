# Runenwacht — experimental presentation packs, 0.7.63

Main mod **0.7.63**, optional **Runenwacht 0.1.0**, Railroader **0.6.3**. Early Alpha; offline checks do not establish in-game rendering or multiplayer acceptance.

## Install and activate

Install the two separately packaged mod folders through the normal mod workflow and restart the game. The main mod works without Runenwacht; Runenwacht requires this exact main version. For this initial experiment, host and clients install the same packages manually. No executable code or assets are transferred automatically.

On the host, load a test world, unlock normal signal construction (or use sandbox), then use the console:

```
/enus_assets status
/enus_assets enable
/enus_assets unlock:enusde.runenwacht
```

The experiment starts OFF after each world load. The world test license persists in the installation, including configuration backups, but needs the installed compatible pack before it can be granted. This is a free developer test license: no payment, commercial license check or purchase system is implemented. A console command is not a security boundary in public source.

Reopen Build after activation. Runenwacht main/distant/shunting buttons appear in its tool panel. Place and finish objects through the existing construction/material workflow. To convert an existing signal, open its properties and choose Runenwacht; locked routes prevent conversion. Internal signal IDs, route associations, track anchors and operating permissions are retained. The built-in presentation button reverses the visual conversion.

Main mast direction settings still control front/back heads and optional distant/shunting modules. Main model selection needs a standard-height mast without Hl; conversion chooses this supported geometry. The new standalone shunting model cannot be promoted to a low main model until reverted to built-in presentation. Existing light brightness/emission and label-size controls also apply. Halo-specific settings currently concern the built-in H/V/Hl renderer; Runenwacht uses directional opaque crystal surfaces without separate halos or point lights.

`/enus_assets disable` blocks new pack placements/conversions. Installed existing models continue to render and operate; disabling does not destroy construction or revoke a signal. If the pack is absent, incompatible or throws while creating/updating a view, the main mod retains saved pack IDs and uses visible built-in signals. An in-session faulty provider is quarantined, and the settings status reports its error. Restart after correcting/reinstalling it. Explicitly restoring built-in presentation removes the saved selection. No signal aspect is inferred from a missing model.

Keep a full save and configuration backup before testing. Versions before 0.7.63 do not preserve the new pack/license fields when rewriting an installation; use the backup if rolling back.

## What is modeled

Original procedural meshes: a forged mast, ladder, copper conduit, foundation, retaining claws, ceramic insulators, riveted rings, rune inlays, faceted crystals, opaque rear cups and side/top shields. This is an initial 3D implementation based on the concept, with no baked textures, LOD system, video, audio, new wagon load models or streaming feature. Art quality, day/night visibility, oblique viewing, exposure, performance and clearances need game tests.

Each crystal channel has **one fixed colour**. Every crystal mesh remains installed; only emission changes. Main head: separate red, green and amber cores, red diagonal rim guides and a small white marker. This deliberately replaces the earlier colour-changing single-core sketch with separate physical crystals. Distant head: three unequal spires (two amber, one green), a permanent red horizontal expectation bar and white marker. Shunting head: red waiting bar, violet permission crystal and white marker.

| Head | Stop / stop expected | Clear / clear expected | Slow / slow expected | Shunting permission |
|---|---|---|---|---|
| Main | Red core and rim guides | Green core | Amber core | Main remains red |
| Distant | Red base bar | Green tall spire | Amber spires | Dark on a combined mast |
| Shunting | Red bar | No independent train authority | No independent train authority | Violet crystal |

Combined head states come from the main mod's existing `SignalLampPlan`: a stopped main head darkens its attached distant head; Sh 1 keeps the main red and lights the shunting head only. Dark extinguishes all channels. Invalid is restrictive red (the attached shunting head is dark); Kennlicht uses a white fixed channel at the applicable head. Unfamiliar states never become a permissive indication. A distant head is an announcement, never movement authority.

Permissive ignition fades in over 0.24 seconds. Red extinguishes before a new permissive channel lights; a new restrictive red suppresses old permissive light immediately. Safety-related extinguishing is immediate. A restrained shimmer changes brightness by only ±2.5%, without blinking or changing colour; red remains steady. Hoods, closed rear cups and opaque materials limit recognition from behind. These features still need real camera/day/night checks.

## Game acceptance checklist

1. With only the main mod, load an existing installation: no asset pack is required and no model choice changes.
2. With both packages installed, confirm default OFF, host-only activation and per-world license isolation. A client must not grant a license or place/convert a signal.
3. Place all three variants, both directions and combined heads. Check ground support, click targets, captions/scaling, native building supplies and construction-site visibility.
4. Exercise Hp 0 / Hp 1 / Hp 2 / Sh 1, distant links, invalid, white marker and dark. Observe the frame-by-frame changes, approach/rear views and brightness controls.
5. Save/reload, move and reconvert. Confirm the license/model persist, activation resets, route IDs stay stable and a route-locked signal cannot convert.
6. Remove the optional pack in a copied test world: visible standard signals and intact operations must remain. Reinstall without changing the saved selection; check cleanup after unload/reload.
7. Repeat with host/client, late joining, differing camera sides and missing/mismatched package rejection. Test several simultaneous signals for frame time and mesh/material cleanup.

Do not treat these steps as completed game tests. Settings remain in the launcher gear; no new game preferences tab is added.
