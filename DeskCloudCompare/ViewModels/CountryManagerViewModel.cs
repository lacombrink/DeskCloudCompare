using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeskCloudCompare.Data;
using DeskCloudCompare.Models;
using DeskCloudCompare.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using System.Data;
using System.IO;
using System.Windows;

namespace DeskCloudCompare.ViewModels;

public partial class CountryManagerViewModel : ObservableObject
{
    private readonly AppDbContext _db;
    private readonly CountryManagerScanService _scanService;

    [ObservableProperty] private string _rootFolderPath = string.Empty;
    [ObservableProperty] private string _masterCountryCode = string.Empty;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _statusMessage = "Not yet scanned.";
    [ObservableProperty] private DataTable? _frameworkMatrixTable;
    [ObservableProperty] private DataTable? _fileDetailTable;
    [ObservableProperty] private string _selectedFrameworkName = string.Empty;
    [ObservableProperty] private int _selectedFrameworkId;

    public bool IsNotBusy => !IsBusy;

    private List<string> _countryCodes = new();
    private CancellationTokenSource? _cts;

    public CountryManagerViewModel(AppDbContext db, CountryManagerScanService scanService)
    {
        _db = db;
        _scanService = scanService;
    }

    partial void OnIsBusyChanged(bool value) => OnPropertyChanged(nameof(IsNotBusy));

    public async Task LoadAsync()
    {
        var settings = await _db.CountryManagerSettings.FirstOrDefaultAsync();
        if (settings != null)
        {
            RootFolderPath = settings.RootFolderPath;
            MasterCountryCode = settings.MasterCountryCode ?? string.Empty;
        }

        var hasData = await _db.CountryEntries.AnyAsync();
        if (hasData)
            await BuildFrameworkMatrixAsync();
        else
            StatusMessage = "No scan data. Configure root folder and click Scan.";
    }

    // -----------------------------------------------------------------------
    // Scan
    // -----------------------------------------------------------------------

    [RelayCommand]
    private void BrowseRootFolder()
    {
        var dialog = new OpenFolderDialog { Title = "Select root folder containing all country folders" };
        if (dialog.ShowDialog() == true)
            RootFolderPath = dialog.FolderName;
    }

