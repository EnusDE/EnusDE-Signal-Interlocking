# 0.7.23-P0 test guide

Railroader 0.6.2. Install the same version on host and clients. Use a copy of your installation; live gameplay and multiplayer acceptance are still required.

- **Vehicle information:** open the vehicle's **Signalling** tab, then **Signalling information…**. Movement-role controls now have their own resizable window. They are not appended to the normal car panel.
- **Editor:** right-click the actual field to select it and open its applicable actions. Editing uses a separate window, preserving the diagram's full width. General functions are under the header Menu.
- **Point assignment:** select a world point, choose the target interlocking, then click an unassigned point field. Alternatively, right-click a point field → **Assign in world…**, then right-click the real point → **Assign point**. Escape cancels. Replacing an existing assignment still needs explicit acceptance.
- **Routes:** select start and destination in Dispatcher. By default, each route is calculated from the current track and protection data. No new persistent route-catalogue entry is created. Protection, occupancy and direction checks still apply.
- **Fallback:** Game options → Signal & Interlocking → Automation and AI → Route calculation. Disable **Calculate routes on request**, then **Apply route mode**, to use the previous saved-route workflow. First clear active routes, direction locks and pending contact requests. Old definitions remain available.
- **Open line / GWB:** Editor → Menu → Interlocking functions → Open line / GWB, or the master-module window. The shortcut has moved out of the interlocking-selection list.
- **Camera:** right mouse no longer cancels object placement. Use Escape to cancel.
- **Start selection:** an incomplete start selection expires after five seconds. It does not cancel a complete request, active route or contact-automation task.

First test: vehicle tab/window, two point-assignment directions, free start/destination route, occupied/locked rejection, new target track, primary/alternate contact targets, opposing block direction, host/client indication, save/reload and safe cancellation. Do not use experimental contact axle counts to release occupancy.

Not included yet: full diagram regeneration, additional independent master interlockings, new A/B/C semantics, route-dependent multiple distant-signal targets, or configurable numeric route-speed limits.
