# 0.7.36-P0 – GFM rescan and inspection

Test build for Railroader 0.6.2. Use the existing test save; host and clients need matching mod versions. Includes 0.7.35. Native gameplay and multiplayer acceptance remain pending.

## Full rescan

Open **Editor → Menu → Detect GFM in this diagram…**. Enable **Include all interlockings** to clean the entire test installation, including old diagram views. Otherwise only the selected diagram is affected; linked local modules are included when selecting a modular master.

Choose **Rebuild GFM assignments · preview**. Review replaced sections, cleared fields, corrected point-arm mappings and unresolved connections. The live installation remains unchanged until **Replace GFM assignments with this preview**. Release all routes/direction locks and withdraw pending automatic requests before applying. A backup is required first; a failed backup or stale preview prevents application.

The rebuild replaces section definitions, raw metre spans and diagram section bindings. It retains drawing geometry, signal/point world links, boundary marks, physical boxes/positions and train-number field identities. It neither constructs missing detectors nor enables disabled point detection or changes signal operating states. A local rebuild retains old sections still referenced by untouched diagrams; use installation-wide scope for complete cleanup. The existing **Find GFM sections in existing diagram** button remains an additive scan.

## Point arms and occupancy

A drawn straight arm is not necessarily the native normal arm. Two unambiguous physical boundary connections identify the native arm mapping; rotation and mirroring are respected. One connection or conflicting evidence does not justify guessing.

For manual correction, right-click a point field → Detection → **Physical point arms in this diagram**. Choose common and native normal arms using the visible direction arrows, save the field, then rescan. Changes require idle operation and affect diagram mapping, not the native point position.

Point rails now use their own guard readings rather than painting every arm red whenever the aggregate point is occupied. Operational protection still regards the whole point as occupied when any guard is occupied. A train actually spanning several arms can still produce multiple red arms.

## Section list and route test

Open **Inspect sections and coverage gaps…** from detection. The list shows related saved sections/points and newly detected physical proposals, their boundaries, lengths, metre ranges, linked-field count and definition errors. Unsaved proposals do not count as configured coverage.

**Show section and boundaries** adds cyan highlights while retaining live occupancy colours. **Edit section assignment at field…** opens an associated track/signal/point field for the existing manual tools. Refresh inspection after edits.

Select start/destination signals and movement type, then **Calculate test paths · no operation**. Choose an alternative and **Show coverage and exact gaps**. This performs calculation only: no signal/point commands, route/direction reservations or persisted route catalogue entries. Valid saved circuits and enabled point guards count as coverage. Exact missing track/metre intervals are listed; orange frames identify fields associated with gaps. Gaps without any diagram binding cannot be placed by guessing grid coordinates.

Configuration and network changes require refreshed inspection; stale actions must be rejected. Clear highlights explicitly or leave the inspection page.

## Long sections and acceptance

Zero unbound signals, missing signal detectors and disabled point detectors does not prove complete section assignment. Check the long section in the inspection list: a physical proposal with zero fields is not yet assigned; an invalid definition needs boundary/point correction; an unresolved diagram connection needs matching ports or corrected arm roles; a missing opposite boundary needs assignment. Rescan does not redraw missing track or invent a detector.

Test old manual circuits, shared A/B, swapped arms, rotation/mirroring, long sections, cancelled previews and complete resets with no detected pairs. Unresolved areas must remain unmonitored. Occupied or unknown track must continue blocking new authority. Inspection must never operate signals or points. The reported test-world long section still needs practical verification; report complete errors, versions and Player.log.
