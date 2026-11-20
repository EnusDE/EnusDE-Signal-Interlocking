# 0.7.55-P0 – Editable block views and completed destination links

For Railroader 0.6.3. All participants need the same mod version.

## Edit an existing assembly

In the editor, right-click any field inside a block assembly and choose **Edit block
assembly view: BL-…**. The same editor is available from line setup and through
**Edit display detail and reflection** in the block placement window.

Choose the panel, then select a view:

- Complete track diagram with signals.
- Track and train numbers, without signal symbols.
- Train-number fields only, as a compact one-row strip.

On additional display panels, choose the first physical section in A-to-B order and
how many sections to show. One section is sufficient for an arrival announcement.
Reflection reverses the horizontal order. A separate **Rotate the complete assembly
180°** setting turns the rail, signal directions, permission symbol and label position
together. Both settings affect drawing only, never the real-world signal direction
or the line's permission and route definitions.

Use **Prepare view preview**, optionally click a free new top-left position in the
preview, then **Apply this view**. Closing without applying leaves the saved diagram
unchanged. During pending block construction this updates only the proposal and
returns to placement; construction still needs its existing final confirmation.

The operating interlocking retains a full view with every original number source
and signal assignment. It can be reflected or turned 180°. Reduced views belong on
additional display panels. To move operating responsibility, use the existing block
owner selection. Display detail never grants signal authority.

The assembly keeps its identity, line, section links and per-panel settings across
saving/loading, dragging and block refresh. Hidden display labels and permission
links are retained when returning to complete detail. A confirmed block-division
replacement retains the view settings, adjusts them to the new section count and
links the display numbers to the new sources. Manually added content inside the
group prevents automatic rebuilding rather than being erased.

## Train numbers inside the rail

New and refreshed display assemblies put repeated number fields directly in the
rail row. They read the existing source's number and occupancy/route display; no
second number, detector or operating authority is created. Complete strips use two
rows: label/permission and rail/numbers. Numbers-only views use one row. Existing
saved strips switch to this layout when their view is applied or the diagram is
refreshed; opening a save alone does not move existing fields.

## Tutorial destinations

The tutorial now links every selected departure signal to the corresponding route
destination. Its displayed arrow faces opposite the local entry signal, respecting
explicit displayed head orientation and mirror side. The destination remains linked
to the shared permission. Existing conflicting departure links require review rather
than being overwritten. Inline permission fields support a one-port schematic track
end as well as a through track; this does not change physical detection geometry.

## Verification

Local regression checks cover source identity, display-only authority, section
selection, reflection/180° round trips, number fields in the rail, save/reload,
rebuilding and replacement, manual-content protection, deletion cleanup, multiple
departure links and destination orientation. Gameplay/UI and multiplayer acceptance
remain pending. Local compilation uses the available older game libraries with the
existing 0.6.3 path adapter, not an original-0.6.3 DLL build.
