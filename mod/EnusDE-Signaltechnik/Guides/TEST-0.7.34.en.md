# 0.7.34-P0 – GFM path validation

Bug-fix test build for Railroader 0.6.2. Host and clients need the same version. Use the existing test save. Includes [0.7.33](TEST-0.7.33.en.md).

The previous ambiguous-path/point-crossing message combined several different causes. Validation compared serialized meter values against a new search result. A native float/double position round trip, equivalent contiguous fragments or the proposal search limits on a long path could reject a physically correct section.

Validation now checks the saved physical path directly: available segments, connected nodes, meter ranges, direction, endpoint boundaries and intervening GFM boundaries. Position differences below 5 mm use the existing endpoint tolerance. No alternative route is guessed and no missing coverage is invented.

Real point crossings, interrupted track, inconsistent direction and skipped boundaries remain blocked. Errors identify the boundary pair and, where applicable, the point or skipped boundary. Physical boundary changes invalidate cached block validation; existing direction leases must still clear safely first. Shared A/B boxes also remain point boundaries when forming direction chains under their signal-GFM identity.

Test the previously rejected BL-494658157A request, both directions, long sections and shared A/B. If rejection remains, inspect the newly identified boundary pair and point. An old circuit that really crosses a point is not automatically repaired: check/enable point detection and detect/apply the complete sections in the existing diagram again. Occupied or unknown track must still block new direction reservations. Add or reverse an interior C boundary on a clear installation and check that previous through-coverage becomes invalid.

AI shunting reversal and route lock/release rules are unchanged. Actual gameplay, native AI and multiplayer acceptance remain pending. Report remaining failures with the complete new message, Player.log and version numbers.
