# Build mode and native MOW materials — 0.7.61

[Deutsch](BUILD-MOW-0.7.61.de.md)

Early Alpha test build for Railroader **0.6.3**. Install manually with your mod manager, then restart the entire game. Host and all clients need **0.7.61**. Back up the full save and installation configuration first. No automatic installation or game launch is performed by the build tools.

## Enter and leave Build

Open **IXL / STW beside the clock → Build**. A compact resizable panel opens on the right. Construction permissions and milestone prerequisites still apply; entering Build does not grant client editing rights.

Choose a tool symbol: main signal, distant signal, shunting signal, speed sign, clearance marker or GFM boundary B/C. Hover for its name. The selected icon is cyan and the selected tool is also named below the palette. Place on the track with left-click; continuous placement remains active. The panel stays available. Right mouse still controls the camera. **Escape cancels the current placement**, rather than deleting accepted plans.

Click **End build mode**, close the panel with its native X, or switch to Editor/Dispatcher to return to normal play. Unconfirmed cursor previews disappear. Confirmed work orders remain. Clicking an object outside Build opens its information; the host can choose **Edit in build mode**.

All global settings remain in the **launcher gear**, outside the construction panel.

## Plans and construction sites

With material requirements enabled, confirmed unfinished objects appear as cyan virtual models in the local builder's view. Outside Build, including other players' ordinary views, they appear as original cone/barrier models with an object label. The site is a visual representation of the same saved object, not another installation. Finished infrastructure keeps its normal model.

Private cursor previews are not shared. Confirmed plans use the existing host-owned installation synchronization. Planned signals retain their existing non-operational safeguards. With free construction enabled, objects may finish immediately and therefore have no persistent site phase.

## Order, store and deliver

1. Open **Material** in the panel, then **Open native wagon ordering**, or use the game's normal **Company → Equipment** page.
2. Order a captive supply wagon using the normal game controls. Choose **Signal building supplies** for an **FL/FM flat car**, or **Signal equipment** for an **XM boxcar**. Native ordering, payment and capacity rules apply.
3. Deliver to a native MOW material warehouse and use its normal unloading/loading controls. Both signal cargos are added to matching existing material storage components; a separate mod depot is unnecessary. The standard map's Bryson and East Whittier MOW component definitions were inspected offline. Other maps need compatible material loader/unloader pairs and their own unlock conditions.
4. Reload the required materials into supply wagons, couple them to an owned MOW work car, activate its native MOW equipment, and stop within **250 metres along the track** of the confirmed plan. Materials are consumed from the coupled owned supply cars as each object is completed. Unrelated freight is not consumed.
5. Inspect **Material → Construction sites** for remaining requirements and status. The construction milestone also lists open work and per-object status. The permanent programme stays open for later extensions. The host can explicitly finish individual objects free in Sandbox.

The first connection of an existing point retains its previous one-time wiring charge. It can now use a stocked native MOW warehouse or retained legacy depot within the existing **250 m spatial radius**. This wiring step does not yet require an MOW car at the point. Already connected points are not charged again when saving routes.

## Existing stock and save safety

Existing objects, routes, diagrams, identifiers and legacy stocks are retained. Material shows any non-empty legacy depot. Select an MOW warehouse and explicitly choose **Transfer to selected MOW warehouse**. Only whole packages that fit are moved, without a second charge; fractions already in native stock and excess legacy stock remain. Further transfers can move the remainder after space becomes available.

A receipt in the host's shared save properties guards against crediting the same old balance again after restoring only an older installation configuration. If stock and receipt no longer match, restore a matching **full game save** before attempting another transfer. Native industry quantities belong to the game save; the mod's installation backup alone does not restore native inventory. Transfer is an administrative stock migration, not a wagon delivery animation.

## Acceptance still needed

The code compiles against the local game libraries with **0.6.3** player metadata. Offline checks cover material registration, host-only transfers, fractional/capacity handling, rollback, restored-balance rejection, point charges, native API contracts, local mode policy and site/icon geometry. They do not simulate a Unity game session.

On a copied save, test:

- Order both cargos, pay once, unload/reload at an unlocked MOW warehouse and verify stock after save/reload.
- Build using an active MOW car and coupled supplies; verify insufficient supplies, distance and inactive-car cases.
- Transfer a small legacy balance and an over-capacity balance; verify leftovers and persistence.
- Enter/exit Build repeatedly, cancel the cursor, switch Editor/Dispatcher, restore a configuration and reopen the panel.
- Check the right-hand layout, tooltips, language changes, transparent models, terrain support and finished models in daylight/night.
- With matching host/client versions, verify confirmed sites and completion on late join, with only the builder seeing virtual plans and no new client editing rights.

No game, rendering, native delivery or multiplayer acceptance is claimed for 0.7.61 yet.
