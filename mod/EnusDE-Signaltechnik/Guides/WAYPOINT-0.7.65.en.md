# Waypoint automation in 0.7.65

[Deutsch](WAYPOINT-0.7.65.de.md)

Test build for Railroader **0.6.3**. Install with your mod manager, then restart the entire game. Host and clients need the same main mod version. If you use Runenwacht, install **0.2.1 for 0.7.65**.

## Start a movement

1. Enable the **Auto Engineer signal connection** in the launcher settings. Under **Automation and AI**, enable automatic block direction coordination and save the automation settings. For the first attempt use a local interlocking with two assigned signals and complete track detection.
2. Set a fixed **Auto Engineer waypoint** beyond the next signal. For the first attempt choose a stop before or at the following signal under the same local interlocking. A waypoint before the next governing signal requires no request there.
3. In the locomotive inspector open **Signalling → Signalling information**. Apply **Train** or **Shunting** to the current consist, then press **Enable waypoint automation**. This selection applies to the current session. Apply the movement role again after coupling or uncoupling changes the consist.

Locomotive information shows preparation, release and reasons for waiting. Preparation can start within 300 track metres. Actual admission requires the requesting train to be positively identified within 25 track metres of the signal. The mod works one route at a time along the directed waypoint path. The interlocking checks points, occupancy and locks. The Auto Engineer drives its normal order and stops at its waypoint.

## Take over

Press **Pause waypoint automation** in locomotive information. This stops further automatic requests and withdraws pending requests before their route is set. Cleared or occupied routes continue through the usual locking and release checks. Take over driving using the normal Auto Engineer controls.

With mod automation paused, the host can use **Request route to waypoint** for a single request. It waits for a readable path to the selected waypoint. A paused native order can be used if the Auto Engineer retains the waypoint and its matching path. Once the route is set, the driver can take over and drive it.

The dispatcher sees the locomotive, movement, start, destination and waiting reason in **Automation status**. The host can withdraw requests before entry. Master takeover suspends new requests for the selected interlocking. **STOP** pauses the locomotive automations for this session and applies normal locking rules.

## First game checks

- **Train:** Choose a fixed waypoint beyond a start signal and assign both route endpoints to the same interlocking. Try a clear route and an obstructed route. The waiting request should proceed after the obstruction clears.
- **Shunt:** Choose a fixed local waypoint with a suitable shunting aspect and clear destination. Leave occupied shunting destinations to the dispatcher.
- **Takeover:** Pause during preparation and check that the request does not reappear. Pause after clearing, take over the Auto Engineer, and check that ordinary route supervision continues until release.
- **Waypoint boundary:** Try a waypoint before or at the next signal and one on a neighbouring track. Every request should match the actual path. With an electric point initially set the wrong way, check whether the Auto Engineer can read the path and the interlocking sets the point correctly.

The test build covers local train and shunting movements with fixed waypoints. Coupling orders, reversals, multistep jobs and unattended operation between interlockings are follow-up work. Initial request communication is through locomotive information and the host dispatcher’s automation view. Clients can read the replicated request status. Unreadable paths, ambiguous signals and incomplete detection keep requests waiting. Actual vehicle controls, Auto Engineer behaviour and multiplayer still need game verification.
