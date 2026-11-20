# 0.7.47-P0 — Compact train-number overview

Test build for Railroader 0.6.2. Host and clients require matching version 0.7.47.
Includes 0.7.46 changes. Back up the installation before gameplay testing.
Older test archives remain unchanged.

## Display model

A narrow row of 1–16 number fields mirrors existing original fields. A source may
belong to another interlocking or a manually drawn panel. This creates no additional
block installation, world signals or independent train-number assignment. The
selected viewing interlocking does not need a full line-diagram copy.

Example: three physical blocks, two views of the same data:

    Full operation:  A ── Block 1 ── Signal ── Block 2 ── Signal ── Block 3 ── B
    Compact view:         [12345]                 [-----]                 [-----]

The actual compact view uses three adjacent cells, without the full diagram's
spacing or signal controls. It is read-only. Number and occupancy come from each
source. A missing source displays `?`; stale numbers are not treated as current.

## Setup

1. Open **Interlocking editor → Open line, blocks and bidirectional working** and
   select a saved line. Apply new or changed line settings separately first; the
   overview uses the saved line configuration.
2. Find **Compact train-number overview**.
3. Choose **Display on interlocking**, for example VITA or ELA. This selects the
   viewing board, without transferring signal ownership or line permission.
4. Choose **Create new overview**, or select an existing overview to edit it.
5. Set **Number of fields**. **Use block order A → B** or **B → A** arranges existing
   block sources from left to right. A/B are the neighbours configured under
   Connection. A smaller field count uses the first sources of that order.
6. Each source is also individually selectable. For a one-field arrival announcement,
   choose the last block before the desired station. This mirrors that block
   regardless of train direction; it does not grant direction authority. Manually
   drawn panels can supply sources too. When automatic sources are ambiguous or
   missing, select the original fields explicitly.
7. Open **Preview and place number overview**. Click the desired leftmost position
   on the target board. Green means free; red means occupied or outside the panel.
   A valid click saves the complete strip. **Cancel placement** discards the
   proposal, including any newly assigned source IDs.
8. To reposition it later, open the target board in the editor and grab any number
   field in Select mode. The complete strip moves together. Existing fields cannot
   be overwritten.
9. **Remove this overview only** removes the viewing fields while retaining all
   original sources, numbers and physical block installations.

Several compact views of one line are supported. A full diagram remains separately
selectable in the existing block setup. Existing full views are not automatically
reformatted by this version.

## Handover and ownership

The last line field retains its original source. An arrival announcement can mirror
that source. Actual transfer into a station track still requires the existing
verified passage and correctly configured receiving fields/routes. Showing a number
alone assigns no train to a station track and changes no GWB permission.

Rebuilding the full block diagram or changing its operating interlocking retains
stable source links. If a source is deleted, the overview displays `?` instead of
guessing a replacement. Deliberately select its new source afterwards.

## Verification and remaining gameplay acceptance

Automated checks cover source order, mirroring, missing/stale sources, group
movement, collisions, persistence, manual sources, full-diagram rebuilding and
operator transfer. They do not replace gameplay or multiplayer acceptance.

In game: place three fields on a neighbouring board, run a train through the blocks,
compare source and mirrored displays, move the strip, save/reload, and observe the
existing number handover at the receiving entry signal. The placement preview is
built against the installed game libraries but has not received visual acceptance
in a running Unity UI here.

Separate follow-ups: logical diagram continuations across layout breaks, narrower
full line groups with number fields inside block sections, configurable sides for
automatically placed signals, and small neighbour-board views. These are not part
of 0.7.47.
