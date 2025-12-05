# NameBuilder Configurator – XrmToolBox Plugin

A WinForms-based XrmToolBox plug-in that builds and publishes NameBuilder JSON configurations directly against a Dataverse environment.

## Documentation

- Detailed usage guide: [docs/USAGE.md](docs/USAGE.md)
- Canonical JSON schema and plug-in internals: [Dataverse-NameBuilder Docs](https://github.com/mscottsewell/Dataverse-NameBuilder/tree/main/Docs)

## Features

- 🔌 **Connection-aware startup** – Reuses the current XrmToolBox connection and validates that the NameBuilder plug-in assembly exists in the org before enabling the UI.
- 📋 **Entity and attribute explorer** – Load entities, browse attributes, and double-click to add any attribute to the configuration.
- ✨ **Visual pattern builder** – Add, reorder, and remove field blocks while seeing a live preview of the generated name string and JSON payload.
- ⚙️ **Reusable defaults** – Store prefixes, suffixes, formats, and timezone preferences in `%APPDATA%\NameBuilderConfigurator\settings.json`; the tool reapplies them when loading configs or adding new fields.
- 📄 **Import / export / publish** – Import existing JSON, export to disk, copy to clipboard, or publish directly back to the NameBuilder plug-in steps (Create/Update) in Dataverse.
- 🧰 **Scripted build pipeline** – `build.ps1` restores packages, increments version numbers, builds, and deploys artifacts to XrmToolBox and the `Ready To Run` folder.

## Installation

### From the XrmToolBox Tool Library (recommended)

1. Open XrmToolBox.
2. Select **Tool Library** from the Configuration menu.
3. Search for **NameBuilder Configurator**.
4. Click **Install** and restart XrmToolBox if prompted.

### Manual installation

1. Build the project in Release mode (instructions below).
2. Copy `NameBuilderConfigurator.dll` and the `Assets` folder to `%APPDATA%\MscrmTools\XrmToolBox\Plugins`.
3. Restart XrmToolBox; the tool will appear in the list.

## Building from source

### Prerequisites

- Visual Studio 2022 (or newer) with the **.NET desktop development** workload.
- .NET Framework 4.8 targeting packs.
- PowerShell 7 or later.
- XrmToolBox installed (for local testing / deployment).

### Build steps

#### Option 1 – Scripted build (recommended)

```pwsh
pwsh -File .\build.ps1 -Configuration Release
```

The script will:

1. Increment the assembly version inside `Properties\AssemblyInfo.cs`.
2. Restore NuGet packages and run MSBuild using your installed Visual Studio toolset.
3. Copy the resulting DLL plus `Assets` into `%APPDATA%\MscrmTools\XrmToolBox\Plugins` (if XrmToolBox exists).
4. Mirror the DLL and assets into the `Ready To Run` folder for manual distribution.

#### Option 2 – Visual Studio

1. Open `NameBuilderConfigurator.sln`.
2. Restore NuGet packages when prompted.
3. Build the **Release** configuration (`Ctrl+Shift+B`).

Build output is written to `bin\Release\NameBuilderConfigurator.dll`.

### Testing inside XrmToolBox

1. Ensure the DLL and `Assets` folder are located under `%APPDATA%\MscrmTools\XrmToolBox\Plugins`.
2. Launch XrmToolBox and connect to a Dataverse organization that already has the **NameBuilder** plug-in installed.
3. Open **NameBuilder Configurator** from the tool list; the plug-in will confirm the NameBuilder assembly is present before enabling the designer surface.

### Packaging for the XrmToolBox store

1. Ensure a Release build has been produced (`pwsh -File .\build.ps1 -Configuration Release`).
1. Run `pwsh -File .\pack-nuget.ps1` (the script parses `AssemblyInfo.cs` for the version, downloads `nuget.exe` if needed, and executes `nuget pack NameBuilderConfigurator.nuspec`).
1. The resulting `.nupkg` lands in `artifacts\nuget`. Upload that package when submitting to the XrmToolBox Tool Library.

## Usage

1. **Connect** – Use the standard XrmToolBox connection wizard.
2. **Load Entities** – Click **Load Entities** to populate the entity dropdown.
3. **Select Entity** – Pick the entity you plan to configure.
4. **Build Pattern** – Double-click attributes (or use the contextual buttons) to add them as field blocks, apply prefixes/suffixes, formats, or truncation rules.
5. **Review Output** – Inspect the live preview string and the JSON payload tab.
6. **Publish / Export** – Publish back to Dataverse steps, export to disk, or copy JSON to the clipboard.

### Example pattern

```text
{firstname} {lastname}
```

### Generated JSON

```json
{
  "entity": "contact",
  "targetField": "name",
  "enableTracing": false,
  "fields": [
    { "field": "firstname", "type": "string" },
    { "field": "lastname", "type": "string", "prefix": " " }
  ]
}
```

## Settings & default behavior

- Preferences such as splitter positions, prefixes, suffixes, date/number formats, and timezone offsets persist in `%APPDATA%\NameBuilderConfigurator\settings.json`.
- When loading an existing configuration, any missing prefix/suffix/format values inherit the stored defaults.
- On startup the tool verifies that the **NameBuilder** plug-in assembly exists in the connected environment; if not, it prompts the user to install the plug-in before continuing.

## Publishing to the XrmToolBox Tool Library

1. Create a NuGet package (`.nupkg`) for the plug-in.
2. Submit it via the XrmToolBox contribution guidelines.
3. Follow the official [Publishing a plug-in](https://github.com/MscrmTools/XrmToolBox/wiki/Publishing-a-plugin) checklist (metadata, icons, descriptions, etc.).

## License

[Add your license information here.]

## Support

- File issues or feature requests in this repository.
- Need help with XrmToolBox packaging? Consult the [official wiki](https://github.com/MscrmTools/XrmToolBox/wiki/Publishing-a-plugin).
