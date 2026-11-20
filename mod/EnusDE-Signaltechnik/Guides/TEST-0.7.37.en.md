# 0.7.37-P0 – Station routes and manual world GFM setup

Test build for Railroader 0.6.2. Use the existing save; host and clients need matching versions. The first public release does not contain these new tools.

## Station routes

Internal BL direction chains were being checked for ordinary train movements within a station. A train behind the start signal could therefore block an otherwise clear onward route. Local train and shunting routes now use local route protection; crossing an internal entry/exit boundary or a configured open line is not treated as purely local. Directly overlapping valid GFM circuits still reject occupied or unreadable states by boundary pair. Track occupancy, point position, flank protection and locks remain active.

**Company → Settings → Mods → Signal & Interlocking → Automation and AI → Route calculation** offers **Allow manual routes with incomplete GFM coverage**, enabled by default for this test build including older saves. The dispatcher checks unmonitored areas; they remain unmonitored on the panel, never confirmed clear by this option. The request records a diagnostic warning with missing intervals. Contact automation still requires complete configured GFM coverage, and explicit line/permission protection retains its checks.

Saved-route planning is accessible regardless of the current calculation mode. On-request calculation is labelled experimental. Disable it and apply the route mode to use the previous saved-route workflow. Existing definitions are retained.

Direction topology signatures now exclude general installation revisions, display names, lateral offsets and unrelated signals without detectors. Actual track, detector, section-path and station-boundary changes remain relevant; existing leases are not forcibly deleted.

Default point guards are derived from actual native arms and physical detectors. A signal without GFM no longer shortens them, and an old generated point-B limit cannot perpetuate its own incorrect position during the next scan.

## Clear GFM without rescanning

Open **Editor → Menu → Detect GFM in this diagram…**, select scope and choose **Clear GFM assignments only · preview**. Use **Include all interlockings** for complete installation cleanup. Review and apply the clear preview after releasing routes/direction holds and withdrawing pending automatic requests. A backup is required first.

This clears section bindings and unused circuit definitions, disables scoped point detection and removes scoped manual point-boundary links. It preserves drawing geometry, signal/point world links and constructed objects. A local clear retains definitions still used by other diagrams. Background GFM catalog updates and outstanding background results are paused/discarded, preventing immediate recreation. The experimental background option can be re-enabled under Route calculation. Explicit scans and manual setup remain available.

## World boundary-to-boundary setup

Open **Editor → Menu → Set up GFM from world objects…**. Select start and end consecutively in the world, or use the separate buttons. Click real signal detectors or point-arm boxes; Escape cancels selection.

Find the physical section, review its length and save it. Physical setup is independent of complete schematic assignment; unambiguous diagram paths are linked where possible. Intermediate detector boundaries and actual points cannot be skipped. Long physically catalogued sections are tried before the bounded fallback search.

## Link a detector to a native point arm

In the same tool, select a real point and its native common/tip, normal or reverse arm. Select an active signal detector or standalone B box on that same native segment and apply the shared boundary. The monitored point arm extends to it. A signal-A detector becomes shared A/B; a B box stays B. One-sided C and other/parallel segments are rejected. Multi-segment arm links are not supported yet.

Detect/configure the adjoining sections afterwards. Individual manual links can be removed. Links persist through save transfer and point renaming. Missing, disabled or inappropriately moved detectors make the affected point detection unreadable rather than clear. Active point locks prevent moving/rebuilding a linked detector.

## Inspect errors and find recordings

Unresolved section proposals offer **Inspect: …** buttons that open the corresponding diagram inspection and highlight known boundaries without applying the proposal. Return to detection afterwards; changed configuration invalidates the old preview.

**Company → Settings → Mods → Signal & Interlocking → Optional local diagnostics** shows the recording folder and opens the diagnostic window. The window also shows the current path. Windows default:

`%USERPROFILE%\AppData\LocalLow\Giraffe Lab LLC\Railroader\EnusDE-Signaltechnik\Diagnostics\`

Session folders use UTC timestamps and contain `session.json` and `events-0001.jsonl` or subsequent parts. Prefer host recordings for operating issues. Client recordings depend on local opt-in and contain only their local view. `Player.log` is directly in the Railroader LocalLow folder.

Test local onward movement with the train behind its start signal, real occupied/unreadable route detection, manual unmonitored operation, strict contact coverage, configured line counter-movements, genuine topology changes, retained locks, clear-only stability, world picks and every point-arm link case. Gameplay and multiplayer acceptance remain pending. The supplied Diagnostics.zip could not yet be imported because the attachment downloader has read-only filesystem access; findings here come from source inspection and targeted tests, not analysis of that archive.
