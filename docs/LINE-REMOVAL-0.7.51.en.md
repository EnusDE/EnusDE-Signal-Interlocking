# 0.7.51-P0 – Delete GWB line connections

Fix for Railroader 0.6.3. Host and clients need the same mod version.

## Cause

Previously, deletion removed only the line catalogue entry. Permission fields and
auxiliary buttons still referenced it, so configuration validation rejected saving.
The guided assistant also exposed deletion only in its last step.

## Steps

1. In the interlocking editor, open **Block / line connections**.
2. Select the saved connection.
3. Choose **Delete this line connection…**. The action is available at every assistant
   step, including for incomplete drafts.
4. Review the connection and counts of affected permission fields, auxiliary buttons,
   groups and automation settings.
5. Choose **Confirm deletion of this connection** or **Cancel deletion**.

The preview makes no changes. Installation changes since preview require a fresh
preview. Confirmation rechecks affected routes, direction holds and physical track
occupancy. Occupied/unreadable affected track blocks deletion with a location/section
message. Unrelated routes elsewhere may remain in service.

## Result

- Remove the shared line connection and its GWB permission.
- Unlink its permission indicators on every interlocking.
- Turn bound auxiliary destinations into unassigned permission controls, retaining
  their names, positions and settings for reuse. A protected train destination does
  not silently become an unprotected departure destination.
- Release managed block/number group bindings, leaving ordinary drawing areas.
- Disable associated block-signal automation and contacts.
- Preserve independent connections.

World signals, GFM sections, drawing fields and train-number sources remain; no world
object is demolished. Existing number mirrors remain valid. Reconfigure affected
saved routes before using them again.

Local project, language and regression tests cover cleanup and admission guards.
Gameplay/multiplayer acceptance is still required. Local compilation continues to use
the older available game libraries with the existing path adapter; a build against
the original 0.6.3 DLLs is not verified by these checks.
