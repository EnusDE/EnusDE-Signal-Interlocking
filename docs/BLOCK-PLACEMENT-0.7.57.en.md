# 0.7.57-P0 – Stable block preview placement

For Railroader 0.6.3. All participants need the same mod version.

The view editor no longer rebuilds its window or refits the camera after a valid
placement click. The current assembly stays visible, while pointer movement shows
a separate green/red destination preview. Leaving the diagram clears that temporary
overlay instead of moving it to an invalid negative coordinate.

Choose a free top-left destination in the preview. **Placement status** confirms
**Position set** and the chosen column/row, or explains a collision. The **Move left,
right, up, down** buttons below the diagram provide exact one-cell positioning.
Then choose **Apply this view** to save. Closing without applying still discards the
proposal. Recreating a preview from its settings starts again from the captured base.

The preview includes clickable room beyond the current right/bottom panel edges.
A successful move grows the proposed panel as needed. One-cell moves also support
left/top growth. Existing surrounding fields, assembly profiles, number identities
and permission-field coordinates are retained. Overlapping fields or managed groups
and the hard 2048-column/512-row limit remain blocked. Hover alone makes no changes.
The block-construction placement window uses the same growth/collision rules;
construction is still saved only with its existing final confirmation.

Local regression checks cover full-width offset placement, repeated placement,
non-mutating hover/display, collision rejection without partial changes, left/top
growth, hard limits and retained unrelated permission bindings. Actual mouse input,
UI scaling and multiplayer still require gameplay acceptance. Compilation uses the
available older game libraries with the existing 0.6.3 path adapter, not original
0.6.3 DLLs.
