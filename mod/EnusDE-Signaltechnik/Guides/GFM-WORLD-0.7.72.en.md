# World GFM sections — test version 0.7.72

Back up the game save and installation configuration; test a copy in Railroader 0.6.3. Host and all clients require main version 0.7.72.

Host: enter Build mode and choose **Show GFM sections and boundaries**. Saved detection sections appear as coloured lines over the rails, with end posts on their saved main path. Colours are automatically assigned section identities, may repeat and preferably differ between neighbours. They never indicate occupancy or movement permission. The selected section is wider.

Click a line/end post, or use the saved-section search. The menu shows the section name, length, circuit boundary names and linked interlockings. **Inspect / edit diagram assignment** opens the existing GFM inspection in the chosen diagram with linked fields highlighted. Use its existing checked assignment/boundary tools. An unassigned section still needs to be assigned through existing GFM/diagram setup; no binding is invented. Conversely, GFM inspection offers **Show saved section in the world (Build mode)**. There is no automatic camera teleport: move to the relevant track yourself.

Hide the overlay or end Build mode to remove lines and pick targets. World changes, authority loss and unloading also clean up. This is local host build tooling, not shared client selection. Pick colliders are triggers, not physical obstacles.

This first version displays saved paths, including saved point/protection sections; it does not silently redraw an invalid path as a new valid one. End posts mark saved path endpoints, not a C boundary's effective direction. Unavailable native intervals are omitted and reported. Uncoloured rails are not confirmed clear: they may be unconfigured, invalid, outside range or omitted by the display budget. Camera range is 400 m with at most 3,000 drawn pieces. Camera movement updates nearby geometry; overlapping saved paths may cover each other, so use the search to select them.

Editing remains in the existing checked editor. Arbitrary new world-click from/to cuts, automatic joining/splitting, automatic detection on every signal placement and C-boundary flank-protection exceptions are not included. Merely displaying/selecting sections changes no saved IDs, detection intervals, signals, points, routes, locks or automation. Use GFM inspection for actual validity and coverage checks.

Game acceptance remains open: adjacent colours, curves/points, physical end positions, picking near signals, daylight/night shaders, editor/build navigation, invalid/moved paths, range/zoom and floating-origin changes, hide/reload and host/client behaviour. Offline checks do not prove these flows in Unity. Existing archives and optional Runenwacht 0.2.2 remain unchanged.
