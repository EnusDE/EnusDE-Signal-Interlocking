# 0.7.54-P0 – Multiple signals in each GWB tutorial step

For Railroader 0.6.3. Host and clients need the same mod version.

The tutorial retains its ordered roles: entry A, departure A, entry B, departure B.
Each role now accepts multiple main signals on the same physical line track.

1. In the current step, click every assigned signal that belongs to this role.
   For example, click both HG departure signals HS-008 and HS-007.
2. Each click adds the signal. All selected fields stay green simultaneously.
   Click a selected signal again, or use its Deselect button, to remove only it.
3. The selected count and signal names appear in the tutorial. The separate diagram
   picker also stays open for repeated selection; Back to tutorial keeps the selection.
   World selection adds or removes one signal per visit and returns to this step.
4. Use Check and continue when the role is complete. Previous/next retains all choices.

The first selected head remains the geometry reference (and for entries, the later
linked destination reference). Removing it promotes the next selected head. Every
selected head is checked for its local ownership and role; all configured endpoints
must share the same free-line corridor and direction before path capture succeeds.
Parallel physical line tracks still require separate connections. Up to 32 signals
per role are supported. Missing coverage, occupancy and opposing permission are
never bypassed by selecting more signals.

Local checks cover multi-selection, independent deselection and green state,
secondary-signal validation, reference promotion, save/reload, shared turnout paths,
control generation and opposing-permission rejection. Gameplay/UI and multiplayer
acceptance remain pending. Local compilation uses the older available game libraries
with the existing 0.6.3 path adapter; it is not an original-0.6.3 DLL build.
