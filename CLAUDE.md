# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

BoardSketch is a Unity drawing/notes app for the **board.fun** hardware — a 24" touchscreen game board (1920x1080 @ 60Hz, MediaTek Genio 700, Mali-G57 GPU, 4GB RAM). Users draw with their fingers and can switch tools by placing physical game pieces on the board.

## Unity Project

- **Unity Version:** 6000.4.0f1 (Unity 6)
- **Project Root:** `BoardSketch/`
- **Build Target:** Android
- **Scene:** `Assets/Scenes/BoardSketchScene.unity` (single scene)
- **MCP Integration:** `.mcp.json` configures `unityMCP` server for editor automation via Claude Code

## Key Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| `fun.board` | 3.2.1 (local tgz) | Board SDK — touch input, glyph detection, simulation |
| `com.unity.inputsystem` | 1.7.0 | Input System (used by Board SDK simulation) |
| `com.coplaydev.unity-mcp` | git/main | MCP for Unity editor automation |

## Architecture

### Input Pipeline

```
Board hardware (Android) ──→ Native plugin ──→ BoardInput event queue
                                                      │
Editor simulation (mouse) ──→ BoardContactSimulation ─┘
                                                      │
                                          BoardInput.GetActiveContacts()
                                                      │
                                              SketchManager.Update()
                                                      │
                                    ┌─────────────────┼──────────────────┐
                              HandleFingerInput()              HandleGlyphInput()
                              (drawing strokes)              (piece → tool switch)
```

- **SketchManager** polls `BoardInput.GetActiveContacts(BoardContactType.Finger)` each frame
- Each contact tracked by `contactId` in a `Dictionary<int, StrokeState>` for multi-touch
- Physical game pieces detected via `BoardContactType.Glyph` and mapped to tools via `PieceToolConfig`

### Coordinate System

Board SDK positions are **Y-down** (0 at top, 1080 at bottom). Unity screen space is **Y-up**.

`BrushRenderer.ScreenToRT()` converts: SDK screenPosition → flip Y → `Camera.ScreenToWorldPoint` → normalize to quad bounds → 1920x1080 RenderTexture coordinates.

The canvas quad spans world coords (-9.6, -5.4) to (9.6, 5.4) with an orthographic camera. GL pixel matrix is loaded Y-down to compensate for RT display flip.

### Rendering

- **Canvas:** 1920x1080 ARGB32 RenderTexture, white background
- **Brush:** 64x64 soft circle texture (radial gradient alpha via SmoothStep)
- **Stamping:** GL immediate mode quads via custom `BrushStamp` shader (SrcAlpha/OneMinusSrcAlpha blend)
- **Interpolation:** Linear for ≤2 points, Catmull-Rom spline for 3+ points
- **Stamp spacing:** `max(brushSize * 0.25, 1)` pixels
- **Eraser:** Stamps white with full alpha over existing content

### State Flow

Gallery View (default) → New/Open → Canvas View (drawing + toolbar) → Back → auto-save if dirty → Gallery View

Storage: `Application.persistentDataPath/BoardSketch/` with PNG files + 432x243 thumbnails + `sketches.json` index.

## Editor Testing

`EditorTouchSimulator` (`Assets/Scripts/EditorTouchSimulator.cs`) enables two testing modes:

### Mouse-as-Finger (automatic)
Enters automatically on Play mode via `[RuntimeInitializeOnLoadMethod]`. Click and drag in the Game view to draw. Uses the Board SDK's built-in `BoardContactSimulation` with `useMouseAsFinger = true`.

### Programmatic Touch Injection (via MCP)
Menu: **BoardSketch → Run Touch Test**

Injects a 10-point stroke across frames using `EditorApplication.update`. Uses reflection to access the internal `BoardInput.QueueStateEvent(BoardContactEvent)` method since `BoardContactEvent` is internal to the SDK assembly.

MCP testing workflow:
1. `manage_editor(action="play")` — enter Play mode
2. `execute_menu_item("BoardSketch/Run Touch Test")` — inject test stroke
3. `read_console` — verify no errors (look for `[EditorTouchSimulator]` prefixed logs)

### Editor Menu Items
- **BoardSketch / Run Touch Test** — programmatic touch injection during Play mode
- **BoardSketch / Setup Full UI** — procedurally creates the full gallery + toolbar UI hierarchy

## Board SDK Notes

- `BoardInput.GetActiveContacts()` returns empty if `BoardSupport.enabled` is false (auto-set `true` in editor)
- `BoardContactEvent` is **internal** — must use reflection from Assembly-CSharp
- `BoardContactPhase`: None, Began, Moved, Ended, Canceled, Stationary
- Contacts that begin AND end in the same frame are filtered out (not surfaced to callers)
- Editor simulation lives in `Board.Input.Simulation` namespace (`#if UNITY_EDITOR` only)
