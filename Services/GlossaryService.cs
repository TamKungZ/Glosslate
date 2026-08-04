using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Glosslate.Models;

namespace Glosslate.Services;

public class GlossaryService
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    };

    public List<GlossaryTerm> Load(string path)
    {
        if (!File.Exists(path)) return new List<GlossaryTerm>();
        string json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<List<GlossaryTerm>>(json) ?? new List<GlossaryTerm>();
    }

    public void Save(string path, IEnumerable<GlossaryTerm> terms)
    {
        File.WriteAllText(path, JsonSerializer.Serialize(terms.ToList(), Options));
    }
}
