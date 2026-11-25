# 0.7.70 — route colours and automatic movement preference

Development test build for Railroader 0.6.3. All players need matching main 0.7.70. Existing compatible Runenwacht 0.2.2 remains usable without a new asset-pack update; presentation API v1 is unchanged. This is a step toward 0.8.0, not completed release acceptance.

## Track indication

| State | Track colour |
|---|---|
| Admitted train route, including reduced-speed train route | Green |
| Admitted shunting route | White |
| Occupied section, including an occupied shunting destination | Red |
| Unknown/unavailable detection | Amber |
| Route preparing / waiting for protection | Existing amber flashing cue |
| Released / free unreserved section | Existing idle grey |

The actual `RouteDefinition.Shunting` flag determines the route type, not the starting signal model, an intermediate Sh 1 aspect, code prefix or last selection in the UI. Colours apply to main track fields, independently assigned circuit half-rails, active switch arms, the rail between signals, inline number fields and inline mirrored number displays. Signal lamp colours and permission-arrow semantics stay unchanged. Only held sections and unreleased non-flank switch paths receive an admitted route cue. Unknown/occupied readings cannot be covered by white/green. If one coarse display block combines disjoint route types, train colour has deterministic priority; precise local GFM assignments are still important. Retained fault locks keep their separate existing indication.

A permitted shunting movement into occupied track does not make the occupancy disappear. Its free approach is white while the occupied area remains red. Existing occupancy, points, flank protection, direction permissions and train identification checks are unchanged. This update does not authorize movement through occupied switch protection or unknown detection.

## Automatic selection

The locomotive's waypoint movement choice now describes its preference as **Automatic · prefer train routes**. Explicit Train and Shunting selections stay binding. Existing automatic handling of declared shunt-only signals is retained.

At a mixed-capability start, a structurally reachable train destination takes priority. Temporarily unavailable physical train signals still count in this static query, so occupancy, unavailable signals, permission refusal, ambiguity or planning failures cannot trigger a switch to shunting.

Only if no train destination exists can automatic detection choose a supported local shunting destination. This alternative needs the same local interlocking, the correct direction and a path proven inside the current native waypoint leg. It cannot cross a configured free line (even disabled), use a block signal or invent a path beyond the current proven leg. Normal route preparation and admission run afterwards and may still refuse the request. No catch-and-retry as Shunting is performed after an unsuccessful train admission. Manually selected movement kinds are not changed.

## Validation and game acceptance

Full **Test.ps1: 2,170 PASS, 0 FAIL** on 25 November 2026 against local original Railroader 0.6.3 libraries; both assemblies compile. New checks cover route metadata, red/unknown priority, preparation/fault/cancelled stages, released intervals, deterministic merged display types, lazy train preference, explicit choices, propagated target errors, local/foreign/opposite-direction paths, free-line overlap and block-signal exclusion. The current shared frame already contains the route kind; no persistent configuration migration or network schema change was introduced.

In-game checks remain open: green train routes including slow and intermediate shunt heads; white shunting paths into red occupied destinations; refusal at occupied/unknown switch protection; matching white switch arms, half-rails and inline/mirrored number fields; released sections and fault retention; automatic train preference versus local shunting-only destination; no fallback on train occupancy or missing line permission; both directions and explicit movement choices; host/client and master-panel rendering. Also retain the outstanding 0.7.69 block-continuation/following-train and native turnback tests. No game launch, installation, tag or public release was performed here.
