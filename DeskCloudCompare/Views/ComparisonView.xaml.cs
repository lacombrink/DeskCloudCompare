using DeskCloudCompare.Services;
using DeskCloudCompare.ViewModels;
using Microsoft.Win32;
using System.Collections.Specialized;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DeskCloudCompare.Views;

public partial class ComparisonView : UserControl
{
    public ComparisonView()
    {
        InitializeComponent();
    }

    private ComparisonViewModel? Vm => DataContext as ComparisonViewModel;

    private List<ComparisonRowViewModel> SelectedRows =>
        _resultsGrid.SelectedItems.Cast<ComparisonRowViewModel>().ToList();

    private List<string> ActiveSlots =>
        Vm?.Slots
           .Where(s => !string.IsNullOrWhiteSpace(s.FolderPath))
           .Select(s => s.SlotLabel)
           .ToList() ?? new List<string>();

    private static string? GetPath(ComparisonRowViewModel row, string slot) => slot switch
    {
        "A" => row.PathA,
        "B" => row.PathB,
        "C" => row.PathC,
        "D" => row.PathD,
        _ => null
    };

    // -----------------------------------------------------------------------
    // Ctrl+A — select all visible (filtered) rows
    // -----------------------------------------------------------------------

    private void ResultsGrid_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.A && Keyboard.Modifiers == ModifierKeys.Control)
        {
            _resultsGrid.SelectAll();
            e.Handled = true;
        }
    }

    // -----------------------------------------------------------------------
    // Right-click — if clicking an unselected row, select only that row;
    // if clicking an already-selected row, keep the current multi-selection.
    // -----------------------------------------------------------------------

    private void ResultsGrid_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        var row = ItemsControl.ContainerFromElement(
            _resultsGrid, e.OriginalSource as DependencyObject) as DataGridRow;

        if (row == null) return;

        if (!row.IsSelected)
        {
            _resultsGrid.SelectedItems.Clear();
            row.IsSelected = true;
        }
    }

    // -----------------------------------------------------------------------
    // Context menu — rebuilt dynamically each time based on active slots
    // -----------------------------------------------------------------------

    private void ResultsGrid_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        var cm = _resultsGrid.ContextMenu!;
        cm.Items.Clear();

        var rows = SelectedRows;
        var active = ActiveSlots;

        if (rows.Count == 0 || active.Count == 0)
        {
            e.Handled = true;
            return;
        }

        // 1. Copy File Names
        cm.Items.Add(MakeItem(
            $"Copy File Names  ({rows.Count} selected)",
            () => CopyFileNames(rows)));

        cm.Items.Add(new Separator());

        // 2. Copy Paths → submenu per active slot
        var copyPaths = new MenuItem { Header = "Copy Paths" };
        foreach (var slot in active)
        {
            var s = slot;
            int count = rows.Count(r => GetPath(r, s) != null);
            copyPaths.Items.Add(MakeItem(
                $"Slot {s}  ({count} paths)",
                () => CopyPaths(rows, s)));
        }
        cm.Items.Add(copyPaths);

        cm.Items.Add(new Separator());

        // 3. Copy Files → From X → To Y
        var copyFiles = new MenuItem { Header = "Copy Files" };
        foreach (var from in active)
        {
            var toSlots = active.Where(s => s != from).ToList();
            if (toSlots.Count == 0) continue;

            var fromMenu = new MenuItem { Header = $"From Slot {from}" };
            foreach (var to in toSlots)
            {
                var f = from; var t = to;
                int count = rows.Count(r => GetPath(r, f) != null);
                fromMenu.Items.Add(MakeItem(
                    $"To Slot {t}  ({count} files)",
                    () => CopyFilesBetweenSlots(rows, f, t)));
            }
            copyFiles.Items.Add(fromMenu);
        }
        cm.Items.Add(copyFiles);

        cm.Items.Add(new Separator());

        // 4. Copy Files to Clipboard → submenu per active slot
        var copyToClipboard = new MenuItem { Header = "Copy Files to Clipboard" };
        foreach (var slot in active)
        {
            var s = slot;
            int count = rows.Count(r => GetPath(r, s) != null);
            copyToClipboard.Items.Add(MakeItem(
                $"From Slot {s}  ({count} files)",
                () => CopyFilesToClipboard(rows, s)));
        }
        cm.Items.Add(copyToClipboard);

        cm.Items.Add(new Separator());

        // 5. Copy Files with Path → submenu per active slot
        var copyWithPath = new MenuItem { Header = "Copy Files with Path" };
        foreach (var slot in active)
        {
            var s = slot;
            int count = rows.Count(r => GetPath(r, s) != null);
            copyWithPath.Items.Add(MakeItem(
                $"From Slot {s}  ({count} files)",
                () => CopyFilesWithPath(rows, s)));
        }
        cm.Items.Add(copyWithPath);
    }

    private static MenuItem MakeItem(string header, Action onClick)
    {
        var item = new MenuItem { Header = header };
        item.Click += (_, _) => onClick();
        return item;
    }

    // -----------------------------------------------------------------------
    // Operation implementations
    // -----------------------------------------------------------------------

    // 1. Copy file names as a newline-separated list
    private static void CopyFileNames(List<ComparisonRowViewModel> rows)
    {
        Clipboard.SetText(string.Join(Environment.NewLine, rows.Select(r => r.FileName)));
    }

    // 2. Copy full paths of a given slot as a newline-separated list
    private static void CopyPaths(List<ComparisonRowViewModel> rows, string slot)
    {
        var paths = rows
            .Select(r => GetPath(r, slot))
            .Where(p => !string.IsNullOrEmpty(p));
        Clipboard.SetText(string.Join(Environment.NewLine, paths));
    }

    // 3. Physically copy files from one slot's path to the other slot's path.
    //    When the destination file does not exist yet but the destination slot is a Desktop
    //    slot, the Desktop path is derived from the canonical path (reversing the
    //    Desktop→Canonical rules), directories are created, and the file is copied in.
    private void CopyFilesBetweenSlots(
        List<ComparisonRowViewModel> rows, string fromSlot, string toSlot)
    {
        // Gather destination slot info so we can derive Desktop paths for new files.
        var destSlotVm   = Vm?.Slots.FirstOrDefault(s => s.SlotLabel == toSlot);
        var destRoot     = destSlotVm?.FolderPath;
        var isDesktopDest = string.Equals(
            destSlotVm?.SelectedFolderType?.Name, "Desktop",
            StringComparison.OrdinalIgnoreCase);

        int copied = 0, derived = 0, skipped = 0;
        var errors = new List<string>();

        foreach (var row in rows)
        {
            var src = GetPath(row, fromSlot);
            var dst = GetPath(row, toSlot);

            if (string.IsNullOrEmpty(src))
            {
                skipped++;
                continue;
            }

            // If the destination file doesn't exist yet and the slot is Desktop,
            // derive the Desktop relative path from the canonical path and prepend
            // the slot's configured root folder.
            if (string.IsNullOrEmpty(dst) && isDesktopDest && !string.IsNullOrEmpty(destRoot))
            {
                var rel = PathTranslationService.DeriveDesktopRelativePath(row.CanonicalPath);
                if (rel != null)
                {
                    dst = Path.Combine(destRoot, rel);
                    derived++;
                }
            }

            if (string.IsNullOrEmpty(dst))
            {
                skipped++;
                continue;
            }

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(dst)!);
                File.Copy(src, dst, overwrite: true);
                copied++;
            }
            catch (Exception ex)
            {
                errors.Add($"{row.FileName}: {ex.Message}");
            }
        }

        var msg = $"Copied: {copied}";
        if (derived > 0) msg += $" (including {derived} placed in new Desktop folders)";
        msg += $"\nSkipped (path unknown in one slot): {skipped}";
        if (errors.Count > 0)
            msg += $"\n\nErrors ({errors.Count}):\n" +
                   string.Join("\n", errors.Take(10)) +
                   (errors.Count > 10 ? $"\n…and {errors.Count - 10} more" : string.Empty);

        MessageBox.Show(msg, $"Copy Files — Slot {fromSlot} → Slot {toSlot}",
            MessageBoxButton.OK,
            errors.Count > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
    }

    // 4. Put the actual file objects onto the Windows clipboard (paste-into-Explorer)
    private static void CopyFilesToClipboard(List<ComparisonRowViewModel> rows, string slot)
    {
        var files = rows
            .Select(r => GetPath(r, slot))
            .Where(p => !string.IsNullOrEmpty(p) && File.Exists(p))
            .ToList();

        if (files.Count == 0)
        {
            MessageBox.Show("No existing files found for the selected slot.",
                "Copy Files to Clipboard", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var sc = new StringCollection();
        sc.AddRange(files.ToArray()!);
        Clipboard.SetFileDropList(sc);
    }

    // 5. Copy files to a user-chosen root folder, recreating the canonical folder structure.
    private static void CopyFilesWithPath(List<ComparisonRowViewModel> rows, string slot)
    {
        var dialog = new OpenFolderDialog
        {
            Title = $"Select destination root — Slot {slot} files will be placed under this folder"
        };
        if (dialog.ShowDialog() != true) return;

        var destRoot = dialog.FolderName;
        int copied = 0, skipped = 0;
        var errors = new List<string>();

        foreach (var row in rows)
        {
            var src = GetPath(row, slot);
            if (string.IsNullOrEmpty(src) || !File.Exists(src))
            {
                skipped++;
                continue;
            }

            // Use canonical path as relative folder structure under destRoot
            var rel = row.CanonicalPath.TrimStart('\\', '/');
            var dest = Path.Combine(destRoot, rel);

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                File.Copy(src, dest, overwrite: true);
                copied++;
            }
            catch (Exception ex)
            {
                errors.Add($"{row.FileName}: {ex.Message}");
            }
        }

        var msg = $"Copied: {copied}\nSkipped (file not found in slot): {skipped}";
        if (errors.Count > 0)
            msg += $"\n\nErrors ({errors.Count}):\n" +
                   string.Join("\n", errors.Take(10)) +
                   (errors.Count > 10 ? $"\n…and {errors.Count - 10} more" : string.Empty);

        MessageBox.Show(msg, $"Copy Files with Path — Slot {slot}",
            MessageBoxButton.OK,
            errors.Count > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
    }
}
