# 0.7.28-P0 – shared A/B detection box

Railroader 0.6.2. Replace the package and restart; keep the test save. Host and clients require the same version. Includes the changes from [0.7.27](TEST-0.7.27.en.md).

## Shared signal and point boundary

When a fully resolved B point boundary occupies **exactly the same logical track position** as an enabled A detector on a signal, one shared box is drawn. It is marked **A/B** with code **GFM-A/B-…**, using the B housing and retaining the signal box's existing entity number.

The duplicate point box at that position is hidden. Signal and point-arm IDs remain unchanged. Matching existing detection sections can retain their IDs and names during recognition. Network capture uses one canonical boundary at this position.

Several point boundaries may use the same unambiguous signal boundary. The signal's detection section lists A/B and the associated points. Point detection offers **Open shared A/B box**. Existing point-boundary object actions redirect to the corresponding signal box.

Diagram boundary labels also show A/B. This creates neither a new signal type nor movement authority. Technical signal codes such as RS-009 or HS-009 remain unchanged.

## Limits

- Proximity alone does not merge boundaries: different track distances or segments remain separate. Positions are compared at the game's native track precision.
- One-sided C boundaries remain separate. Ambiguous physical boundaries are not merged.
- Disabling point detection restores A on the signal. Removing the signal detector or moving its logical boundary restores separate point-box presentation. Saved individual point-box offsets are retained.
- Sharing is derived from the installation without creating extra persistent signal objects. General placement of other nearby boxes has not been redesigned.

## Gameplay checks

1. Enable detection on the affected point and wait for network capture. If its boundary coincides with the signal's A location, one A/B box should replace two boxes.
2. Open the signal and check its A/B information and associated point. Check the diagram boundary code.
3. Check an existing section using its original point-boundary ID. Sharing must not remove or duplicate it.
4. With routes released, disable and re-enable point detection. A ↔ A/B should retain its entity number.

Offline checked. Actual drawing, interaction and multiplayer validation remain pending, as do the earlier AI stop tests.
