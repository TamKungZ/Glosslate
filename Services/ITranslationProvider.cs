namespace Glosslate.Services;

public interface ITranslationProvider
{
    string Name { get; }
    Task<string> TranslateAsync(string text, string sourceLang, string targetLang, CancellationToken ct = default);
}
