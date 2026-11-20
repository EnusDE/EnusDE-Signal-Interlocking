# 0.7.59-P0 – Continue editing saved block views

For Railroader 0.6.3. All participants need the same mod version.

Opening the block-view editor now prepares an editable preview from the current
saved configuration. A second preparation click is no longer required. Changes to
detail, section range, reflection and 180° rotation refresh the working preview
instead of discarding it. The staged position is retained when free; the diagram
can grow to keep that position when changing from a compact to a wider view.
Existing collision and hard size checks remain.

Apply saves the view and leaves the editor open with a fresh working copy of the
just-saved state. Another move, change and save can follow immediately. Closing and
reopening also loads a fresh candidate at its saved position and detail. Each native
window build has its own callback token; callbacks from before a save, an option
change or a native window rebuild cannot modify the current candidate.

The optional button is now **Refresh preview**. It retries validation using the
current working state and keeps its staged position. A failed refresh preserves
the concrete error for Apply rather than replacing it with a generic instruction.
Closing without applying discards only edits made since the last successful save.
The pending-construction workflow still returns to its placement window and requires
the existing construction confirmation; it does not save world construction early.

## Verification

The production-window callback test now uses four sections and exercises the reported
180°-rotated fourth-section view. It covers moving after saving in the same window,
closing/reopening, changing to numbers-only and three complete save/serialize/reload/
reopen cycles across all display details. Separate saved/working library copies model
the game's store. It checks position retention when growing a view, stale callback
rejection after saves and native redraws, visible validation failures and live-number
traffic. The complete project/language/regression suite is also run.

These are headless callbacks and domain checks. Actual game mouse/layout and
multiplayer acceptance remain pending. Compilation uses the available older game
libraries with the existing 0.6.3 path adapter, not original 0.6.3 DLLs.
