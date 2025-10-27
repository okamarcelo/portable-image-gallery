# Internationalization (i18n) Guide

This document explains how the application supports multiple languages and how to add new translations.

## Overview

The application uses .NET's built-in resource file system (`.resx` files) for internationalization. All user-facing strings are stored in resource files and referenced throughout the code.

## Project Structure

```
src/ImageGallery/
  Resources/
    Strings.resx              # Default language (English)
    Strings.Designer.cs       # Auto-generated code for type-safe access
    Strings.pt-BR.resx        # Example: Portuguese (Brazil) translation
    Strings.es.resx           # Example: Spanish translation
```

## Current Language Support

- **English (en)** - Default language, defined in `Strings.resx`

## How It Works

### 1. Resource Files

The main resource file is `Resources/Strings.resx`, which contains:
- Key-value pairs for all translatable strings
- Comments explaining placeholders (e.g., `{0}`, `{1}`)
- The default language (English)

### 2. Usage in Code

**In C# files:**
```csharp
using ImageGallery.Resources;

// Simple string
string title = Strings.MainWindow_Title;

// String with formatting
string message = string.Format(Strings.Error_LoadMessage, ex.Message);
```

**In XAML files:**
```xml
xmlns:resx="clr-namespace:ImageGallery.Resources"

<!-- Simple binding -->
<TextBlock Text="{x:Static resx:Strings.InputDialog_Prompt}" />

<!-- In window title -->
<Window Title="{x:Static resx:Strings.MainWindow_Title}" />
```

## Adding a New Language

### Step 1: Create Language-Specific Resource File

1. Copy `Strings.resx` to `Strings.[culture].resx`
   - For Spanish: `Strings.es.resx`
   - For Portuguese (Brazil): `Strings.pt-BR.resx`
   - For French: `Strings.fr.resx`
   - For German: `Strings.de.resx`

2. **Important:** Ensure the file is saved with **UTF-8 encoding with BOM**
   - In Visual Studio: File ? Advanced Save Options ? UTF-8 with signature
   - In VS Code: Click encoding in status bar ? Save with Encoding ? UTF-8 with BOM

3. Open the new file and translate all `<value>` elements

**Example: Strings.pt-BR.resx**
```xml
<data name="MainWindow_Title" xml:space="preserve">
  <value>Galeria de Imagens</value>
</data>
<data name="Loading_Images" xml:space="preserve">
  <value>Carregando imagens...</value>
</data>
<data name="Button_OK" xml:space="preserve">
  <value>OK</value>
</data>
<data name="Button_Cancel" xml:space="preserve">
  <value>Cancelar</value>
</data>
```

### Step 2: Preserve Format Placeholders

When translating strings with placeholders, **keep the placeholders intact**:

```xml
<!-- English -->
<data name="Progress_CurrentTotal" xml:space="preserve">
  <value>{0} / {1}</value>
  <comment>{0} is current, {1} is total</comment>
</data>

<!-- Portuguese -->
<data name="Progress_CurrentTotal" xml:space="preserve">
  <value>{0} / {1}</value>
  <comment>{0} é atual, {1} é total</comment>
</data>

<!-- Spanish -->
<data name="Error_LoadMessage" xml:space="preserve">
  <value>Error al cargar la aplicación:\n{0}</value>
  <comment>{0} es el mensaje de error</comment>
</data>
```

### Step 3: Testing the Translation

The application automatically selects the language based on the system's culture settings:

**Method 1: Change Windows Language**
1. Go to Settings ? Time & Language ? Language
2. Set your preferred language
3. Restart the application

**Method 2: Test Programmatically**

Add this code to `App.xaml.cs` before initializing components:

```csharp
// In App.xaml.cs, OnStartup method
protected override void OnStartup(StartupEventArgs e)
{
    // Force a specific culture for testing
    var culture = new System.Globalization.CultureInfo("pt-BR");
    System.Threading.Thread.CurrentThread.CurrentCulture = culture;
    System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
    
    base.OnStartup(e);
    // ... rest of the code
}
```

### Step 4: Build and Test

```bash
cd src/ImageGallery
dotnet build
dotnet run
```

## Resource Key Categories

The resource keys are organized by category:

### Window Titles
- `MainWindow_Title` - Main window title
- `InputDialog_Title` - Input dialog title

### Loading Messages
- `Loading_Images` - "Loading images..."
- `Loading_ImagesFrom` - "Loading images from {0}..."
- `Importing_Files` - "Importing files..."

### Progress Messages
- `Progress_CurrentTotal` - "{0} / {1}"
- `Progress_CurrentTotalErrors` - "{0} / {1} ({2} errors)"

### Error Messages
- `Error_NoImages` - No images found message
- `Error_InitializationTitle` - Error dialog title
- `Error_InitializationMessage` - Error dialog message
- `Error_LoadTitle` - Load error title
- `Error_LoadMessage` - Load error message

