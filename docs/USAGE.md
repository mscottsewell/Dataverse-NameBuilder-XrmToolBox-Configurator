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
3. **Solution Selection for Plugin Installation**: When installing or updating the NameBuilder plugin assembly, you'll be prompted to select an unmanaged solution. Only unmanaged solutions are shown to ensure compatibility with ALM workflows. Your selection is saved per connection.
4. Once validated, the utility caches the located `plugintype` so it can query/update the corresponding `sdkmessageprocessingstep` rows when you publish configuration changes.
5. **Component Organization**: All plugin components (assemblies and SDK message processing steps) are automatically added to your selected solution, enabling proper solution layering and transport between environments.

## 5. UI Tour & Core Concepts

| Area | Description |
| --- | --- |
| **Ribbon** | Top toolbar with buttons: Load Entities (reload metadata), Retrieve Configured Entity (pull from Dataverse), Register Plug-in (verify/deploy), Import JSON, Export JSON, Copy JSON, and Publish Configuration. Hover over each button for a tooltip. |
| **Solution Dropdown** | Optional filter to scope entities to a specific Dataverse solution. Display names are shown; selecting a solution loads only entities in that solution. Choose "(Default)" to see all entities. |
| **Entity Dropdown** | Select the Dataverse entity whose NameBuilder pattern you want to edit. Populates from the filtered solution or all available entities. |
| **View Dropdown** | Optional view selector. Personal views are listed first, then a separator, then system views. The separator is non-selectable. When set, the Sample Record dropdown only shows rows from that view. Leave blank to show all records. |
| **Sample Record Dropdown** | Pick a row to feed the live preview. Records come from the selected view (or all rows if no view is chosen). As you change the sample record, the preview updates in real-time. |
| **Available Attributes List** | Shows all attributes from the selected entity. Double-click any attribute to append it as a field block; logical name shown in parentheses below display name. |
| **Field Blocks Panel** | Center area showing ordered field blocks. Each block displays the attribute name, type, and key properties (prefix/suffix/format if configured). Click a block to edit it; drag ▲/▼ buttons to reorder; click ✕ to delete. |
| **Properties Panel (right)** | Context-sensitive: shows **Global Configuration** (target field, max length, tracing, defaults) when the entity header is selected, or **Field Properties** when a field block is selected. |
| **JSON Tab** | Read-only `Consolas` view of the generated NameBuilder JSON payload. Can be copied to clipboard or exported to disk. |
| **Live Preview Tab** | Shows the assembled name string for the currently selected sample record, reflecting all truncation, defaults, alternates, and field formatting. |
| **Status Label** | Displays progress (e.g., "Loading entities...") or success/error messages. Hover for tooltips with more detail. |

### Global Configuration Section

When you click the entity header block or first load an entity, the right panel switches to **Global Configuration**, showing:

- **Target Field Name** – Logical name of the destination column (defaults to `name`). This becomes `targetField` in the JSON.
- **Global Max Length** – Optional length cap applied to the final assembled string. Set to 0 for unlimited.
- **Enable Tracing** – When checked, the NameBuilder plug-in writes verbose traces to the Dataverse Plug-in Trace Log for debugging.
- **Default Field Properties** (expandable section) – Reusable prefix, suffix, number format, date format, and timezone values that propagate to new blocks and can retroactively update existing blocks that still use the prior default value.

### Field Block Properties

Each block maps directly to the NameBuilder JSON schema. Important options exposed in the property pane when a field block is selected:

- `Type` (auto-detect/string/lookup/date/datetime/optionset/number/currency/boolean).
- `Prefix` / `Suffix` (string).
- `Format` (date/number/currency patterns as defined in the NameBuilder docs).
- `MaxLength` & `TruncationIndicator`.
- `Default if blank` button (opens the fallback dialog so you can chain an alternate attribute and/or a literal default string that only renders when the primary field is empty).
- `TimezoneOffsetHours` (exposed for date/datetime blocks when the upstream plug-in needs to normalize local time).
- `IncludeIf` builder (launches the Condition dialog to author simple comparisons or compound `anyOf` / `allOf` trees).
- Behavior summary panel (read-only text beneath the buttons that explains—in plain English—the fallback chain plus any includeIf logic so you can sanity-check complex rules).

