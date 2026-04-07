# Overview

## What IdleOps Does

- **Desktop automation**: Drive keyboard/mouse input, launch applications, and execute scripted workflows across the desktop (not limited to browsers)
- **Media capture**: Record system audio and screen/window video, merge them into synchronized output files
- **OCR-driven interaction**: Find text on screen via OCR and click it, find UI elements by image template matching
- **Scriptable workflows**: Define repeatable automation flows in YAML — open apps, interact with them, capture the results — for use cases like automated video documentation and screenshot generation

## Current Status

- **13 tools** across the toolkit, all building and running on Windows
- **Core automation**: audcap, vidcap, outcap, playbk, inpctl — the original capture and input tools
- **OCR & vision**: txtfnd (OCR text finder), imgfnd (image template matching), scrcap (screenshot capture)
- **Utilities**: waitfr (wait for conditions), stpcap (input recorder), spkbak (text-to-speech), cnvrtr (universal converter)
- **Cross-platform**: audcap and vidcap have macOS/Linux implementations via ffmpeg. Windows-only tools: inpctl, txtfnd, scrcap, imgfnd, waitfr, stpcap, spkbak

## Tech Stack

- **.NET 10.0** — all projects target `net10.0` with nullable reference types and implicit usings
- **NAudio 2.2.1** — Windows audio capture via WASAPI loopback (audcap only)
- **YamlDotNet 15.3.0** — script parsing with snake_case → PascalCase naming convention
- **Microsoft.Extensions.FileSystemGlobbing 8.0.0** — input file pattern matching
- **OpenCvSharp4** — image template matching (imgfnd only)
- **Windows.Media.Ocr** — built-in Windows OCR API (txtfnd, waitfr)
- **Windows.Media.SpeechSynthesis** — built-in Windows TTS (spkbak)
- **ffmpeg** — video capture, audio capture on non-Windows, A/V merge, file conversion (cnvrtr)
- **P/Invoke** — Windows API access for window enumeration, input simulation, screenshot capture
- **xUnit 2.7.0 + coverlet** — testing and code coverage (13 test projects)
