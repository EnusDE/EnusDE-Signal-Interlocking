# 0.7.32-P0 – setup and gameplay checks

Test build for Railroader 0.6.2. Host and clients need the same mod version. Use the existing test save. This update does not install the mod or publish it on Nexus.

## Whittier–Ela block connection

1. Open Whittier in the editor. Place an **open-line/permission indicator** on an empty cell; setup opens immediately. Right-click an existing indicator to configure it. Also available through interlocking management → **Lines / block** or the general block/line menu.
2. Choose Whittier as neighbour A and Ela as B. Assign departure and opposite entry signals in both directions: four logical main-signal faces, which may share physical masts. Signal fields must already belong to their local interlockings.
3. Detect the track and review proposals. Select compatible forward/reverse paths where there is a choice. Recognition initially modifies only the draft.
4. **Find detection sections for this line**, review, then explicitly **apply**. This uses existing GFM boundaries and enabled point detection within the line path. It preserves the drawing and does not build missing signal boxes or enable point detection.
5. **Apply line configuration** saves the shared connection and originating indicator together. Incomplete drafts can be saved disabled. Enabling requires valid endpoints, topology and complete detection coverage.
6. Place an Ela indicator and choose the same existing connection. A master indicator can also reference it. Include the local interlockings as master modules. All indications share one permission, not separate independent copies.

Check shared changes, opposing requests, occupied/unknown track rejection, passage and complete tail-clear proof before direction release. Line permission alone never clears a signal; route requests or separately configured target automation are still needed. Changing topology/detection before applying must invalidate the recognition proposal.

## Hl test setting

Open **Company → Options → Mods → Signaltechnik → Signal operation and display → Enable Hl signal system (Test)** and apply. Disabled by default; required for new Hl builds and conversions. Existing Hl signals remain operational when disabled and may be converted back to H/V. Native AI speed handling is unchanged.

## Construction defaults and the existing diagram

**Company → Options → Mods → Signaltechnik → Signal operation and display → Build new signals with a GFM box** is enabled by default. It affects new construction only. Existing signal boundaries remain individually configurable.

In a local diagram use **Menu → Detect GFM in this diagram… → Find GFM sections in existing diagram**. Review the complete sections and counts of unassigned signals and missing signal/point detection. Apply with **Apply detected GFM without redrawing**. Unique connected sections and inner signal faces are assigned without moving the drawing. Missing boxes and disabled point detection are not silently enabled. Block-check errors list physical segments and meter ranges still lacking coverage.

## Electric points and world assignment

Discovery and an automatically generated master field do not count as local point ownership. Electric selection requires exactly one local diagram assignment; bulk conversion is scoped to that local interlocking. Native CTC retains its original rules.

Right-click a point field → **Assign in world…**. The world track menu exposes only **Assign point** until completion or Escape. The selected point is assigned and the editor reopens. Signal fields offer **Assign signal in world…**; click the desired real signal face and choose **Assign signal**. Changed or deleted target fields must be rejected.

## Release checks

- Normal safe cancellation releases immediately only when the required checks pass. Otherwise the reason appears with **no countdown**. Resolve the cause and request cancellation again. It does not silently start auxiliary release.
- Auxiliary release shows remaining protection seconds both in release status and at the bottom of the diagram. Expiry still requires the existing release checks; a moving approach train or unknown occupancy cannot be released by time alone.
- Explicit dispatcher-confirmed auxiliary release retains its existing meaning and minimum wait. Display feedback does not replace inspection.
- Check host/client countdowns, blocked reason after expiry, immediate normal cancellation on clear track, and disappearance of obsolete countdowns after release.

## Out of service and backups

Atomic backup staging uses short unique names in the existing backup folder. Previously appending another name reached 260 characters at the reported Windows LocalLow path and prevented the checkpoint required before taking an interlocking out of service. Existing snapshots and recovery remain compatible; backup failures still block the change rather than being ignored.

Check a free interlocking with no affected route/direction locks: disable, verify identification lights and native AI bypass for exclusively assigned points, then re-enable. Existing occupancy and route protections must still apply. Confirm the new checkpoint is readable in backup settings.

## Automation contacts in the world

Right-click track → **Place automation contact** opens a setup draft before building. Initial direction follows the camera view. Only a unique first main signal is proposed automatically; ambiguous first signals require selection. Review direction, target, axle test, display and activation, then **Build configured contact**. Cancel builds nothing. Existing contacts and signal automation offer world selection for start, primary and alternate target signals, without typing codes. Signal options list linked contacts under **Automatic triggering detected**. Target automation and global contact automation still require explicit configuration; recognition alone grants no authority.

**Count experimental axle test** and **Show axle-test display on contact** are separate options. Apply with **Save contact**. Lettering now sits on both readable display faces; the previous single label faced into the plate. Check visibility and count updates from both directions. Missing current data displays a dash. This axle test is not track-clear proof and does not create detection sections. This display fix does not add a separate contact symbol to the interlocking diagram.

## Route discovery

Directed target reachability prunes irrelevant dead ends without increasing search limits. Turnout connectivity, intermediate main signals and flank checks remain enforced. Retry the previously limited start/destination request, including reverse direction and intermediate main signals.

Automated checks do not replace testing in the actual world with native AI and multiplayer. Report mod/game versions, action, relevant object codes and Player.log.
