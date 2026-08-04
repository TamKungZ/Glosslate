using System.Text.RegularExpressions;
using Glosslate.Models;

namespace Glosslate.Services;

public class ProtectedTermUsage
{
    public string Placeholder { get; set; } = "";
    public string MatchedText { get; set; } = "";
    public string Translation { get; set; } = "";
}

public class ProtectResult
{
    public string ProtectedText { get; set; } = "";
    public List<ProtectedTermUsage> Usages { get; set; } = new();
}

/// <summary>
/// Replaces glossary terms with stable placeholders before translation and restores
/// their fixed translations afterward. Longer terms are matched first to avoid a
/// shorter glossary entry consuming part of a longer one.
/// </summary>
public class TermProtector
{
    private const string PlaceholderPrefix = "__G";
    private const string PlaceholderSuffix = "G__";

    public ProtectResult Protect(string text, IEnumerable<GlossaryTerm> glossary)
    {
        var result = new ProtectResult { ProtectedText = text };
        if (string.IsNullOrEmpty(text))
            return result;

        var terms = glossary
            .Where(term => !string.IsNullOrWhiteSpace(term.Term))
            .OrderByDescending(term => term.Term.Length)
            .ToList();

        var working = text;
        var counter = 0;

        foreach (var glossaryTerm in terms)
        {
            var pattern = Regex.Escape(glossaryTerm.Term);
            if (glossaryTerm.WholeWord)
                pattern = $@"(?<![A-Za-z0-9_]){pattern}(?![A-Za-z0-9_])";

            var options = glossaryTerm.CaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
            var regex = new Regex(pattern, options);

            working = regex.Replace(working, match =>
            {
                var placeholder = $"{PlaceholderPrefix}{counter}{PlaceholderSuffix}";
                counter++;
                result.Usages.Add(new ProtectedTermUsage
                {
                    Placeholder = placeholder,
                    MatchedText = match.Value,
                    Translation = glossaryTerm.Translation
                });
                return placeholder;
            });
        }

        result.ProtectedText = working;
        return result;
    }

    public string Restore(string translatedText, IEnumerable<ProtectedTermUsage> usages)
    {
        if (string.IsNullOrEmpty(translatedText))
            return translatedText;

        var result = translatedText;
        foreach (var usage in usages)
        {
            // Some translation engines insert whitespace into unknown tokens or alter case.
            // Match the generated placeholder loosely so it can still be restored safely.
            var numericPart = usage.Placeholder.Substring(
                PlaceholderPrefix.Length,
                usage.Placeholder.Length - PlaceholderPrefix.Length - PlaceholderSuffix.Length);

            var loosePattern = Regex.Escape(PlaceholderPrefix) + @"\s*" +
                               Regex.Escape(numericPart) + @"\s*" +
                               Regex.Escape(PlaceholderSuffix);

            result = Regex.Replace(result, loosePattern, usage.Translation, RegexOptions.IgnoreCase);
        }

        return result;
    }
}
