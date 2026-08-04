using System.ComponentModel;

namespace Glosslate.Models;

public enum EntryStatus
{
    NotTranslated,
    Translated,
    Reviewed,
    Error
}

public class TranslationEntry : INotifyPropertyChanged
{
    public string Key { get; set; } = "";
    public string Original { get; set; } = "";

    private string _translated = "";
    public string Translated
    {
        get => _translated;
        set
        {
            _translated = value ?? "";
            OnPropertyChanged(nameof(Translated));
        }
    }

    private EntryStatus _status = EntryStatus.NotTranslated;
    public EntryStatus Status
    {
        get => _status;
        set
        {
            _status = value;
            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(StatusText));
        }
    }

    public string StatusText => Status switch
    {
        EntryStatus.NotTranslated => "Not translated",
        EntryStatus.Translated => "Translated",
        EntryStatus.Reviewed => "Reviewed",
        EntryStatus.Error => "Error",
        _ => Status.ToString()
    };

    public string LastError { get; set; } = "";

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
