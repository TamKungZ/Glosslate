using System.Text.Json;

namespace Glosslate.Services;

/// <summary>
/// Uses Google's unofficial translate.googleapis.com endpoint. It requires no API key,
/// but it has no supported SLA or documented quota and may change without notice.
/// </summary>
public class GoogleFreeTranslationProvider : ITranslationProvider
{
    public string Name => "Google Translate (Free/Unofficial)";

    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromSeconds(20)
    };

    public async Task<string> TranslateAsync(
        string text,
        string sourceLang,
        string targetLang,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        var url = "https://translate.googleapis.com/translate_a/single" +
                  $"?client=gtx&sl={Uri.EscapeDataString(sourceLang)}&tl={Uri.EscapeDataString(targetLang)}" +
                  $"&dt=t&q={Uri.EscapeDataString(text)}";

        using var response = await Http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync(ct);

        using var document = JsonDocument.Parse(body);
        var builder = new System.Text.StringBuilder();
        foreach (var segment in document.RootElement[0].EnumerateArray())
        {
            var piece = segment[0].GetString();
            if (piece is not null)
                builder.Append(piece);
        }

        return builder.ToString();
    }
}