#### Default-if-blank workflow & fallbacks

Click **Default if blank** to open the combined fallback dialog:

1. Pick another attribute from the entity (or leave the field set to `(None)` if you only want a literal default).
2. Provide the string that should appear once every upstream attribute in the chain is empty. The dialog enforces entering a default when you pick an alternate field so Dataverse never shows a blank placeholder unexpectedly.
3. Save to push either of these outcomes: a nested `alternateField` element (when you selected another attribute, optionally with its own default), or a top-level `default` value (when you only supplied literal text).

The property pane now explains the evaluation order via the behavior summary box directly under the buttons (e.g., “If blank -> use prioritycode. If blank -> default to "Hot". Condition: prioritycode equals "1"”). Use this read-only text to verify the final behavior without drilling into the raw JSON.

## 6. Typical Workflows

### 6.1 Build a configuration from scratch

1. **Connect** via XrmToolBox connection wizard.
2. **Load Entities** by clicking the ribbon button (metadata and views load automatically). This fetches all solutions, entities, views, and sample records from Dataverse.
3. (Optional) **Filter by Solution** using the Solution dropdown to narrow the entity list.
4. **Choose an Entity** from the Entity dropdown to begin configuring its NameBuilder pattern. The tool automatically fetches the published configuration for that entity (prefers the Update step, falls back to the Create step) and reports if none is found.
5. (Optional) **Choose a View** to restrict which sample records appear in the Sample Record dropdown. Personal views are listed first; a non-selectable separator precedes the system views.
6. (Optional) **Pick a Sample Record** to seed the live preview with real data as you build.
7. **Add Field Blocks** by double-clicking attributes in the **Available Attributes** list. Field type auto-detects based on metadata but can be overridden in properties.
8. (Optional) **Adjust Global Properties**: Click the entity header block to show **Global Configuration**, then set target field, max length, enable tracing, and default prefix/suffix/formats.
9. **Edit each field block**: Click the block to open its properties, set prefix/suffix/format/max length/truncation indicator, set timezone offset for date/datetime fields, use **Default if blank** for fallbacks, **Add Condition** for include-if logic, and acknowledge the confirmation prompt if you try to switch entities with unsaved edits.
10. **Monitor Live Preview** as you build to ensure output matches expectations.
11. **Validate JSON** by switching to the JSON tab to inspect the schema (copy/export if needed for offline review).
12. **Publish** back to Dataverse or **Export/Copy** the JSON for later use.

**Solution Selection for Publishing**: When you click **Publish Configuration** for the first time (or after the plugin is installed), you'll be prompted to select an unmanaged solution where the NameBuilder steps will be registered. This selection is remembered per connection for future publishes. The tool ensures only one Create step and one Update step exist per entity, updating existing steps rather than creating duplicates.

### 6.2 Import existing JSON

