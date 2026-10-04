# Contributing

Use small, focused changes with a clear explanation. Preserve the lightweight Windows/.NET architecture unless a dependency change has a demonstrated need. Keep passwords out of logs, settings, CLI arguments and examples. Do not change Windows root trust automatically.

Build with `Build_App.bat`, run `02_run_checks.bat` on Windows, inspect the UI at minimum size and multiple DPI settings, and complete the relevant release checklist steps. Add meaningful failure-path checks for changes affecting file commit, PFX handling or signing validation. Do not claim Windows signing or installer validation based on source inspection alone.

By submitting code, you agree it may be distributed under the repository's MIT License. Forks should identify themselves clearly and must not imply endorsement by NorthStar Software Works.
