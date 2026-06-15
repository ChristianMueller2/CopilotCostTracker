# CopilotCostTracker - Feature Catalog

This document provides a quick overview of the product capabilities.

## Cost Intelligence

1. Multi-format cost calculation: Computes usage costs consistently across all supported session formats.
2. Editable pricing model table: Lets you manage per-model pricing directly in app settings.
3. USD-first pricing display: Shows costs in a clear currency layout for faster scanning.
4. Consistent pricing UI: Keeps units and column formatting uniform across the pricing table.
5. Missing-price detection: Warns when a session references a model without configured pricing.

## Data Handling

1. Concurrent-safe JSONL reading: Reads session files even while they are still being written.
2. Reduced file lock issues: Minimizes conflicts during active logging and live analysis.

## Usability

1. Guided folder onboarding: Adds useful default folders to reduce initial setup time.
2. Improved folder add flow: Makes folder registration faster and less error-prone.
3. Credits/USD quick toggle: Allows fast switching between credit and currency views.
4. Icon-based navigation: Uses clear SVG icons for easier orientation in the app.
5. Notification controls: Includes settings to fine-tune notification behavior.

## Platform and Delivery

1. MAUI desktop foundation: Built on a maintainable architecture for ongoing feature growth.
2. Clean project hygiene: Excludes build artifacts from source control for a cleaner repo.
3. Reliable packaging flow: Supports consistent MSIX versioning and distribution readiness.
