# 0.7.53-P0 – GWB as a step-by-step setup tutorial

For Railroader 0.6.3. Every participant needs the same mod version.

The assistant focuses on complex line/GWB setup. Individual signals and points can
still be linked through the existing diagram context menu without a tutorial.

## Start

Open a local interlocking → Menu → Setup assistant → **Set up or change a line / GWB
connection**. Required main-signal heads must already be built and assigned to their
local diagrams. Preassigned entry/exit roles, target buttons and completed GFM are
not prerequisites for the initial geometry search. A missing physical departure
main head still needs to be built/assigned: a destination button cannot replace it.
Entry and departure can use different enabled heads on one mast.

## Nine steps

1. Choose the neighbouring local interlockings by their actual names and identify
   the physical track. Existing connections are reused. This tutorial covers
   single-track/GWB operation; existing purely directional tracks retain manual setup.
2. Select the first station's **entry** from the neighbour, facing the approaching
   train outside the station.
3. Select the first station's **departure toward the neighbour**, facing outward and
   supporting a train proceed aspect. Do not select the same inward head for both roles.
4. Select the second station's **entry** from the first station.
5. Select the second station's **departure back toward the first**. Both directions
   must use the same shared physical line track.
6. Review **track geometry and detection**. Choose between physical alternatives if
   offered. Enable missing point detection on the selected path and apply section
   proposals. Geometry discovery does not require a fully designed operational route
   and never clears signals. Remaining GFM can also be constructed in the block step;
   complete required coverage is mandatory before activation.
7. Review **destination and permission controls** for both neighbours. Missing
   destinations reference the local entry in the opposite direction and link to
   the shared line permission. Suitable existing controls are reused. Permission
   positions can be chosen in the diagram. This preview makes no persistent changes.
8. Optionally configure **intermediate blocks**, spacing, contacts and diagram
   positions using the existing construction tools. Explicit block-construction
   confirmation is a separate save action and includes its destination/permission
   links. Tutorial construction keeps the line inactive until final completion.
   Material-dependent plans must be finished first.
9. Run **final checks**, then complete setup and enable line protection. Permission
   initially remains without authority until its normal clear proofs succeed.

Each signal page shows only one selection. Click its already assigned symbol in
the main diagram, use diagram selection or choose the head in the world. The tutorial
opens the appropriate neighbour's diagram and marks the current selection green.
**Check and continue** is offered after a suitable single signal is selected. Additional
departures sharing the same line may be added later through manual settings.

## Guided feedback

An incomplete or unsuitable selection stays at its step with the next action
explained. Returning from world selection restores the same tutorial page. The
four simultaneous signal-group forms are no longer shown in tutorial mode.

Geometry setup is independent of operational route/flank-protection calculation.
Wrong directions, disconnected track and missing detection remain invalid; they
are never inferred safe. Existing route, occupancy and direction holds remain
protected. Opposing-permission departures remain rejected.

This configures the line connection to a future passing/overtaking site. Complete
automatic crossing/overtaking scheduling, bots and communications remain separate.

## Verification

Local project, language and regression checks cover ordered signal steps, ordinary
main heads without preassigned roles, geometry before GFM/target setup, unchanged
operating data during preview, linked generated controls, existing-field reuse and
opposing-permission rejection. Actual interaction in your save and multiplayer still
need gameplay testing. Local compilation continues to use the older available game
libraries with the existing path adapter, not a verified original-0.6.3 DLL build.
