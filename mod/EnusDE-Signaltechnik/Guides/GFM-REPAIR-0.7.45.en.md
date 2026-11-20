# 0.7.45-P0 — GFM fault overview and targeted repair

Internal test build for Railroader 0.6.2; host and clients need the same version.
Includes 0.7.44. Previously prepared ZIPs remain unchanged.

## Changes

- GFM faults distinguish deleted objects, unfinished construction plans, disabled
  detectors, moved boundaries and unavailable point arms. Human codes and saved
  track/metre locations replace messages consisting only of two internal IDs.
- Inspection lists all saved invalid sections together, including orphaned entries
  without diagram links and faults at other interlockings. Related boards are
  identified. Invalid entries appear first in the section list.
- Dispatcher line/GWB controls offer **Review all GFM setup faults in Editor**.
- **Preview cleanup of all sections with deleted boundaries** collects eligible
  stale entries. Individual invalid assignments can also be selected.
- **Find replacement up to the next active boundaries** offers a complete,
  physically validated section. Removing an intermediate detector can extend it
  to the next actual GFM boundary. Signals without active detectors do not divide
  detection sections. Point areas and other active boundaries are not skipped.
- A replacement also collects contained invalid neighbouring sections referencing
  deleted boundaries. Confirmation updates related references on all affected
  boards together. Removal alone clears those references and missing boundary
  marks while retaining drawings, world objects, signal links and number IDs.
- Line checks report multiple block faults and exact track/metre coverage gaps.
  Only usable physical sections count as coverage.

## Repairing a deleted boundary

1. Open **Section manager / GFM inspection** in Editor, or use the new line-window
   shortcut. **Refresh GFM inspection** reads the current installation.
2. Review **All saved GFM faults**, including their boards. **Inspect this fault**
   selects an entry. Both boundary origins show their state and saved locations.
3. Choose **Find replacement up to the next active boundaries**, review the offered
   section and length, then **Preview replacement and related stale entries**.
4. Alternatively choose **Preview removal of this stale assignment**, or preview
   cleanup of all eligible entries with deleted boundaries.
5. The preview names removed sections, changed fields and affected interlockings.
   Only **Apply this GFM repair** saves. Cancel changes nothing. A configuration
   checkpoint is created before saving.
6. Review remaining faults and coverage gaps. Removal alone provides no clear-track
   proof. The host's existing automatic-catalog setting is retained: when enabled,
   it may continue adding valid sections from current physical detection.

## Deliberate limits

Cleanup excludes unfinished plans: complete construction first. Existing disabled
signals/detectors can be opened and re-enabled. Internal sections of constructed
block assemblies are not deleted here; restore their shared detectors or edit the
assembly separately. The error identifies that dependency.

Confirmation rechecks the preview, world and configuration revisions. Affected
track must be clear and readable with no related route or direction holds.
Unrelated running routes do not cause a blanket refusal. Withdraw pending route
and automatic requests first. Repair never clears real residual locks.

## Gameplay acceptance

- Delete an intermediate detector between two saved sections: see both faults,
  preview the next valid complete section and replace both stale entries on
  multiple boards without duplicating number sources.
- Test removal alone: drawings remain, stale references disappear and unmonitored
  coverage gaps remain visible.
- Test unfinished plans, disabled detectors and moved boundaries independently.
  Plans must remain identifiable and excluded from deleted-object cleanup.
- Occupied/unreadable track, affected routes/holds and stale previews must refuse.
  An unrelated route may stay active.
- Further active detector boundaries and points must not be bridged.
- Test save/reload, host/client views and existing compatible parallel routes.

Build/regression checks use the available game libraries. Unity gameplay on the
user's save and multiplayer acceptance remain pending.
