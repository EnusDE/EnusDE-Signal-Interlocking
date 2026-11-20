# 0.7.58-P0 – Block view actions remain usable during train-number traffic

For Railroader 0.6.3. All participants need the same mod version.

The previous view editor compared its opening installation revision with the live
revision. Assigning or forwarding a train number also increments that revision.
After such a normal operating update, option callbacks, preview preparation and
placement returned without acting. An existing snapshot could therefore become
unusable even though no construction or diagram configuration had changed.

The block workflows now compare construction data separately: world objects,
material/placement settings and retired signals remain protected. Live train-number
assignments and signal-number fields do not invalidate the proposal. Saving object
construction still starts from the current installation, retaining its latest live
numbers. Actual configuration changes, changed construction, world changes and a
replaced pending proposal remain reasons to refuse the old proposal.

The view window has a persistent status field with errors and the next action.
Apply remains visible. If no preview exists it asks to prepare one and saves nothing.
Preview and save failures appear in this window instead of only the main editor.
Repeated same-value control events leave the current callbacks valid; old callbacks
from a previous option state remain ignored. Reopening uses the current shared saved
configuration. Temporary data unavailability is explained and can recover on retry.

Workflow: choose settings → Prepare view preview → optionally place → Apply this
view. When editing a pending construction proposal, applying stages the view and
returns to placement; the existing construction confirmation is still required.

## Verification

A new test compiles the production BlockViewWindow.cs with UI adapters and runs its
actual callbacks in deferred-rebuild order. It covers number traffic between option,
preview, placement and save actions; same-value notifications; stale callbacks;
missing preview; genuine changes; temporary readiness; visible domain errors; and
return to pending construction without an early save. Pure snapshot tests also
check that real signal movement/direction/removal and placement/material settings
remain protected and that current numbers survive object application.

This is a headless callback test, not an actual game mouse/layout test. The complete
project/language/regression suite is also run. Gameplay/UI scaling and multiplayer
acceptance remain pending. Compilation uses the older available game libraries with
the existing 0.6.3 path adapter, not original 0.6.3 DLLs.
