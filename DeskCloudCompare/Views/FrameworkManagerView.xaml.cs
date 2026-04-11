using DeskCloudCompare.ViewModels;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace DeskCloudCompare.Views;

public partial class FrameworkManagerView : UserControl
{
    public FrameworkManagerView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private FrameworkManagerViewModel? Vm => DataContext as FrameworkManagerViewModel;

    // Persists user-resized column widths across ItemsSource refreshes.
    private readonly Dictionary<string, double> _savedFileGridWidths =
        new(StringComparer.OrdinalIgnoreCase);

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is FrameworkManagerViewModel vm)
        {
            vm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(FrameworkManagerViewModel.TypeMatrixTable))
                    _matrixGrid.ItemsSource = vm.TypeMatrixTable?.DefaultView;

                if (args.PropertyName == nameof(FrameworkManagerViewModel.FileDetailTable))
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
    // Type matrix grid
    // -----------------------------------------------------------------------

    private static readonly Dictionary<string, Color> TypeGroupColors = new()
    {
        ["IFRS"]   = Color.FromRgb(0xE3, 0xF2, 0xFD), // light blue
        ["FRS"]    = Color.FromRgb(0xF3, 0xE5, 0xF5), // light purple
        ["Legacy"] = Color.FromRgb(0xFF, 0xF9, 0xC4), // light yellow
        ["Arabic"] = Color.FromRgb(0xE8, 0xF5, 0xE9), // light green
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
            Width = 120
        };
    }

    private void MatrixGrid_LoadingRow(object? sender, DataGridRowEventArgs e)
    {
        if (e.Row.Item is not DataRowView drv) return;
        var tg = drv["_TypeGroup"]?.ToString() ?? string.Empty;
        if (TypeGroupColors.TryGetValue(tg, out var color))
            e.Row.Background = new SolidColorBrush(color);
    }

    private async void MatrixGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (Vm == null) return;
        if (_matrixGrid.SelectedItem is not DataRowView drv) return;
        var tg = drv["_TypeGroup"]?.ToString() ?? string.Empty;
        if (!string.IsNullOrEmpty(tg))
            await Vm.SelectTypeGroupCommand.ExecuteAsync(tg);
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
            e.Column.Width = _savedFileGridWidths.TryGetValue(name, out var fw) ? fw : 420;
            e.Column.MinWidth = 200;
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
                Width = _savedFileGridWidths.TryGetValue(name, out var sw) ? sw : 80,
                CellStyle = cellStyle
            };
        }
    }

    private static readonly SolidColorBrush _dxdbBrush   = new(Color.FromRgb(0xFF, 0xF9, 0xC4));
    private static readonly SolidColorBrush _finBrush    = new(Color.FromRgb(0xE3, 0xF2, 0xFD));
    private static readonly SolidColorBrush _aliasBrush  = new(Color.FromRgb(0xC8, 0xE6, 0xC9)); // light green

    private void FileGrid_LoadingRow(object? sender, DataGridRowEventArgs e)
    {
        if (e.Row.Item is not DataRowView drv) return;
        var isDxdb    = drv["_IsDxdb"] as bool? ?? false;
        var isFinData = drv["_IsFinancialData"] as bool? ?? false;
        var isAlias   = drv["_IsAlias"] as bool? ?? false;
        e.Row.Background = isDxdb ? _dxdbBrush : isFinData ? _finBrush : isAlias ? _aliasBrush : null;
    }

    // -----------------------------------------------------------------------
    // Right-click context menu — exception marking / removal / canonical alias creation
    // -----------------------------------------------------------------------

    private async void FileGrid_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (Vm == null) return;
        var cell = FindVisualParent<DataGridCell>((DependencyObject)e.OriginalSource);

        // ---- Multi-select: canonical alias creation ----
        // If multiple rows are selected, offer canonical alias creation regardless of which cell was clicked.
        var selectedRows = _fileGrid.SelectedItems
            .OfType<DataRowView>()
            .ToList();

        if (selectedRows.Count > 1)
        {
            var menu = new ContextMenu();
            var aliasItem = new MenuItem { Header = $"Create canonical alias from {selectedRows.Count} selected files…" };
            aliasItem.Click += async (_, _) => await ShowCanonicalAliasDialogAsync(selectedRows);
            menu.Items.Add(aliasItem);
            menu.PlacementTarget = cell ?? (UIElement)sender;
            menu.Placement = PlacementMode.MousePoint;
            menu.IsOpen = true;
            e.Handled = true;
            return;
        }

        // ---- Single-row: exception marking / removal ----
        if (cell == null) return;
        var frameworkName = cell.Column?.Header?.ToString() ?? string.Empty;
        if (frameworkName.Length == 0 || frameworkName == "File") return;
        if (cell.DataContext is not DataRowView drv) return;
        var cellValue = drv[frameworkName]?.ToString() ?? string.Empty;

        var fileId = (int)drv["_FileId"];
        var menu2 = new ContextMenu();

        if (cellValue == "X")
        {
            var allMissing = _fileGrid.Columns
                .Select(col => col.Header?.ToString() ?? string.Empty)
                .Where(h => h.Length > 0 && h != "File" && !h.StartsWith("_"))
                .Where(h => drv[h]?.ToString() == "X")
                .ToList();

            var item1 = new MenuItem { Header = $"Mark exception for '{frameworkName}' only" };
            item1.Click += async (_, _) => await Vm.MarkExceptionAsync(fileId, frameworkName);
            menu2.Items.Add(item1);

            if (allMissing.Count > 1)
            {
                var item2 = new MenuItem { Header = $"Mark all {allMissing.Count} missing frameworks as exception" };
                item2.Click += async (_, _) => await Vm.MarkRowExceptionsAsync(fileId, allMissing);
                menu2.Items.Add(item2);
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
            menu2.Items.Add(item1);

            if (allExceptions.Count > 1)
            {
                var item2 = new MenuItem { Header = $"Remove all {allExceptions.Count} exceptions for this file" };
                item2.Click += async (_, _) => await Vm.RemoveRowExceptionsAsync(fileId, allExceptions);
                menu2.Items.Add(item2);
            }
        }
        else
        {
            return;
        }

        menu2.PlacementTarget = cell;
        menu2.Placement = PlacementMode.MousePoint;
        menu2.IsOpen = true;
        e.Handled = true;
    }

    private async Task ShowCanonicalAliasDialogAsync(List<DataRowView> selectedRows)
    {
        if (Vm == null) return;

        var fileIds = selectedRows.Select(r => (int)r["_FileId"]).ToList();
        var fileNames = selectedRows.Select(r => r["File"]?.ToString() ?? string.Empty).ToList();

        // Simple picker dialog: choose which file is the canonical master
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

        if (!confirmed) return;
        if (listBox.SelectedItem is not ListBoxItem selectedItem) return;

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
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }
}
