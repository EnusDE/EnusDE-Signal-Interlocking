# GWB permission module — 0.7.41-P0

This is interlocking software and a track-diagram module. It does not build world objects, move track, place detectors or charge construction materials. Existing signals and GFM must already be configured. Railroader 0.6.2; matching host/client mod versions required. Gameplay and multiplayer acceptance are pending.

## Configure a line

1. Open the interlocking Editor. In Add fields choose **Line permission** and click an empty cell or a plain drawn rail field. New modules include a continuous diagram rail; existing rail ports and GFM assignments are preserved. A switch or existing signal cannot be overwritten.
2. The line setup opens. Choose the two local neighbouring interlockings A and B, one physical track, and its mode: single track, fixed directional track A → B, or GWB.
3. Assign the departure and arrival groups. Several departures can share a line. A route-destination auxiliary target can serve as an arrival; a permission-only auxiliary button cannot.
4. Detect the line path, check coverage and apply. Existing detection and point boundaries are required; direction protection is not a substitute for GFM.
5. In bidirectional modes choose the normal permission direction used after startup clear proving. New lines default to manual handling with a neighbour request required. Optionally allow deliberate owner giving without a request, or automatic replies to explicit requests when all conditions are met.
6. On two parallel GWB tracks create two independent line definitions, normally with opposite normal directions. Do not create two permissions for one physical track.

## Connect an auxiliary button

Right-click the auxiliary field → Properties. Choose **Route destination** or **Line permission controls**.

- Route destination retains its real-track approach and movement settings. Select a start signal, then the target to request its ordinary route.
- Permission controls needs no world anchor or start-signal selection. Assign the line, save, then click the button in Dispatcher to open request/giving controls. This function cannot be used as a train, shunt, automatic-route or line-arrival destination.

Right-click the permission module → Properties to select its line and optional linked auxiliary button. In permission mode both must name the same line. Linking does not silently change the auxiliary function. The module also opens the shared line controls; a linked destination can be selected there after choosing a start.

Do not repurpose a destination used by saved/active routes or line arrival groups. Remove those references first. Mode changes are blocked while routes or line direction holds remain.

## Operate permission

After startup, the whole physical line must provide three separate complete-clear readings before the configured normal owner is established. Live owner, requests, holds and signal aspects are not restored from a saved green indication. If the line is occupied at startup, it waits.

The arrow in field direction means A → B; the opposite arrow means B → A. Rotate the field to match the diagram. A highlighted arrow shows ownership; a flashing opposite arrow shows a request. The rail keeps its independent GFM colour. Read the station names and details in the line controls when in doubt.

1. At the end without permission, choose **Request permission from neighbour**. The owner remains unchanged while the request waits, including while a train occupies the line.
2. At the owning end, choose **Give permission to neighbour**. Only that owner may give. The transfer requires the whole line clear, no active routes or residual holds, three complete-clear readings and another current clear check at the command.
3. The requester can cancel its own request. The owner cannot cancel it on the requester's behalf. Optional automatic granting answers an explicit pending request only after the same full-line proving; a departure request never creates or grants a permission request.
4. Permission remains with the new owner between trains. Every departure still needs its ordinary route, point, flank and block checks. The existing block-chain occupancy checks remain active and can independently refuse a following train; this update does not loosen them.

A local interlocking operates only its own end. The host's master view must explicitly select the end it represents. This is not a new player-account dispatcher-role system. Clients receive indications; operation remains with the host. Refresh controls after a request or owner change; stale actions are rejected.

## Existing layouts and limits

Old floating indicators remain floating until **Inline module with continuous diagram rail** is enabled in Properties. New modules are inline by default. Remove an inline symbol through the normal confirmed single-field deletion to retain connected underlying rail. Drawing through a module is refused; move/remove it first.

Existing stable line, signal and target IDs remain. Legacy auxiliary targets stay destinations. Existing automatic-line settings now mean automatic replies to explicit permission requests, rather than changing direction as a side effect of a route request. Normal direction defaults to A → B unless configured otherwise. Review this before operating an old layout.

The general separation of Erlaubnis, block availability and route protection follows German operating concepts. Request/accept is a configurable gameplay policy, not a claim to reproduce every German block type. A specific DB rulebook installation and actual gameplay remain to be checked. This update does not fix all earlier detection geometry issues or add automatic renumbering.
