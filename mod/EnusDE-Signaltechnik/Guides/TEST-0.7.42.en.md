# 0.7.42-P0 — queue setup and game acceptance

Route queueing is automatic; no new world objects, contacts or setup are required for manual requests. Use the same host/client version. Existing configured contacts use the same queue.

1. Select a start and destination across several main signals. The request is queued and the completed button pair clears, allowing another selection immediately. Open the dispatcher context for Pending requests, Progress and Cancel pending preparations.
2. Request several different routes rapidly. Duplicate clicks for the same request should not create another preparation. Requests should advance one at a time; camera, UI and safety ticks should remain responsive. Check both calculated and saved-route modes and saved-chain buttons.
3. With alternative paths, choose or dismiss the route-choice window. Check that completing an older request does not clear a newly selected start signal. Incompatible requests may still fail their normal admission checks.
4. Occupy a later portion while its chain is preparing. Final admission must refuse/roll back the entire new chain, leaving existing locks intact. Repeat with unknown GFM, point obstruction, conflicting route and neighbour-owned GWB permission.
5. Cancel before and during snapshot/search/protection calculation; repeat with STOP. Deferred work must never submit afterwards. Existing routes and residual locks retain normal supervision.
6. Change signal position/settings or native track topology during preparation. No stale result may be admitted. Switch interlocking/editor, restore a backup and unload/reload; old queued work must be discarded. Pending work is not saved as movement authority.
7. Test several existing contacts, primary/alternate targets and a central automation pause. Change consist, order or direction while queued. Automatic submission must recheck the live train and must not ignore missing detection or permission.
8. At 32 queued preparations the next distinct request is rejected with a readable queue-full message. Existing limits of 16 active route parts and 32 alternatives still apply; the preparation queue does not bypass them.
9. Compare frame-time recordings on the same large test map before/after, including a long multi-signal request. Record route length, signal/point count, options, queue phase and maximum frame time. The 2 ms work budget is cooperative: a single native operation/final admission can exceed it. A synthetic test is not an actual frame-time measurement.

Automated checks cover FIFO, time/step budgets, duplicate/capacity limits, cancellation, errors, native callback integration, worker search through eleven intermediate route parts, atomic rejection after occupation changes and unchanged flank protection with 6000 extra nodes. Practical gameplay and multiplayer acceptance remain pending.
