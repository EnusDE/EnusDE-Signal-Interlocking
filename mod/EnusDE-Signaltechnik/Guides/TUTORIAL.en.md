# EnusDE Signal and Interlocking — Tutorial / User Guide

**WORK IN PROGRESS — build/material sections updated for 0.7.62-P0, Railroader 0.6.3.**

[Baumodus und MOW-Materialien auf Deutsch](BUILD-MOW-0.7.61.de.md). Older operating sections remain a developing alpha guide.

This guide describes the current alpha. Illustrations, a complete worked station example and gameplay verification are still being developed. Follow the latest version-specific notes where older instructions differ. The GWB permission module is implemented in 0.7.41; see the [current GWB setup guide](FREE-LINE-GWB.en.md), which supersedes older permission-operation descriptions below. Automatic renumbering remains a proposal.

In 0.7.42 route requests enter a preparation queue. You can choose another start/destination while earlier work is prepared. The dispatcher context shows pending count, progress and cancellation. STOP cancels pending preparation while retaining existing locks. Pure path search runs on an isolated snapshot; final route/block/GWB admission still checks current conditions. See [queue operation and acceptance](TEST-0.7.42.en.md). This changes preparation of existing manual/contact functions; the wider communication/automation redesign remains a concept.

## 1. Install and open the mod

Install the matching mod ZIP through your mod manager, restart Railroader and enable the mod. Host and clients need the same version. Begin with a test save or a copy of your existing save.

Open **IXL / STW** beside the clock. The main work areas are **Build**, **Editor** and **Dispatcher**. Settings are under **IXL / STW launcher → gear → Settings**. English is available without another mod; German can also be selected.

If construction is locked, check the signalling unlock in **Materials and construction costs**. The standard map can inherit native signalling prerequisites without requiring purchase of native CTC. Sandbox and existing unlocks remain available. Custom maps and host overrides may use different prerequisites. See [Map-dependent progression](PROGRESSION.en.md).

## 2. Place and configure world signals

Choose **Build** in the launcher to enter the local build session. The compact right-hand icon palette remains open while placing signals. Hover an icon for its name; the selected tool is highlighted. Right-mouse camera control remains available. **Escape** cancels the cursor. **End build mode**, the native X or a workspace change exits Build and retains confirmed plans.

With material requirements enabled, the local builder sees cyan virtual models; ordinary viewers see confirmed sites as cones/barriers. Use **Material → Open native wagon ordering** for captive FL/FM building supplies and XM signal equipment. To buy cargo, separately choose its native **Buy** destination at an interchange under **EnusDE Signal & Interlocking**, deliver the wagon and await normal service/return. No chat command is required. See [purchasing and native cargo model expectations](INTERCHANGE-0.7.62.en.md). Store/reload at an existing MOW warehouse, then build using an active owned MOW work car and coupled loaded supply cars within 250 track metres. Existing legacy stocks can be transferred explicitly without discarding excess. See the [full material workflow, backup boundary and game tests](BUILD-MOW-0.7.61.en.md).

In Build, click a signal to configure it. Outside Build, click its information and choose **Edit in build mode** as host. Verify its real track position and direction, available train/shunting aspects, role and any distant-signal target. Each enabled face of a two-sided mast has its own logical direction.

New signals can include a GFM box by default; this is configurable in settings and does not rebuild existing signals. Optional Hl models require the **Test** setting for new builds or conversions. See [Hl models and limitations](HL-TEST.en.md).

An unbound signal displays an invalidity cross. A properly assigned signal normally starts at Stop. A signal or installation explicitly taken out of service uses the mod's identification-light policy. Taking an installation out of service requires affected authorities to be released first.

## 3. Draw and link a local interlocking

Create or open a local interlocking in Editor. Draw the track layout, add signal and point fields, then link the fields to real objects. A schematic symbol alone does not identify a real signal or point.

- Right-click a signal field → **Assign signal in world…** → click the intended real face once.
- Right-click a point field → **Assign in world…** → click the intended real point once.
- Press **Escape** to cancel world selection.

The assignment click should return to the editor without opening an extra signal-properties window or throwing the point. Detailed properties remain available separately. Quick rotation saves directly; drawing rotation and real-world approach direction are separate settings.

Master views can display local interlockings as linked modules. These are views of one installation, not duplicate physical signals or point authorities. Move and arrange modules through their workspace. A neighbouring signal can be added as a read-only display while its actual owner keeps control.

## 4. Configure GFM track detection

GFM is the mod's track-detection system. A complete section needs valid boundaries and a continuous physical path.

| Boundary | Current purpose |
| --- | --- |
| A | Detector attached to a signal. |
| B | Independent or point-associated detector. |
| A/B | One exactly shared signal/point boundary, retaining both identities. |
| C | One-sided monitoring limit; its configured arrow side matters. |

For an existing drawing, use **Menu → Detect GFM in this diagram…**. Inspect the proposal before applying it without redrawing. The detector search cannot invent missing boxes or resolve every ambiguous connection.

Use world selection to assign an actual boundary, or use the world **from/to** tool to define a physical section between two boundaries. Explicit point-arm links must refer to a valid detector on that native arm. For a complete rebuild of assignments, use the previewed GFM rescan. **Clear-only reset** keeps the drawing and world objects and pauses the background catalogue.

Open **Menu → Section manager / GFM inspection…** to inspect boundary origins, section lengths, diagram assignments and coverage gaps. A physically valid section with **zero linked diagram fields** is not yet drawn as a continuous monitored section on that panel.

Virtual destinations and signals without their own active detector do not automatically split a physical detection section. A long train can occupy several sections at once; each becomes clear only after the relevant vehicles leave it.

## 5. Set up auxiliary destinations

