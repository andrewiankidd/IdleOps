# Tech Debt

## Test coverage gaps

- Tests for audcap, vidcap, and playbk are Windows-only and require real hardware (audio device, display, ffmpeg)
- No integration tests for cross-platform paths (macOS/Linux ffmpeg capturers)
- outcap tests only cover option parsing, not actual capture/merge logic
- inpctl tests only cover window matching and key mapping, not actual input sending
- waitfr, imgfnd, spkbak test projects exist but have no tests yet (require live desktop / hardware)

## No CI/CD pipeline

No GitHub Actions, Azure Pipelines, or other CI configuration exists. Tests must be run manually.
