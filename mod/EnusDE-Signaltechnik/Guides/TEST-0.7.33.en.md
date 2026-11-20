# 0.7.33-P0 – Setup and gameplay tests

Test build for Railroader 0.6.2. Host and clients need the same version. Use an existing test save; no new game is required. These workflows supersede the corresponding 0.7.32 instructions. Automated checks do not replace gameplay acceptance.

## Select a real GFM boundary, including C

In **Editor**, right-click a continuous two-port track field → **Select GFM in world…**, then click the actual active GFM box once. The original diagram returns with that boundary assigned. Escape cancels. This does not construct a box.

Next open **Menu → Detect GFM for this diagram → Find GFM sections in existing diagram**, review the proposal and choose **Apply detected GFM without redrawing**. Missing opposite boundaries and ambiguous paths are not invented. Assigning a box alone does not configure a complete detection section.

A free-standing **C boundary** does not require a signal at the same location. Its boundary mark and GFM-C identity are visible in Dispatcher too. Its configured direction determines which side can connect to a detection section. The other side is not monitored by this C detector and receives no assured clear/occupied indication from it. Independently configured monitoring elsewhere remains in place. Track drawn beyond C is not deleted. An old circuit that skips an additional physical boundary is read as unknown rather than clear.

Test world selection, preserved drawing, dispatcher C identity, allowed-side detection and reversed C orientation. Planned, disabled or invalid boxes must not become active boundaries. Changed target fields and stale proposals must be rejected.

## One-click assignment and quick actions

- Signal field → **Assign signal in world…**, then click the intended physical signal face once. No second confirmation or additional signal properties window.
- Point field → **Assign in world…**, then click the real point once. That click must not throw the native point. Escape cancels.
- **Rotate field 90°** saves immediately. Four rotations restore the original geometry while retaining signal and GFM identity. Detailed editing remains available separately.
- Ordinary right-click on a world signal initially offers **Edit**. A technical seam exists for future communication; no visible communication command is added yet.

Test that the initiating UI click and final selection click cannot fall through into object editing or native point movement. Test rotation with no properties window and with an existing detailed draft.

## Continuous occupancy

Detection handles both independently configured faces of a physical mast, continuous diagram paths through virtual destinations, and crossings with separate opposite rails. A crossing never invents a turning connection. Uniquely named unbound signal fields can be registered during detection; anonymous or ambiguous fields still need world assignment. Cases that cannot be represented unambiguously remain reported.

A genuinely shared **A/B box** assigns the matching drawn connection to the actual point guard. There is no fictitious zero-length circuit between two aliases of the same box. Merely nearby A and B boxes remain separate boundaries; the intervening track needs a real circuit.

The optional search length starts empty, meaning no length filter. A supplied value filters detection proposals, not the physical extent of a saved occupancy section. Long circuits are checked in full; an intermediate signal without its own detector does not automatically split them. Actual additional GFM boundaries remain effective.

Test the occupied point stem and both arms, shared A/B, separate nearby boundaries, cross connections, opposite direction, a virtual HZ destination between signals, and a section exceeding 100 m until the train tail clears. Occupancy must not leak onto the other independent crossing track. Gold/unknown is not a clear indication.

## Route alternatives

Alternatives use a separate selection window. Two routes with different lengths are labelled **Short route** and **Long route**. Equal-length routes or three or more alternatives use distinguishing signal/point names in order. Captions omit internal AUTO identities. Test selection, cancellation, long names and small windows.

## Open lines and neighbour signals

Line setup supports multiple departures and arrivals per direction. The first member is the geometric reference; additional members must traverse the same shared line axis correctly. Four physical signals are not universally required. A configured train auxiliary destination may substitute for a missing arrival signal. Missing endpoints are not invented; an active connection still needs complete paths and detection coverage. Test multiple departures with one arrival and both directions.

**Assign neighbour signal for display…** adds a read-only representation to a local diagram. The actual signal aspect and configured detection remain visible; ownership stays with the controlling interlocking. A neighbouring local panel cannot clear it as its own route start. Central master control remains possible. Taking a display-only panel out of service must not disable the actual owner's signal. This does not add multiplayer account roles.

## Deferred to the next update

Moving the complete consist beyond a protecting signal before AI shunting waypoint reversal is **not included**. Train-tail clearance, point fouling limits, new requests and insufficient space need to be handled together. Test the current setup changes first.

For bugs provide mod/game versions, action, signal/point/GFM identities and Player.log. Installation, native AI, rendering and multiplayer still need gameplay acceptance.
