using DeskCloudCompare.ViewModels;
using System.Data;
using System.Windows;
using System.Windows.Controls;
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
            var col = new DataGridCheckBoxColumn
            {
                Header = name,
                Binding = new System.Windows.Data.Binding($"[{name}]"),
                Width = 42,
                IsReadOnly = true
            };
            e.Column = col;
            return;
        }

        // Text columns
        e.Column.Width = name == "Category" ? 100 : 220;
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

        // "File" column — wide
        if (name == "File")
        {
            e.Column.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
            return;
        }

        // Country columns — narrow, centered text
        if (e.PropertyType == typeof(string) && name.Length <= 3)
        {
            e.Column.Width = 46;
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
}
