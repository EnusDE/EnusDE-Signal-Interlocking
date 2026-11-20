# 0.7.50-P0 – Setup assistant, first test build

For Railroader 0.6.3. All participants need the same mod version. The user reported
successful gameplay, including GWB, with 0.7.49. The new workflows still need gameplay
and multiplayer acceptance. Local compilation continues to use the older game
libraries available in this workspace and the existing path adapter; this does not
prove a build against the original 0.6.3 libraries.

## Open the assistant

Interlocking editor → Menu → **Setup assistant**. Choose world-object assignment,
signal-field setup, line/GWB setup, diagram GFM recognition, detection inspection or
optional receiving capacity. Manual tools remain available. Completed assignments
are saved; an unconfirmed preview is not applied when leaving it.

## Assign existing signals and points

1. Choose **Assign signals and points step by step**.
2. The queue lists completed world signals and points without a controlling local
   diagram assignment. Distant signals are included; read-only copies do not count
   as controlling assignments.
3. The orientation map shows the selected object at the red cross, north up. It uses
   real track positions and sampled curves rather than the mast's lateral offset.
4. Choose **Choose field in diagram**, select the local interlocking and click the
   matching free track/signal field or unassigned point symbol.
5. Review the destination and **Confirm assignment and continue**. Completed objects
   leave the queue; skipped objects remain explicitly outstanding.
6. Verify schematic signal directions and native point arms, then recognize and
   inspect GFM. Schematic coordinates alone cannot determine real point geometry.

Changed objects, diagrams or selection invalidate the proposal. A signal cannot gain
controlling assignments at two local interlockings. Rows/columns remain editable in
the main editor.

## Start from a drawn signal field

Choose **Configure a signal field: existing or new signal**, or open the assistant
from an unassigned signal field's context menu.

- **Yes:** click the existing world signal.
- **No:** select main, shunting or distant signal, then place one signal using the
  existing world preview. Escape cancels.
- Review and confirm the assignment. Material-dependent construction plans must be
  completed first; the assistant does not commission unfinished objects.

## Guided line/GWB setup

1. Select neighbours, track label and mode. The assistant reopens existing connections,
   including when neighbour order is reversed or label case/outer spaces differ.
   Multiple legacy duplicates are reported. Saving also checks physical path overlap;
   genuinely parallel tracks remain separate connections.
2. Select departures and arrival targets using actual station names, optionally by
   clicking their diagrams. Preassigned entry/exit duties are not required.
3. Detect the physical path, review alternatives and GFM proposals, then optionally
   prepare block construction. Remaining topology/detection faults prevent completing
   this check. Missing detection is never inferred clear.
4. Review permission fields and number views, enable protection and apply. Existing
   permission fields remain; move them in the editor. New fields can be placed in
   free cells. A second controlling copy is not silently added.

The optional block-construction confirmation in step 3 is a separate, explicitly
labelled save action. The final page does not replace that confirmation.

### Replace an existing division, for example 2 → 10 blocks

Enable **Set up again: replace existing block division** in block construction.
Choose count or spacing, preview, review detector and diagram positions, then confirm.
Until confirmation, old objects and configuration remain intact. Connection identity
and permission links stay. Associated old block heads, automatic contacts and obsolete
sections are replaced; station endpoint signals remain.

Complete generated compact number overviews are regenerated for the new division
while retaining their group identity. Custom partial views and manually added fields
are not guessed onto new sources. External saved routes, distant signals, auxiliary
targets, point boundaries, receiving tracks or manual displays referring to old
objects block replacement with the specific reference identified. Reassign/remove
those references first. Active routes, direction holds and occupied affected track
continue to prevent rebuilding.

## Optional receiving capacity

1. Select **Configure station receiving capacity** and a local interlocking.
2. Name each physical receiving track, click its eligible arrival signals and select
   its complete actual detection sections. Different receiving tracks must not share
   physical detection.
3. Save tracks individually, then apply a simultaneous acceptance limit no greater
   than the track count. **0** disables this additional check.
4. If the local receiving route is selected later, its approach reservation moves to
   the actual receiving track; a track reserved for another arrival stays protected.
5. Track status shows detection and reservations. Occupied/unknown tracks are not
   available. Incoming routes reserve capacity; a reused automatic route ID cannot
   reuse an earlier train's slot.
6. An outgoing route does not free occupied capacity. After arrival, a reservation
   remains until its receiving detection has been occupied and later cleared, its
   associated routes have ended and three separate clear proofs succeed. An unentered
   cancelled request can release after corresponding clear proof.

This first version requires correctly and completely defined receiving paths.
Reservations belong to the running operating session; track definitions and limits
are saved. This is not full automatic passing/overtaking, bot or communication
scheduling. Train numbers are not an occupancy proof.

## Targeted GFM fix

Directly adjacent point symbols can now receive their connecting detection section
without an invented middle track cell. Native point-arm validation remains required.

## Gameplay acceptance

Use a backed-up test world. Check existing connection reuse after reopening/reloading;
existing/new signal assignment, cancellation and diagram edits during selection;
main/shunt/distant displays; block count expansion/shrink, cancelled previews and
external-reference failures; complete compact number overviews; permission in both
directions, opposing departure rejection, block signals, tail release and number
transfer; two receiving tracks with a third incoming train, unknown/occupied detection,
cancelled arrivals and an outgoing route alone; adjacent-point detection and occupancy.

The reported visual displacement in the previous map has not been conclusively
diagnosed without a concrete affected dataset. The new orientation map is an
additional view, not a verified fix for every existing map projection.
