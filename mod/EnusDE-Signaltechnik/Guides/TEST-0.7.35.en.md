# 0.7.35-P0 – Continuous signal placement

Test build for Railroader 0.6.2. Use the existing test save. Host and clients need the same mod version. Includes earlier changes and the [0.7.34 GFM validation fix](TEST-0.7.34.en.md).

Open **IXL → Build mode** and select a main, standalone distant or shunting signal. Optional Hl signals use the same workflow. Left-click valid track to place a signal; the same type remains selected for the next placement. No signal configuration window opens after placing.

Move freely between placements; right mouse remains available for camera control. Placed signals cannot be opened for editing during the active series. Holding left mouse does not keep constructing: each placement requires a new click.

Press **Escape** to finish. Only the unsaved preview is discarded; placed signals remain. Click individual signals afterwards to edit them. Switching to Editor/Dispatcher or reopening the build menu also finishes the series.

When starting from a track position captured through the world menu, review/confirm the first preview as before. Confirmation continues the same signal series in the world. Closing an unconfirmed positioning window ends that series.

The signal type, Hl model and new-signal GFM default stay selected. If view-following orientation is enabled, each new placement follows the current viewing direction. Signals receive separate automatic codes and IDs. Moving an existing object and placing signs or standalone GFM boxes retain their single-action workflow.

Test three successive placements with movement and camera rotation, unique codes, no editing windows, held-click behavior, invalid track, clicks over game UI, Escape, mode changes, existing-object movement and the initial captured-track confirmation. Test each signal type, Hl when enabled, and GFM defaults on/off. Native input, camera and multiplayer behavior still need gameplay acceptance; provide version, action and Player.log for failures.
