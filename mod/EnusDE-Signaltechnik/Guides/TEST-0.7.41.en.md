# 0.7.41-P0 acceptance tests

Automated build, localization and regression checks are run through Test.ps1. Actual gameplay and multiplayer acceptance have not been performed.

1. Place a module on a monitored rotated rail; verify rail connections and GFM colour before/after placement and confirmed symbol removal. Verify the left arrow above/right arrow below match the supplied concept, rotation and zoom.
2. Configure two neighbours and one bidirectional track. Before initial full-line clear proving no departure is permitted. After three independent clear readings the configured normal end owns permission.
3. Request from the other end. Ownership stays unchanged. A route request in the opposing direction must fail; the owner can still dispatch subject to route/block checks.
4. Attempt giving from the wrong end, with old controls, a train on the line, an active route, a residual block hold or unknown detection. Each must fail without changing ownership. After full release and clear proving the owner can give once.
5. Verify requester-only cancel, optional unsolicited owner giving, automatic request replies, pause/resume and central takeover. Automatic mode must never invent a request from a departure command.
6. Configure each parallel track independently with opposite normal directions. Verify one track's request/owner never changes the other.
7. Configure a permission-only auxiliary without a world position. It must open controls without a start and never appear as a route, automation or arrival destination. Verify normal auxiliary destinations keep approach direction and route behaviour.
8. Verify local station and host master end selection, client indications, stale-action rejection, deletion/link validation and reload initialization. Reload must not restore a green signal or old pending request.
9. Keep an occupied block on the requested path: permission alone must never bypass the normal block refusal. Check following trains separately; aggregate block checks remain unchanged.
10. Verify no track/world object is built and no materials are charged by setup or permission operations. Use an existing test installation and compare its objects/materials before and after.

[Setup guide](FREE-LINE-GWB.en.md). The 0.7.40 release ZIP remains unchanged.
