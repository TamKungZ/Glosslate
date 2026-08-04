using System.Text.Json;

namespace Glosslate.Services;

/// <summary>Translation provider backed by the official DeepL API.</summary>
public class DeepLTranslationProvider : ITranslationProvider
{
    public string Name => "DeepL";

    private readonly string _apiKey;
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(20) };

    public DeepLTranslationProvider(string apiKey)
    {
        _apiKey = apiKey;
    }

    public async Task<string> TranslateAsync(
        string text,
        string sourceLang,
        string targetLang,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        if (string.IsNullOrWhiteSpace(_apiKey))
            throw new InvalidOperationException("A DeepL API key is required. Add it under Translation Settings.");

        var isFreeAccount = _apiKey.TrimEnd().EndsWith(":fx", StringComparison.OrdinalIgnoreCase);
        var host = isFreeAccount ? "api-free.deepl.com" : "api.deepl.com";

        using var request = new HttpRequestMessage(HttpMethod.Post, $"https://{host}/v2/translate");
        request.Headers.Add("Authorization", $"DeepL-Auth-Key {_apiKey}");

        var form = new List<KeyValuePair<string, string>>
        {
            new("text", text),
            new("target_lang", targetLang.ToUpperInvariant())
        };

        if (!string.IsNullOrWhiteSpace(sourceLang) &&
            !sourceLang.Equals("auto", StringComparison.OrdinalIgnoreCase))
        {
            form.Add(new("source_lang", sourceLang.ToUpperInvariant()));
        }

        request.Content = new FormUrlEncodedContent(form);

        using var response = await Http.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"DeepL returned HTTP {(int)response.StatusCode}: {body}");

        using var document = JsonDocument.Parse(body);
        return document.RootElement.GetProperty("translations")[0].GetProperty("text").GetString() ?? "";
    }
}
