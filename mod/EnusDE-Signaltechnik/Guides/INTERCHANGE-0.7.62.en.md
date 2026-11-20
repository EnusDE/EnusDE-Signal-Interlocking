# Buy signal materials at the interchange — 0.7.62

[Deutsch](INTERCHANGE-0.7.62.de.md)

Test build for **Railroader 0.6.3**. Install manually and restart the entire game; host and clients need the same **0.7.62**. Existing wagon selection, warehouses, stock and installation IDs are retained.

## Why 0.7.61 did not offer buying

The two cargo definitions made compatible wagons selectable and extended MOW storage. They did not create the native **InterchangedIndustryLoader** suppliers. Captive service in Equipment lists what a wagon can carry; it does not itself purchase its cargo.

0.7.62 adds two suppliers under **EnusDE Signal & Interlocking** before the game creates an interchange industry. These use that interchange's original track spans. The native factory registers their identifiers and the game's normal destination, payment, capacity, service and return logic handles buying. No instant-fill command or parallel shop is added. Existing suppliers and cached map definitions are not modified. Industries with no interchange, ambiguous multiple interchanges or unusable interchange track references are left unchanged.

## What to click

1. Obtain an owned compatible supply wagon through the existing Equipment selection. **FM/FL** can carry Signal building supplies; **XM** carries Signal equipment. For a visible first test use a normal **FM flat car**, not a logging skeleton.
2. Open the wagon's normal freight/destination menu. Choose **Buy** at an enabled, reachable interchange, then the appropriate signal cargo under **EnusDE Signal & Interlocking**. This is separate from collecting material already in an MOW warehouse. Native menu wording follows the game's language.
3. Deliver the wagon to that interchange and let it perform its normal service. It orders/fills the wagon through the native off-map service and schedules its return. It is **not an immediate fill when you click the destination**. The normal service time, money and capacity conditions apply, including the native handling charge.
4. On return inspect the wagon's actual cargo/quantity. Deliver to an MOW warehouse to unload/store/reload, or couple the loaded supply wagon to an active MOW work car for construction. Existing construction radius remains **250 track metres**.

No chat/console command is required: **Buy → interchange service/return → unload or build** is the normal workflow. This update does not change other cargo, set a wagon's contents manually, bypass a waybill or run the interchange early.

## Models and textures for the test

No dedicated signal-supply model or texture is shipped yet. The inspected native FM definition has a `LoadModelVolume`; the game's generic `rr/loadmodel-crate01` definition accepts wildcard cargo and provides crate models. Generic crates are therefore **expected** on a compatible FM with an actual positive load. The final appearance still needs a game test.

The inspected FL skeleton has only a specific logs model, so do not expect it to display crates merely because it can carry the cargo technically. The inspected closed XM boxcar does not expose an external cargo model. An unchanged outside appearance does not prove an empty wagon; use the native quantity display. Other wagon definitions/mods can differ. Native models/textures remain in the installed game and are not copied into this mod.

## Checks and acceptance

The source compiles against the local 0.6.3 libraries. Offline tests exercise the production DTO preparation and actual prefix, both cargo suppliers, native track/filter/accounting contracts, original-map preservation, repeated preparation, pre-existing suppliers, key collisions and ambiguous/empty tracks. Static inspection confirms native Buy/service/return and the generic crate fallback; no new game session was started by the assistant.

The user confirmed that the previous build panel and wagon selection work sufficiently for continued testing. Actual interchange purchase/payment, outbound/return cycle, cargo display, MOW unloading and save/reload still need acceptance, preferably on a copied save. In multiplayer also test matching-version clients and late join. Native suppliers are recreated deterministically when loading the map; old installation/configuration IDs and balances are not migrated.
