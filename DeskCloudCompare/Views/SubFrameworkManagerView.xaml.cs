using DeskCloudCompare.Models;
using DeskCloudCompare.ViewModels;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace DeskCloudCompare.Views;

public partial class SubFrameworkManagerView : UserControl
{
    public SubFrameworkManagerView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private SubFrameworkManagerViewModel? Vm => DataContext as SubFrameworkManagerViewModel;

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is SubFrameworkManagerViewModel vm)
        {
            vm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(SubFrameworkManagerViewModel.SubMatrixTable))
                    _matrixGrid.ItemsSource = vm.SubMatrixTable?.DefaultView;
                if (args.PropertyName == nameof(SubFrameworkManagerViewModel.FileDetailTable))
                    _fileGrid.ItemsSource = vm.FileDetailTable?.DefaultView;
            };
        }
    }

    // -----------------------------------------------------------------------
    // Sub-group matrix grid
    // -----------------------------------------------------------------------

    private static readonly Dictionary<string, Color> SubGroupColors = new()
    {
        ["FRS102"]    = Color.FromRgb(0xE3, 0xF2, 0xFD), // light blue
        ["FRS102 1A"] = Color.FromRgb(0xF3, 0xE5, 0xF5), // light purple
        ["FRS105"]    = Color.FromRgb(0xFF, 0xF9, 0xC4), // light yellow
        ["FRS SORP"]  = Color.FromRgb(0xFC, 0xE4, 0xEC), // light pink (legacy)
        ["Charity"]   = Color.FromRgb(0xF1, 0xF8, 0xE9), // light green
        ["IFRS Plus"] = Color.FromRgb(0xE0, 0xF2, 0xF1), // light teal
        ["IFRS SME"]  = Color.FromRgb(0xFF, 0xF3, 0xE0), // light orange
        ["ASPE Plus"] = Color.FromRgb(0xE8, 0xEA, 0xF6), // light indigo
    };

    private void MatrixGrid_AutoGeneratingColumn(object? sender, DataGridAutoGeneratingColumnEventArgs e)
    {
        var name = e.Column.Header?.ToString() ?? string.Empty;

        if (name.StartsWith("_")) { e.Cancel = true; return; }

        if (e.PropertyType == typeof(bool))
        {
            e.Column = new DataGridCheckBoxColumn
            {
                Header = name,
                Binding = new System.Windows.Data.Binding($"[{name}]"),
                Width = 100,
                IsReadOnly = true
            };
            return;
        }

        e.Column = new DataGridTextColumn
        {
            Header = name,
            Binding = new System.Windows.Data.Binding($"[{name}]"),
            Width = 160
        };
    }

    private void MatrixGrid_LoadingRow(object? sender, DataGridRowEventArgs e)
    {
        if (e.Row.Item is not DataRowView drv) return;
        // The display column has underscores replaced with spaces
        var sgDisplay = drv["Sub-Framework Group"]?.ToString() ?? string.Empty;
        if (SubGroupColors.TryGetValue(sgDisplay, out var color))
            e.Row.Background = new SolidColorBrush(color);
    }

    private async void MatrixGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (Vm == null) return;
        if (_matrixGrid.SelectedItem is not DataRowView drv) return;
        var sg = drv["_SubGroup"]?.ToString() ?? string.Empty;
        if (!string.IsNullOrEmpty(sg))
            await Vm.SelectSubGroupCommand.ExecuteAsync(sg);
    }

    // -----------------------------------------------------------------------
    // File detail grid
    // -----------------------------------------------------------------------

    private void FileGrid_AutoGeneratingColumn(object? sender, DataGridAutoGeneratingColumnEventArgs e)
    {
        var name = e.Column.Header?.ToString() ?? string.Empty;

        if (name.StartsWith("_")) { e.Cancel = true; return; }

        if (name == "File")
        {
            e.Column.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
            return;
        }

        if (e.PropertyType == typeof(string))
        {
            var cellStyle = new Style(typeof(DataGridCell));

            cellStyle.Triggers.Add(new DataTrigger
            {
                Binding = new System.Windows.Data.Binding($"[{name}]"), Value = "X",
                Setters =
                {
                    new Setter(BackgroundProperty, new SolidColorBrush(Color.FromRgb(0xFF, 0xCC, 0xCC))),
                    new Setter(ForegroundProperty, new SolidColorBrush(Color.FromRgb(0xC6, 0x28, 0x28)))
                }
            });
            cellStyle.Triggers.Add(new DataTrigger
            {
                Binding = new System.Windows.Data.Binding($"[{name}]"), Value = "≠",
                Setters =
                {
                    new Setter(BackgroundProperty, new SolidColorBrush(Color.FromRgb(0xFF, 0xCC, 0xCC))),
                    new Setter(ForegroundProperty, new SolidColorBrush(Color.FromRgb(0xC6, 0x28, 0x28)))
                }
            });
            cellStyle.Triggers.Add(new DataTrigger
            {
                Binding = new System.Windows.Data.Binding($"[{name}]"), Value = "E",
                Setters =
                {
                    new Setter(BackgroundProperty, new SolidColorBrush(Color.FromRgb(0xFF, 0xE0, 0x82))),
                    new Setter(ForegroundProperty, new SolidColorBrush(Color.FromRgb(0x7B, 0x4A, 0x00)))
                }
            });

            e.Column = new DataGridTextColumn
            {
                Header = name,
                Binding = new System.Windows.Data.Binding($"[{name}]"),
                Width = 100,
                CellStyle = cellStyle
            };
        }
    }

    private static readonly SolidColorBrush _dxdbBrush = new(Color.FromRgb(0xFF, 0xF9, 0xC4));
    private static readonly SolidColorBrush _finBrush  = new(Color.FromRgb(0xE3, 0xF2, 0xFD));

    private void FileGrid_LoadingRow(object? sender, DataGridRowEventArgs e)
    {
        if (e.Row.Item is not DataRowView drv) return;
        var isDxdb    = drv["_IsDxdb"] as bool? ?? false;
        var isFinData = drv["_IsFinancialData"] as bool? ?? false;
        e.Row.Background = isDxdb ? _dxdbBrush : isFinData ? _finBrush : null;
    }

    // -----------------------------------------------------------------------
    // Right-click context menu — exception marking / removal
    // -----------------------------------------------------------------------

    private async void FileGrid_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (Vm == null) return;
        var cell = FindVisualParent<DataGridCell>((DependencyObject)e.OriginalSource);
        if (cell == null) return;
        var frameworkName = cell.Column?.Header?.ToString() ?? string.Empty;
        if (frameworkName.Length == 0 || frameworkName == "File") return;
        if (cell.DataContext is not DataRowView drv) return;
        var cellValue = drv[frameworkName]?.ToString() ?? string.Empty;

        var fileId = (int)drv["_FileId"];
        var menu = new ContextMenu();

        if (cellValue == "X")
        {
            var allMissing = _fileGrid.Columns
                .Select(col => col.Header?.ToString() ?? string.Empty)
                .Where(h => h.Length > 0 && h != "File" && !h.StartsWith("_"))
                .Where(h => drv[h]?.ToString() == "X")
                .ToList();

            var item1 = new MenuItem { Header = $"Mark exception for '{frameworkName}' only" };
            item1.Click += async (_, _) => await Vm.MarkExceptionAsync(fileId, frameworkName);
            menu.Items.Add(item1);

            if (allMissing.Count > 1)
            {
                var item2 = new MenuItem { Header = $"Mark all {allMissing.Count} missing frameworks as exception" };
                item2.Click += async (_, _) => await Vm.MarkRowExceptionsAsync(fileId, allMissing);
                menu.Items.Add(item2);
            }
        }
        else if (cellValue == "E")
        {
            var allExceptions = _fileGrid.Columns
                .Select(col => col.Header?.ToString() ?? string.Empty)
                .Where(h => h.Length > 0 && h != "File" && !h.StartsWith("_"))
                .Where(h => drv[h]?.ToString() == "E")
                .ToList();

            var item1 = new MenuItem { Header = $"Remove exception for '{frameworkName}'" };
            item1.Click += async (_, _) => await Vm.RemoveExceptionAsync(fileId, frameworkName);
            menu.Items.Add(item1);

            if (allExceptions.Count > 1)
            {
                var item2 = new MenuItem { Header = $"Remove all {allExceptions.Count} exceptions for this file" };
                item2.Click += async (_, _) => await Vm.RemoveRowExceptionsAsync(fileId, allExceptions);
                menu.Items.Add(item2);
            }
        }
        else
        {
            return;
        }

        menu.PlacementTarget = cell;
        menu.Placement = PlacementMode.MousePoint;
        menu.IsOpen = true;
        e.Handled = true;
    }

    private static T? FindVisualParent<T>(DependencyObject child) where T : DependencyObject
    {
        var current = child;
        while (current != null)
        {
            if (current is T match) return match;
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }
}
