# 0.7.39-P0 – Directional signal passage and physical track intervals

Internal Railroader 0.6.2 build. Host and clients need the same version. Actual gameplay and multiplayer acceptance remain pending.

## Shared passage point

Open **Company → Settings → Mods → Signal & Interlocking → Automation and AI → Route calculation**, set **Signal passage point behind signal (m)**, then **Apply route mode**. Default **5 m**, range **0–25 m**. The editor's route calculation settings expose the same host setting. Release existing routes, direction holds and pending automatic requests before changing it.

The assigned consist's leading end must pass this point in the directed route. The same event drops the start signal or passed intermediate shunt signal and verifies train-number advancement. It does not wait for arrival or the train tail. Short paths cap the offset at half the remaining path length. Zero retains a small rounding threshold. This is distance-based, not a timer.

Occupation anywhere in the reservation no longer implies entry when directional vehicle tracking is available. An unrelated vehicle intrusion or unknown route occupancy still withdraws authority immediately. Tail-based release remains separate.

## Physical occupancy and A/B sections

Occupancy now uses vehicle endpoints and their actual track-segment/metre intervals rather than a projection of world coordinates. A vehicle spanning segments can occupy both; a parallel track is distinct. A short multi-segment body path must resolve. Unresolvable geometry remains unknown; unclear multi-segment body location cannot manufacture clear track. Unresolved vehicle IDs are included in diagnostics.

Diagram, route, GFM, point and flank readers share this interval basis. Occupancy clears when the vehicle actually leaves; it is not artificially advanced or delayed to another section. Long trains may occupy several sections simultaneously.

For additional local train-route admission, a long A/B circuit extending behind the start is clipped to the requested route portion. Occupation behind the start does not by itself occupy the physically clear requested portion. The complete circuit and its display remain, and configured open-line/direction checks keep their own conditions. A/B boxes are not split or renumbered.

## Shunting

Known occupation is allowed along the shunt path outside point protection intervals, including intermediate track before later points. Occupied point guards, conflicting reservations, unknown readings and invalid bindings remain rejection reasons. This does not authorize moving an occupied point. An intermediate occupied track is not turned into an early destination-release zone.

## Section manager and refresh

**Editor → Menu → Section manager / GFM inspection…** directly opens the existing boundary/section inspection. Rejection messages identify start/destination, point and GFM pair where possible, with a rounded native track interval; precise values remain in diagnostic exports.

**Editor → Menu → Refresh and inspect installation… → Refresh all installation elements** reloads saved world objects, models, bindings, routes and occupancy readers, preserving stored settings and stable IDs. Release current authorities and pending automatic requests first. Runtime readers and contact observations are reinitialized.

The following audit lists invalid GFM paths, missing signals/points, duplicate number bindings and invalid or obsolete routes. It does not guess or delete physical assignments. Existing previewed GFM rescan and world from/to tools remain available.

## Gameplay checks

Reproduce HS-044 → HS-036 with the train behind the start; compare the readable interval and export a host ZIP on rejection. Drive slowly through the five-metre point in both directions and verify one shared signal-stop/number-transfer event; optionally repeat at ten metres. Check long-train occupancy until tail clearance, separation of parallel tracks, occupied shunting paths outside point guards, rejection at occupied guards, and a long A/B section containing the start. Refresh elements after releasing routes and compare IDs/settings before correcting audited bindings.

Renumbering remains a proposal, not a tool in this package: preview old→new visible codes, use diagram columns locally and a fixed map axis such as west→east globally, retain IDs/directions/bindings, then validate and apply the whole name set together. Renumbering does not repair a faulty track connection.

[Diagnostic export and local synchronization folder](TEST-0.7.38.en.md) remain available. This task has not published or installed the build on your Windows PC. The reported game-state failure still requires practical verification or a current host diagnostic export.
