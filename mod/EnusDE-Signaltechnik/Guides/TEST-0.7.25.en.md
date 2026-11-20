# 0.7.25-P0 – setup and short gameplay tests

Railroader 0.6.2. Replace the mod, restart the game and keep your existing test save. Host and clients need matching versions. Offline checks do not replace the gameplay checks below.

## Recognition and master views

Open **Editor → Interlocking selection → Automatic recognition**. By default, drawing is limited to 1000 m along the tracks around placed mod signals. The physical section catalogue still covers the captured network. The drawing radius accepts 100–10000 m. Marked network boundaries are drawing limits, not physical track ends or movement authorities.

Generate a proposal, inspect its preview and explicitly apply it. Continuous common/normal switch legs form horizontal schematic strands; other tracks occupy parallel lanes. World curves no longer determine the drawing. Disable the scope to draw the entire network; whole maps can still be very large and difficult to read. Start with a small area.

The previous drawing is archived on replacement. Incomplete proposals are rejected. Limits are 2048 × 512 cells, 65536 fields and 16 MiB for the shared interlocking document, including archives. Use a smaller scope or a new empty master view if needed.

Existing signal IDs and detected points are linked in the new sketch. Enabled detection boundaries are considered and matching existing section IDs are reused. Missing boxes do not become valid detection equipment merely because a diagram was drawn. Unbounded or invalid sections remain unmonitored or unknown.

Choose **Modules** next to a master in the editor catalogue. Select a local interlocking and insert its linked module. Edit X/Y and choose **Move whole module** to update the visible arrangement. Use **Edit selected local diagram** for individual tracks. The arrangement view does not operate trains. Master-owned fields and linked local drawings remain separate.

## Detection quick tool and A/B/C

[Model preview](images/gfm-abc-preview.png) – rendered from the procedural model data, not an in-game screenshot.

Open **Build → Detection boundary quick tool**, choose an action and activate it. Click successive world signals; the tool stays active. **Escape** stops it. Stop the tool before opening a normal object menu.

- **A:** signal-attached boundary. New signals default to A; enable it explicitly on existing signals without boxes.
- **B:** free-standing or automatic point boundary. Legacy free A boxes are treated as B with their existing boundary IDs and numbers.
- **C:** only the arrow side forms a monitored section. Its back cannot close another section. Reverse monitoring direction independently on signal or point boxes. The C arrow follows logical track direction; cosmetic yaw is ignored for C.

Sections cannot skip an intervening configured boundary. Invalid or changed boundaries make an existing section unknown. Changing a point box's logical C role requires routes and direction locks to be released. Moving its model does not move its logical boundary.

Attached boxes have scale-dependent mast clearance. Nearby alternative positions are tried if another model obstructs them. If none fits, the box is hidden and its object menu reports the problem. Check these positions visually in game.

## Signal codes

Technical signal codes and direction IDs are read-only; display names remain editable. Upgrading a shunting signal assigns an HS prefix while retaining its internal ID and detection number. The former RS code is inactive history only. Previously upgraded RS signals are corrected when opened. Other custom legacy codes are retained rather than globally renumbered.

Numbers are not automatically recycled in this build. Reuse requires complete cleanup of old references first.

## AI checks

1. With AI signal integration enabled, give a waypoint beyond an own main or shunting signal at Stop, without a set route. The AI should stop. Own signals are registered for native AI even before diagram assignment. The reported red-signal overrun still needs gameplay reproduction.
2. Approach a wrongly lined electric point with waypoint AI, both facing and trailing from the wrong branch. It must neither throw the point nor run through it. Protection now also covers the native look-ahead throwability check.
3. With contact and signal automation configured, request a diverging waypoint path. Matching the waypoint must precede preference for currently lined points. The interlocking lines the protected route; detection, occupancy and lock checks still apply.

Automatic recovery moves, reversing to a signal, passenger-specific rules and a general autonomous timetable controller are not included. Retrying a waiting request never authorizes an unprotected movement.