    [RelayCommand(CanExecute = nameof(IsNotBusy))]
    private async Task Scan()
    {
        if (string.IsNullOrWhiteSpace(RootFolderPath) || !Directory.Exists(RootFolderPath))
        {
            MessageBox.Show("Please select a valid root folder first.", "Scan",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        IsBusy = true;
        FileDetailTable = null;
        SelectedFrameworkId = 0;
        SelectedFrameworkName = string.Empty;
        _cts = new CancellationTokenSource();

        try
        {
            var progress = new Progress<string>(msg => StatusMessage = msg);
            await _scanService.ScanAsync(RootFolderPath,
                string.IsNullOrWhiteSpace(MasterCountryCode) ? null : MasterCountryCode,
                progress, _cts.Token);
            await BuildFrameworkMatrixAsync();
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Scan cancelled.";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Scan failed: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            StatusMessage = "Scan failed.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void CancelScan() => _cts?.Cancel();

    // -----------------------------------------------------------------------
    // Framework selection → file detail
    // -----------------------------------------------------------------------

    [RelayCommand(CanExecute = nameof(IsNotBusy))]
    private async Task SelectFramework(int frameworkId)
    {
        if (frameworkId <= 0) return;
        SelectedFrameworkId = frameworkId;
        var fw = await _db.CanonicalFrameworks.FindAsync(frameworkId);
        SelectedFrameworkName = fw != null ? $"{fw.Category}: {fw.Name}" : string.Empty;
        await BuildFileDetailTableAsync(frameworkId);
    }

    // -----------------------------------------------------------------------
    // Binary compare for selected framework
    // -----------------------------------------------------------------------

    [RelayCommand(CanExecute = nameof(CanRunBinaryCompare))]
    private async Task BinaryCompare()
    {
        if (SelectedFrameworkId <= 0) return;
        if (string.IsNullOrWhiteSpace(MasterCountryCode))
        {
            MessageBox.Show("Select a master country before running binary compare.", "Binary Compare",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        IsBusy = true;
        _cts = new CancellationTokenSource();

        try
        {
            var progress = new Progress<string>(msg => StatusMessage = msg);
            await _scanService.BinaryCompareAsync(SelectedFrameworkId, MasterCountryCode,
                progress, _cts.Token);
            await BuildFileDetailTableAsync(SelectedFrameworkId);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Binary compare cancelled.";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Binary compare failed: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanRunBinaryCompare() =>
        IsNotBusy && SelectedFrameworkId > 0 && !string.IsNullOrWhiteSpace(MasterCountryCode);

    // -----------------------------------------------------------------------
    // DataTable builders
    // -----------------------------------------------------------------------

    private async Task BuildFrameworkMatrixAsync()
    {
        var countries = await _db.CountryEntries.OrderBy(c => c.SortOrder).ToListAsync();
        _countryCodes = countries.Select(c => c.Code).ToList();

        var frameworks = await _db.CanonicalFrameworks
            .OrderBy(f => f.Category)
            .ThenBy(f => f.Name)
            .ToListAsync();

        var presences = await _db.CountryFrameworkPresences.ToListAsync();

        var dt = new DataTable();
        dt.Columns.Add("_Id", typeof(int));   // hidden, used to drive SelectFramework
        dt.Columns.Add("Category", typeof(string));
        dt.Columns.Add("Framework / Methodology", typeof(string));

        foreach (var c in countries)
            dt.Columns.Add(c.Code, typeof(bool));

        foreach (var fw in frameworks)
        {
            var row = dt.NewRow();
            row["_Id"] = fw.Id;
            row["Category"] = fw.Category.ToString();
            row["Framework / Methodology"] = fw.Name;
            foreach (var c in countries)
                row[c.Code] = presences.Any(p => p.CanonicalFrameworkId == fw.Id && p.CountryCode == c.Code);
            dt.Rows.Add(row);
        }

        FrameworkMatrixTable = dt;
        StatusMessage = $"Matrix ready — {frameworks.Count} frameworks/methodologies × {countries.Count} countries.";
    }

    private async Task BuildFileDetailTableAsync(int frameworkId)
    {
        var countries = await _db.CountryEntries.OrderBy(c => c.SortOrder).ToListAsync();

        var files = await _db.CanonicalFiles
            .Where(f => f.CanonicalFrameworkId == frameworkId)
            .OrderBy(f => f.RelativePath)
            .ToListAsync();

        var presences = await _db.CountryFilePresences
            .Where(p => p.CanonicalFile.CanonicalFrameworkId == frameworkId)
            .ToListAsync();

        // Determine master hashes (from master country if binary compare was run)
        var masterHashes = new Dictionary<int, string?>();
        if (!string.IsNullOrWhiteSpace(MasterCountryCode))
        {
            foreach (var p in presences.Where(p => p.CountryCode == MasterCountryCode))
                masterHashes[p.CanonicalFileId] = p.BinaryHash;
        }

        var dt = new DataTable();
        dt.Columns.Add("_FileId", typeof(int));
        dt.Columns.Add("File", typeof(string));
        dt.Columns.Add("_IsDxdb", typeof(bool));
        dt.Columns.Add("_IsFinancialData", typeof(bool));

        foreach (var c in countries)
            dt.Columns.Add(c.Code, typeof(string));

        foreach (var file in files)
        {
            var row = dt.NewRow();
            row["_FileId"] = file.Id;
            row["File"] = file.RelativePath;
            row["_IsDxdb"] = file.IsDxdb;
            row["_IsFinancialData"] = file.IsFinancialData;

            foreach (var c in countries)
            {
                var presence = presences.FirstOrDefault(
                    p => p.CanonicalFileId == file.Id && p.CountryCode == c.Code);

                row[c.Code] = BuildCellStatus(file, presence, masterHashes);
            }

            dt.Rows.Add(row);
        }

        FileDetailTable = dt;
        StatusMessage = $"{SelectedFrameworkName} — {files.Count} files × {countries.Count} countries.";
    }

    private string BuildCellStatus(
        CanonicalFile file,
        CountryFilePresence? presence,
        Dictionary<int, string?> masterHashes)
    {
        if (presence == null) return "✗";

        if (file.IsDxdb) return "dxdb";
        if (file.IsFinancialData) return "fin";

        // Binary compare results
        if (presence.BinaryHash != null)
        {
            if (masterHashes.TryGetValue(file.Id, out var masterHash) && masterHash != null)
                return presence.BinaryHash == masterHash ? "=" : $"≠ {presence.CountryCode}";
            return "✓";   // present but master has no hash
        }

        return "✓";
    }
}
