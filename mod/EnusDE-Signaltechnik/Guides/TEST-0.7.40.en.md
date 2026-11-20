# 0.7.40-P0 – Shared physical point-protection boundaries

Internal Railroader 0.6.2 test build. Host and clients need the same version. The actual HS-044 train position and multiplayer still need gameplay verification.

## Correction

Diagram GFM already derived point intervals from real point arms, physical detectors and explicit point-arm bindings. Route construction independently added up to 25 m on every arm. Those extra intervals could include a waiting vehicle beyond the effective detector boundary even when the GFM point display was clear.

Train routes, shunt routes and diverting flank points now use the same effective physical point definition as GFM. A nearer real detector limits that arm; other arms remain protected. Explicit manual point-arm bindings remain effective. Without a nearer physical boundary the existing up-to-25-m default stays. A drawn arrow or signal without active GFM cannot shorten protection.

This does not clear an actual occupancy. Vehicles inside the correctly bounded protection still matter. Known occupied shunt track outside point guards remains permitted under the 0.7.39 policy.

Flank protection means holding a lateral protecting signal at Stop or holding a point diverting away, together with any watched threat corridor. Point protection means the monitored intervals around the common/tip, normal and reverse arms where a vehicle can occupy or foul the point. Occupancy itself is independent of direction; route search and signal passage use the real signal direction.

## Existing definitions

Newly calculated routes use the correction immediately. Recalculate saved routes with points from older builds once: **Editor → Menu → Saved route planning → select definition → align real points for the intended path → Recalculate route**. Release active authorities first. Existing route ID and protection wait are retained; verify the newly calculated path.

Each new route stores a signature of its effective point guards. Moving a detector or changing a binding invalidates obsolete geometry before reuse; a fault during an established route keeps its locks. Renaming or changing enumeration order does not change this signature. Rebuilding the entire station or renumbering is unnecessary. Genuine bad detector bindings still need correction in the section manager.

## Gameplay checks

Keep the train at HS-044 and retry HS-036 using a newly calculated route. Repeat as a shunt to RS-067. Rejections explicitly identify train versus shunt and track, point-protection or flank area; use the shunt half/action when intended.

Inspect W207 and GFM-A-019 / GFM-A/B-020: a separate old 25-m guard must no longer extend beyond their effective physical boundary into the waiting train. Test both directions and both train/shunt requests. As a countercheck, genuine occupied point protection must still reject the request and retain required locks.

Export a host diagnostic ZIP immediately if rejection persists. It includes the route guard signature, vehicle intervals and detector positions. A screenshot alone cannot establish that every protection interval is clear.

The 0.7.39 shared signal-stop/number-transfer passage point remains 5 m by default, adjustable to 0–25 m. It does not replace correct point boundaries. Installation refresh/audit and the section manager remain available.
