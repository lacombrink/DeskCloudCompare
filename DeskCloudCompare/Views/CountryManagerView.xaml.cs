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

    private readonly Dictionary<string, double> _savedFileGridWidths =
        new(StringComparer.OrdinalIgnoreCase);

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is CountryManagerViewModel vm)
        {
            vm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(CountryManagerViewModel.FrameworkMatrixTable))
                    _matrixGrid.ItemsSource = vm.FrameworkMatrixTable?.DefaultView;

                if (args.PropertyName == nameof(CountryManagerViewModel.FileDetailTable))
                {
                    SaveFileGridWidths();
                    _fileGrid.ItemsSource = vm.FileDetailTable?.DefaultView;
                }
            };
        }
    }

    private void SaveFileGridWidths()
    {
        foreach (var col in _fileGrid.Columns)
        {
            var header = col.Header?.ToString() ?? string.Empty;
            if (header.Length > 0 && !header.StartsWith("_"))
            {
                var px = col.ActualWidth > 0 ? col.ActualWidth : col.Width.Value;
                if (px > 0) _savedFileGridWidths[header] = px;
            }
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
            e.Column.Width = _savedFileGridWidths.TryGetValue(name, out var ffw) ? ffw : 280;
            e.Column.MinWidth = 160;
            return;
        }

        // "File" column — wide fixed width (supports horizontal scrolling)
        if (name == "File")
        {
            e.Column.Width = _savedFileGridWidths.TryGetValue(name, out var ffw2) ? ffw2 : 380;
            e.Column.MinWidth = 180;
            return;
        }

        // Country columns — narrow, with "E" (exception) cell highlight
        if (e.PropertyType == typeof(string) && name.Length <= 3)
        {
            e.Column.Width = _savedFileGridWidths.TryGetValue(name, out var cw) ? cw : 46;

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
    // Right-click context menu — exception marking / removal / canonical alias creation
    // -----------------------------------------------------------------------

    private async void FileGrid_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (Vm == null) return;

        var cell = FindVisualParent<DataGridCell>((DependencyObject)e.OriginalSource);

        // ---- Multi-select: canonical alias creation ----
        var selectedRows = _fileGrid.SelectedItems.OfType<DataRowView>().ToList();
        if (selectedRows.Count > 1)
        {
            var aliasMenu = new ContextMenu();
            var aliasItem = new MenuItem { Header = $"Create canonical alias from {selectedRows.Count} selected files…" };
            aliasItem.Click += async (_, _) => await ShowCanonicalAliasDialogAsync(selectedRows);
            aliasMenu.Items.Add(aliasItem);
            aliasMenu.PlacementTarget = cell ?? (UIElement)sender;
            aliasMenu.Placement = PlacementMode.MousePoint;
            aliasMenu.IsOpen = true;
            e.Handled = true;
            return;
        }

        // Walk up the visual tree to find the clicked DataGridCell
        if (cell == null) return;

        var countryCode = cell.Column?.Header?.ToString() ?? string.Empty;

        // Only show menu on country columns (short header, not a text/hidden column)
        if (countryCode.Length == 0 || countryCode.Length > 3 || countryCode.StartsWith("_"))
            return;

        if (cell.DataContext is not DataRowView drv) return;

        var cellValue = drv[countryCode]?.ToString() ?? string.Empty;
        var fileId = (int)drv["_FileId"];
        var fileName = System.IO.Path.GetFileName(drv["File"]?.ToString() ?? string.Empty);

        var menu = new ContextMenu();

        if (cellValue == "X")
        {
            var allMissingCodes = _fileGrid.Columns
                .Select(col => col.Header?.ToString() ?? string.Empty)
                .Where(h => h.Length > 0 && h.Length <= 3 && !h.StartsWith("_"))
                .Where(h => drv[h]?.ToString() == "X")
                .ToList();

            var item1 = new MenuItem { Header = $"Mark exception for {countryCode} only" };
            item1.Click += async (_, _) => await Vm.MarkExceptionAsync(fileId, countryCode);
            menu.Items.Add(item1);

            if (allMissingCodes.Count > 1)
            {
                var item2 = new MenuItem { Header = $"Mark all missing countries as exception ({allMissingCodes.Count} countries)" };
                item2.Click += async (_, _) => await Vm.MarkRowExceptionsAsync(fileId, allMissingCodes);
                menu.Items.Add(item2);
            }

            var item3 = new MenuItem { Header = $"Mark all frameworks — '{fileName}' missing in {countryCode}" };
            item3.Click += async (_, _) =>
                await Vm.MarkAllFrameworksExceptionsAsync(fileName, new[] { countryCode });
            menu.Items.Add(item3);

            if (allMissingCodes.Count > 1)
            {
                var item4 = new MenuItem { Header = $"Mark all frameworks — '{fileName}' missing in all {allMissingCodes.Count} countries" };
                item4.Click += async (_, _) =>
                    await Vm.MarkAllFrameworksExceptionsAsync(fileName, allMissingCodes);
                menu.Items.Add(item4);
            }
        }
        else if (cellValue == "E")
        {
            var allExceptionCodes = _fileGrid.Columns
                .Select(col => col.Header?.ToString() ?? string.Empty)
                .Where(h => h.Length > 0 && h.Length <= 3 && !h.StartsWith("_"))
                .Where(h => drv[h]?.ToString() == "E")
                .ToList();

            var item1 = new MenuItem { Header = $"Remove exception for {countryCode}" };
            item1.Click += async (_, _) => await Vm.RemoveExceptionAsync(fileId, countryCode);
            menu.Items.Add(item1);

            if (allExceptionCodes.Count > 1)
            {
                var item2 = new MenuItem { Header = $"Remove all {allExceptionCodes.Count} exceptions for this file" };
                item2.Click += async (_, _) => await Vm.RemoveRowExceptionsAsync(fileId, allExceptionCodes);
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

    private async Task ShowCanonicalAliasDialogAsync(List<DataRowView> selectedRows)
    {
        if (Vm == null) return;

        var fileIds = selectedRows.Select(r => (int)r["_FileId"]).ToList();
        var fileNames = selectedRows.Select(r => r["File"]?.ToString() ?? string.Empty).ToList();

        var dialog = new Window
        {
            Title = "Select canonical (master) file name",
            Width = 600,
            Height = 300,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = Window.GetWindow(this),
            ResizeMode = ResizeMode.NoResize
        };

        var panel = new StackPanel { Margin = new Thickness(16) };
        panel.Children.Add(new TextBlock
        {
            Text = "Choose which file name is the canonical (master) name.\nAll other selected files will alias to this one.",
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 12)
        });

        var listBox = new ListBox { Height = 150 };
        for (int i = 0; i < fileNames.Count; i++)
            listBox.Items.Add(new ListBoxItem { Content = fileNames[i], Tag = fileIds[i] });
        listBox.SelectedIndex = 0;
        panel.Children.Add(listBox);

        var btnPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 12, 0, 0)
        };
        var okBtn = new Button { Content = "Create Alias", Padding = new Thickness(16, 4, 16, 4), IsDefault = true, Margin = new Thickness(0, 0, 8, 0) };
        var cancelBtn = new Button { Content = "Cancel", Padding = new Thickness(16, 4, 16, 4), IsCancel = true };
        btnPanel.Children.Add(okBtn);
        btnPanel.Children.Add(cancelBtn);
        panel.Children.Add(btnPanel);
        dialog.Content = panel;

        bool confirmed = false;
        okBtn.Click += (_, _) => { confirmed = true; dialog.Close(); };
        cancelBtn.Click += (_, _) => { dialog.Close(); };
        dialog.ShowDialog();

        if (!confirmed || listBox.SelectedItem is not ListBoxItem selectedItem) return;

        var masterFileId = (int)selectedItem.Tag;
        var (success, error) = await Vm.ValidateAndCreateCanonicalAliasAsync(fileIds, masterFileId);

        if (success)
        {
            _fileGrid.SelectedItems.Clear();
            MessageBox.Show("Canonical alias created successfully.", "Success",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            MessageBox.Show(error, "Invalid Selection",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
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
