# 0.7.26-P0 – setup and gameplay checks

Railroader 0.6.2. Replace the mod-manager package and restart the game. Keep the existing test save; host and clients need the same version. Offline checks do not replace gameplay validation.

## Settings

**Company → Settings/Options → Mods → Signal & Interlocking**. Switch from Installed mods to Signal & Interlocking at the top if necessary.

## Enable point boundaries directly

Right-click track near the desired point and select **Enable point detection boundaries**. No point-list selection or previous diagram assignment is required. The nearest point within 30 m is used; equally close points require a closer click.

Alternatively, right-click its assigned diagram symbol in the editor and use the same command. Three boundaries are detected and boxes appear once network capture completes. Changes to detection are rejected while routes or direction locks remain active. Enabling detection does not turn unknown sections into clear track.

Open an individual box afterwards to change B/C, C direction, track side or model offset. Existing assignments and individual offsets are retained.

## Find station signals for an existing diagram

Open the local interlocking in the editor, then **Menu → Automatic recognition**. At the top use **Find signals for the existing diagram**. Check the target diagram, select an optional station starting signal, set the search distance and click **Find station signals and detection boundaries**.

The search follows actual track connections within the configured area. Without an explicit starting signal, signals already linked to the diagram are used as seeds. It does not search every signal in the world and does not replace the drawing.

Found signal codes and the count of active detection boundaries are shown. Unbound signal fields are assigned only when their code or display name matches uniquely. Anonymous Signal placeholders still need an individual world assignment; schematic coordinates alone do not identify a physical signal. Connecting track sections are not automatically configured by this search.

Already bound fields refresh in the background: generic Signal captions no longer hide the code, and main/shunting self-pairs follow changes in signal type and Sh-1 capability. Explicit second signals and auxiliary targets are retained. Active attached detection boundaries follow the physical mast. None of this grants movement authority.

## Master modules

**Interlocking selection → Modules** beside the master. Select a local interlocking on the left and use **Insert module** if required. Drag a module in the diagram; a translucent area shows its proposed position. Release to apply. Overlapping or out-of-bounds drops are rejected and retain the previous position.

Right-click a module to **Edit local diagram** or **Unlink module**. Drag empty space to pan. Fit diagram, +/− and 100% are at the top. This arrangement view never operates trains.

## Gameplay checks

1. Resize the module window, drag a module and open its local diagram. Controls and drawing must remain separate.
2. Enable point detection from the world menu. After detection completes, inspect all three boxes and edit one.
3. Search the existing station. Distinguish found codes from actual field assignments; ambiguous symbols must remain unbound.
4. Upgrade a bound shunt signal and convert the low main to a high mast. Check its updated code and main/shunt split; identity and position must remain stable.
5. Inspect a C box at night. Its arrow is ordinary painted direction marking, not a lamp or occupancy indicator. Build previews may remain highlighted.

The outstanding AI stop tests in [0.7.25](TEST-0.7.25.en.md) still apply. This version adds no AI driving logic.
