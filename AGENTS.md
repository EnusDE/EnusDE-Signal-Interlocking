# Project instructions

## Interface language

- English is the default interface language. German must be selectable without another mod.
- Every new or changed player-visible text must be provided in English and German in the same change. Use LocalText.Select(english, german), WindowText(english, german), or a LocalText.Get key present in both language packs.
- Maintain mod/EnusDE-Signaltechnik/Languages/en.json and de.json together. Regenerate src/BuiltInLanguage.cs with tools/update-language-catalog.py when the packs change.
- Do not add new untranslated UI literals. The localization check only exempts the project name and the language-neutral X/Y coordinate labels; do not extend that list with interface prose.
- Keep technical identifiers, user-provided names and native game error messages intact. Do not translate them into different identifiers.
- Run the localization checks through Test.ps1 after UI text changes. Keep prepared release archives unchanged unless a new package version is explicitly being built.

## Source checkpoints (standing user authorization)

- GitHub `EnusDE/EnusDE-Signal-Interlocking`, branch `main`, is the canonical current source. At the beginning of each task, inspect the live remote state and the local worktree; preserve pending work and coordinate any concurrent changes before editing.
- After each completed requested development/fix step, run the checks appropriate to that change, review and explicitly stage the intended source/tests/public documentation, commit, upload to GitHub and verify the remote commit. Routine source uploads are already authorized; do not ask again.
- Use normal forward commits. Never force-push, reset away work, overwrite others' changes or resolve conflicts blindly. If remote changes appear, read and integrate them safely; report any unresolved conflict or upload failure explicitly.
- A clearly labelled intermediate source checkpoint is allowed when useful. Report tests actually run and remaining limitations; do not claim a checkpoint is a tested release or that an unconfirmed upload is backed up.
- Exclude game libraries, binaries/install ZIPs, SDKs, saves/world or installation data, personal settings, diagnostics/logs, secrets, caches and private working notes/chat handoffs. Review the staged/new files, not just ignore patterns.
- This workflow runs at task checkpoints, not as a continuously running background synchronization service. Releases, tags, runtime installation and game launches require a separate request. Keep existing prepared archives unchanged.
