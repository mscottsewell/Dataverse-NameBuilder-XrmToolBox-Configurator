# NameBuilder Configurator - XrmToolBox Plugin

A tool for building JSON configurations for the NameBuilder project directly within XrmToolBox.

## Features

- 🔌 **No Authentication Needed** - Leverages XrmToolBox's existing connection
- 📋 **Entity Selection** - Browse and select from all available entities
- 🏷️ **Attribute Browser** - View all attributes for the selected entity
- ✨ **Visual Pattern Builder** - Click to add attributes to your name pattern
- 📄 **JSON Export** - Generate and export configuration files
- 📋 **Copy to Clipboard** - Quick copy for immediate use

## Installation

### From XrmToolBox Tool Library (Recommended)
1. Open XrmToolBox
2. Go to "Tool Library" (Configuration menu)
3. Search for "NameBuilder Configurator"
4. Click Install

### Manual Installation
1. Build the project in Visual Studio
2. Copy `NameBuilderConfigurator.dll` to your XrmToolBox plugins folder
3. Restart XrmToolBox

## Building from Source

### Prerequisites
- Visual Studio 2019 or later
- .NET Framework 4.6.2 or higher
- XrmToolBox installed (for testing)

### Build Steps

1. Clone the repository
2. Open `NameBuilderConfigurator.csproj` in Visual Studio
3. Restore NuGet packages
4. Build the solution (Ctrl+Shift+B)

The compiled DLL will be in `bin\Release\NameBuilderConfigurator.dll`

### Testing

1. Copy the built DLL to your XrmToolBox plugins folder:
   ```
   %AppData%\MscrmTools\XrmToolBox\Plugins\
   ```
2. Launch XrmToolBox
3. Connect to a Dataverse environment
4. Open "NameBuilder Configurator" from the tools list

## Usage

1. **Connect** - Use XrmToolBox to connect to your Dataverse environment
2. **Load Entities** - Click "Load Entities" to retrieve all available entities
3. **Select Entity** - Choose the entity you want to configure from the dropdown
4. **Build Pattern** - Double-click attributes or use the "Add to Pattern" button to build your name pattern
   - Attributes are added as `{attributename}` placeholders
   - You can add spaces and text between attributes
5. **Review JSON** - The JSON configuration is generated automatically
6. **Export** - Copy to clipboard or export to a file

### Example Pattern
```
{firstname} {lastname}
```

### Generated JSON
```json
{
  "entityLogicalName": "contact",
  "namePattern": "{firstname} {lastname}",
  "generatedBy": "XrmToolBox - NameBuilder Configurator",
  "generatedDate": "2025-12-03T10:30:00Z"
}
```

## Publishing to XrmToolBox Tool Library

To make this plugin available in the XrmToolBox Tool Library:

1. Create a NuGet package (.nupkg)
2. Submit to the XrmToolBox GitHub repository
3. Follow the [XrmToolBox plugin submission guidelines](https://github.com/MscrmTools/XrmToolBox/wiki/Publishing-a-plugin)

## License

[Add your license here]

## Support

For issues or feature requests, please visit the [GitHub repository](https://github.com/yourusername/NameBuilderConfigurator).
