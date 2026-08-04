using Eto.Drawing;
using Eto.Forms;
using Glosslate.Models;
using Glosslate.Services;

namespace Glosslate;

public class GlossaryForm : Dialog
{
    private readonly List<GlossaryTerm> _terms;
    private readonly GridView _grid;
    private readonly GlossaryService _service = new();

    public string? GlossaryPath { get; private set; }

    public GlossaryForm(List<GlossaryTerm> terms, string? currentPath)
    {
        _terms = terms;
        GlossaryPath = currentPath;

        Title = "Glossary Editor";
        ClientSize = new Size(720, 430);

        _grid = new GridView
        {
            DataStore = _terms,
            Height = 300,
            Columns =
            {
                new GridColumn
                {
                    HeaderText = "Source term",
                    Editable = true,
                    Width = 180,
                    DataCell = new TextBoxCell { Binding = Binding.Property((GlossaryTerm t) => t.Term) }
                },
                new GridColumn
                {
                    HeaderText = "Fixed translation",
                    Editable = true,
                    Width = 180,
                    DataCell = new TextBoxCell { Binding = Binding.Property((GlossaryTerm t) => t.Translation) }
                },
                new GridColumn
                {
                    HeaderText = "Whole word",
                    Editable = true,
                    Width = 100,
                    DataCell = new CheckBoxCell { Binding = Binding.Property((GlossaryTerm t) => t.WholeWordNullable) }
                },
                new GridColumn
                {
                    HeaderText = "Case sensitive",
                    Editable = true,
                    Width = 110,
                    DataCell = new CheckBoxCell { Binding = Binding.Property((GlossaryTerm t) => t.CaseSensitiveNullable) }
                }
            }
        };

        var addButton = new Button { Text = "Add Term" };
        addButton.Click += (_, _) =>
        {
            _terms.Add(new GlossaryTerm());
            ReloadGrid();
        };

        var removeButton = new Button { Text = "Remove Selected" };
        removeButton.Click += (_, _) =>
        {
            if (_grid.SelectedItem is not GlossaryTerm term)
                return;

            _terms.Remove(term);
            ReloadGrid();
        };

        var loadButton = new Button { Text = "Load..." };
        loadButton.Click += (_, _) => LoadGlossary();

        var saveButton = new Button { Text = "Save..." };
        saveButton.Click += (_, _) => SaveGlossary();

        var closeButton = new Button { Text = "Close" };
        closeButton.Click += (_, _) => Close();

        Content = new TableLayout
        {
            Padding = 10,
            Spacing = new Size(6, 6),
            Rows =
            {
                new Label
                {
                    Text = "Glossary terms are protected before machine translation and restored afterward, so names and project-specific terminology remain consistent.",
                    Wrap = WrapMode.Word
                },
                _grid,
                new TableLayout
                {
                    Spacing = new Size(6, 0),
                    Rows = { new TableRow(addButton, removeButton, loadButton, saveButton, null) }
                },
                null,
                new TableLayout { Rows = { new TableRow(null, closeButton) } }
            }
        };
    }

    private void LoadGlossary()
    {
        var dialog = new OpenFileDialog { Filters = { new FileFilter("Glossary JSON", ".json") } };
        if (dialog.ShowDialog(this) != DialogResult.Ok)
            return;

        try
        {
            var loaded = _service.Load(dialog.FileName);
            _terms.Clear();
            _terms.AddRange(loaded);
            GlossaryPath = dialog.FileName;
            ReloadGrid();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Could not load glossary", MessageBoxType.Error);
        }
    }

    private void SaveGlossary()
    {
        var dialog = new SaveFileDialog
        {
            Filters = { new FileFilter("Glossary JSON", ".json") },
            FileName = string.IsNullOrWhiteSpace(GlossaryPath) ? "glossary.json" : GlossaryPath
        };

        if (dialog.ShowDialog(this) != DialogResult.Ok)
            return;

        try
        {
            var path = dialog.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                ? dialog.FileName
                : dialog.FileName + ".json";

            _service.Save(path, _terms);
            GlossaryPath = path;
            MessageBox.Show(this, "Glossary saved.", "Saved");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Could not save glossary", MessageBoxType.Error);
        }
    }

    private void ReloadGrid()
    {
        _grid.DataStore = null;
        _grid.DataStore = _terms;
    }
}
