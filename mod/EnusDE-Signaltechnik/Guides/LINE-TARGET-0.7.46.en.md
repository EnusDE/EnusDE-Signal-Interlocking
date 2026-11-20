# 0.7.46-P0 — Auxiliary destination bound to line permission and block chain

Internal test build for Railroader 0.6.2. Host and clients require 0.7.46.
Older archives remain unchanged. Includes the 0.7.45 GFM repair workflow.

## Intended operation

The normal auxiliary button remains a route destination. Linking it to a permission
module additionally requires that line's departure permission. A request without
permission refuses even when the former local destination ends before the shared
physical line axis.

With permission, preparation follows the complete path to the neighbour's actual
arrival main signal, including intermediate block signals as component routes.
Normal GFM, point, flank, operating-state and acceptance checks remain active.
Opposing signals stay at Stop; the neighbour's entry signal is not cleared by this
outbound request.

## Setup: for example WHB → ELA

1. Review STR-001 neighbours, real departure/arrival signals and detected physical
   paths. The far arrival must be an available main signal. The local auxiliary
   must not itself replace that arrival signal in the line configuration.
2. In WHB, edit **To Ela**: choose **Auxiliary button function → Route destination**,
   enable train movements and save.
3. Edit permission module **STR-001**. Choose **To Ela · Route destination** under
   **Linked auxiliary button**, then save the field. The module operates permission;
   the auxiliary keeps its destination function.
4. Alternatively select **Required line permission for this destination** directly
   on the normal auxiliary. Either assignment is sufficient; conflicting assignments
   refuse. The auxiliary's local owner determines departure end A/B. A global view
   alone does not replace that local ownership.
5. In Dispatcher, select the start signal and **To Ela**. Without WHB permission,
   the request must explicitly refuse. With permission, the exit and associated
   block chain to ELA's arrival are queued and established after normal checks.
   This manual request needs neither global contact automation nor individual
   block approach contacts.
6. The local drawn destination position no longer shortens this bound request.
   Its actual direction and far endpoint come from the assigned line connection.

A **Line permission controls** auxiliary still opens request/giving only. Clicking
it during start selection now explains that a normal route destination is still
required. **Automatic confirmation** in the permission window remains specific
to permission requests.

## Automatic train requests

For train-triggered operation, configure the start-signal approach contact, its
target automation and locomotive train duty as before. Use the bound normal
auxiliary as the primary target. Enable global contact automation for contact
operation. The same permission and block path are checked; a request never takes
the neighbour's permission implicitly.

Waypoint orders may limit a chain at the current waypoint. Such a shortened
request retains its explicit line-permission binding. Existing consist/order
change checks continue to apply.

## Enforcement and existing routes

Permission is checked before preparation and again during actual admission.
Changing permission during queued work cannot create a wrong-direction clearance.
Every component retains destination/binding identity. Changed or removed bindings
invalidate old chains. Affected active routes and direction holds prevent reassignment.

Re-request or recalculate old local helper routes ending before the line. The new
complete chain carries the binding. Local operation may include only the assigned
line's block signals in the departure direction; other neighbour signals retain
their ownership. Read-only views do not grant operating authority.

## Short GFM sections between points

Repair now offers a shorter valid path between the same boundaries along the same
track corridor. Regression example: an obsolete 57.29-m total interval, current
boundaries at 25.00 and 32.29 m, leaving a 7.29-m section. These are fixture values,
not current positions confirmed from the user's save.

In **GFM inspection and repair**, select the entry → **Re-detect this boundary pair /
find complete replacement** → review the current proposal → **Preview replacement
and related stale entries** → apply. Saved and current positions are shown together.
A mismatch is no longer described as proof that the player moved a boundary. The
full old and new intervals must be clear and free of associated holds on confirmation.
Constructed block-assembly internals remain protected against orphaning.

## Acceptance

- Wrong-end permission: both an old local target before the axis and a complete
  block request must refuse without a green entry signal.
- Correct permission: one request clears the exit and appropriate block signals;
  opposing heads and the neighbour's entry signal remain untouched.
- Give permission away during preparation: final admission must refuse.
- Test changed/removed links, foreign block heads, incomplete GFM, occupancy,
  waypoint limits, save/reload and host/client views.
- For W198/W212, compare actual stored/current positions and preview the valid
  shorter section before replacing stale data.

Build/regression checks use the available game libraries. Unity gameplay on the
current user save and multiplayer acceptance remain pending. Linked diagram
continuations across display breaks remain a separate queued next step.
