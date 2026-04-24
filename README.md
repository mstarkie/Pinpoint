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

- Left click a placement surface to create a marker.
- Left click an existing marker to select it.
- Edit marker details in the details panel.
- Drag the details panel by its drag handle.
- Use `Delete` in the details panel to remove the selected marker.
- Use `New` or press `N` to clear the current session.
- Use `Save` or press `S` to write the current session JSON.
- Use `Load` or press `L` to reload the saved session JSON.
- Use `Export` to write the analysis export JSON.

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
- `PinpointMarkerView` owns marker selection visuals.
- `MarkerDetailsPanel` binds selected marker data to the UI.
- `PinpointSessionDto` defines serialized session, marker, anchor, and analysis export data.
- `PinpointSessionStorage` handles JSON save/load and analysis export writing.
- `IPointerRayProvider` allows simulator mouse rays to later become headset or controller rays.
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

1. Confirm the session status starts with zero markers.
2. Place two or more markers.
3. Select a marker and edit its title, severity, status, and notes.
4. Confirm the save button changes to `Save*`.
5. Drag the details panel and confirm it stays usable.
6. Save the session.
7. Start a new session and confirm markers clear.
8. Load the session and confirm markers, IDs, timestamps, and marker count are restored.
9. Export analysis JSON and inspect the generated file.

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
