using Eto.Drawing;
using Eto.Forms;
using Glosslate.Models;

namespace Glosslate;

public class SettingsForm : Dialog
{
    public AppSettings Result { get; private set; }
    public bool Saved { get; private set; }

    private readonly DropDown _providerDropDown;
    private readonly PasswordBox _apiKeyBox;
    private readonly TextBox _sourceLangBox;
    private readonly TextBox _targetLangBox;
    private readonly NumericStepper _delayStepper;

    public SettingsForm(AppSettings current)
    {
        Result = current;
        Title = "Translation Settings";
        ClientSize = new Size(470, 270);

        _providerDropDown = new DropDown();
        _providerDropDown.Items.Add(new ListItem
        {
            Text = "Google Translate (free, unofficial, no API key)",
            Key = ProviderKind.GoogleFree.ToString()
        });
        _providerDropDown.Items.Add(new ListItem
        {
            Text = "DeepL (official API, API key required)",
            Key = ProviderKind.DeepL.ToString()
        });
        _providerDropDown.SelectedKey = current.Provider.ToString();

        _apiKeyBox = new PasswordBox { Text = current.DeepLApiKey };
        _sourceLangBox = new TextBox { Text = current.SourceLang };
        _targetLangBox = new TextBox { Text = current.TargetLang };
        _delayStepper = new NumericStepper
        {
            MinValue = 0,
            MaxValue = 5000,
            Increment = 50,
            Value = current.RequestDelayMs
        };

        var saveButton = new Button { Text = "Save" };
        saveButton.Click += (_, _) =>
        {
            Result = new AppSettings
            {
                Provider = Enum.Parse<ProviderKind>(_providerDropDown.SelectedKey ?? ProviderKind.GoogleFree.ToString()),
                DeepLApiKey = _apiKeyBox.Text,
                SourceLang = string.IsNullOrWhiteSpace(_sourceLangBox.Text) ? "auto" : _sourceLangBox.Text.Trim(),
                TargetLang = string.IsNullOrWhiteSpace(_targetLangBox.Text) ? "en" : _targetLangBox.Text.Trim(),
                RequestDelayMs = (int)_delayStepper.Value,
                LastGlossaryPath = current.LastGlossaryPath
            };
            Saved = true;
            Close();
        };

        var cancelButton = new Button { Text = "Cancel" };
        cancelButton.Click += (_, _) =>
        {
            Saved = false;
            Close();
        };

        Content = new TableLayout
        {
            Padding = 12,
            Spacing = new Size(8, 10),
            Rows =
            {
                new TableRow(new Label { Text = "Translation provider" }, _providerDropDown),
                new TableRow(new Label { Text = "DeepL API key" }, _apiKeyBox),
                new TableRow(new Label { Text = "Source language (e.g. auto, en)" }, _sourceLangBox),
                new TableRow(new Label { Text = "Target language (e.g. en, ja)" }, _targetLangBox),
                new TableRow(new Label { Text = "Delay between requests (ms)" }, _delayStepper),
                null,
                new TableRow(null, new TableLayout { Rows = { new TableRow(cancelButton, saveButton) } })
            }
        };

        AbortButton = cancelButton;
        DefaultButton = saveButton;
    }
}
