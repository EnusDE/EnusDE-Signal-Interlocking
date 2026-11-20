# 0.7.41-P0 — explicit GWB permission and inline diagram module

- Added inline permission modules with two outlined opposing arrows and continuous diagram rail. Placement on a plain rail preserves ports, section and half-rail GFM assignments; old floating indicators remain supported.
- Added explicit neighbour request, owner-only giving and requester-only cancellation with station/end checks and stale-control revisions.
- Departure requests cannot acquire or reverse line permission. Permission, route protection and block checks remain separate. Existing block occupancy checks stay fully active.
- Added a configurable normal permission direction after startup clear proving, manual request/give defaults for new lines, optional giving without a request, and optional safe automatic replies to explicit requests.
- Direction changes require three complete-clear samples, no routes/residual holds and a current whole-line clear check. Permission remains with its owner between trains; parallel tracks remain independent.
- Auxiliary buttons can be configured as route destinations or permission controls. Permission-only buttons need no physical anchor and are excluded from all route and arrival target pickers.
- Added shared live request/owner/revision indications, module-to-auxiliary bindings, host master end selection, migration validation and deletion cleanup.
- No world construction, track changes, new detector placement or construction material charges.
- Updated English/German UI, setup guide and tests. The general tutorial remains WORK IN PROGRESS. Runtime gameplay and multiplayer acceptance remain pending.

[Setup and operation](../mod/EnusDE-Signaltechnik/Guides/FREE-LINE-GWB.en.md) · [Tests](../mod/EnusDE-Signaltechnik/Guides/TEST-0.7.41.en.md)
