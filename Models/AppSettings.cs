namespace Glosslate.Models;

public enum ProviderKind
{
    GoogleFree,
    DeepL
}

public class AppSettings
{
    public ProviderKind Provider { get; set; } = ProviderKind.GoogleFree;
    public string DeepLApiKey { get; set; } = "";
    public string SourceLang { get; set; } = "auto";
    public string TargetLang { get; set; } = "en";

    /// <summary>Delay between translation requests in milliseconds.</summary>
    public int RequestDelayMs { get; set; } = 350;

    public string LastGlossaryPath { get; set; } = "";
}
