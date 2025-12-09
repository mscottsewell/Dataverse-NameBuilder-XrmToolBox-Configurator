# NameBuilder Configurator – XrmToolBox Plugin

> Visual designer for the [NameBuilder](https://github.com/mscottsewell/Dataverse-NameBuilder) Dataverse plug-in. Connect through XrmToolBox, assemble name patterns, preview the output, and publish JSON back to Create/Update steps without writing code.
<img width="1591" height="1037" alt="image" src="https://github.com/user-attachments/assets/8a509dbe-70af-49e1-b634-0a037c92006d" />

## Table of Contents

- [Overview](#overview)
- [Documentation Map](#documentation-map)
- [Feature Highlights](#feature-highlights)
- [Installation](#installation)
- [Building From Source](#building-from-source)
- [Getting Started](#getting-started)
- [UI Guide](#ui-guide)
- [Designer Workflows](#designer-workflows)
- [Defaults, Fallbacks, and Conditions](#defaults-fallbacks-and-conditions)
- [Publishing & Deployment](#publishing--deployment)
- [Settings & Persistence](#settings--persistence)
- [Troubleshooting](#troubleshooting)
- [Packaging for the XrmToolBox Store](#packaging-for-the-xrmtoolbox-store)
- [Support & License](#support--license)

## Overview

NameBuilder Configurator is a WinForms-based XrmToolBox plug-in that:

- Reads entity metadata, views, and sample records directly from Dataverse.
- Visualizes the NameBuilder JSON schema as draggable field blocks with per-block property panes.
- Offers dialogs for include-if conditions and default-if-blank fallbacks.
- Publishes JSON payloads back to the NameBuilder Create/Update steps without leaving XrmToolBox.

## Documentation Map

- **Full walkthrough & screenshots** – [docs/USAGE.md](docs/USAGE.md)
- **Canonical schema & plug-in internals** – [Dataverse-NameBuilder Docs](https://github.com/mscottsewell/Dataverse-NameBuilder/tree/main/Docs)

## Feature Highlights

- 🔌 **Connection-aware startup** – Reuses your XrmToolBox connection, validates that the NameBuilder assembly exists, and surfaces hash mismatches before publishing.
- 📋 **Solution-scoped entity browser** – Filter entities by Dataverse solution, load metadata and views, pick sample records, and double-click attributes to insert them.
- 🎯 **Solution-based deployment** – Select an unmanaged solution to organize NameBuilder plugin assemblies and steps; prevents duplicate step registrations per entity.
- ✨ **Visual block editor** – Manage ordered field blocks with drag handles (▲/▼), move buttons, deletion, and inline summaries showing configured properties.
- ⚙️ **Reusable defaults** – Persist global prefix, suffix, number/date formats, and timezone offsets in `%APPDATA%\NameBuilderConfigurator\settings.json`; automatically propagate changes to untouched blocks.
- 🧮 **Default-if-blank dialog** – Select alternate attributes or literal defaults from one dialog, backed by a read-only behavior summary in the property pane showing the fallback chain.
- 🧱 **Condition builder** – Compose simple field comparisons or nested `anyOf`/`allOf` trees with support for operators like `equals`, `contains`, `isNull`, etc.
- 📄 **Import/export/publish** – Round-trip JSON files, copy payloads to the clipboard, retrieve existing configurations from Dataverse steps, or publish directly back to Create/Update steps.
- 🔍 **Live preview** – See assembled name strings in real-time as you edit, using selected sample record data.
- 🧰 **Plug-in validation & deployment** – Check NameBuilder assembly presence, verify version hashes, and deploy/update the plug-in from within the tool.
- 🧰 **Scripted build pipeline** – `build.ps1` restores packages, compiles, deploys to the local XrmToolBox folder, and mirrors outputs into `Ready To Run/`.

## Installation

### XrmToolBox Tool Library (recommended)

1. In XrmToolBox, open **Configuration ➜ Tool Library**.
2. Search for **NameBuilder Configurator**.
3. Click **Install** and restart XrmToolBox if prompted.

### Manual install

1. Produce a Release build (see below).
2. Copy `NameBuilderConfigurator.dll` and the `Assets/` folder to `%APPDATA%\MscrmTools\XrmToolBox\Plugins`.
3. Restart XrmToolBox; the plug-in appears in the tool list.

## Building From Source

### Prerequisites

- Visual Studio 2022+ with the **.NET desktop development** workload.
- .NET Framework 4.8 targeting packs.
- PowerShell 7 or later.
- XrmToolBox installed (for local validation).

### Scripted build (recommended)

```pwsh
pwsh -File .\build.ps1 -Configuration Release
```

Script behavior:

1. Increments `Properties/AssemblyInfo.cs`.
2. Restores NuGet packages and runs MSBuild.
3. Copies the plug-in + assets into `%APPDATA%\MscrmTools\XrmToolBox\Plugins`.
4. Mirrors artifacts into `Ready To Run/` for manual distribution.

### Visual Studio workflow

1. Open `NameBuilderConfigurator.sln`.
2. Restore NuGet packages when prompted.
3. Build the **Release** configuration (`Ctrl+Shift+B`).
4. Copy `bin/Release/NameBuilderConfigurator.dll` + `Assets/` into the XrmToolBox plugin folder to test.

### Testing inside XrmToolBox

1. Ensure the DLL and `Assets/` reside under `%APPDATA%\MscrmTools\XrmToolBox\Plugins`.
2. Launch XrmToolBox, connect to a Dataverse org with the NameBuilder plug-in installed.
3. Open **NameBuilder Configurator**; it verifies the assembly and loads the designer.

## Getting Started

1. **Connect** via the XrmToolBox connection wizard.
2. **Load Entities** to populate metadata, views, and sample records.
3. **Select an entity** (and optional view/sample record).
4. **Build the pattern** by double-clicking attributes, configuring block properties, and observing the live preview/JSON tabs.
5. **Publish, export, or copy** the resulting JSON.

Every ribbon button, dropdown, and property control now exposes a tooltip—hover to see inline guidance about what the control does and how it maps to the JSON schema.

## UI Guide

| Area | Description |
| --- | --- |
| **Ribbon** | Load entities, retrieve configured entities, import/export/copy JSON, update the NameBuilder assembly, and publish. Tooltips summarize each command. |
| **Solution dropdown** | Filter entities by Dataverse solution (optional). Display names shown; solution IDs stored for lookups. |
| **Entity explorer** | Entity picker, optional view selector (personal views first, separator, then system views), sample record dropdown, and the Available Attributes list (double-click to add). |
| **Field blocks** | Ordered list with drag handles (▲/▼ buttons), delete icons, and inline summary showing attribute name, type, and key properties. |
| **Properties tab** | Shows global settings (target field, max length, tracing, default prefix/suffix/format/timezone) when the entity header is selected; otherwise exposes field type, prefix/suffix, format, truncation, timezone, Default-if-blank button, includeIf button, and behavior summary. |
| **JSON tab** | Read-only `Consolas` rendering of the generated payload. |
| **Live preview** | Displays the assembled string for the selected sample record, reflecting truncation, defaults, and alternates. |
| **Status label** | Shows connection/build/publish feedback (hover for tooltip). |

## Designer Workflows

### Build from scratch

1. Connect to a Dataverse environment with the NameBuilder plug-in installed.
2. (Optional) Select a **Solution** to filter the available entities list.
3. Choose an **Entity** to configure the NameBuilder pattern for.
4. (Optional) Choose a **View** to scope which sample records are loaded (personal views are listed first, followed by a separator, then system views).
5. (Optional) Select a **Sample Record** from the view to see live preview updates.
6. Double-click attributes in the **Available Attributes** list to create field blocks (type auto-detects but can be overridden).
7. Click any field block to edit its properties: prefix/suffix, formats, truncation, timezone.
8. Click **Default if blank** to configure fallback chains (alternates or literal defaults).
9. Click **Add Condition** to gate the block with `includeIf` logic.
10. Validate the preview and **Publish** back to Dataverse, or **Export** the JSON.

### Import JSON

1. Click **Import JSON** and open a schema-compliant file.
2. The designer recreates the blocks, reapplies defaults where values were missing, and updates the preview + JSON tabs.

### Retrieve from Dataverse

1. Click **Retrieve Configured Entity**.
2. Choose a NameBuilder plug-in type and step.
3. The unsecure configuration is parsed and loaded for editing (the ribbon button is redundant if an entity already auto-loaded; see note above).
4. Make edits and publish/export as needed.

### Publish back to Dataverse

1. Click **Publish Configuration**.
2. Select whether to update the Create step, Update step, or both.
3. The tool writes the JSON + filtering attributes and reports the touched steps in the status bar.
4. The selected **Plugin Solution** will be updated with the plugin and the steps.
<img width="1591" height="1038" alt="image" src="https://github.com/user-attachments/assets/24c1242c-0823-43f4-ac07-8341c052c7f6" />

<img width="1275" height="595" alt="image" src="https://github.com/user-attachments/assets/abbead87-396f-494b-8dbc-490a9ad5d9f1" />

## Defaults, Fallbacks, and Conditions

- **Global defaults** – Target field, global max length, tracing, and prefix/suffix/number format/date format/timezone defaults live in the Global Configuration section (shown when the entity header is selected). Editing them updates `%APPDATA%\NameBuilderConfigurator\settings.json` and can cascade to existing blocks that still use the previous value.
- **Default-if-blank dialog** – Pick another attribute from the same entity (or `(None)` for literal-only defaults) plus optional default text. The dialog enforces providing default text when an alternate field is chosen to prevent blank output. Nested alternates are supported (each alternate field can have its own fallback).
- **Behavior summary** – Read-only text below the buttons in the property pane explains the fallback chain ("If [primaryField] is blank → use [alternateField]. If blank → default to \"[text]\".") plus any `includeIf` statements so you can sanity-check complex rules without drilling into raw JSON.
- **Condition dialog** – Toggle between simple field comparisons (e.g., `statuscode equals 1`) and compound `anyOf`/`allOf` groups, leveraging the operator list defined in the NameBuilder schema (`equals`, `notEquals`, `contains`, `startsWith`, `isNull`, `isEmpty`, etc.).

Need deeper schema detail? See [docs/USAGE.md](docs/USAGE.md) or the upstream Docs folder for examples and property references.

## Publishing & Deployment

- The control confirms that the NameBuilder assembly exists when the connection updates. Use **Update NameBuilder Plug-in** to install/repair it.
- **Solution selection**: When installing the plugin or publishing configurations, you'll be prompted to select an unmanaged solution. This ensures all NameBuilder components (assemblies and steps) are organized within your chosen solution for ALM workflows.
- **Duplicate prevention**: The tool automatically detects existing steps for each entity/message combination and updates them rather than creating duplicates. If multiple steps exist, a diagnostic warning is logged.
- **Component management**: Plugin assemblies and SDK message processing steps are automatically added to your selected solution if not already present.
- Publishing synchronizes both the JSON (`configuration`) and `filteringattributes` columns on each selected `sdkmessageprocessingstep`.
- Status updates and tooltips explain each operation (metadata load, publish success, errors, etc.).

## Settings & Persistence

- `%APPDATA%\NameBuilderConfigurator\settings.json` stores splitter positions, preview height, default prefixes/suffixes/formats/timezones, and other UI preferences.
- **Per-connection preferences**: Each connection remembers its selected plugin solution (ID and unique name) using a stable key (connection name + org URL). Legacy keys are migrated and cleaned up automatically. Preferences restore when you reconnect or switch connections without you reselecting the solution.
- Defaults propagate automatically to new blocks and can retroactively update existing ones that matched the prior default.
- The plug-in caches local vs. Dataverse NameBuilder assembly hashes so you can see whether the server version matches your local DLL before publishing.

## Troubleshooting

| Symptom | Resolution |
| --- | --- |
| “NameBuilder plug-in must be installed first.” | Deploy/Register the NameBuilder assembly (use **Update NameBuilder Plug-in**) and reload. |
| Entities fail to load | Verify connection privileges and click **Load Entities** again. |
| Publish button disabled | Ensure an entity is selected, at least one block exists, and the NameBuilder plug-in type is loaded. |
| JSON import errors | Validate the file against the schema documented in the Dataverse-NameBuilder repo. |

## Packaging for the XrmToolBox Store

1. Produce a Release build (`pwsh -File .\build.ps1 -Configuration Release`).
2. Run `pwsh -File .\pack-nuget.ps1` to create `artifacts\nuget\NameBuilderConfigurator.<version>.nupkg`.
3. Submit the package via the [Publishing a plug-in](https://github.com/MscrmTools/XrmToolBox/wiki/Publishing-a-plugin) process (supply metadata, icons, changelog, etc.).

## Support & License

- File issues or feature requests in this repository.
- Need packaging guidance? Review the [XrmToolBox wiki](https://github.com/MscrmTools/XrmToolBox/wiki/Publishing-a-plugin).
- _License_: Add your preferred license text or SPDX identifier here.
