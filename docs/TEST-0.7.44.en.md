# 0.7.44-P0 — Setup and gameplay acceptance

Internal test build for Railroader 0.6.2. Use a save copy; host and clients need
the same version. These are pending gameplay checks, not a claim that Unity or
multiplayer acceptance has already been performed.

## 1. Select the connection

1. In Editor, open a permission module → **Open line, blocks and bidirectional working**.
2. Select both neighbouring interlockings and their departures/arrival targets.
   Name lists and diagram/world selection remain available.
3. **Detect line path automatically** and review both directions. The construction
   assistant requires a plain line without points and exactly one active detector
   endpoint at each end. It does not replace point detection.
4. For a new installation, choose 1–16 blocks or approximate spacing. Three
   kilometres / three blocks produce two intermediate boundaries. Retain existing
   signal/detector boundaries in the proposal.

## 2. Plan and place the assembly

1. Choose the **Block signal operating interlocking**, for example ELA. This
   controls the block signals independently of neighbours A/B.
2. Select any **Additional read-only diagrams**. These boards cannot operate
   the displayed signals or permission module.
3. Set **Signal stagger per side (m)**, initially 10. Review lateral spacing,
   side, height and scale in placement settings first. Short sections limit stagger.
4. **Activate line protection with this confirmation** is recommended. Select
   optional approach contacts. Select **Also enable global contact automation**
   only if all configured contacts in this world should run. Without this checkbox,
   the existing global automation state is retained.
5. **Preview block layout**. Review signal positions, reuse and contact count.
   Boundary distances can be edited before first construction; existing 0.7.44
   assembly boundaries remain fixed.
6. Open **Place assembly in …** for each new board. The preview shows the whole
   unit. Green means free; red means collision/outside. Click the desired top-left
   corner. Cancel discards this placement step.
7. **Confirm block construction and diagram** saves world objects, boards and
   line protection together. With material requirements, new objects remain plans
   until completed. Line status must then prove complete detection and assignments;
   an inactive or unfinished installation must not authorize departure.

One intermediate boundary, with A → B facing right:

```text
A ───── [Signal →] ───── | shared detector | ───── [← Signal] ───── B
                        one physical rail line
```

The signal locations are separate masts. Their detector is a common boundary
between adjacent sections; sharing it does not authorize both directions.

## 3. Migrate or move an existing installation

- **From 0.7.43:** open the connection, reuse its block count, rebuild the preview,
  select operator/displays and confirm positions. The old two-face mast splits
  into separate locations while preserving both signal identities. Only a clearly
  recognized old automatic diagram strip is replaced. A hand-drawn layout is not
  erased globally.
- **Ownership/display only:** on an existing 0.7.44 assembly choose **Change existing
  assembly ownership / display only**, confirm any new positions and apply.
  This does not change world masts or detectors.
- **Move:** use Select mode in Editor. Hold the left mouse button on an assembly
  field and drag; the whole assembly follows. Green means valid; red means collision.
  Escape cancels. Dispatcher also offers **Move this assembly in Editor**;
  the move itself is performed in Editor.
- Before rebuilding, move manually added unrelated fields outside the marked
  assembly. Ordinary custom signal labels, auxiliary links and number-source
  identities survive rebuilding.

## 4. Test operation in both directions

1. Prove the whole line clear. After three separate clear readings, normal
   permission should be visible. The other neighbour requests it; giving follows
   the selected manual/automatic conditions.
2. Establish departure in the permitted direction. An opposing movement must be
   rejected even if distant blocks remain clear. A green station approach ending
   before the common line axis is not by itself proof of opposing line authority.
3. Move a train beyond a block signal but before its shared detector: passage
   proving must not return it to Stop prematurely. Once beyond the shared detector
   and normal passage distance, that signal returns to Stop and number transfer
   is triggered. Other protection checks can independently withdraw authority.
4. Reverse permission and repeat. Check signal facing, lateral clearance and the
   common boundary. The head triggers passage proving; retained locks still wait
   for the tail and complete clear proving.
5. For contact operation select locomotive **train duty**, check global automation
   and next-signal destinations. Manual operation belongs to the owning local
   interlocking. Chains operating several interlockings still require the master;
   a local route may end at its neighbour's signal.
6. Check read-only views: signals/numbers follow their originals; clicking cannot
   issue signal or permission commands. After owner transfer, only the new local
   interlocking may own the block signals.

## 5. Configuration blockers and regression

- Retain an unrelated route/direction lease and edit a local auxiliary on another
  line: no global blanket refusal.
- Try changing the affected held line: it must refuse. **Last change blockers**
  must identify affected names and reasons, including occupied/unreadable intervals
  when available. This is the last attempt, not live clear proving. Retry after
  complete release and three clear readings.
- If the former auxiliary target is a line endpoint, change that specific
  dependency first. Changing the button function must not orphan the endpoint.
- Repeat the successful compatible parallel-route case from 0.7.43. Then request
  contrary point positions or a real flank conflict: these must still refuse.
- A configuration/world change must reject stale previews. Cancelling a preview
  must create no objects, material charges or diagram edits.
- Save/reload and check client views. IDs/assignments remain; previous proceed
  authorities still do not reload from the save.

For a failure, retain the exact version, action, message and diagnostic export.
Bot orders, autonomous crossing/siding control and new signal models remain
later steps, outside this acceptance checklist.
