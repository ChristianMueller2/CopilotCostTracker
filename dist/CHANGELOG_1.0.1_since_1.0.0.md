# CopilotCostTracker - Version 1.0.1 (Since 1.0.0)

Generated from Git history.

- Baseline before 1.0.1 work: 20b4b64
- Version commit (1.0.1): c1c12c2
- Covered range for this release line: 20b4b64..c1c12c2

## Summary

Version 1.0.1 focused on cost-calculation correctness, pricing usability, folder onboarding improvements, and resilience when reading active JSONL files.

## Key Features and Fixes

1. Improved cost calculation accuracy across all three supported formats.
   - Commit: a99ff70

2. Added editable model-pricing table in settings.
   - Commit: 07ca7fb

3. Added USD unit prefix to pricing table columns.
   - Commit: 68cb0c5

4. Cleaned up pricing unit display in the table.
   - Commit: 716b8e7

5. Improved folder onboarding by auto-adding defaults and refining Add Folder UX.
   - Commit: c1c16c4

6. Fixed JSONL file access conflicts using FileShare.ReadWrite (prevents EBUSY-like lock issues).
   - Commit: 6ee81da

7. Added a notification when a session references a model without pricing metadata.
   - Commit: e38a513

8. Enhanced credits/USD toggle UI and updated release version metadata.
   - Commit: c1c12c2

## Commit List

- c1c12c2 feat: update version numbers and enhance UI for credits/USD toggle
- e38a513 feat: notify when session uses model with no pricing entry
- 6ee81da fix: open JSONL files with FileShare.ReadWrite to prevent EBUSY conflict
- c1c16c4 feat: auto-add default folders and improve Add Folder UX
- 716b8e7 fix: clean up pricing table units display
- 68cb0c5 feat: add $ unit prefix to pricing table columns
- 07ca7fb feat: editable model-pricing table in settings
- a99ff70 fix: improve cost calculation accuracy across all three formats
