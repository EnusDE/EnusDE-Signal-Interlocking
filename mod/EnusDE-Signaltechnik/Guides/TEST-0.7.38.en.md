# 0.7.38-P0 – Occupancy causes, flank protection and diagnostic export

Internal Railroader 0.6.2 test build. Host and clients need the same version. These changes are not in the first public release. Actual gameplay and multiplayer acceptance remain pending.

## Admission and flank protection

Admission errors identify the route/flank section or point, native segment and metre interval, and distinguish occupied from unreadable/unknown. Missing data is not reported as a confirmed train occupancy.

A diverting flank point no longer adds its other two legs to reserved flank sections. The threat corridor and protecting signals remain held. All point guards must be clear before moving that point. If already diverting away, it stays locked in that position without rejecting a separate occupied away leg. Another route using the same held point may still conflict.

After releasing their authorities, recalculate saved train routes with points from older versions: **Editor → Menu → Saved route planning**, choose the definition, align the real points for the intended path, then **Recalculate route**. Planning remains accessible in dynamic route mode; switch that mode off to operate saved definitions. Old flank definitions are retained and explicitly request recalculation. Newly calculated routes use the correction immediately.

## Auxiliary destinations and train numbers

Linked auxiliary destinations are offered for the chosen start signal and selected signal field without depending on the last selected cell. Both signal halves resolve the link. Choose a start, then **Set to …** in its context. Movement and approach direction remain checked; intervening signals cannot be skipped. An ordinary main-signal click keeps its ordinary destination meaning.

Number passages are marked handled after successful transfer. Temporarily occupied destination number fields can retry, while a delayed passage cannot take a different following train's number. An empty source does not acquire a later number retroactively. Prefix variants share the same five-digit train identity.

A train straddling detection sections gets one stable ordinary display owner for vehicle-derived numbers. Up to 512 independent fields are evaluated; larger layouts report unknown for these derived displays. Explicit read-only mirrors and master projections of the same source remain supported.

Independent number fields sharing signal bindings and pre-existing duplicate registered numbers show **?**. Keep one source and link other views as mirrors. Re-save a duplicated registered number at the intended source to transfer ownership and remove its stale entries. Merely opening the inspector does not rewrite them.

## Finding GFM-A-051

GFM codes are independent of signal codes. Inspection prioritizes the selected field's actual assignment and then sections with both boundaries drawn on the current diagram. Longer sections with only one drawn end remain inspectable. Each boundary shows its source signal/box/generated point, stable ID, native track/metres and diagrams/field coordinates. Physical object properties can be opened directly; this does not move the camera.

**Linked diagram fields: 0** means the section is not assigned to the rails of this drawing. Highlighting may show only its drawn endpoints. **Physical definition is valid** does not assert clear track or a correct local drawing assignment. Signals without active GFM and virtual destinations do not split a physical circuit. Inserting an effective C boundary requires detecting adjoining sections again. A stale signal endpoint without an active detector can no longer masquerade as a valid GFM boundary.

The reported A-051 ↔ A-050 section of 2773.74 m still needs current game data to explain. The screenshot does not establish that the detector was invented.

## Diagnostic ZIP and cloud workflow

1. Reproduce the rejection on the **host**.
2. Open **Company → Settings → Mods → Signal & Interlocking → Optional local diagnostics → Export diagnostic ZIP**, or export from the diagnostic window.
3. The result shows the full filename. Default Windows folder: `%USERPROFILE%\AppData\LocalLow\Giraffe Lab LLC\Railroader\EnusDE-Signaltechnik\Diagnostics\Exports`.
4. Optionally enter an absolute local export folder and **Apply & Save** first. A user-synchronized local folder is supported, then export again.

Contents include versions/time/host role, installation, diagrams, saved and active routes, the last attempt with cached admission readings, recent attempts, current native section/block/point occupancy with car IDs, vehicle positions/couplings, detector origins, network and number bindings. `Sampled: false` means that section was not queried in that attempt, not an observed occupancy. Current readings may differ from the attempted request. Client data is not host authority. Limits and capture errors are explicit.

Every export uses a new filename and issues no operating commands. The archive is limited to 64 MiB of uncompressed JSON, with 32 MiB per part; oversized exports fail explicitly. It is a diagnostic snapshot, not a restorable backup. Background recordings remain separate in the displayed recording folder. Export works with recording switched off.

There is no automatic upload or remote Windows access. Share the relevant ZIP for investigation while source stays in the cloud. A synchronized local folder alone does not automatically connect this chat to your game.

## Restart and gameplay checks

Signals intentionally start at Stop after loading. Active authorities and runtime locks are not restored from the previous session. Configuration, saved routes and number assignments remain; request routes again.

Test the previously rejected HS-044 → HS-036 route and export any remaining precise error; a separate track beside an already-diverting flank point; refused point movement over occupied guards; linked auxiliary destinations after changing cell selection; number advancement across overlapping detectors and blocked destinations; independent mirrors; boundary origins for A/B-045 ↔ A-050 versus A-051 ↔ A-050; a newly inserted C limit; Stop after reload followed by a new clear route; and two separate diagnostic exports.
