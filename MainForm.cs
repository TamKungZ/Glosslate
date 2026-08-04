using Eto.Drawing;
using Eto.Forms;
using Glosslate.Models;
using Glosslate.Services;

namespace Glosslate;

public class MainForm : Form
{
    private List<TranslationEntry> _entries = new();
    private readonly List<GlossaryTerm> _glossary = new();
    private AppSettings _settings;
    private string? _glossaryPath;
    private string? _projectPath;

    private readonly GridView _grid;
    private readonly Label _statusLabel = new() { Text = "Ready" };
    private readonly ProgressBar _progress = new() { Visible = false };
    private CancellationTokenSource? _cts;

    private readonly JsonProjectService _jsonService = new();
    private readonly GlossaryService _glossaryService = new();
    private readonly SettingsService _settingsService = new();
    private readonly TermProtector _protector = new();

    public MainForm()
    {
        Title = "Glosslate - Glossary-aware Translation Tool";
        ClientSize = new Size(1000, 620);

        _settings = _settingsService.Load();
        LoadSavedGlossary();

        _grid = new GridView
        {
            DataStore = _entries,
            AllowMultipleSelection = true,
            Columns =
            {
                new GridColumn
                {
                    HeaderText = "Key",
                    Width = 180,
                    DataCell = new TextBoxCell { Binding = Binding.Property((TranslationEntry e) => e.Key) }
                },
                new GridColumn
                {
                    HeaderText = "Source",
                    Width = 320,
                    DataCell = new TextBoxCell { Binding = Binding.Property((TranslationEntry e) => e.Original) }
                },
                new GridColumn
                {
                    HeaderText = "Translation",
                    Width = 320,
                    Editable = true,
                    DataCell = new TextBoxCell { Binding = Binding.Property((TranslationEntry e) => e.Translated) }
                },
                new GridColumn
                {
                    HeaderText = "Status",
                    Width = 100,
                    DataCell = new TextBoxCell { Binding = Binding.Property((TranslationEntry e) => e.StatusText) }
                }
            }
        };

        Content = new TableLayout
        {
            Padding = 8,
            Spacing = new Size(6, 6),
            Rows =
            {
                new TableRow(new TableCell(_grid, true)) { ScaleHeight = true },
                BuildBottomBar()
            }
        };

        Menu = BuildMenu();
    }

    private void LoadSavedGlossary()
    {
        if (string.IsNullOrWhiteSpace(_settings.LastGlossaryPath) || !File.Exists(_settings.LastGlossaryPath))
            return;

        try
        {
            _glossary.AddRange(_glossaryService.Load(_settings.LastGlossaryPath));
            _glossaryPath = _settings.LastGlossaryPath;
        }
        catch
        {
            // A stale or invalid glossary path should never prevent the app from starting.
            _settings.LastGlossaryPath = "";
            _settingsService.Save(_settings);
        }
    }

    private Control BuildBottomBar()
    {
        var translateAllButton = new Button { Text = "Translate All" };
        translateAllButton.Click += async (_, _) => await RunTranslateAsync(_entries);

        var translateMissingButton = new Button { Text = "Translate Missing" };
        translateMissingButton.Click += async (_, _) =>
            await RunTranslateAsync(_entries
                .Where(x => x.Status is EntryStatus.NotTranslated or EntryStatus.Error)
                .ToList());

        var translateSelectedButton = new Button { Text = "Translate Selected" };
        translateSelectedButton.Click += async (_, _) =>
        {
            var selected = _grid.SelectedItems.OfType<TranslationEntry>().ToList();
            if (selected.Count == 0)
            {
                MessageBox.Show(this, "Select at least one row first.", "Nothing selected");
                return;
            }

            await RunTranslateAsync(selected);
        };

        var cancelButton = new Button { Text = "Cancel" };
        cancelButton.Click += (_, _) => _cts?.Cancel();

        return new TableLayout
        {
            Spacing = new Size(6, 0),
            Rows =
            {
                new TableRow(translateAllButton, translateMissingButton, translateSelectedButton, cancelButton, null, _progress),
                new TableRow(_statusLabel)
            }
        };
    }

