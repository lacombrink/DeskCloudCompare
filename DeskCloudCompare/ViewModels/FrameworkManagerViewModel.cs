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

public partial class FrameworkManagerViewModel : ObservableObject
{
    private readonly AppDbContext _db;
    private readonly FrameworkManagerScanService _scanService;

    [ObservableProperty] private string _masterFolderPath = string.Empty;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _statusMessage = "Not yet scanned.";
    [ObservableProperty] private DataTable? _typeMatrixTable;
    [ObservableProperty] private DataTable? _fileDetailTable;
    [ObservableProperty] private string _fileDetailHeader = "File Detail  (double-click a type group row above)";
    [ObservableProperty] private FrameworkTypeGroup _selectedTypeGroup;
    [ObservableProperty] private string _selectedTypeGroupName = string.Empty;
    [ObservableProperty] private bool _hasBinaryCompareData;
    [ObservableProperty] private bool _showIssuesOnly;

    public bool IsNotBusy => !IsBusy;

    private List<string> _frameworkNames = new();
    private CancellationTokenSource? _cts;
    private HashSet<(FrameworkTypeGroup, string, string)> _exceptions = new(ExceptionComparer3.Instance);

    public HashSet<string> TypeGroupPresentFrameworks { get; private set; } =
        new(StringComparer.OrdinalIgnoreCase);

    public FrameworkManagerViewModel(AppDbContext db, FrameworkManagerScanService scanService)
    {
        _db = db;
        _scanService = scanService;
    }

