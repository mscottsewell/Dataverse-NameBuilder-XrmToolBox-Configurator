# NameBuilder Configurator – Full Documentation

> This utility produces JSON payloads that power the **NameBuilder** Dataverse plug-in. For canonical schema definitions and in-depth plug-in behavior, see the official [Dataverse-NameBuilder docs](https://github.com/mscottsewell/Dataverse-NameBuilder/tree/main/Docs), especially the configuration and deployment guides contained there. The sections below explain how the XrmToolBox utility wraps that functionality into a visual designer.

## 1. Solution Overview

| Component | Purpose |
| --- | --- |
| NameBuilder plug-in | Dataverse server-side plug-in that assembles primary name strings during Create/Update operations. Lives in the target environment. |
| NameBuilder Configurator (this repo) | XrmToolBox plug-in + WinForms designer used to build JSON payloads that the NameBuilder plug-in consumes. |
| JSON configuration | Schema understood by NameBuilder (see the upstream Docs folder). Contains global metadata plus an ordered collection of field blocks, conditional rules, and formatting directives. |

The configurator connects through XrmToolBox, interrogates both entity metadata _and_ existing NameBuilder steps, and keeps the JSON in sync with Dataverse registrations.

## 2. Dependencies & Prerequisites

1. **Dataverse environment** with the [NameBuilder plug-in](https://github.com/mscottsewell/Dataverse-NameBuilder) deployed. The configurator will refuse to load if the plug-in assembly is missing.
2. **XrmToolBox** (desktop) with an active connection to the target organization.
3. **.NET 4.8 desktop workload** (Visual Studio 2022+) or the `build.ps1` PowerShell script if you are building locally.
4. Optional: access to the upstream Docs folder for the canonical spec (for example, `Docs/NameBuilder-Configuration.md` in the Dataverse-NameBuilder repo) when authoring complex JSON manually.

## 3. Installation & Build Paths

### 3.1 Install through the XrmToolBox Tool Library (recommended)

```text
Configuration ➜ Tool Library ➜ search “NameBuilder Configurator” ➜ Install
```

Restart XrmToolBox and the plug-in appears in the tool list.

### 3.2 Manual deployment (development builds)

1. Clone this repository.
2. Run `pwsh -File .\build.ps1 -Configuration Release`.
   - Script increments `Properties/AssemblyInfo.cs`, restores NuGet packages, runs MSBuild, deploys the DLL + `Assets` into `%APPDATA%\MscrmTools\XrmToolBox\Plugins`, and mirrors everything into `Ready To Run/`.
3. Launch XrmToolBox and open **NameBuilder Configurator**.

> VS workflow: open `NameBuilderConfigurator.sln`, restore packages, build Release, then copy `bin\Release\NameBuilderConfigurator.dll` plus the `Assets` folder into `%APPDATA%\MscrmTools\XrmToolBox\Plugins`.

## 4. Connection Lifecycle & Plug-in Verification

1. Use the XrmToolBox connection wizard as normal.
2. When the control loads, it automatically runs a WorkAsync job that queries `pluginassembly` for "NameBuilder". If the plug-in (or its `plugintype` entries) is missing, the tool surfaces a blocking dialog instructing you to install the server component first.
3. Once validated, the utility caches the located `plugintype` so it can query/update the corresponding `sdkmessageprocessingstep` rows when you publish configuration changes.

## 5. UI Tour & Core Concepts

| Area | Description |
| --- | --- |
| Ribbon | Buttons for Load Entities, Retrieve Configuration (step picker), Import JSON, Export JSON, Copy JSON, Publish Configuration. |
| Left panel | Entity dropdown, view picker, sample record picker, attribute list. Double-click attributes to add field blocks. |
| Center panel | Field block surface. Each block encapsulates a `FieldConfiguration`. Drag handles allow reordering; delete, move up/down, and edit icons provide block management. |
| Right panel | Split into Preview (text), JSON tab, and a property pane that switches between “Global Configuration” and the selected block. |
| Status bar | Shows success/error text, especially after publishing or entity loads. |

### Global configuration tab

- **Target Field** – writes `targetField` in the JSON.
- **Global Max Length** – sets root `maxLength` (0 = unlimited).
- **Enable Tracing** – toggles `enableTracing` in the JSON.
- **Default field properties** – prefix/suffix/number format/date format/timezone offset values persisted at `%APPDATA%\NameBuilderConfigurator\settings.json`. When you change them you can push the new defaults into existing blocks that still match the previous default.

### Field block properties

Each block maps directly to the NameBuilder JSON schema documented upstream. Important options:

- `Type` (auto-detect/string/lookup/date/datetime/optionset/number/currency/boolean).
- `Prefix` / `Suffix` (string).
- `Format` (date/number/currency patterns as defined in the NameBuilder docs).
- `MaxLength` & `TruncationIndicator`.
- `Default` value (displayed if the referenced attribute is null/empty).
- `TimezoneOffsetHours` (exposed for date/datetime blocks when the upstream plug-in needs to normalize local time).
- `IncludeIf` builder (launches the Condition dialog to author simple comparisons or compound `anyOf` / `allOf` trees).
- `AlternateField` builder (launches the Alternate Field dialog to supply fallback logic if the primary attribute is empty).

## 6. Typical Workflows

### 6.1 Build a configuration from scratch

1. Connect and **Load Entities**.
2. Choose an entity ➜ the tool loads metadata and populates the attribute list.
3. Double-click attributes (or drag from the list) to add blocks. The configurator infers types from the metadata but you can override them.
4. Use the property pane to add prefixes (“INV-”), suffixes, date / number formats, etc.
5. Watch the Live Preview to ensure the rendered name string looks correct.
6. Switch to the JSON tab to inspect the payload (it matches the schema described in the upstream Docs folder).
7. Export, copy, or publish the JSON (see section 8).

### 6.2 Import existing JSON

1. Click **Import JSON**.
2. Choose a `.json` file that uses the canonical schema described in the [Dataverse-NameBuilder Docs](https://github.com/mscottsewell/Dataverse-NameBuilder/tree/main/Docs).
3. The designer recreates the field blocks, reapplies defaults where missing, and updates the preview/JSON panes.

### 6.3 Pull configuration from Dataverse steps

1. Click **Retrieve Configuration**.
2. The tool locates plug-in types under the NameBuilder assembly and enumerates their `sdkmessageprocessingstep` rows.
3. Choose a step from the dialog. The unsecure configuration (JSON) is parsed and applied to the designer.
4. Make edits and publish back (see below).

### 6.4 Publish back to Dataverse

1. Click **Publish Configuration**.
2. The tool bundles the JSON + attribute filter list into a `PublishContext` and ensures the appropriate Create/Update steps exist for the selected entity (reusing the same logic as described in the upstream plugin documentation’s deployment guide).
3. Updated JSON and `filteringattributes` values are applied to each step’s `configuration` column.
4. Status bar displays the steps touched (e.g., `Published to: contact - Create, contact - Update`).

## 7. JSON Schema Reference (high-level)

This section mirrors the canonical schema in the Dataverse-NameBuilder Docs folder. Use that repo for authoritative details—especially for operator lists, supported formats, and conditional syntax.

### 7.1 Root object (`PluginConfiguration`)

| Property | Type | Description |
| --- | --- | --- |
| `entity` | string | Logical name of the Dataverse entity (e.g., `account`). Required when saving/publishing. |
| `targetField` | string | Column logical name that receives the generated text (defaults to `name`). |
| `maxLength` | int? | Optional global limit; `null` = unlimited (NameBuilder enforces per-field truncation as well). |
| `fields` | `FieldConfiguration[]` | Ordered pipeline used during name generation. |
| `enableTracing` | bool? | Emits trace log entries when the plug-in runs (useful for troubleshooting). |

### 7.2 Field configuration (`FieldConfiguration`)

| Property | Type | Notes |
| --- | --- | --- |
| `field` | string | Attribute logical name, e.g., `subject`. |
| `type` | string | `auto-detect`, `string`, `lookup`, `date`, `datetime`, `optionset`, `number`, `currency`, or `boolean`. Exact semantics are in the upstream Docs. |
| `format` | string | Date/number format patterns as described in the [formatting spec](https://github.com/mscottsewell/Dataverse-NameBuilder/tree/main/Docs). |
| `maxLength` | int? | Field-level truncation. |
| `truncationIndicator` | string | Suffix appended when truncation occurs (default `...`). |
| `default` | string | Substitute when the field is null/empty. |
| `alternateField` | `FieldConfiguration` | Nested fallback block that renders only when the main `field` is empty. |
| `prefix` / `suffix` | string | Static characters appended before/after the rendered value. |
| `includeIf` | `FieldCondition` | Conditional gate (see below). |
| `timezoneOffsetHours` | int? | Used for date/time adjustments; matches the NameBuilder plug-in’s expectations. |

### 7.3 Conditional expressions (`FieldCondition`)

- **Simple comparison**: `{ "field": "statecode", "operator": "equals", "value": "1" }`.
- **Compound OR (`anyOf`) / AND (`allOf`)**: Provide arrays of nested conditions.
- **Operators**: follows the upstream spec (`equals`, `notEquals`, `contains`, `startsWith`, `isNull`, etc.). See the Docs folder: `Docs/Conditions.md` (or equivalent) for the authoritative list.

### 7.4 Sample payload

```json
{
  "entity": "contact",
  "targetField": "name",
  "enableTracing": false,
  "maxLength": 100,
  "fields": [
    {
      "field": "firstname",
      "type": "string",
      "suffix": " "
    },
    {
      "field": "lastname",
      "type": "string",
      "default": "(no last name)",
      "maxLength": 50,
      "truncationIndicator": "…"
    },
    {
      "field": "preferredcontactmethodcode",
      "type": "optionset",
      "prefix": " [",
      "suffix": "]",
      "includeIf": {
        "field": "preferredcontactmethodcode",
        "operator": "isNotNull"
      }
    }
  ]
}
```

> For more samples, review the `Docs` folder in the upstream repository (`Docs/Samples/*.json`).

## 8. Settings, Defaults, and Local Storage

- Stored at `%APPDATA%\NameBuilderConfigurator\settings.json`.
- Properties include splitter positions, preview height, and all default prefix/suffix/format values.
- Defaults propagate when you add new fields **and** when existing fields still match the previous default (so changing the default suffix can update multiple blocks automatically).

## 9. Troubleshooting & Tips

| Symptom | Resolution |
| --- | --- |
| “NameBuilder plug-in must be installed first” dialog | Deploy the NameBuilder assembly/steps described in the [Dataverse-NameBuilder README](https://github.com/mscottsewell/Dataverse-NameBuilder) and retry. |
| Version conflicts during build | Ensure `MscrmTools.Xrm.Connection` and `XrmToolBoxPackage` NuGet packages are on the versions referenced in the `.csproj`. Run `dotnet restore` after editing. |
| JSON validation errors while importing | Validate the file against the schema documented in the upstream Docs folder; the configurator expects identical casing/property names. |
| Publish failures | Check that you have Create/Update steps registered for the entity and that your XrmToolBox connection has privileges to update `sdkmessageprocessingstep`. Enable tracing in the JSON to capture plug-in-side diagnostics. |

## 10. Additional Resources

- [Dataverse-NameBuilder repository](https://github.com/mscottsewell/Dataverse-NameBuilder) – server-side plug-in source & Docs.
- [Dataverse-NameBuilder Docs folder](https://github.com/mscottsewell/Dataverse-NameBuilder/tree/main/Docs) – JSON schema, deployment instructions, advanced formatting rules.
- [XrmToolBox wiki – Publishing a plug-in](https://github.com/MscrmTools/XrmToolBox/wiki/Publishing-a-plugin) – guidance for distributing this configurator once you customize it.
