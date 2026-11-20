# 0.7.44-P0 — Block assemblies and scoped configuration checks

Internal test build for Railroader 0.6.2. Host and clients require 0.7.44.
This build addresses the block-construction feedback from 0.7.43.

## Construction and diagram

- Each intermediate boundary has one shared B detector and two separate,
  longitudinally staggered signal masts for opposing movements. A directional
  track receives only its required direction.
- Stagger is adjustable from 0.5 to 25 m per side, initially 10 m. Short sections
  limit it to a quarter of the adjacent block lengths. Lateral spacing, side,
  height and scale follow current placement settings, including game spacing
  when selected.
- The diagram uses one rail line with separate signal points, shared detection
  boundaries, train-number fields and a permission module.
- Choose the local interlocking controlling the block signals independently of
  the two line neighbours. Additional boards are read-only views: neither their
  signal fields nor their permission module can issue operating commands.
  Train numbers mirror the same source instead of creating additional numbers.
- Confirm the assembly position on each new board in a diagram preview: green
  means free; red means collision or outside the board. Nothing is committed
  until final confirmation.
- In Editor, grabbing an assembly field selects the complete assembly for
  dragging. Dispatcher offers a direct shortcut into Editor for moving it.
  Signal, detector and number-source identities remain stable.
- Existing assemblies have a separate ownership/display-only preview that does
  not reposition world objects. Full rebuilding preserves existing auxiliary
  links and signal labels. Manually added unrelated fields are never silently
  deleted: rebuilding asks you to move them outside the assembly first.

## Protection and operation

- Construction, diagrams and the line definition are saved together. This also
  persists the selected line protection for a previously unsaved connection.
  Installed but inactive block lines refuse movements on their line axis.
- Whole-line permission rejects opposing movements even when the requested
  routes occupy different clear block sections. Same-direction following trains
  still need their own clear routes.
- A block signal returns to Stop after the assigned train is verified beyond
  its shared detector, using the actual direction and at least the normal signal
  passage distance. The same verified event drives existing number transfer.
  Train-tail proving and lock release remain separate.
- Optional approach contacts receive next-block destinations. Global contact
  automation changes only through its explicit checkbox. Existing rules remain;
  a locomotive still needs train duty. Construction alone does not introduce
  vehicle-specific order automation.
- Missing, unfinished or wrongly assigned signals/detectors and incomplete GFM
  coverage prevent authority. Changing the block owner transfers block signals;
  station entry and exit signals remain with their own stations.

## Local edits and retained directions

Auxiliary changes check the affected target and dependencies. Line edits check
both the existing and proposed physical axis. Unrelated routes or retained
directions no longer block these local edits globally. Permission ownership
without a residual hold does not itself prevent renaming or additional display
changes.

Remaining blockers identify the route/line, state, clear-proving count and reason.
Direction holds also identify the first occupied or unreadable interval when
available. The editor list describes the last save attempt; retry after the
route or clear proving completes. Real occupancy, train-tail holds and protective
locks are not reset globally. Unrelated GFM catalog changes may proceed while a
direction lease exists; the held axis geometry cannot change underneath it.

## Migration and limits

Existing 0.7.43 installations are not rebuilt automatically on load. Use the
block assistant to preview and explicitly confirm migration. Splitting an old
two-face mast preserves both logical signal IDs/codes and its detector number.
Only a clearly identified old automatic diagram strip is replaced.

An existing 0.7.44 assembly retains its block division in this build. Signal
stagger, operator and diagram positions remain adjustable; replacing its section
division is rejected to avoid orphaning detectors or sections. The shortcut
continues to exclude point areas and does not calculate prototype braking distances.

Previously confirmed parallel routes are retained. Autonomous siding/crossing
control, bot order chains and higher-quality models remain later steps. Public
0.7.40 manuals and all previously prepared archives remain unchanged.

Build, regression and EN/DE checks use the available game libraries. The precise
cause of the reported live situation remains unproven without a current diagnostic
export. Unity gameplay and multiplayer acceptance of this build remain pending;
see [setup and acceptance](TEST-0.7.44.en.md).