    private MenuBar BuildMenu()
    {
        var openSource = new ButtonMenuItem { Text = "Open Source JSON..." };
        openSource.Click += (_, _) => OpenSourceJson();

        var openProject = new ButtonMenuItem { Text = "Open Project..." };
        openProject.Click += (_, _) => OpenProject();

        var saveProject = new ButtonMenuItem { Text = "Save Project" };
        saveProject.Click += (_, _) => SaveProject();

        var exportJson = new ButtonMenuItem { Text = "Export Translated JSON..." };
        exportJson.Click += (_, _) => ExportTranslatedJson();

        var exitItem = new ButtonMenuItem { Text = "Exit" };
        exitItem.Click += (_, _) => Application.Instance.Quit();

        var glossaryItem = new ButtonMenuItem { Text = "Edit Glossary..." };
        glossaryItem.Click += (_, _) => EditGlossary();

        var settingsItem = new ButtonMenuItem { Text = "Translation Settings..." };
        settingsItem.Click += (_, _) => EditSettings();

        var aboutItem = new ButtonMenuItem { Text = "About Glosslate..." };
        aboutItem.Click += (_, _) =>
        {
            MessageBox.Show(
                this,
                "Glosslate\n\n" +
                "A cross-platform, glossary-aware translation tool for flat JSON localization files.\n\n" +
                "Developed by TamKungZ_\n" +
                "dev@tamkungz.me",
                "About Glosslate",
                MessageBoxType.Information);
        };

        return new MenuBar
        {
            Items =
            {
                new ButtonMenuItem
                {
                    Text = "&File",
                    Items = { openSource, openProject, saveProject, exportJson, new SeparatorMenuItem(), exitItem }
                },
                new ButtonMenuItem { Text = "&Glossary", Items = { glossaryItem } },
                new ButtonMenuItem { Text = "&Settings", Items = { settingsItem } }
            },
            AboutItem = aboutItem
        };
    }

    private void OpenSourceJson()
    {
        var dialog = new OpenFileDialog { Filters = { new FileFilter("JSON files", ".json") } };
        if (dialog.ShowDialog(this) != DialogResult.Ok)
            return;

        try
        {
            _entries = _jsonService.LoadSourceJson(dialog.FileName);
            _grid.DataStore = _entries;
            _projectPath = null;
            _statusLabel.Text = $"Loaded {_entries.Count} entries from {Path.GetFileName(dialog.FileName)}.";
        }
        catch (Exception ex)
        {
            ShowError("Could not open source JSON", ex);
        }
    }

    private void OpenProject()
    {
        var dialog = new OpenFileDialog
        {
            Filters =
            {
                new FileFilter("Glosslate project", ".glosslate.json"),
                new FileFilter("Legacy translation project", ".trproj.json")
            }
        };

        if (dialog.ShowDialog(this) != DialogResult.Ok)
            return;

        try
        {
            var (entries, sourceLanguage, targetLanguage) = _jsonService.LoadProject(dialog.FileName);
            _entries = entries;
            _settings.SourceLang = sourceLanguage;
            _settings.TargetLang = targetLanguage;
            _grid.DataStore = _entries;
            _projectPath = dialog.FileName;
            _statusLabel.Text = $"Opened {Path.GetFileName(dialog.FileName)} with {_entries.Count} entries.";
        }
        catch (Exception ex)
        {
            ShowError("Could not open project", ex);
        }
    }

    private void SaveProject()
    {
        if (_entries.Count == 0)
            return;

        string? path = _projectPath;
        if (path is null)
        {
            var dialog = new SaveFileDialog
            {
                Filters = { new FileFilter("Glosslate project", ".glosslate.json") },
                FileName = "translation.glosslate.json"
            };

            if (dialog.ShowDialog(this) != DialogResult.Ok)
                return;

            path = EnsureExtension(dialog.FileName, ".glosslate.json");
        }

        try
        {
            _jsonService.SaveProject(path, _entries, _settings.SourceLang, _settings.TargetLang);
            _projectPath = path;
            _statusLabel.Text = $"Saved project: {Path.GetFileName(path)}.";
        }
        catch (Exception ex)
        {
            ShowError("Could not save project", ex);
        }
    }

