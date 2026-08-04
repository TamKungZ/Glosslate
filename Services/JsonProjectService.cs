using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Glosslate.Models;

namespace Glosslate.Services;

/// <summary>
/// Reads and writes flat JSON localization maps in the form
/// { "key": "text", ... } and Glosslate project files.
/// </summary>
public class JsonProjectService
{
    private static readonly JsonSerializerOptions PrettyOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    };

    public List<TranslationEntry> LoadSourceJson(string path)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(path));

        if (doc.RootElement.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException("Source JSON must be a flat object such as { \"key\": \"text\" }.");

        var entries = new List<TranslationEntry>();
        foreach (var property in doc.RootElement.EnumerateObject())
        {
            if (property.Value.ValueKind != JsonValueKind.String)
            {
                throw new InvalidDataException(
                    $"Key \"{property.Name}\" does not contain a string value. Nested JSON must be flattened before import.");
            }

            entries.Add(new TranslationEntry
            {
                Key = property.Name,
                Original = property.Value.GetString() ?? ""
            });
        }

        return entries;
    }

    public void ExportTranslatedJson(string path, IEnumerable<TranslationEntry> entries)
    {
        var translations = entries.ToDictionary(entry => entry.Key, entry => entry.Translated);
        File.WriteAllText(path, JsonSerializer.Serialize(translations, PrettyOptions));
    }

    public void SaveProject(
        string path,
        IEnumerable<TranslationEntry> entries,
        string sourceLanguage,
        string targetLanguage)
    {
        var project = new ProjectDto
        {
            SourceLang = sourceLanguage,
            TargetLang = targetLanguage,
            Entries = entries.Select(entry => new ProjectEntryDto
            {
                Key = entry.Key,
                Original = entry.Original,
                Translated = entry.Translated,
                Status = entry.Status.ToString()
            }).ToList()
        };

        File.WriteAllText(path, JsonSerializer.Serialize(project, PrettyOptions));
    }

    public (List<TranslationEntry> entries, string sourceLang, string targetLang) LoadProject(string path)
    {
        var project = JsonSerializer.Deserialize<ProjectDto>(File.ReadAllText(path))
                      ?? throw new InvalidDataException("The project file could not be read.");

        var entries = project.Entries.Select(entry => new TranslationEntry
        {
            Key = entry.Key,
            Original = entry.Original,
            Translated = entry.Translated,
            Status = Enum.TryParse<EntryStatus>(entry.Status, out var status)
                ? status
                : EntryStatus.NotTranslated
        }).ToList();

        return (entries, project.SourceLang, project.TargetLang);
    }

    private sealed class ProjectEntryDto
    {
        public string Key { get; set; } = "";
        public string Original { get; set; } = "";
        public string Translated { get; set; } = "";
        public string Status { get; set; } = nameof(EntryStatus.NotTranslated);
    }

    private sealed class ProjectDto
    {
        public string SourceLang { get; set; } = "auto";
        public string TargetLang { get; set; } = "en";
        public List<ProjectEntryDto> Entries { get; set; } = new();
    }
}
