# 0.7.42-P0 — queued route preparation and large-network performance

- Manual start/destination requests, saved routes/chains and existing contact automation now use a bounded FIFO preparation queue instead of calculating complete multi-signal paths inside the click or contact callback.
- Work advances each frame with a 2 ms cooperative budget and a 512-step cap. Native protection calculations yield between partial routes. A single native call or the final atomic admission can exceed that soft budget; this is not a hard frame-time guarantee.
- Pure path searches run on isolated data snapshots on a worker. Unity, graph reads, signal/point operations and protection calculations remain on the main thread. Only one queued preparation runs at a time.
- Added queue progress, duplicate suppression, a 32-request bound and cancellation. STOP, world unload and backup reload cancel deferred work. Aborting preparation does not release existing locks.
- Mod configuration and installation changes invalidate preparation. Native topology is captured and compared again; every prepared route is revalidated before guarded admission. Occupation is checked from current data, not from the search snapshot.
- Retained whole-chain admission and rollback. No partial route is reserved during search. Existing GFM, point, flank, block, GWB and active-route limits remain in force.
- Contact automation consumes prepared plans only after rechecking the train, consist, order, direction and operating state. At most one prepared contact admission is processed per automation tick; configured retry and alternate-target behavior remain.
- Replaced repeated whole-network scans per node in flank-protection adjacency construction with one linear incidence lookup. Reused calculated proposal prefixes within each preparation.
- Updated English/German queue help and corrected superseded auxiliary/GWB descriptions in the embedded F1 handbook. Communication and general automation expansion remain concepts, not new features in this update.

Actual game frame-time, the reported near-crash and multiplayer acceptance remain to be verified in the user's world. The synthetic large-network regression records timing separately and does not prove game FPS.
