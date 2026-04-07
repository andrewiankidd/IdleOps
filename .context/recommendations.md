# Recommendations

## Next Steps

- **CI pipeline**: Add GitHub Actions for build + test on Windows (and optionally Linux/macOS for cross-platform validation)
- **Linux/macOS window capture**: Implement `--window` support on non-Windows platforms using xdotool/xwininfo (Linux) or CGWindowListCreateImage (macOS)

## Future Considerations

- **Browser automation bridge**: Optional integration with Playwright/Puppeteer for hybrid desktop+browser workflows
- **Script variables and conditionals**: Richer script model with variables, conditionals, and loops for complex automation scenarios
- **Parallel step execution**: Run multiple steps concurrently in playbk (e.g., launch two apps at once)
