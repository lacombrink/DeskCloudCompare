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
using System.Windows.Controls;

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
    [ObservableProperty] private bool _hasBinaryCompareData;
    [ObservableProperty] private bool _showIssuesOnly;
    [ObservableProperty] private string _fileDetailHeader = "File Detail  (double-click a framework row above)";

    public bool IsNotBusy => !IsBusy;

    private List<string> _countryCodes = new();
    private CancellationTokenSource? _cts;

    // In-memory exception set: (frameworkName, frameworkCategory, relativeFilePath, countryCode)
    // Keyed by stable names so exceptions survive a full rescan.
    private HashSet<(string, FrameworkCategory, string, string)> _exceptions = new(
        ExceptionComparer.Instance);

    public CountryManagerViewModel(AppDbContext db, CountryManagerScanService scanService)
    {
        _db = db;
        _scanService = scanService;
    }

    partial void OnIsBusyChanged(bool value)
    {
        OnPropertyChanged(nameof(IsNotBusy));
        BinaryCompareCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedFrameworkIdChanged(int value) =>
        BinaryCompareCommand.NotifyCanExecuteChanged();

    partial void OnMasterCountryCodeChanged(string value) =>
        BinaryCompareCommand.NotifyCanExecuteChanged();

    partial void OnShowIssuesOnlyChanged(bool value)
    {
        if (SelectedFrameworkId > 0)
            _ = BuildFileDetailTableAsync(SelectedFrameworkId);
    }

    public async Task LoadAsync()
    {
        var settings = await _db.CountryManagerSettings.FirstOrDefaultAsync();
        if (settings != null)
        {
            RootFolderPath = settings.RootFolderPath;
            MasterCountryCode = settings.MasterCountryCode ?? string.Empty;
        }

        await LoadExceptionsAsync();

        var hasData = await _db.CountryEntries.AnyAsync();
        if (hasData)
            await BuildFrameworkMatrixAsync();
        else
            StatusMessage = "No scan data. Configure root folder and click Scan.";
    }

    private async Task LoadExceptionsAsync()
    {
        var all = await _db.CountryFileExceptions.ToListAsync();
        _exceptions = all
            .Select(e => (e.FrameworkName, e.FrameworkCategory, e.RelativePath, e.CountryCode))
            .ToHashSet(ExceptionComparer.Instance);
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
        HasBinaryCompareData = false;
        ShowIssuesOnly = false;
        _cts = new CancellationTokenSource();

        try
        {
            var progress = new Progress<string>(msg => StatusMessage = msg);
            await _scanService.ScanAsync(RootFolderPath,
                string.IsNullOrWhiteSpace(MasterCountryCode) ? null : MasterCountryCode,
                progress, _cts.Token);
            await LoadExceptionsAsync();
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
    // Global views — all issues / all exceptions across every framework
    // -----------------------------------------------------------------------

    [RelayCommand(CanExecute = nameof(IsNotBusy))]
    private async Task ShowAllIssues()
    {
        IsBusy = true;
        try
        {
            await BuildGlobalTableAsync(GlobalViewMode.Issues);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand(CanExecute = nameof(IsNotBusy))]
    private async Task ShowAllExceptions()
    {
        IsBusy = true;
        try
        {
            await BuildGlobalTableAsync(GlobalViewMode.Exceptions);
        }
        finally { IsBusy = false; }
    }

    private enum GlobalViewMode { Issues, Exceptions }

    private async Task BuildGlobalTableAsync(GlobalViewMode mode)
    {
        var allCountries = await _db.CountryEntries.OrderBy(c => c.SortOrder).ToListAsync();
        var countries = allCountries
            .Where(c => !c.Code.Equals(MasterCountryCode, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var frameworks = await _db.CanonicalFrameworks
            .OrderBy(f => f.Category).ThenBy(f => f.Name)
            .ToListAsync();

        var allFwPresences = await _db.CountryFrameworkPresences.ToListAsync();

        var allFiles = await _db.CanonicalFiles
            .OrderBy(f => f.RelativePath)
            .ToListAsync();

        var allFilePresences = await _db.CountryFilePresences.ToListAsync();

        var masterHashes = allFilePresences
            .Where(p => p.CountryCode.Equals(MasterCountryCode, StringComparison.OrdinalIgnoreCase)
                     && p.BinaryHash != null)
            .ToDictionary(p => p.CanonicalFileId, p => p.BinaryHash);

        var dt = new DataTable();
        dt.Columns.Add("_FileId", typeof(int));
        dt.Columns.Add("Framework", typeof(string));
        dt.Columns.Add("File", typeof(string));
        dt.Columns.Add("_IsDxdb", typeof(bool));
        dt.Columns.Add("_IsFinancialData", typeof(bool));
        foreach (var c in countries)
            dt.Columns.Add(c.Code, typeof(string));

        foreach (var fw in frameworks)
        {
            var fwPresentCodes = allFwPresences
                .Where(p => p.CanonicalFrameworkId == fw.Id)
                .Select(p => p.CountryCode)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var files = allFiles.Where(f => f.CanonicalFrameworkId == fw.Id);

            foreach (var file in files)
            {
                var cellValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                bool hasIssue = false, hasException = false;

                foreach (var c in countries)
                {
                    var applicable = fwPresentCodes.Contains(c.Code);
                    var presence = allFilePresences.FirstOrDefault(
                        p => p.CanonicalFileId == file.Id && p.CountryCode == c.Code);

                    string cell;
                    if (!applicable)
                    {
                        cell = "";
                    }
                    else if (presence != null)
                    {
                        cell = BuildPresentCellStatus(file, presence, masterHashes);
                        if (cell == "≠") hasIssue = true;
                    }
                    else if (IsException(fw.Name, fw.Category, file.RelativePath, c.Code))
                    {
                        cell = "E";
                        hasException = true;
                    }
                    else
                    {
                        cell = "X";
                        hasIssue = true;
                    }
                    cellValues[c.Code] = cell;
                }

                bool include = mode == GlobalViewMode.Issues
                    ? hasIssue
                    : hasException;

                if (!include) continue;

                var row = dt.NewRow();
                row["_FileId"] = file.Id;
                row["Framework"] = $"{fw.Category}: {fw.Name}";
                row["File"] = file.RelativePath;
                row["_IsDxdb"] = file.IsDxdb;
                row["_IsFinancialData"] = file.IsFinancialData;
                foreach (var c in countries)
                    row[c.Code] = cellValues[c.Code];
                dt.Rows.Add(row);
            }
        }

        SelectedFrameworkId = 0;
        SelectedFrameworkName = string.Empty;

        if (mode == GlobalViewMode.Issues)
        {
            FileDetailHeader = $"All Issues — {dt.Rows.Count} files with missing or mismatched content";
            StatusMessage = $"Found {dt.Rows.Count} files with issues across all frameworks.";
        }
        else
        {
            FileDetailHeader = $"All Exceptions — {dt.Rows.Count} files with marked exceptions";
            StatusMessage = $"Found {dt.Rows.Count} files with exceptions across all frameworks.";
        }

        FileDetailTable = dt;
    }

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
            HasBinaryCompareData = true;
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
    // Exception marking (called from view code-behind)
    // -----------------------------------------------------------------------

    // Resolve the framework name/category for the file currently being marked.
    private async Task<(string name, FrameworkCategory category)?> GetFrameworkForFileAsync(int fileId)
    {
        var file = await _db.CanonicalFiles
            .Include(f => f.CanonicalFramework)
            .FirstOrDefaultAsync(f => f.Id == fileId);
        if (file == null) return null;
        return (file.CanonicalFramework.Name, file.CanonicalFramework.Category);
    }

    private async Task AddExceptionAsync(
        string frameworkName, FrameworkCategory category, string relPath, string countryCode)
    {
        var key = (frameworkName, category, relPath, countryCode);
        if (_exceptions.Contains(key)) return;

        _db.CountryFileExceptions.Add(new CountryFileException
        {
            FrameworkName = frameworkName,
            FrameworkCategory = category,
            RelativePath = relPath,
            CountryCode = countryCode
        });
        _exceptions.Add(key);
    }

    /// <summary>Mark one specific (file, country) pair as an exception.</summary>
    public async Task MarkExceptionAsync(int fileId, string countryCode)
    {
        var file = await _db.CanonicalFiles
            .Include(f => f.CanonicalFramework)
            .FirstOrDefaultAsync(f => f.Id == fileId);
        if (file == null) return;

        await AddExceptionAsync(
            file.CanonicalFramework.Name, file.CanonicalFramework.Category,
            file.RelativePath, countryCode);

        await _db.SaveChangesAsync();
        await BuildFileDetailTableAsync(SelectedFrameworkId);
    }

    /// <summary>Mark all X cells in the file's row as exceptions.</summary>
    public async Task MarkRowExceptionsAsync(int fileId, IEnumerable<string> missingCountryCodes)
    {
        var file = await _db.CanonicalFiles
            .Include(f => f.CanonicalFramework)
            .FirstOrDefaultAsync(f => f.Id == fileId);
        if (file == null) return;

        foreach (var cc in missingCountryCodes)
            await AddExceptionAsync(
                file.CanonicalFramework.Name, file.CanonicalFramework.Category,
                file.RelativePath, cc);

        await _db.SaveChangesAsync();
        await BuildFileDetailTableAsync(SelectedFrameworkId);
    }

    /// <summary>
    /// Across ALL frameworks, wherever a file with the same name is absent for the
    /// given countries (and the framework IS applicable there), mark as exception.
    /// </summary>
    public async Task MarkAllFrameworksExceptionsAsync(string fileName, IEnumerable<string> missingCountryCodes)
    {
        var codes = missingCountryCodes.ToList();

        var matchingFiles = await _db.CanonicalFiles
            .Where(f => f.FileName == fileName)
            .Include(f => f.CanonicalFramework)
            .ToListAsync();

        var fileIds = matchingFiles.Select(f => f.Id).ToList();
        var filePres = await _db.CountryFilePresences
            .Where(p => fileIds.Contains(p.CanonicalFileId))
            .ToListAsync();

        var fwIds = matchingFiles.Select(f => f.CanonicalFrameworkId).Distinct().ToList();
        var fwPres = await _db.CountryFrameworkPresences
            .Where(p => fwIds.Contains(p.CanonicalFrameworkId))
            .ToListAsync();

        foreach (var file in matchingFiles)
        {
            var presentCodes = filePres
                .Where(p => p.CanonicalFileId == file.Id)
                .Select(p => p.CountryCode)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var fwCountries = fwPres
                .Where(p => p.CanonicalFrameworkId == file.CanonicalFrameworkId)
                .Select(p => p.CountryCode)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var cc in codes)
            {
                if (!fwCountries.Contains(cc)) continue;   // framework not applicable — skip
                if (presentCodes.Contains(cc)) continue;   // file exists there — skip
                await AddExceptionAsync(
                    file.CanonicalFramework.Name, file.CanonicalFramework.Category,
                    file.RelativePath, cc);
            }
        }

        await _db.SaveChangesAsync();
        await BuildFileDetailTableAsync(SelectedFrameworkId);
    }

    private bool IsException(string frameworkName, FrameworkCategory category, string relPath, string countryCode) =>
        _exceptions.Contains((frameworkName, category, relPath, countryCode));

    /// <summary>
    /// Remove a single exception from the country manager file grid.
    /// </summary>
    public async Task RemoveExceptionAsync(int fileId, string countryCode)
    {
        var file = await _db.CanonicalFiles
            .Include(f => f.CanonicalFramework)
            .FirstOrDefaultAsync(f => f.Id == fileId);
        if (file == null) return;

        var ex = await _db.CountryFileExceptions.FirstOrDefaultAsync(e =>
            e.FrameworkName == file.CanonicalFramework.Name &&
            e.FrameworkCategory == file.CanonicalFramework.Category &&
            e.RelativePath == file.RelativePath &&
            e.CountryCode == countryCode);

        if (ex != null)
        {
            _db.CountryFileExceptions.Remove(ex);
            _exceptions.Remove((file.CanonicalFramework.Name, file.CanonicalFramework.Category,
                file.RelativePath, countryCode));
            await _db.SaveChangesAsync();
        }
        await BuildFileDetailTableAsync(SelectedFrameworkId);
    }

    /// <summary>
    /// Remove all exceptions for a file in the supplied country codes.
    /// </summary>
    public async Task RemoveRowExceptionsAsync(int fileId, IEnumerable<string> countryCodes)
    {
        var file = await _db.CanonicalFiles
            .Include(f => f.CanonicalFramework)
            .FirstOrDefaultAsync(f => f.Id == fileId);
        if (file == null) return;

        foreach (var cc in countryCodes)
        {
            var ex = await _db.CountryFileExceptions.FirstOrDefaultAsync(e =>
                e.FrameworkName == file.CanonicalFramework.Name &&
                e.FrameworkCategory == file.CanonicalFramework.Category &&
                e.RelativePath == file.RelativePath &&
                e.CountryCode == cc);
            if (ex != null)
            {
                _db.CountryFileExceptions.Remove(ex);
                _exceptions.Remove((file.CanonicalFramework.Name, file.CanonicalFramework.Category,
                    file.RelativePath, cc));
            }
        }
        await _db.SaveChangesAsync();
        await BuildFileDetailTableAsync(SelectedFrameworkId);
    }

    /// <summary>
    /// Merge selected canonical files into a master, reassigning all country presences.
    /// Also creates a MasterFileAlias entry so future Framework Manager scans auto-merge.
    /// Returns (true, "") on success or (false, errorMessage) on failure.
    /// </summary>
    public async Task<(bool success, string error)> ValidateAndCreateCanonicalAliasAsync(
        List<int> fileIds, int masterFileId)
    {
        if (fileIds.Count < 2)
            return (false, "Select at least two files to create a canonical alias.");

        // Validate: no country should have both files for the same framework
        var presences = await _db.CountryFilePresences
            .Where(p => fileIds.Contains(p.CanonicalFileId))
            .ToListAsync();

        var conflict = presences
            .GroupBy(p => p.CountryCode)
            .FirstOrDefault(g => g.Select(p => p.CanonicalFileId).Distinct().Count() > 1);

        if (conflict != null)
            return (false, "Invalid selection for canonical creation: at least one country has more than one of the selected files.");

        var masterFile = await _db.CanonicalFiles
            .Include(f => f.CanonicalFramework)
            .FirstOrDefaultAsync(f => f.Id == masterFileId);
        if (masterFile == null)
            return (false, "Master file not found.");

        var slaveFileIds = fileIds.Where(id => id != masterFileId).ToList();
        var slaveFiles = await _db.CanonicalFiles
            .Where(f => slaveFileIds.Contains(f.Id))
            .Include(f => f.Presences)
            .ToListAsync();

        var masterCountryCodes = presences
            .Where(p => p.CanonicalFileId == masterFileId)
            .Select(p => p.CountryCode)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var slave in slaveFiles)
        {
            // Create a MasterFileAlias so future Framework Manager scans auto-merge
            var folderPath = Path.GetDirectoryName(slave.RelativePath)
                             ?.Replace('/', Path.DirectorySeparatorChar) ?? string.Empty;
            var slaveFileName = Path.GetFileName(slave.RelativePath);
            var masterFileName = Path.GetFileName(masterFile.RelativePath);

            var existingAlias = await _db.MasterFileAliases.FirstOrDefaultAsync(a =>
                a.FolderPath == folderPath && a.ActualFileName == slaveFileName);

            if (existingAlias == null)
                _db.MasterFileAliases.Add(new MasterFileAlias
                {
                    FolderPath = folderPath,
                    ActualFileName = slaveFileName,
                    CanonicalFileName = masterFileName
                });
            else
                existingAlias.CanonicalFileName = masterFileName;

            // Reassign or remove country presences
            foreach (var presence in slave.Presences.ToList())
            {
                if (masterCountryCodes.Contains(presence.CountryCode))
                    _db.CountryFilePresences.Remove(presence);
                else
                    presence.CanonicalFileId = masterFileId;
            }

            _db.CanonicalFiles.Remove(slave);
        }

        await _db.SaveChangesAsync();
        if (SelectedFrameworkId > 0)
            await BuildFileDetailTableAsync(SelectedFrameworkId);
        return (true, string.Empty);
    }

    /// <summary>
    /// Countries that actually have the currently-selected framework.
    /// Used by the view to decide whether to show the right-click exception menu.
    /// </summary>
    public HashSet<string> FrameworkPresentCountries { get; private set; } =
        new(StringComparer.OrdinalIgnoreCase);

    // -----------------------------------------------------------------------
    // DataTable builders
    // -----------------------------------------------------------------------

    private async Task BuildFrameworkMatrixAsync()
    {
        var allCountries = await _db.CountryEntries.OrderBy(c => c.SortOrder).ToListAsync();
        // Exclude the master country — it's only used as the binary compare reference
        var countries = allCountries
            .Where(c => !c.Code.Equals(MasterCountryCode, StringComparison.OrdinalIgnoreCase))
            .ToList();
        _countryCodes = countries.Select(c => c.Code).ToList();

        var frameworks = await _db.CanonicalFrameworks
            .OrderBy(f => f.Category)
            .ThenBy(f => f.Name)
            .ToListAsync();

        var presences = await _db.CountryFrameworkPresences.ToListAsync();

        var dt = new DataTable();
        dt.Columns.Add("_Id", typeof(int));
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
        var allCountries = await _db.CountryEntries.OrderBy(c => c.SortOrder).ToListAsync();
        var countries = allCountries
            .Where(c => !c.Code.Equals(MasterCountryCode, StringComparison.OrdinalIgnoreCase))
            .ToList();

        // Load framework to get its stable name/category for exception lookups
        var framework = await _db.CanonicalFrameworks.FindAsync(frameworkId);
        if (framework == null) return;
        var fwName = framework.Name;
        var fwCategory = framework.Category;

        // Which countries actually have this framework (determines whether a missing file is an issue)
        var frameworkPresences = await _db.CountryFrameworkPresences
            .Where(p => p.CanonicalFrameworkId == frameworkId)
            .ToListAsync();
        FrameworkPresentCountries = frameworkPresences
            .Select(p => p.CountryCode)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var files = await _db.CanonicalFiles
            .Where(f => f.CanonicalFrameworkId == frameworkId)
            .OrderBy(f => f.RelativePath)
            .ToListAsync();

        var filePresences = await _db.CountryFilePresences
            .Where(p => p.CanonicalFile.CanonicalFrameworkId == frameworkId)
            .ToListAsync();

        var masterHashes = new Dictionary<int, string?>();
        if (!string.IsNullOrWhiteSpace(MasterCountryCode))
        {
            foreach (var p in filePresences.Where(p => p.CountryCode == MasterCountryCode))
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
            var cellValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var hasIssue = false;

            foreach (var c in countries)
            {
                var frameworkApplicable = FrameworkPresentCountries.Contains(c.Code);
                var presence = filePresences.FirstOrDefault(
                    p => p.CanonicalFileId == file.Id && p.CountryCode == c.Code);

                string cell;
                if (!frameworkApplicable)
                {
                    // Framework not applicable to this country — leave blank, never an issue
                    cell = "";
                }
                else if (presence != null)
                {
                    cell = BuildPresentCellStatus(file, presence, masterHashes);
                    if (cell.StartsWith("≠")) hasIssue = true;
                }
                else if (IsException(fwName, fwCategory, file.RelativePath, c.Code))
                {
                    // Framework applicable, file intentionally absent — mark with E
                    cell = "E";
                }
                else
                {
                    // Framework applicable, file missing, no exception → real issue
                    cell = "X";
                    hasIssue = true;
                }

                cellValues[c.Code] = cell;
            }

            if (ShowIssuesOnly && !hasIssue) continue;

            var row = dt.NewRow();
            row["_FileId"] = file.Id;
            row["File"] = file.RelativePath;
            row["_IsDxdb"] = file.IsDxdb;
            row["_IsFinancialData"] = file.IsFinancialData;
            foreach (var c in countries)
                row[c.Code] = cellValues[c.Code];

            dt.Rows.Add(row);
        }

        FileDetailTable = dt;
        FileDetailHeader = $"File Detail — {SelectedFrameworkName}";
        StatusMessage = $"{SelectedFrameworkName} — {files.Count} files × {countries.Count} countries.";
    }

    private string BuildPresentCellStatus(
        CanonicalFile file,
        CountryFilePresence presence,
        Dictionary<int, string?> masterHashes)
    {
        if (file.IsDxdb) return "dxdb";
        if (file.IsFinancialData) return "fin";

        if (presence.BinaryHash != null)
        {
            if (masterHashes.TryGetValue(file.Id, out var masterHash) && masterHash != null)
                return presence.BinaryHash == masterHash ? "=" : "≠";
            return "✓";
        }

        return "✓";
    }
}

/// <summary>
/// Case-insensitive equality for (frameworkName, category, relPath, countryCode) tuples.
/// </summary>
file sealed class ExceptionComparer
    : IEqualityComparer<(string name, FrameworkCategory cat, string path, string country)>
{
    public static readonly ExceptionComparer Instance = new();

    public bool Equals(
        (string name, FrameworkCategory cat, string path, string country) x,
        (string name, FrameworkCategory cat, string path, string country) y) =>
        x.cat == y.cat
        && string.Equals(x.name, y.name, StringComparison.OrdinalIgnoreCase)
        && string.Equals(x.path, y.path, StringComparison.OrdinalIgnoreCase)
        && string.Equals(x.country, y.country, StringComparison.OrdinalIgnoreCase);

    public int GetHashCode((string name, FrameworkCategory cat, string path, string country) obj) =>
        HashCode.Combine(
            obj.cat,
            obj.name.ToUpperInvariant(),
            obj.path.ToUpperInvariant(),
            obj.country.ToUpperInvariant());
}