1. Click **Import JSON**.
2. Choose a `.json` file that uses the canonical schema described in the [Dataverse-NameBuilder Docs](https://github.com/mscottsewell/Dataverse-NameBuilder/tree/main/Docs).
3. The designer recreates the field blocks, reapplies defaults where missing, and updates the preview/JSON panes.

### 6.3 Retrieve configuration from Dataverse steps

1. Click **Retrieve Configured Entity** on the ribbon.
2. A dialog appears listing all NameBuilder plug-in types and their registered **Create** and **Update** steps for each entity.
3. Select a specific step containing the configuration you want to edit.
4. The tool queries the step's unsecure configuration (JSON), parses it, and populates the designer with all field blocks and global settings. This is useful when you want to load a different step than the auto-load picked.
5. Edit as needed and publish back (see 6.4) or export for backup.

### 6.4 Publish back to Dataverse

1. Ensure an entity is selected and at least one field block exists.
2. Click **Publish Configuration** on the ribbon.
3. A dialog appears asking which steps to update: **Create step**, **Update step**, or **both**.
4. Select the desired steps and confirm. The tool:
   - Bundles the JSON and calculates the attribute filter list based on all referenced fields.
   - Updates (or creates if missing) the corresponding `sdkmessageprocessingstep` rows in Dataverse.
   - Sets both the `configuration` (JSON) and `filteringattributes` columns.
5. Status bar displays success or errors (e.g., "Published to: contact - Create, contact - Update").

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

The tool persists user preferences and defaults at `%APPDATA%\NameBuilderConfigurator\settings.json`. Properties include:

- **Splitter positions** – window layout (left/right and top/bottom split pane positions).
- **Preview height** – height of the live preview area.
- **Default Prefix** – prepended to new string fields; propagates to existing blocks still using the prior default.
- **Default Suffix** – appended to new string fields; propagates similarly.
- **Default Number Format** – applied to new number/currency fields (e.g., `#,##0.00` or `0.0K`).
- **Default Date Format** – applied to new date/datetime fields (e.g., `yyyy-MM-dd` or `MM/dd/yyyy`).
- **Default Timezone Offset** – applied to new date/datetime fields for UTC conversion.

**Per-connection solution preference**: The selected unmanaged solution for plugin installation/publishing is remembered per connection using a stable key (connection name + org URL); legacy keys are migrated and cleaned up automatically when you reconnect.

**Propagation Behavior**: When you change a default value (e.g., suffix), the tool automatically updates any existing field blocks that still match the _previous_ default. This makes bulk edits painless: change the suffix once and watch all blocks that still used it update automatically.

Defaults are applied automatically when new blocks are created but can also be manually propagated via a button in the Global Configuration panel.

## 9. Troubleshooting & Tips

| Symptom | Resolution |
| --- | --- |
| "NameBuilder plug-in must be installed first" dialog | Click **Register Plug-in** on the ribbon, browse to `Assets\DataversePlugin\NameBuilder.dll`, and confirm the deployment. The tool will deploy the assembly and create necessary plug-in types. Retry after deployment. |
| Entities fail to load | Verify your XrmToolBox connection has Customizer/System Administrator privileges. Click **Load Entities** again to retry. |
| Sample records don't appear | Ensure a View is selected (if required) and that the view contains at least one row. Change the Sample Record dropdown to refresh the list. |
| Publish button is disabled | Ensure: (1) an entity is selected, (2) at least one field block exists, (3) the NameBuilder plug-in is registered (use **Register Plug-in**). |
| JSON import errors | Validate the file against the schema in the [Dataverse-NameBuilder Docs](https://github.com/mscottsewell/Dataverse-NameBuilder/tree/main/Docs) folder. The configurator expects exact property names and casing (e.g., `targetField`, not `targetfield`). |
| Publish fails | Check that: (1) you have Create/Update step registrations for the entity, (2) your connection has privileges to update `sdkmessageprocessingstep` rows, (3) the target field exists on the entity. Enable tracing in Global Configuration to capture plug-in-side diagnostics. |
| Multiple steps exist for the same entity | The tool will detect and update the first existing step, logging a diagnostic warning. Review your environment for duplicate NameBuilder steps and remove extras manually via the Plugin Registration Tool. |
| Solution selection not appearing | Verify at least one unmanaged solution exists in your environment. Managed solutions cannot contain new plugin registrations and are filtered from the selection dialog. |
| Truncation not working as expected | Verify `maxLength` is set on the global config or individual field, and `truncationIndicator` (default `...`) is configured. Test with a sample record that exceeds the limit. |
| Timezone offset not applied | Verify the field type is `date` or `datetime` and `timezoneOffsetHours` is set on the field (not 0). Timezone adjustments only apply to date/time types. |
| Missing attributes in the Available Attributes list | Click **Load Entities** to refresh metadata. Some attributes may be hidden or marked as non-searchable in metadata; these still appear in the list but may not be queryable. |

## 10. Additional Resources

- [Dataverse-NameBuilder repository](https://github.com/mscottsewell/Dataverse-NameBuilder) – server-side plug-in source & Docs.
- [Dataverse-NameBuilder Docs folder](https://github.com/mscottsewell/Dataverse-NameBuilder/tree/main/Docs) – JSON schema, deployment instructions, advanced formatting rules.
- [XrmToolBox wiki – Publishing a plug-in](https://github.com/MscrmTools/XrmToolBox/wiki/Publishing-a-plugin) – guidance for distributing this configurator once you customize it.
