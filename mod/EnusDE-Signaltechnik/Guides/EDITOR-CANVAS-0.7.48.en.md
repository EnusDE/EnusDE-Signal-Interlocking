# 0.7.48-P0 — Editor with an expanding working area

Test build for Railroader 0.6.2. Host and clients require 0.7.48.
Includes the compact number overview from 0.7.47. Older ZIP archives remain unchanged.

## Editor controls

Column headers **A, B, … Z, AA …** appear above the diagram, with row numbers
**1, 2, 3 …** on the left. Headers stay visible while scrolling. At low zoom some
captions are skipped for readability, but every individual row/column remains clickable.

    All  | A | B | C | D | …
    -----+---+---+---+---+
       1 |   |   |   |   |
       2 |   |───────|   |
       3 |   |   |   |   |

- **Click a header:** select its complete row or column.
- **Shift-click:** select a contiguous range starting from the first header. Switching
  between rows and columns starts a new anchor.
- **Right-click a header:** insert before/after the selection. Selecting several
  rows/columns inserts the same number of new rows/columns.
- **All:** select the complete panel. Existing selection tools remain available.
- **Menu → Extend working area:** explicitly add at the top, bottom, left or right.
- **Plus headers at the outside edge:** add a row/column there.

These headers and editing functions do not appear in dispatcher mode.

## Automatic expansion

A narrow virtual border surrounds the saved panel in the editor. Drawing track,
placing a symbol, pasting a copied region or moving to this border expands the saved
area when the edit is applied. Two free cells of margin remain when possible.
An ordinary selection click does not enlarge the saved panel.

Adding at the top/left shifts existing fields and named areas together. Signal,
point, GFM, auxiliary-target and stable train-number source identities are retained.
Number mirrors remain linked to their sources. The viewport compensates for the
shift rather than automatically recentering the diagram.

Dragging complete rows/columns includes any touched generated block or number groups
in full. The selection may grow to keep those groups together; other selected fields
remain included. Red destinations cannot be committed. Releasing outside the drawing
viewport cancels the move.

Expansion and its associated edit save together. A refused destination preserves
the previous configuration. **Undo/Redo** also includes expansion. Prefix growth of
a master panel translates its embedded module positions; undo translates them back.

The limit remains **2048 columns × 512 rows**. Existing edge cells can be edited at
the limit; actual overflow refuses. If expanding a local panel would collide with
another master module, make room in the master layout first. Other modules are never
overwritten.

Explicit insertion **inside** an existing drawing creates blank cells. Bindings
remain intact; connect these internal drawing gaps with **Draw track** where needed.
Automatic expansion outside the drawing creates no gap within existing track.

## Zoom

The mouse wheel in the main editor zooms towards the pointer, retaining the diagram
point under the cursor at the same screen position. Zoom buttons use the drawing
viewport centre. **Fit diagram** explicitly centres the view on drawn content.
Dispatcher mode retains its previous zoom behaviour.

## Verification and gameplay acceptance

Automated checks cover growth in four directions, limits, selection anchors, captions,
whole-group movement, collisions, preserved bindings, save/reload and reversal of master
module translations. The mod builds against the installed game libraries. Visual
acceptance in the running Unity interface and multiplayer testing remain pending.

Later gameplay checks:

1. Open a small panel and draw or place a symbol at each of its four edges.
2. Compare a number mirror and its original before/after top/left expansion.
3. Click a row header, Shift-click another, right-click and insert before/after.
   Move the complete selection, then test Undo/Redo.
4. Position the pointer over a distinct diagram feature and zoom in/out repeatedly.
5. Save/reload, then confirm dispatcher mode has no editor headers.
