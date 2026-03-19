# Riot Picker

Auto champion/agent selection tool for League of Legends and Valorant.

**[Turkce README](README_TR.md)**

## Features

### League of Legends
- **Auto Accept** - Automatically accepts when a match is found
- **Auto Champion Pick** - Picks and locks champions based on role-specific priority lists
- **Auto Ban** - Bans champions in priority order
- **Auto Rune Page** - Automatically selects the rune page matching the locked champion's name

### Valorant
- **Auto Agent Select** - Automatically selects and locks the first available agent from your priority list

### General
- **Drag & Drop Reorder** - Reorder priority lists by dragging
- **Language Support** - English / Turkish
- **Single .exe** - No installation required, zero dependencies
- **Low Resource Usage** - Adaptive polling for minimum CPU/RAM usage

## Installation

1. Download `RiotPicker.exe` from the [Releases](../../releases) page
2. Run it - no installation needed

## Usage

1. Open the LoL or Valorant client
2. Run RiotPicker
3. Enable LoL/VAL toggles
4. Set up your champion/agent priority lists
5. Wait for a match - RiotPicker handles the rest

## Build

```bash
# Debug
dotnet run --project RiotPicker

# Release (single exe)
dotnet publish RiotPicker/RiotPicker.csproj -c Release -o ./publish
```

## Tech Stack

- **C# / .NET 8** + **Avalonia UI 11**
- Self-contained single-file publish (~38MB)
- LCU API (LoL) + Valorant Local API

## License

MIT
