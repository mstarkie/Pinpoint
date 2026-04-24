# Pinpoint

Pinpoint is a Unity prototype for AR-enabled spatial knowledge capture and AI-ready decision support in shipbuilding and industrial work environments.

The core workflow lets a user place digital markers in a 3D scene, attach structured observations to those markers, save and reload the session, and export marker data for later analysis. The near-term focus is validating the marker capture workflow in a desktop simulator before extending the same abstractions to AR hardware such as Magic Leap or other OpenXR-capable headsets.

## Current Prototype

The active development slice is the desktop simulator scene:

`Assets/Scenes/Pinpoint_SIM.unity`

Current capabilities include:

- Place, select, edit, and delete spatial markers in the simulator.
- Track stable marker IDs using 32-character GUID strings.
- Capture marker title, severity, status, and raw notes.
- Store marker creation and update timestamps in UTC.
- Show session feedback for marker count and last saved time.
- Mark unsaved edits with `Save*`.
- Save and load marker sessions as JSON.
- Export an AI-ready analysis JSON package.
- Toggle between selection and deliberate marker placement modes.
- Preview marker placement with a small runtime reticle before placing.
- Color markers by severity and show status with a camera-facing badge.
- Drag the marker details panel in the simulator.
- Keep pointer input, panel dragging, note normalization, and marker anchoring behind small interfaces so the simulator can later be swapped for AR controller, headset, voice, and spatial anchor implementations.

## Project Status

This repository is currently an early prototype. It is focused on proving the interaction model, data model, and traceability workflow in the simulator.

It is not yet a production AR deployment. The current code intentionally keeps AR-facing concepts, such as pointer rays and spatial anchors, abstracted behind interfaces so hardware-specific implementations can be added without rewriting the marker workflow.

## Requirements

- Unity `6000.3.10f1`
- Unity packages are restored from `Packages/manifest.json`
- TextMesh Pro / UGUI
- Universal Render Pipeline
- Unity Input System
- XR Interaction Toolkit, XR Management, and OpenXR packages are present for the future XR path

## Getting Started

1. Clone the repository.
2. Open the project root in Unity Hub.
3. Use Unity `6000.3.10f1`.
4. Open `Assets/Scenes/Pinpoint_SIM.unity`.
5. Let Unity import packages and compile scripts.
6. Press Play.

## Simulator Controls

- The simulator starts in `Select` mode.
- Use the `Select` and `Place` buttons, or press `P`, to switch modes.
- The `Select` and `Place` buttons swap colors while placement mode is active, then restore after placement returns to `Select`.
- In `Select` mode, left click an existing marker to select it.
- In `Select` mode, left click empty scene space to deselect the current marker.
- In `Place` mode, a green preview reticle follows valid placement surfaces.
- In `Place` mode, left click a valid placement surface to create a marker. The simulator returns to `Select` mode after placement.
- Edit marker details in the details panel.
- Drag the details panel by its drag handle.
- Use `Delete` in the details panel, or press `Delete` / `Backspace`, to remove the selected marker.
- Use `New` or press `N` to clear the current session.
- Use `Save` or press `S` to write the current session JSON.
- Use `Load` or press `L` to reload the saved session JSON.
- Use `Export` or press `E` to write the analysis export JSON.

## Saved Data

Pinpoint currently writes local JSON files through Unity's `Application.persistentDataPath`.

- Session save file: `pinpoint_session.json`
- Analysis export file: `pinpoint_analysis_export.json`

The session JSON stores marker IDs, marker fields, timestamps, legacy position data, and an anchor DTO. Older JSON files that do not contain anchor or timestamp data are expected to load safely with fallback behavior.

Timestamps are stored as ISO 8601 UTC strings and displayed in the UI as UTC.

## Architecture Overview

The prototype is organized around small, replaceable pieces:

- `PinpointSimulatorController` coordinates marker placement, selection, save/load, session status, and export.
- `PinpointMarkerModel` owns marker data such as ID, title, severity, status, notes, and timestamps.
- `PinpointMarkerView` owns marker selection, severity color, and status badge visuals.
- `MarkerDetailsPanel` binds selected marker data to the UI.
- `PinpointSessionDto` defines serialized session, marker, anchor, and analysis export data.
- `PinpointSessionStorage` handles JSON save/load and analysis export writing.
- `IPointerRayProvider` allows simulator mouse rays to later become headset or controller rays.
- `IPinpointInteractionInputProvider` maps device input to app-level intents such as scene action, placement mode, save, load, delete, and export.
- `IMarkerAnchorProvider` allows simulator positions to later become AR planes, spatial anchors, model coordinates, QR anchors, or another shop-floor localization source.
- `IPanelDragInputProvider` allows mouse-driven panel dragging to later become hand-controller or tracked-pointer dragging.
- `IMarkerNoteNormalizer` is a placeholder seam for future note cleanup, dictation, or AI-assisted normalization.

## Why The Anchor Abstraction Exists

In the simulator, a marker can be restored from a simple world-space position. In an AR shop-floor environment, that is usually not enough: the marker needs to remain tied to a real place even as tracking sessions, devices, or coordinate frames change.

The marker anchor abstraction separates "what marker did the user create?" from "how does this platform resolve that marker's real-world pose?" That keeps the current simulator simple while leaving room for future anchor providers backed by AR planes, persistent spatial anchors, model coordinates, QR codes, or shipyard-specific localization systems.

## Repository Layout

```text
Assets/
  Pinpoint/
    Scripts/          Core prototype scripts
  Scenes/
    Pinpoint_SIM.unity    Current simulator scene
    Pinpoint_XR.unity     Future XR scene path
Packages/
  manifest.json       Unity package dependencies
ProjectSettings/
  ProjectVersion.txt  Unity editor version
```

## Smoke Test

After opening `Pinpoint_SIM.unity` and entering Play Mode:

1. Confirm the session status starts in `Select` mode with zero markers.
2. Left click empty scene space and confirm no marker is created.
3. Click `Place`, or press `P`, and confirm the status changes to `Place` mode and the mode buttons swap colors.
4. Move the pointer over a valid placement surface and confirm the green preview appears.
5. Left click to place a marker and confirm the simulator returns to `Select` mode with the original button colors restored.
6. Place a second marker, select one, and edit its title, severity, status, and notes.
7. Confirm the save button changes to `Save*`.
8. Select a marker and confirm the sphere turns white while selected.
9. Change severity, select another marker, and confirm the marker uses its severity color when not selected.
10. Change status and confirm the camera-facing status badge updates immediately.
11. Drag the details panel and confirm it stays usable.
12. Save the session.
13. Start a new session and confirm markers clear.
14. Load the session and confirm markers, IDs, timestamps, visual state, and marker count are restored.
15. Export analysis JSON and inspect the generated file.

## Near-Term Roadmap

- Add an AR hand-controller or tracked-pointer implementation for panel and marker interaction.
- Replace simulator anchor resolution with a real AR anchor provider.
- Add voice dictation support for marker notes.
- Improve marker visuals for headset use.
- Add filtering, search, and issue review workflows.
- Expand the analysis export into an AI-assisted summarization and trend-identification pipeline.
- Evaluate practical workflows for shipbuilding planning, quality, inspection, production, and engineering handoff.

## License

License information has not been added yet.