### UI Elements
- `Button_OK` - OK button text
- `Button_Cancel` - Cancel button text
- `InputDialog_Prompt` - Input dialog prompt
- `InputDialog_Help` - Input dialog help text

### Status Messages
- `Status_Paused` - "Paused"
- `Status_Resumed` - "Resumed"
- `Status_SlideshowStarted` - "Slideshow started"
- `Status_SlideshowStopped` - "Slideshow stopped"

### Log Messages
- `Log_Zoom` - "Zoom: {0}%"
- `Log_SlideshowSpeed` - "Slideshow speed: {0:0.0}s"
- `Log_MosaicMode` - "Mosaic mode: {0} pane{1}"

### Structured Log Messages (Serilog)
- `SLog_*` - All structured logging messages for debugging

## Best Practices

### 1. Don't Hardcode Strings
? **Bad:**
```csharp
MessageBox.Show("Error loading images", "Error");
```

? **Good:**
```csharp
MessageBox.Show(Strings.Error_LoadImagesMessage, Strings.Error_LoadTitle);
```

### 2. Use Format Strings for Dynamic Content
? **Bad:**
```csharp
text = current + " / " + total;
```

? **Good:**
```csharp
text = string.Format(Strings.Progress_CurrentTotal, current, total);
```

### 3. Keep Resource Keys Descriptive
Use clear, hierarchical naming:
- `Category_Description` format
- Examples: `Error_LoadTitle`, `Button_OK`, `Status_Paused`

### 4. Document Placeholders
Always add comments explaining what placeholders represent:

```xml
<data name="SLog_ImageLoadingCompleted" xml:space="preserve">
  <value>Image loading completed. Found {0} images</value>
  <comment>{0} is image count</comment>
</data>
```

### 5. Test All Translations
- Verify UI layout with longer translations (German, Portuguese)
- Check that placeholders work correctly
- Test dialogs and error messages

## Language-Specific Considerations

### Text Length
Some languages are more verbose:
- German is typically 30% longer than English
- French is typically 15-20% longer
- Ensure UI elements can accommodate longer text

### Right-to-Left Languages
For RTL languages (Arabic, Hebrew), additional work is needed:
1. Set `FlowDirection="RightToLeft"` on the main window
2. Mirror layouts appropriately
3. Test all UI elements

### Pluralization
Handle plurals carefully:

```csharp
// Current approach
string plural = count > 1 ? "s" : "";
string message = string.Format(Strings.Log_MosaicMode, count, plural);

// For languages with complex plural rules, consider separate keys:
// Strings.MosaicMode_One, Strings.MosaicMode_Few, Strings.MosaicMode_Many
```

## Fallback Behavior

If a translation is missing:
1. The application falls back to the default language (English)
2. No error is thrown
3. The key from `Strings.resx` is used

## Tools for Translation

### Visual Studio
- Built-in resource file editor
- Shows keys and values side-by-side
- Validates XML syntax

### ResX Resource Manager (Recommended)
- Free Visual Studio extension
- Manage multiple language files easily
- Shows missing translations
- Export/import for translators

### Manual Editing
You can edit `.resx` files directly in a text editor, but be careful:
- Maintain proper XML structure
- Don't change resource keys
- Keep placeholders (`{0}`, `{1}`) intact

## Example Translation Workflow

1. **Identify missing languages:**
   ```bash
   # Check what languages exist
   ls src/ImageGallery/Resources/Strings.*.resx
   ```

2. **Create new language file:**
   ```bash
   # Copy English to new language
   cp src/ImageGallery/Resources/Strings.resx src/ImageGallery/Resources/Strings.es.resx
   ```

3. **Translate all values:**
   - Open `Strings.es.resx` in Visual Studio or text editor
   - Translate each `<value>` element
   - Keep `<data name="...">` keys unchanged

4. **Build and test:**
   ```bash
   dotnet build
   dotnet run
   ```

5. **Verify all strings appear correctly**

## Future Enhancements

Potential improvements for i18n support:

1. **CLI parameter for language override:**
   ```bash
   ImageGallery.exe --lang pt-BR
   ```

2. **In-app language switcher:**
   - Settings menu to change language
   - No restart required

3. **Crowdin/Transifex integration:**
   - Community translations
   - Automated sync with translation platforms

4. **Date/time formatting:**
   - Respect culture-specific formats
   - Already handled by `{DateTime:HH:mm:ss}`

5. **Number formatting:**
   - Decimal separators (`,` vs `.`)
   - Already handled by string.Format with culture

## Contributing Translations

To contribute a new translation:

1. Fork the repository
2. Create `Strings.[culture].resx` file
3. Translate all strings
4. Test the translation
5. Submit a pull request

Include in your PR:
- Language/culture code (e.g., `pt-BR`, `es`, `fr`)
- Completion percentage
- Any layout issues discovered
- Screenshots of the translated UI

---

**Note:** This application fully supports internationalization. All user-facing strings are externalized and ready for translation into any language.