    private void ExportTranslatedJson()
    {
        if (_entries.Count == 0)
            return;

        var dialog = new SaveFileDialog
        {
            Filters = { new FileFilter("JSON files", ".json") },
            FileName = "translated.json"
        };

        if (dialog.ShowDialog(this) != DialogResult.Ok)
            return;

        try
        {
            var path = EnsureExtension(dialog.FileName, ".json");
            _jsonService.ExportTranslatedJson(path, _entries);
            _statusLabel.Text = $"Exported: {Path.GetFileName(path)}.";
        }
        catch (Exception ex)
        {
            ShowError("Could not export translations", ex);
        }
    }

    private void EditGlossary()
    {
        var dialog = new GlossaryForm(_glossary, _glossaryPath);
        dialog.ShowModal(this);
        _glossaryPath = dialog.GlossaryPath;

        _settings.LastGlossaryPath = _glossaryPath ?? "";
        _settingsService.Save(_settings);
        _statusLabel.Text = _glossaryPath is null
            ? $"Glossary contains {_glossary.Count} terms."
            : $"Glossary: {Path.GetFileName(_glossaryPath)} ({_glossary.Count} terms).";
    }

    private void EditSettings()
    {
        var dialog = new SettingsForm(_settings);
        dialog.ShowModal(this);
        if (!dialog.Saved)
            return;

        _settings = dialog.Result;
        _settingsService.Save(_settings);
        _statusLabel.Text = $"Translation settings updated ({_settings.SourceLang} -> {_settings.TargetLang}).";
    }

    private ITranslationProvider CreateProvider() => _settings.Provider switch
    {
        ProviderKind.DeepL => new DeepLTranslationProvider(_settings.DeepLApiKey),
        _ => new GoogleFreeTranslationProvider()
    };

    private async Task RunTranslateAsync(IReadOnlyList<TranslationEntry> targets)
    {
        if (targets.Count == 0)
            return;

        var provider = CreateProvider();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();
        var cancellationToken = _cts.Token;

        _progress.Visible = true;
        _progress.MinValue = 0;
        _progress.MaxValue = targets.Count;
        _progress.Value = 0;

        var completed = 0;
        var errors = 0;

        try
        {
            foreach (var entry in targets)
            {
                cancellationToken.ThrowIfCancellationRequested();
                _statusLabel.Text = $"Translating {completed + 1}/{targets.Count}: {entry.Key}";

                try
                {
                    var protection = _protector.Protect(entry.Original, _glossary);
                    var translatedProtected = await provider.TranslateAsync(
                        protection.ProtectedText,
                        _settings.SourceLang,
                        _settings.TargetLang,
                        cancellationToken);

                    entry.Translated = _protector.Restore(translatedProtected, protection.Usages);
                    entry.Status = EntryStatus.Translated;
                    entry.LastError = "";
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    entry.Status = EntryStatus.Error;
                    entry.LastError = ex.Message;
                    errors++;
                }

                completed++;
                _progress.Value = completed;
                _grid.ReloadData(Enumerable.Range(0, _entries.Count));

                if (_settings.RequestDelayMs > 0 && completed < targets.Count)
                    await Task.Delay(_settings.RequestDelayMs, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            _statusLabel.Text = $"Translation cancelled after {completed}/{targets.Count} entries.";
            return;
        }
        finally
        {
            _progress.Visible = false;
        }

        _statusLabel.Text = errors == 0
            ? $"Translated {completed}/{targets.Count} entries."
            : $"Translated {completed}/{targets.Count} entries with {errors} error(s).";
    }

    private void ShowError(string title, Exception exception) =>
        MessageBox.Show(this, exception.Message, title, MessageBoxType.Error);

    private static string EnsureExtension(string path, string extension) =>
        path.EndsWith(extension, StringComparison.OrdinalIgnoreCase) ? path : path + extension;
}
