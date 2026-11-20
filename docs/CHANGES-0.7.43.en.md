# 0.7.43-P0 — Click-based setup

New internal test build based on 0.7.42. Existing prepared ZIPs stay unchanged.
Railroader 0.6.2; host and clients need the same version.

- Auxiliary destinations can propose an unambiguous connected entry signal or
  select a reference in a diagram, in the world or by name. Its opposite approach
  becomes the destination. Save the field to confirm; advanced coordinates remain.
- Stable reference IDs follow moved/renamed signals during route validation.
  Missing/unbuilt references cannot authorize routes; active target routes and
  line holds protect their reference signal against modification.
- Each line endpoint group supports diagram/world selection. A separate diagram
  picker includes an interlocking selector and eligible-field highlighting.
  Unique existing entry/exit duties can complete empty groups; ambiguity is reported.
- A local line indicator can add a read-only neighbour-entry indication in a free
  adjacent cell. Granted permission arrows are filled for clearer visibility.
- Optional block construction previews 1–16 sections by count or approximate
  track distance, editable boundary distances, reuse and both signal faces where
  needed. It stages physical GFM sections and a block diagram with number fields.
  Optional approach contacts receive next-block targets in both directions without enabling global automation or replacing existing rules. Confirmation commits objects/configuration together under existing supply rules.
- Three kilometres / three blocks means two intermediate boundaries. Existing
  intermediate signals cannot be skipped. Exact active detector endpoints are
  required. This shortcut excludes point areas and does not calculate prototype
  braking distances. Material-dependent objects remain construction plans until built.
- In Select mode drag a field/selection with a transparent green/red destination
  preview. Drop commits, Escape cancels, collisions reject. Bindings and source
  number identities survive; existing Undo remains available. Empty space pans.
- Existing contact automation now supports diagram selection for start, primary
  and alternate destinations. Line siding and overtaking station have separate
  classifications; central site settings link directly to local configuration.
- Compatible routes may share monitoring-only corridors and unchanged protecting
  point positions. Traversed footprints, opposing point demands, controlled flank
  signals and unreadable occupancy remain protected. One release does not remove
  another route's locks; legacy geometry retains conservative conflict checks.

This is the first implementation of the agreed setup simplification. It does
not add vehicle-specific multi-step bot orders, player communication/accounts,
siding-working/return procedures, automatic crossing-station admission,
route-dependent multi-target distant signals or point-arm speed profiles.
Changing site classification alone enables none of those procedures. The public
0.7.40 manuals remain version-locked and unchanged.

Built and checked offline against supplied game libraries. Actual drag rendering,
terrain/model placement and host/client behaviour require game acceptance.
See `Guides/TEST-0.7.43.en.md` and its German counterpart.
