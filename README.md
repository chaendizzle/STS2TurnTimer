# Turn Timer

A Slay the Spire 2 multiplayer mod that shows a countdown timer when other players are waiting for you to end your turn.

## Features

- Countdown timer bar displayed above the End Turn button
- Visual progress bar with color feedback (gold to red as time runs out)
- Optional automatic turn ending when the timer expires
- Can be set to start from the beginning of each turn or only when others are waiting

## Configuration

All options are configurable in-game via the mod settings menu.

| Option | Values | Default | Description |
|--------|--------|---------|-------------|
| TimerDurationSeconds | 10–120 sec | 30 | How long the countdown lasts |
| AutoEndTurn | On/Off | On | Automatically end turn when timer expires |
| StartTimerFromTurnStart | On/Off | Off | Start timer at turn start instead of when others are waiting |

## Installation

1. Install [BaseLib](https://github.com/Alchyr/BaseLib-StS2) to your mods folder
2. Copy `STS2TurnTimer.dll`, `STS2TurnTimer.json`, and `STS2TurnTimer.pck` to `Slay the Spire 2/mods/STS2TurnTimer/`
3. Enable the mod in-game

## Dependencies

- [BaseLib](https://github.com/Alchyr/BaseLib-StS2)

## Building

```
dotnet build -c Debug
dotnet publish -c Release
```

The build automatically copies the DLL and manifest to the game's mods folder. `dotnet publish` also exports the Godot `.pck` if MegaDot is configured.