An auxiliary destination is a virtual route endpoint, not a physical signal. Configure its real track ID, metres from segment end A, allowed movement types and **approach direction on the real track**.

**Important:** In 0.7.40 each target accepts one configured approach direction: toward segment end A or toward segment end B. The train/shunting checkboxes control movement type, not direction. Rotating the diagram symbol does not reverse its real approach direction. For both approaches, use two separately configured targets at the same physical location with opposite approach directions.

Choose the start signal first, then the auxiliary destination. A linked destination can also be offered from its signal's context. An intervening signal in the same direction still limits the route; a virtual destination does not authorize skipping it.

## 6. Choose a route workflow

**Calculate routes on request** is the experimental default. In Dispatcher, select start and destination. The mod calculates the path and runs normal admission, point-alignment, end-position, occupancy and protection checks. Calculation itself does not grant a proceed aspect.

When several paths are available, choose from the separate route-selection window. Two unequal paths can be labelled **Short route** and **Long route**; more complex alternatives show distinguishing signals or points.

For the saved-route workflow, disable on-request calculation in **Automation and AI → Route calculation** and apply the mode after releasing active authorities and pending automatic requests. Planning saved routes remains available in Editor. Existing saved definitions are retained when changing mode.

**Upgrading to 0.7.40:** Recalculate older saved routes containing points once. Open **Editor → Menu → Saved route planning**, select the definition, align the real points for the intended path, and choose **Recalculate route**. The new route uses the same physical point boundaries as GFM. The station does not need to be rebuilt.

## 7. Operate train and shunting routes

Select a train-capable start for a train route, or the shunting half/action for a shunt. A main-signal mast may support both, so check the selected movement. Rejection messages identify train versus shunting movement and the relevant protection area.

Train routes require their protected path to meet the applicable checks. Known occupied shunting track is permitted outside point-protection intervals, including intermediate track. Occupied point guards, conflicting reservations, unknown readings and invalid bindings can still reject a shunt.

Flank protection holds threatening side signals at Stop or holds a point diverting away from the route. Point protection is the monitored area around a point's common/tip, normal and reverse arms. A vehicle inside that area can occupy or foul the point. These checks have different purposes.

## 8. Understand passage, numbering and release

The shared signal-passage point defaults to **5 m behind the signal**, adjustable from **0 to 25 m** in route-calculation settings. The assigned consist's leading end must pass it in the route direction. The same event returns the signal to Stop and verifies train-number advancement. Short routes limit the offset appropriately.

Occupancy does not simply jump from one section to the next. Long trains may occupy multiple sections simultaneously. Tail-based release remains separate from signal return to Stop and number transfer.

Independent number fields need unambiguous signal/section bindings. Use read-only **display mirrors** for extra views of one source. A duplicated source or pre-existing duplicated registered number can show **?**. Resolve its ownership rather than creating another independent copy.

Use normal cancellation first when its conditions can be satisfied. Auxiliary release shows its waiting time and remaining blocking reason. A completed timer alone does not prove clear track. Explicit local dispatcher confirmation retains its existing meaning and does not erase physical occupancy.

## 9. Contacts and initial automation

Configure a contact before building it: real direction, first main signal, activation and optional axle-test display. Set that signal's primary and alternate destinations, then enable the intended automation. Linked-contact information is available at the signal.

Contact automation respects intermediate main signals and the current waypoint limit. The axle-test display is experimental and is not confirmed track-clear proof. Automatic reversal only after the entire consist has passed a protecting signal is not implemented yet.

## 10. Current open-line / GWB setup

The existing setup supports neighbour interlockings, departure/arrival groups, line paths, detection coverage and direction permissions. Each independent physical line track has its own connection. Existing permission indicators show the shared direction; permission alone does not clear a signal.

In 0.7.41, inline permission modules preserve the drawn rail and its detection. Auxiliary buttons are configurable as route destinations or permission controls. Non-owners request permission; only the current owner can give it after full-line clear proving and release of all holds. Route requests never acquire it implicitly. The normal direction is configured for initialization after load, and each physical track has one shared owner. Block and route checks remain active.

See [Current open-line / GWB setup](FREE-LINE-GWB.en.md) for setup, migration, optional automatic replies and host master end selection. This changes interlocking software and the diagram only; no world construction or materials are required.

## 11. Refresh, back up and report a problem

Use **Editor → Menu → Refresh and inspect installation…** to reload elements and inspect unresolved bindings while keeping saved settings and IDs. Release active routes and direction holds and withdraw pending automatic requests first. This does not guess or delete bad physical assignments. Use the separate GFM tools for deliberate assignment changes.

Configuration backups and diagnostic exports have different purposes. Backups support installation recovery. A diagnostic ZIP records the current configuration, route attempts, occupancy and vehicle data for investigation.

Reproduce a problem on the host, then use **IXL / STW launcher → gear → Settings → Optional local diagnostics → Export diagnostic ZIP**. The result shows the full filename. Recording/export folders are visible in settings; an optional user-synchronized local export folder is supported. No automatic upload or remote-control connection is created.

Include mod/game versions, start and destination, chosen movement, relevant signal/point/GFM identities and the exact error. **Occupied**, **unknown**, **unmonitored** and **locked** describe different conditions.

After loading, signals intentionally start at Stop. Saved configuration and routes remain, while previous live movement authorities are not restored automatically.

## Draft status

Still being worked on: illustrated station construction, a complete tested example, more troubleshooting examples, native AI/multiplayer acceptance and the planned GWB permission-module redesign. Automatic renumbering is a proposal, not a current command.

For the current upgrade details and targeted tests, use [0.7.41 notes](TEST-0.7.41.en.md) and the earlier [0.7.40 point-guard changes](TEST-0.7.40.en.md). Earlier version guides are historical; later changes take precedence.
