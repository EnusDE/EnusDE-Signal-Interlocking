# 0.7.29-P0 – invalidity crosses, identification lights and direct point editing

Railroader 0.6.2. Replace the mod-manager package, restart and keep the existing test save. Host and clients require the same version. Includes all changes from [0.7.28](TEST-0.7.28.en.md). Offline checked; gameplay and multiplayer validation remain pending.

## Operating states

| State | Display | Effect |
| --- | --- | --- |
| Not connected | Normal lamps off; white cross with black outline | Native AI ignores this own signal. |
| Connected and in service | Ordinary aspects | Normal route, occupancy and locking checks. |
| Connected but out of service | Single white identification light; normal lamps off | Native AI bypasses this own signal. This is not interlocking movement authority. |

A saved signal/shunting-button link commissions the signal. Existing stored or running route installations also retain commissioning without a separate diagram field. A separate distant signal can inherit its announced main signal's area and shutdown. Merely placing a signal or selecting its role does not commission it. Material construction status remains separate.

The diagram shows the cross or a single white identification dot, with × or K in its caption. Identification light is distinct from Sh 1. Unavailable or invalid operating data retains Stop; failure does not automatically switch the installation off.

## Individual signal shutdown

Open the signal → main-signal direction A/B → **Signal operating state → Take signal out of service**. Alternatively use the assigned diagram field's properties. **Remove individual shutdown** clears that individual setting; a disabled owning interlocking still takes precedence.

Individual shutdown is stored per signal direction. It does not release points or enable target automation.

## Interlocking shutdown for rebuilding

**Editor → Interlocking selection → Manage beside the interlocking → Interlocking operating state → Take interlocking out of service**.

Also available under **Automation and AI → Interlockings and acceptance**. A master covers its own assignments and linked local interlockings. Unlinked local interlockings remain independent.

Affected routes, retained locks and direction reservations must be safely released first. Unrelated running routes may remain. Affected waiting requests are withdrawn. No existing lock is silently deleted.

Connected signals then show identification lights. Points owned exclusively by the disabled area become available to waypoint AI again. Points still used by an active interlocking remain protected. Their electric model remains visible. Native CTC, occupancy, collision and speed rules still apply; stock and third-party signals are not disabled.

**Return interlocking to service** restores ordinary signal and point-protection rules. Individual signal shutdown remains set until cleared. Previous clear aspects are not restored; new movement needs a fresh checked request. Visibility, acceptance and automation pause remain separate and cannot release points.

## Direct point editing

Right-click track near the point → **Edit point**. Opens that point's drive, detection and box configuration directly. **Assign point** remains the separate diagram-linking workflow. Both choose the nearest point within 30 m; equally close points require a closer click.

## Gameplay checks

1. Place an unbound main or shunting signal: cross, normal lights off, no own native Stop target. Bind it and check that the cross disappears and ordinary Stop applies.
2. Shut down one bound signal: identification light, with no release of its electric point.
3. Release affected routes and shut down the local interlocking: check identification lights and native point handling. An active neighbour must remain protected. An attempt while an affected route is locked must be rejected.
4. Restore service: ordinary Stop without a route, electric points protected again. Include an AI train that had already been waiting at the signal.
5. Check different mast directions, low main signals and separate distant signals, then save/load and check a fresh client view.
6. Use **Edit point** on two different points. Each should open its own configuration without starting diagram assignment.

Earlier general AI stopping and point run-through reports remain unconfirmed in gameplay.