    partial void OnIsBusyChanged(bool value)
    {
        OnPropertyChanged(nameof(IsNotBusy));
        BinaryCompareCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedTypeGroupChanged(FrameworkTypeGroup value) =>
        BinaryCompareCommand.NotifyCanExecuteChanged();

    partial void OnShowIssuesOnlyChanged(bool value)
    {
        if (_frameworkNames.Count > 0)
            _ = BuildFileDetailTableAsync(_selectedTypeGroup);
    }

    public async Task LoadAsync()
    {
        var settings = await _db.FrameworkManagerSettings.FirstOrDefaultAsync();
        if (settings != null)
            MasterFolderPath = settings.MasterFolderPath;

        await LoadExceptionsAsync();

        var hasData = await _db.MasterFrameworkEntries.AnyAsync();
        if (hasData)
            await BuildTypeMatrixAsync();
        else
            StatusMessage = "No scan data. Configure master folder and click Scan.";
    }

    private async Task LoadExceptionsAsync()
    {
        var all = await _db.MasterFileExceptions.ToListAsync();
        _exceptions = all
            .Select(e => (e.TypeGroup, e.RelativePath, e.FrameworkCanonicalName))
            .ToHashSet(ExceptionComparer3.Instance);
    }

    // -----------------------------------------------------------------------
    // Scan
    // -----------------------------------------------------------------------

    [RelayCommand]
    private void BrowseMasterFolder()
    {
        var dialog = new OpenFolderDialog { Title = "Select the 999.ZZ master folder" };
        if (dialog.ShowDialog() == true)
            MasterFolderPath = dialog.FolderName;
    }

    [RelayCommand(CanExecute = nameof(IsNotBusy))]
    private async Task Scan()
    {
        if (string.IsNullOrWhiteSpace(MasterFolderPath) || !Directory.Exists(MasterFolderPath))
        {
            MessageBox.Show("Please select a valid master folder first.", "Scan",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        IsBusy = true;
        FileDetailTable = null;
        SelectedTypeGroupName = string.Empty;
        HasBinaryCompareData = false;
        ShowIssuesOnly = false;
        _cts = new CancellationTokenSource();

        try
        {
            var progress = new Progress<string>(msg => StatusMessage = msg);
            await _scanService.ScanAsync(MasterFolderPath, progress, _cts.Token);
            await LoadExceptionsAsync();
            await BuildTypeMatrixAsync();
        }
        catch (OperationCanceledException) { StatusMessage = "Scan cancelled."; }
        catch (Exception ex)
        {
            MessageBox.Show($"Scan failed: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            StatusMessage = "Scan failed.";
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void CancelScan() => _cts?.Cancel();

    // -----------------------------------------------------------------------
    // Type group selection → file detail
    // -----------------------------------------------------------------------

    [RelayCommand(CanExecute = nameof(IsNotBusy))]
    private async Task SelectTypeGroup(string typeGroupName)
    {
        if (!Enum.TryParse<FrameworkTypeGroup>(typeGroupName, out var tg)) return;
        _selectedTypeGroup = tg;
        SelectedTypeGroupName = typeGroupName;
        HasBinaryCompareData = false;
        ShowIssuesOnly = false;
        await BuildFileDetailTableAsync(tg);
    }

    // -----------------------------------------------------------------------
    // Binary compare
    // -----------------------------------------------------------------------

    [RelayCommand(CanExecute = nameof(CanRunBinaryCompare))]
    private async Task BinaryCompare()
    {
        IsBusy = true;
        _cts = new CancellationTokenSource();
        try
        {
            var progress = new Progress<string>(msg => StatusMessage = msg);
            // Use the first framework in the type group as the reference (IFRS+ for IFRS, FRS102_Company for FRS etc.)
            var refFramework = _frameworkNames.FirstOrDefault() ?? string.Empty;
            await _scanService.BinaryCompareAsync(_selectedTypeGroup, refFramework, progress, _cts.Token);
            HasBinaryCompareData = true;
            await BuildFileDetailTableAsync(_selectedTypeGroup);
        }
        catch (OperationCanceledException) { StatusMessage = "Binary compare cancelled."; }
        catch (Exception ex)
        {
            MessageBox.Show($"Binary compare failed: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally { IsBusy = false; }
    }

    private bool CanRunBinaryCompare() => IsNotBusy && _frameworkNames.Count > 0;

    // -----------------------------------------------------------------------
    // Exception marking
    // -----------------------------------------------------------------------

    private async Task AddExceptionAsync(FrameworkTypeGroup tg, string relPath, string frameworkName)
    {
        var key = (tg, relPath, frameworkName);
        if (_exceptions.Contains(key)) return;
        _db.MasterFileExceptions.Add(new MasterFileException
        {
            TypeGroup = tg,
            RelativePath = relPath,
            FrameworkCanonicalName = frameworkName
        });
        _exceptions.Add(key);
    }

    public async Task MarkExceptionAsync(int fileId, string frameworkName)
    {
        var file = await _db.MasterCanonicalFiles.FindAsync(fileId);
        if (file == null) return;
        await AddExceptionAsync(file.TypeGroup, file.RelativePath, frameworkName);
        await _db.SaveChangesAsync();
        await BuildFileDetailTableAsync(_selectedTypeGroup);
    }

    public async Task MarkRowExceptionsAsync(int fileId, IEnumerable<string> frameworkNames)
    {
        var file = await _db.MasterCanonicalFiles.FindAsync(fileId);
        if (file == null) return;
        foreach (var fw in frameworkNames)
            await AddExceptionAsync(file.TypeGroup, file.RelativePath, fw);
        await _db.SaveChangesAsync();
        await BuildFileDetailTableAsync(_selectedTypeGroup);
    }

    public async Task RemoveExceptionAsync(int fileId, string frameworkName)
    {
        var file = await _db.MasterCanonicalFiles.FindAsync(fileId);
        if (file == null) return;
        var ex = await _db.MasterFileExceptions.FirstOrDefaultAsync(e =>
            e.TypeGroup == file.TypeGroup &&
            e.RelativePath == file.RelativePath &&
            e.FrameworkCanonicalName == frameworkName);
        if (ex != null)
        {
            _db.MasterFileExceptions.Remove(ex);
            _exceptions.Remove((file.TypeGroup, file.RelativePath, frameworkName));
            await _db.SaveChangesAsync();
        }
        await BuildFileDetailTableAsync(_selectedTypeGroup);
    }

    public async Task RemoveRowExceptionsAsync(int fileId, IEnumerable<string> frameworkNames)
    {
        var file = await _db.MasterCanonicalFiles.FindAsync(fileId);
        if (file == null) return;
        foreach (var fw in frameworkNames)
        {
            var ex = await _db.MasterFileExceptions.FirstOrDefaultAsync(e =>
                e.TypeGroup == file.TypeGroup &&
                e.RelativePath == file.RelativePath &&
                e.FrameworkCanonicalName == fw);
            if (ex != null)
            {
                _db.MasterFileExceptions.Remove(ex);
                _exceptions.Remove((file.TypeGroup, file.RelativePath, fw));
            }
        }
        await _db.SaveChangesAsync();
        await BuildFileDetailTableAsync(_selectedTypeGroup);
    }

    /// <summary>
    /// Validate that no framework has more than one of the selected files, then collapse
    /// all slave files into the master: reassign presences, create file aliases, remove slaves.
    /// Returns (true, "") on success, or (false, errorMessage) on validation failure.
    /// </summary>
    public async Task<(bool success, string error)> ValidateAndCreateCanonicalAliasAsync(
        List<int> fileIds, int masterFileId)
    {
        if (fileIds.Count < 2)
            return (false, "Select at least two files to create a canonical alias.");

        // Validate: no framework should have more than one of the selected files
        var presences = await _db.MasterFilePresences
            .Where(p => fileIds.Contains(p.MasterCanonicalFileId))
            .ToListAsync();

        var conflict = presences
            .GroupBy(p => p.MasterFrameworkEntryId)
            .FirstOrDefault(g => g.Select(p => p.MasterCanonicalFileId).Distinct().Count() > 1);

        if (conflict != null)
            return (false, "Invalid selection for canonical creation: at least one framework has more than one of the selected files in the same folder.");

        var masterFile = await _db.MasterCanonicalFiles.FindAsync(masterFileId);
        if (masterFile == null)
            return (false, "Master file not found.");

        var slaveFileIds = fileIds.Where(id => id != masterFileId).ToList();
        var slaveFiles = await _db.MasterCanonicalFiles
            .Where(f => slaveFileIds.Contains(f.Id))
            .Include(f => f.Presences)
            .ToListAsync();

        foreach (var slave in slaveFiles)
        {
            // Build alias: (folderPath, slaveFileName) → masterFileName
            var folderPath = Path.GetDirectoryName(slave.RelativePath)
                             ?.Replace('/', Path.DirectorySeparatorChar) ?? string.Empty;
            var slaveFileName = Path.GetFileName(slave.RelativePath);
            var masterFileName = Path.GetFileName(masterFile.RelativePath);

            var existingAlias = await _db.MasterFileAliases.FirstOrDefaultAsync(a =>
                a.FolderPath == folderPath && a.ActualFileName == slaveFileName);

            if (existingAlias == null)
            {
                _db.MasterFileAliases.Add(new MasterFileAlias
                {
                    FolderPath = folderPath,
                    ActualFileName = slaveFileName,
                    CanonicalFileName = masterFileName
                });
            }
            else
            {
                existingAlias.CanonicalFileName = masterFileName;
            }

            // Reassign each presence from slave → master (skip if master already present there)
            var slavePresences = slave.Presences.ToList();
            var masterPresenceFrameworkIds = presences
                .Where(p => p.MasterCanonicalFileId == masterFileId)
                .Select(p => p.MasterFrameworkEntryId)
                .ToHashSet();

            foreach (var presence in slavePresences)
            {
                if (masterPresenceFrameworkIds.Contains(presence.MasterFrameworkEntryId))
                {
                    // Master already present in this framework — just drop the slave presence
                    _db.MasterFilePresences.Remove(presence);
                }
                else
                {
                    presence.MasterCanonicalFileId = masterFileId;
                }
            }

            _db.MasterCanonicalFiles.Remove(slave);
        }

        await _db.SaveChangesAsync();
        await BuildFileDetailTableAsync(_selectedTypeGroup);
        return (true, string.Empty);
    }

    public bool IsException(FrameworkTypeGroup tg, string relPath, string frameworkName) =>
        _exceptions.Contains((tg, relPath, frameworkName));

    // -----------------------------------------------------------------------
    // DataTable builders
    // -----------------------------------------------------------------------

    private async Task BuildTypeMatrixAsync()
    {
        var allEntries = await _db.MasterFrameworkEntries
            .OrderBy(e => e.TypeGroup).ThenBy(e => e.SortOrder)
            .ToListAsync();

        var typeGroups = Enum.GetValues<FrameworkTypeGroup>();

        var dt = new DataTable();
        dt.Columns.Add("_TypeGroup", typeof(string));
        dt.Columns.Add("Type Group", typeof(string));
        foreach (var e in allEntries)
            dt.Columns.Add(e.CanonicalName, typeof(bool));

        foreach (var tg in typeGroups)
        {
            var row = dt.NewRow();
            row["_TypeGroup"] = tg.ToString();
            row["Type Group"] = tg.ToString();
            foreach (var e in allEntries)
                row[e.CanonicalName] = e.TypeGroup == tg;
            dt.Rows.Add(row);
        }

        TypeMatrixTable = dt;
        StatusMessage = $"Matrix ready — {allEntries.Count} frameworks in {typeGroups.Length} type groups.";
    }

    private async Task BuildFileDetailTableAsync(FrameworkTypeGroup typeGroup)
    {
        var entries = await _db.MasterFrameworkEntries
            .Where(e => e.TypeGroup == typeGroup)
            .OrderBy(e => e.SortOrder)
            .ToListAsync();

        TypeGroupPresentFrameworks = entries
            .Select(e => e.CanonicalName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        _frameworkNames = entries.Select(e => e.CanonicalName).ToList();

        var entryIds = entries.Select(e => e.Id).ToList();

        var files = await _db.MasterCanonicalFiles
            .Where(f => f.TypeGroup == typeGroup)
            .OrderBy(f => f.RelativePath)
            .ToListAsync();

        var presences = await _db.MasterFilePresences
            .Where(p => entryIds.Contains(p.MasterFrameworkEntryId))
            .ToListAsync();

        // Reference hashes = first entry (sorted by SortOrder) that has hashes
        var refEntry = entries.FirstOrDefault(e =>
            presences.Any(p => p.MasterFrameworkEntryId == e.Id && p.BinaryHash != null));
        var refHashes = refEntry == null
            ? new Dictionary<int, string?>()
            : presences
                .Where(p => p.MasterFrameworkEntryId == refEntry.Id && p.BinaryHash != null)
                .ToDictionary(p => p.MasterCanonicalFileId, p => p.BinaryHash);

        var dt = new DataTable();
        dt.Columns.Add("_FileId", typeof(int));
        dt.Columns.Add("File", typeof(string));
        dt.Columns.Add("_IsDxdb", typeof(bool));
        dt.Columns.Add("_IsFinancialData", typeof(bool));
        foreach (var e in entries)
            dt.Columns.Add(e.CanonicalName, typeof(string));

        foreach (var file in files)
        {
            var cellValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            bool hasIssue = false;

            foreach (var e in entries)
            {
                var presence = presences.FirstOrDefault(
                    p => p.MasterCanonicalFileId == file.Id && p.MasterFrameworkEntryId == e.Id);

                string cell;
                if (presence != null)
                {
                    cell = BuildPresentCell(file, presence, refHashes);
                    if (cell == "≠") hasIssue = true;
                }
                else if (IsException(typeGroup, file.RelativePath, e.CanonicalName))
                {
                    cell = "E";
                }
                else
                {
                    cell = "X";
                    hasIssue = true;
                }
                cellValues[e.CanonicalName] = cell;
            }

            if (ShowIssuesOnly && !hasIssue) continue;

            var row = dt.NewRow();
            row["_FileId"] = file.Id;
            row["File"] = file.RelativePath;
            row["_IsDxdb"] = file.IsDxdb;
            row["_IsFinancialData"] = file.IsFinancialData;
            foreach (var e in entries)
                row[e.CanonicalName] = cellValues[e.CanonicalName];
            dt.Rows.Add(row);
        }

        FileDetailTable = dt;
        FileDetailHeader = $"File Detail — {typeGroup} ({entries.Count} frameworks, {files.Count} files)";
        StatusMessage = $"{typeGroup} — {files.Count} canonical files × {entries.Count} frameworks.";

        BinaryCompareCommand.NotifyCanExecuteChanged();
    }

    private string BuildPresentCell(
        MasterCanonicalFile file,
        MasterFilePresence presence,
        Dictionary<int, string?> refHashes)
    {
        if (file.IsDxdb) return "dxdb";
        if (file.IsFinancialData) return "fin";

        if (presence.BinaryHash != null)
        {
            if (refHashes.TryGetValue(file.Id, out var refHash) && refHash != null)
                return presence.BinaryHash == refHash ? "=" : "≠";
            return "✓";
        }
        return "✓";
    }
}

file sealed class ExceptionComparer3
    : IEqualityComparer<(FrameworkTypeGroup tg, string path, string fw)>
{
    public static readonly ExceptionComparer3 Instance = new();
    public bool Equals((FrameworkTypeGroup tg, string path, string fw) x,
                       (FrameworkTypeGroup tg, string path, string fw) y) =>
        x.tg == y.tg
        && string.Equals(x.path, y.path, StringComparison.OrdinalIgnoreCase)
        && string.Equals(x.fw, y.fw, StringComparison.OrdinalIgnoreCase);
    public int GetHashCode((FrameworkTypeGroup tg, string path, string fw) obj) =>
        HashCode.Combine((int)obj.tg, obj.path.ToUpperInvariant(), obj.fw.ToUpperInvariant());
}
