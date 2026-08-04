namespace Glosslate.Models;

/// <summary>
/// A single "protected" term. Before sending text to the translator, every
/// occurrence of Term is swapped for a placeholder. After translation the
/// placeholder is swapped back for Translation, so the fixed term never
/// passes through the machine translator itself.
/// </summary>
public class GlossaryTerm
{
    public string Term { get; set; } = "";
    public string Translation { get; set; } = "";

    /// <summary>Match "Tiri" but not "tiri" when true.</summary>
    public bool CaseSensitive { get; set; } = false;

    /// <summary>Only match whole words (won't match "Tiring" for term "Tiri").</summary>
    public bool WholeWord { get; set; } = true;

    // Eto's CheckBoxCell binds to bool? (it supports an indeterminate state), so these
    // nullable wrappers exist purely for grid binding - use CaseSensitive/WholeWord elsewhere.
    public bool? CaseSensitiveNullable
    {
        get => CaseSensitive;
        set => CaseSensitive = value ?? false;
    }

    public bool? WholeWordNullable
    {
        get => WholeWord;
        set => WholeWord = value ?? false;
    }

    /// <summary>Optional note for yourself, not used by the app logic.</summary>
    public string Note { get; set; } = "";
}
