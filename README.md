# Glosslate

Glosslate is a small, cross-platform desktop translation tool for flat JSON localization files. It combines machine translation with glossary protection so names, terminology, product vocabulary, and other fixed terms stay consistent across a translation batch.

It is built with .NET 8 and [Eto.Forms](https://github.com/picoe/Eto), with the native UI backend selected automatically for Windows, Linux, or macOS at build time.

## Why Glosslate?

Machine translation is useful for drafts, but project-specific terminology is easy to translate inconsistently. Glosslate protects glossary entries before a request is sent to the translation provider, then restores the exact preferred translation afterward.

```text
Source:      Meet Tiri at the Old Facility.
Protected:   Meet __G0G__ at the __G1G__.
Translated:  ... __G0G__ ... __G1G__ ...
Restored:    ... Tiri ... Archive Facility ...
```

The placeholder step keeps the translation provider focused on the surrounding sentence while Glosslate retains control over protected terms.

## Features

- Cross-platform desktop UI for Windows, Linux, and macOS.
- Flat JSON import and export (`string` keys with `string` values).
- Glossary protection with whole-word and case-sensitive matching.
- Batch translation for all, missing, or selected entries.
- Editable translation grid for manual review.
- Resumable `.glosslate.json` project files.
- Google Translate free/unofficial provider with no API key required.
- Official DeepL API provider.
- Provider interface (`ITranslationProvider`) for adding more backends.
- Persistent application settings and last-used glossary path.

## Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Linux runtime only: GTK 3 must be installed to launch the desktop UI.

The project file chooses the matching Eto backend automatically:

| Host OS | Eto backend |
| --- | --- |
| Windows | `Eto.Platform.Wpf` |
| Linux | `Eto.Platform.Gtk` |
| macOS | `Eto.Platform.Mac64` |

The selected Eto 2.11 packages target modern .NET versions compatible with .NET 8. The backend can also be overridden with `-p:EtoBackend=Wpf`, `Gtk`, or `Mac64` when needed.

## Build and run

After cloning the repository:

```bash
dotnet restore
dotnet build
dotnet run --project Glosslate.csproj
```

The same commands are intended to work from PowerShell, Command Prompt, Bash, zsh, and CI runners because the build does not rely on shell-specific scripts or hard-coded paths.

### Build a release

```bash
dotnet build Glosslate.csproj -c Release
```

To explicitly select an Eto backend:

```bash
dotnet build Glosslate.csproj -c Release -p:EtoBackend=Gtk
```

Normally this override is unnecessary; the host OS is detected automatically.

## Input format

Glosslate accepts a flat JSON object whose values are strings:

```json
{
  "menu.start": "Start game",
  "npc.tiri.greeting": "My name is Tiri. Nice to meet you."
}
```

Nested objects and arrays are intentionally not interpreted. If a project stores localization in a nested structure, flatten it before importing and reconstruct the original structure after exporting.

Example files are available in [`examples/`](examples/).

## Glossary format

A glossary is a JSON array:

```json
[
  {
    "Term": "Old Facility",
    "Translation": "Archive Facility",
    "CaseSensitive": false,
    "WholeWord": true,
    "Note": "Preferred localized terminology"
  }
]
```

`Term` is the text to protect in the source sentence. `Translation` is the exact text restored after machine translation.

## Translation providers

### Google Translate (free/unofficial)

The default provider uses the unofficial `translate.googleapis.com` endpoint. It is convenient for testing and lightweight use because it does not require an API key, but it is not an official public API and has no guaranteed quota, compatibility, or SLA.

For production workflows, consider implementing an official provider or using the included DeepL integration.

### DeepL

Select DeepL in **Settings -> Translation Settings...** and provide an API key. Glosslate automatically chooses the free or paid DeepL API host based on the key format.

## Project files

**File -> Save Project** stores the source text, current translation, entry status, source language, and target language in a `.glosslate.json` file. These project files can be reopened later without re-running completed translations.

Glosslate can also open the legacy `.trproj.json` extension used by the earlier version of the project.

## Architecture

The application is intentionally small and split into a few straightforward layers:

```text
Models/
  AppSettings.cs
  GlossaryTerm.cs
  TranslationEntry.cs

Services/
  ITranslationProvider.cs
  GoogleFreeTranslationProvider.cs
  DeepLTranslationProvider.cs
  TermProtector.cs
  GlossaryService.cs
  JsonProjectService.cs
  SettingsService.cs

MainForm.cs
GlossaryForm.cs
SettingsForm.cs
Program.cs
```

Translation providers only need to implement:

```csharp
public interface ITranslationProvider
{
    string Name { get; }
    Task<string> TranslateAsync(
        string text,
        string sourceLang,
        string targetLang,
        CancellationToken ct = default);
}
```

This keeps provider-specific HTTP logic separate from glossary handling, project persistence, and UI code.

## Contributing

Contributions are welcome. Useful areas include:

- additional official translation providers;
- nested JSON, YAML, CSV, PO, or XLIFF import/export;
- translation memory and fuzzy matching;
- glossary import/export standards;
- automated tests for term protection and project serialization;
- packaging for platform-native installers.

See [`CONTRIBUTING.md`](CONTRIBUTING.md) for the development workflow and compatibility guidelines. Keep user-facing text and project documentation in English and avoid OS-specific paths or shell-only build requirements unless a feature specifically requires them.

## Security and API keys

DeepL API keys are stored in the current user's application settings directory as plain JSON. Do not use a sensitive production key on a shared or untrusted machine.

## License

Glosslate is released under the Apache-2.0 license. See [`LICENSE`](LICENSE).

## Author

TamKungZ_ — dev@tamkungz.me
