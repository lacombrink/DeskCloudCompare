using DeskCloudCompare.ViewModels;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace DeskCloudCompare.Views;

public partial class CountryManagerView : UserControl
{
    public CountryManagerView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private CountryManagerViewModel? Vm => DataContext as CountryManagerViewModel;

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is CountryManagerViewModel vm)
        {
            vm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(CountryManagerViewModel.FrameworkMatrixTable))
                    _matrixGrid.ItemsSource = vm.FrameworkMatrixTable?.DefaultView;
                if (args.PropertyName == nameof(CountryManagerViewModel.FileDetailTable))
                    _fileGrid.ItemsSource = vm.FileDetailTable?.DefaultView;
            };
        }
    }

    // -----------------------------------------------------------------------
    // Framework matrix — auto-generating columns
    // -----------------------------------------------------------------------

    private void MatrixGrid_AutoGeneratingColumn(object? sender, DataGridAutoGeneratingColumnEventArgs e)
    {
        var name = e.Column.Header?.ToString() ?? string.Empty;

        // Hide internal columns
        if (name.StartsWith("_"))
        {
            e.Cancel = true;
            return;
        }

        // Country columns (bool) → compact checkbox, centered
        if (e.PropertyType == typeof(bool))
        {
            e.Column = new DataGridCheckBoxColumn
            {
                Header = name,
                Binding = new System.Windows.Data.Binding($"[{name}]"),
                Width = 42,
                IsReadOnly = true
            };
            return;
        }

        // Text columns — explicit binding avoids issues with special characters (e.g. "/" in column name)
        e.Column = new DataGridTextColumn
        {
            Header = name,
            Binding = new System.Windows.Data.Binding($"[{name}]"),
            Width = name == "Category" ? 100 : 220
        };
    }

    private void MatrixGrid_LoadingRow(object? sender, DataGridRowEventArgs e)
    {
        // Methodologies in a slightly different background to separate from frameworks
        if (e.Row.Item is DataRowView drv &&
            drv["Category"]?.ToString() == "Methodology")
        {
            e.Row.Background = new SolidColorBrush(Color.FromRgb(0xE8, 0xF5, 0xE9));
        }
    }

    private async void MatrixGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (Vm == null) return;
        if (_matrixGrid.SelectedItem is not DataRowView drv) return;

        if (int.TryParse(drv["_Id"]?.ToString(), out var id) && id > 0)
            await Vm.SelectFrameworkCommand.ExecuteAsync(id);
    }

    // -----------------------------------------------------------------------
    // File detail grid — auto-generating columns
    // -----------------------------------------------------------------------

    private void FileGrid_AutoGeneratingColumn(object? sender, DataGridAutoGeneratingColumnEventArgs e)
    {
        var name = e.Column.Header?.ToString() ?? string.Empty;

        if (name.StartsWith("_"))
        {
            e.Cancel = true;
            return;
        }

        // "Framework" column (global views) — fixed width
        if (name == "Framework")
        {
            e.Column.Width = 260;
            return;
        }

        // "File" column — wide
        if (name == "File")
        {
            e.Column.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
            return;
        }

        // Country columns — narrow, with "E" (exception) cell highlight
        if (e.PropertyType == typeof(string) && name.Length <= 3)
        {
            e.Column.Width = 46;

            var cellStyle = new Style(typeof(DataGridCell));

            // X = framework applicable, file missing → red
            cellStyle.Triggers.Add(new DataTrigger
            {
                Binding = new System.Windows.Data.Binding($"[{name}]"),
                Value = "X",
                Setters =
                {
                    new Setter(DataGridCell.BackgroundProperty,
                        new SolidColorBrush(Color.FromRgb(0xFF, 0xCC, 0xCC))), // light red
                    new Setter(DataGridCell.ForegroundProperty,
                        new SolidColorBrush(Color.FromRgb(0xC6, 0x28, 0x28)))  // dark red text
                }
            });

            // ≠ = binary mismatch → red (same as missing)
            cellStyle.Triggers.Add(new DataTrigger
            {
                Binding = new System.Windows.Data.Binding($"[{name}]"),
                Value = "≠",
                Setters =
                {
                    new Setter(DataGridCell.BackgroundProperty,
                        new SolidColorBrush(Color.FromRgb(0xFF, 0xCC, 0xCC))), // light red
                    new Setter(DataGridCell.ForegroundProperty,
                        new SolidColorBrush(Color.FromRgb(0xC6, 0x28, 0x28)))  // dark red text
                }
            });

            // E = exception marked → amber
            cellStyle.Triggers.Add(new DataTrigger
            {
                Binding = new System.Windows.Data.Binding($"[{name}]"),
                Value = "E",
                Setters =
                {
                    new Setter(DataGridCell.BackgroundProperty,
                        new SolidColorBrush(Color.FromRgb(0xFF, 0xE0, 0x82))), // amber
                    new Setter(DataGridCell.ForegroundProperty,
                        new SolidColorBrush(Color.FromRgb(0x7B, 0x4A, 0x00)))  // dark brown text
                }
            });

            e.Column.CellStyle = cellStyle;
        }
    }

    private static readonly SolidColorBrush _dxdbBrush   = new(Color.FromRgb(0xFF, 0xF9, 0xC4)); // yellow
    private static readonly SolidColorBrush _finBrush    = new(Color.FromRgb(0xE3, 0xF2, 0xFD)); // blue
    private static readonly SolidColorBrush _updateBrush = new(Color.FromRgb(0xF3, 0xE5, 0xF5)); // purple

    private void FileGrid_LoadingRow(object? sender, DataGridRowEventArgs e)
    {
        if (e.Row.Item is not DataRowView drv) return;

        var isDxdb    = drv["_IsDxdb"] as bool? ?? false;
        var isFinData = drv["_IsFinancialData"] as bool? ?? false;
        var path      = drv["File"]?.ToString() ?? string.Empty;
        var isUpdate  = path.StartsWith("#Updates\\", StringComparison.OrdinalIgnoreCase);

        e.Row.Background = isDxdb    ? _dxdbBrush
                         : isFinData ? _finBrush
                         : isUpdate  ? _updateBrush
                         : null;
    }

    // -----------------------------------------------------------------------
    // Right-click context menu — exception marking
    // -----------------------------------------------------------------------

    private async void FileGrid_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (Vm == null) return;

        // Walk up the visual tree to find the clicked DataGridCell
        var cell = FindVisualParent<DataGridCell>((DependencyObject)e.OriginalSource);
        if (cell == null) return;

        var countryCode = cell.Column?.Header?.ToString() ?? string.Empty;

        // Only show menu on country columns (short header, not a text/hidden column)
        if (countryCode.Length == 0 || countryCode.Length > 3 || countryCode.StartsWith("_"))
            return;

        if (cell.DataContext is not DataRowView drv) return;

        var cellValue = drv[countryCode]?.ToString() ?? string.Empty;

        // Only show menu on X cells (framework applicable, file missing, no exception)
        if (cellValue != "X") return;

        var fileId = (int)drv["_FileId"];
        var fileName = System.IO.Path.GetFileName(drv["File"]?.ToString() ?? string.Empty);

        // Collect all X cells in this row (framework applicable + missing + no exception)
        var allMissingCodes = _fileGrid.Columns
            .Select(col => col.Header?.ToString() ?? string.Empty)
            .Where(h => h.Length > 0 && h.Length <= 3 && !h.StartsWith("_"))
            .Where(h => drv[h]?.ToString() == "X")
            .ToList();

        var menu = new ContextMenu();

        var item1 = new MenuItem { Header = $"Mark exception for {countryCode} only" };
        item1.Click += async (_, _) => await Vm.MarkExceptionAsync(fileId, countryCode);
        menu.Items.Add(item1);

        if (allMissingCodes.Count > 1)
        {
            var item2 = new MenuItem
            {
                Header = $"Mark all missing countries as exception ({allMissingCodes.Count} countries)"
            };
            item2.Click += async (_, _) => await Vm.MarkRowExceptionsAsync(fileId, allMissingCodes);
            menu.Items.Add(item2);
        }

        var item3 = new MenuItem
        {
            Header = $"Mark all frameworks — '{fileName}' missing in {countryCode}"
        };
        item3.Click += async (_, _) =>
            await Vm.MarkAllFrameworksExceptionsAsync(fileName, new[] { countryCode });
        menu.Items.Add(item3);

        if (allMissingCodes.Count > 1)
        {
            var item4 = new MenuItem
            {
                Header = $"Mark all frameworks — '{fileName}' missing in all {allMissingCodes.Count} countries"
            };
            item4.Click += async (_, _) =>
                await Vm.MarkAllFrameworksExceptionsAsync(fileName, allMissingCodes);
            menu.Items.Add(item4);
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
            current = System.Windows.Media.VisualTreeHelper.GetParent(current);
        }
        return null;
    }
}
