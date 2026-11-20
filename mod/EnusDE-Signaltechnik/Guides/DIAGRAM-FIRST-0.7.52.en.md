# 0.7.52-P0 – Start in the diagram, assign with a world click

For Railroader 0.6.3. All participants need the same mod version.

## Short workflow

1. Open the desired local interlocking diagram in the editor.
2. **Menu → Setup assistant → Assign signals and points from this diagram**.
3. Click the desired unassigned signal/point field directly in the main diagram.
   A previously selected open field is preferred; otherwise the first open field in
   this diagram is marked.
4. The current target has a **bright green background and border**. Other open fields
   have a darker green background. The assistant shows its caption and spreadsheet
   coordinate. **Show green field** brings it into view.
5. Choose **Select this signal in the world…** or **Select this point in the world…**,
   then click the matching real object.
6. **The world click saves the assignment immediately.** No additional confirmation.
   The same local diagram reopens and the next open field is marked green. You can
   click another open field whenever you want.

While selecting, the signal tooltip says **Click: assign to green field** with the
cell coordinate. A single enabled main-signal head can be selected from either side
of its mast. With two enabled directions, the clicked mast side remains significant.

**Escape** during existing-object selection returns to the same target without
assigning it. **Skip this field for now** leaves it explicitly outstanding. **Recheck
open fields** includes skipped fields again. Completed assignments remain saved after
reloading.

## Removed from the primary workflow

No global signal/point list, identifier search or empty map area is required. The
assistant works through the drawn open fields of the current diagram. Other local
interlockings are not assigned as a side effect. After world selection, this workflow
explicitly restores the local diagram rather than leaving the interlocking catalogue
open.

## Missing world signal

Only then expand **Signal missing? Place a new one…**, choose main/shunting/distant
signal and use the existing construction preview. Confirmed placement of a completed
signal assigns it to the selected diagram field. Material-dependent plans must be
completed before operating assignment. Existing-object selection needs none of these
construction options.

## Checks and limits

Assigned fields, read-only copies and train-number displays are excluded from the
open signal/point queue. Custom field captions stay. Signal/point types and exclusive
local ownership are checked. Drawing changes during world selection, including row
or column changes, require a fresh target click; stale callbacks cannot overwrite a
shifted field. GFM recognition, native point-arm calibration and operational inspection
remain separate tools. Diagram assignment alone does not prove clear track.

Local project, language and regression tests cover targeting, assignment, persistence,
types, ownership and stale drawings. Actual highlighting, world-pick return and
multiplayer interaction still require gameplay testing. Local compilation uses the
older available game libraries with the existing path adapter; this does not verify
a build against the original 0.6.3 DLLs.
