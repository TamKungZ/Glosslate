# Contributing to Glosslate

Thanks for considering a contribution.

## Development setup

1. Install the .NET 8 SDK.
2. Clone the repository.
3. Run `dotnet restore`.
4. Run `dotnet build`.
5. Run `dotnet run --project Glosslate.csproj`.

Glosslate selects the Eto.Forms backend from the host operating system automatically. Avoid adding machine-specific absolute paths, generated `bin/` or `obj/` files, or build steps that only work in one shell.

## Pull requests

Keep pull requests focused and explain the behavior being changed. User-facing text, source comments, documentation, commit-ready examples, and new settings should be written in English.

For translation providers, keep HTTP/API-specific code behind `ITranslationProvider` so the UI and glossary pipeline remain provider-independent.

## Build compatibility

Changes should continue to build on Windows, Linux, and macOS. The GitHub Actions build matrix is the baseline compatibility check for pull requests.
