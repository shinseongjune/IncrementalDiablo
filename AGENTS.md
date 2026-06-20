# IncrementalDiablo Automation Workspace Guard

This repository's daily production automation has one writable checkout:
`D:\Unity\IncrementalDiablo`.

When an automation starts in a detached Codex worktree such as
`C:\Users\sodau\.codex\worktrees\<id>\IncrementalDiablo`, treat that checkout as
read-only inspection context. Do not edit, validate, stage, commit, or push from it.

Before the first write in a daily automation run:

1. Run `git -C D:\Unity\IncrementalDiablo rev-parse --show-toplevel` and confirm it
   resolves to `D:\Unity\IncrementalDiablo`.
2. Run `git -C D:\Unity\IncrementalDiablo status --short` and identify pre-existing
   user changes.
3. Use explicit `D:\Unity\IncrementalDiablo` paths or that directory as the working
   directory for every edit, validation command, and Git command in the run.

If the Unity Editor is open, modify the primary checkout only and state that the
open `Gameplay` scene may need to reload an external scene-file change. This guard
applies to the daily automation workflow; a user can explicitly override it for a
separate branch, worktree, or pull-request task.
