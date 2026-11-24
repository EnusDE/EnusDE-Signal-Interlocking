# Requesting routes at signals and through Auto Engineer

[Deutsch](WAYPOINT-0.7.66.de.md)

Test build 0.7.66 for Railroader 0.6.3. Install with your mod manager and restart Railroader completely. Host and clients need the same main mod version. Runenwacht 0.2.2 remains compatible.

## Request a route at a signal

1. Left-click the signal in the world. If it supports train and shunt movements, choose the movement type.
2. Select a destination in the responsible local interlocking and request the route. The interlocking reports the result after checking the path and its protection.
3. Right-click for object actions. Players with construction permission can open the signal in build mode there.

This test build accepts manual requests at signals on the host. Other players can view the signal and ask the dispatcher to set a route.

## Drive with Auto Engineer

1. Open the gear in the clock toolbar launcher. Under **Operation and automation**, select **Enable automatic waypoint requests**. This enables the host's Auto Engineer signal connection and block coordination.
2. Set a fixed Auto Engineer waypoint beyond the next signal. The interlocking reads the current directed waypoint path and requests the next eligible leg. Auto Engineer continues its normal order to the waypoint.
3. **Locomotive info → Signalling** shows the movement and reason for waiting. Choose Train or Shunt explicitly, pause automatic requests, or ask for one route there. **STOP** in the dispatcher pauses discovery. The host can resume it.

Start with two working signals in the same local interlocking and a fully monitored path. Automatic requests wait if the path is unreadable, track detection is incomplete, or a different interlocking is responsible. Existing routes are released through the regular interlocking. Coupling orders, multi-step reversals and services across interlockings require further work.

## Check track detection

In the **Editor → Menu → Set up and check**, detect and link existing boundaries or inspect a route for gaps. Select the start, destination and movement, then **Find detection gaps**. Known gaps are marked orange on the diagram and listed by track and metre range. Inspecting the route retains existing diagrams, signals and switch links.

Also test a circuit that passes a signal without its own detector and a train moving from signal to point detection. The locomotive status shows why a stopped train is waiting. The game log records changes to that status. Gameplay and multiplayer acceptance for this build remain open.
