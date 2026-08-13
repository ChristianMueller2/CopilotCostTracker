# CopilotCostTracker - Changes Since 1.0.1 Line (up to 1.0.2)

Generated from Git history and merge metadata.

- Mainline baseline before cost-improvements PR: 20b4b64
- Feature head merged in PR #1: c1c12c2
- Post-merge version bump for 1.0.2: 6a95754
- Informational merge commit: 24bf168 (PR #1)

## Summary

This release line introduced substantial cost-tracking and pricing UX improvements before the 1.0.2 version bump.

## Functional Changes

1. Improved cost calculation accuracy across all supported input formats.
   - Commit: a99ff70

2. Added editable model-pricing table in settings.
   - Commit: 07ca7fb

3. Added USD unit prefix to pricing table columns.
   - Commit: 68cb0c5

4. Cleaned up pricing table unit rendering.
   - Commit: 716b8e7

5. Improved Add Folder UX and auto-added default folders.
   - Commit: c1c16c4

6. Fixed JSONL file opening by using FileShare.ReadWrite to avoid EBUSY conflicts.
   - Commit: 6ee81da

7. Added notification when a session uses a model without pricing.
   - Commit: e38a513

8. Updated version numbers and enhanced credits/USD toggle UI.
   - Commit: c1c12c2

## Packaging and Versioning

1. Bumped application display version from 1.0.1 to 1.0.2.
   - File: CopilotCostTracker.csproj
   - Commit: 6a95754

2. Bumped Windows package identity version from 1.0.1.0 to 1.0.2.0.
   - File: Platforms/Windows/Package.appxmanifest
   - Commit: 6a95754

## Commit List

- 6a95754 feat: bump application version to 1.0.2
- c1c12c2 feat: update version numbers and enhance UI for credits/USD toggle
- e38a513 feat: notify when session uses model with no pricing entry
- 6ee81da fix: open JSONL files with FileShare.ReadWrite to prevent EBUSY conflict
- c1c16c4 feat: auto-add default folders and improve Add Folder UX
- 716b8e7 fix: clean up pricing table units display
- 68cb0c5 feat: add $ unit prefix to pricing table columns
- 07ca7fb feat: editable model-pricing table in settings
- a99ff70 fix: improve cost calculation accuracy across all three formats
