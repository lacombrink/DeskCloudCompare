using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeskCloudCompare.Data;
using DeskCloudCompare.Models;
using DeskCloudCompare.Services;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.IO;
using System.Windows;

namespace DeskCloudCompare.ViewModels;

public partial class SubFrameworkManagerViewModel : ObservableObject
{
    private readonly AppDbContext _db;
    private readonly FrameworkManagerScanService _scanService;

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _statusMessage = "Use the Framework Manager tab to scan first.";
    [ObservableProperty] private DataTable? _subMatrixTable;
    [ObservableProperty] private DataTable? _fileDetailTable;
    [ObservableProperty] private string _fileDetailHeader = "File Detail  (double-click a sub-framework row above)";
    [ObservableProperty] private SubFrameworkGroup _selectedSubGroup;
    [ObservableProperty] private bool _hasBinaryCompareData;
    [ObservableProperty] private bool _showIssuesOnly;

    public bool IsNotBusy => !IsBusy;

    private List<string> _frameworkNames = new();
    private CancellationTokenSource? _cts;
    private HashSet<(SubFrameworkGroup, string, string)> _exceptions = new(SubExceptionComparer.Instance);

    public HashSet<string> SubGroupPresentFrameworks { get; private set; } =
        new(StringComparer.OrdinalIgnoreCase);

    public SubFrameworkManagerViewModel(AppDbContext db, FrameworkManagerScanService scanService)
    {
        _db = db;
        _scanService = scanService;
    }

    partial void OnIsBusyChanged(bool value)
    {
        OnPropertyChanged(nameof(IsNotBusy));
        BinaryCompareCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedSubGroupChanged(SubFrameworkGroup value) =>
        BinaryCompareCommand.NotifyCanExecuteChanged();

    partial void OnShowIssuesOnlyChanged(bool value)
    {
        if (_frameworkNames.Count > 0)
            _ = BuildFileDetailTableAsync(_selectedSubGroup);
    }

    public async Task LoadAsync()
    {
        await LoadExceptionsAsync();

        var hasData = await _db.MasterFrameworkEntries.AnyAsync();
        if (hasData)
            await BuildSubMatrixAsync();
        else
            StatusMessage = "No scan data. Use the Framework Manager tab to scan first.";
    }

    private async Task LoadExceptionsAsync()
    {
        var all = await _db.SubFrameworkFileExceptions.ToListAsync();
        _exceptions = all
            .Select(e => (e.SubGroup, e.RelativePath, e.FrameworkCanonicalName))
            .ToHashSet(SubExceptionComparer.Instance);
    }

    // -----------------------------------------------------------------------
    // Sub-group selection → file detail
    // -----------------------------------------------------------------------

    [RelayCommand(CanExecute = nameof(IsNotBusy))]
    private async Task SelectSubGroup(string subGroupName)
    {
        if (!Enum.TryParse<SubFrameworkGroup>(subGroupName, out var sg)) return;
        _selectedSubGroup = sg;
        HasBinaryCompareData = false;
        ShowIssuesOnly = false;
        await BuildFileDetailTableAsync(sg);
    }

    // -----------------------------------------------------------------------
    // Refresh — reload matrix after Framework Manager has scanned
    // -----------------------------------------------------------------------

    [RelayCommand(CanExecute = nameof(IsNotBusy))]
    private async Task Refresh()
    {
        IsBusy = true;
        try
        {
            FileDetailTable = null;
            HasBinaryCompareData = false;
            ShowIssuesOnly = false;
            _frameworkNames.Clear();
            await LoadExceptionsAsync();
            await BuildSubMatrixAsync();
        }
        finally { IsBusy = false; }
    }

    // -----------------------------------------------------------------------
    // Binary compare — hashes the parent type group
    // -----------------------------------------------------------------------

    [RelayCommand(CanExecute = nameof(CanRunBinaryCompare))]
    private async Task BinaryCompare()
    {
        IsBusy = true;
        _cts = new CancellationTokenSource();
        try
        {
            var typeGroup = GetParentTypeGroup(_selectedSubGroup);
            var progress = new Progress<string>(msg => StatusMessage = msg);
            await _scanService.BinaryCompareAsync(typeGroup, string.Empty, progress, _cts.Token);
            HasBinaryCompareData = true;
            await BuildFileDetailTableAsync(_selectedSubGroup);
        }
        catch (OperationCanceledException) { StatusMessage = "Binary compare cancelled."; }
        catch (Exception ex)
        {
            MessageBox.Show($"Binary compare failed: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void CancelOperation() => _cts?.Cancel();

    private bool CanRunBinaryCompare() => IsNotBusy && _frameworkNames.Count > 0;

    private static FrameworkTypeGroup GetParentTypeGroup(SubFrameworkGroup sg) =>
        sg switch
        {
            SubFrameworkGroup.FRS102    => FrameworkTypeGroup.FRS,
            SubFrameworkGroup.FRS102_1A => FrameworkTypeGroup.FRS,
            SubFrameworkGroup.FRS105    => FrameworkTypeGroup.FRS,
            SubFrameworkGroup.FRS_SORP  => FrameworkTypeGroup.FRS,
            SubFrameworkGroup.Charity   => FrameworkTypeGroup.FRS,
            SubFrameworkGroup.IFRS_Plus => FrameworkTypeGroup.IFRS,
            SubFrameworkGroup.IFRS_SME  => FrameworkTypeGroup.IFRS,
            SubFrameworkGroup.ASPE_Plus => FrameworkTypeGroup.IFRS,
            _                           => FrameworkTypeGroup.Legacy
        };

    // -----------------------------------------------------------------------
    // Exception marking
    // -----------------------------------------------------------------------

    private async Task AddExceptionAsync(SubFrameworkGroup sg, string relPath, string frameworkName)
    {
        var key = (sg, relPath, frameworkName);
        if (_exceptions.Contains(key)) return;
        _db.SubFrameworkFileExceptions.Add(new SubFrameworkFileException
        {
            SubGroup = sg,
            RelativePath = relPath,
            FrameworkCanonicalName = frameworkName
        });
        _exceptions.Add(key);
    }

    public async Task MarkExceptionAsync(int fileId, string frameworkName)
    {
        var file = await _db.MasterCanonicalFiles.FindAsync(fileId);
        if (file == null) return;
        await AddExceptionAsync(_selectedSubGroup, file.RelativePath, frameworkName);
        await _db.SaveChangesAsync();
        await BuildFileDetailTableAsync(_selectedSubGroup);
    }

    public async Task MarkRowExceptionsAsync(int fileId, IEnumerable<string> frameworkNames)
    {
        var file = await _db.MasterCanonicalFiles.FindAsync(fileId);
        if (file == null) return;
        foreach (var fw in frameworkNames)
            await AddExceptionAsync(_selectedSubGroup, file.RelativePath, fw);
        await _db.SaveChangesAsync();
        await BuildFileDetailTableAsync(_selectedSubGroup);
    }

    public async Task RemoveExceptionAsync(int fileId, string frameworkName)
    {
        var file = await _db.MasterCanonicalFiles.FindAsync(fileId);
        if (file == null) return;
        var key = (_selectedSubGroup, file.RelativePath, frameworkName);
        var ex = await _db.SubFrameworkFileExceptions.FirstOrDefaultAsync(e =>
            e.SubGroup == _selectedSubGroup &&
            e.RelativePath == file.RelativePath &&
            e.FrameworkCanonicalName == frameworkName);
        if (ex != null)
        {
            _db.SubFrameworkFileExceptions.Remove(ex);
            _exceptions.Remove(key);
            await _db.SaveChangesAsync();
        }
        await BuildFileDetailTableAsync(_selectedSubGroup);
    }

    public async Task RemoveRowExceptionsAsync(int fileId, IEnumerable<string> frameworkNames)
    {
        var file = await _db.MasterCanonicalFiles.FindAsync(fileId);
        if (file == null) return;
        foreach (var fw in frameworkNames)
        {
            var ex = await _db.SubFrameworkFileExceptions.FirstOrDefaultAsync(e =>
                e.SubGroup == _selectedSubGroup &&
                e.RelativePath == file.RelativePath &&
                e.FrameworkCanonicalName == fw);
            if (ex != null)
            {
                _db.SubFrameworkFileExceptions.Remove(ex);
                _exceptions.Remove((_selectedSubGroup, file.RelativePath, fw));
            }
        }
        await _db.SaveChangesAsync();
        await BuildFileDetailTableAsync(_selectedSubGroup);
    }

    /// <summary>
    /// Validate and collapse selected files into a single canonical.
    /// Returns (true, "") on success or (false, errorMessage) on failure.
    /// </summary>
    public async Task<(bool success, string error)> ValidateAndCreateCanonicalAliasAsync(
        List<int> fileIds, int masterFileId)
    {
        if (fileIds.Count < 2)
            return (false, "Select at least two files to create a canonical alias.");

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

        var masterPresenceFrameworkIds = presences
            .Where(p => p.MasterCanonicalFileId == masterFileId)
            .Select(p => p.MasterFrameworkEntryId)
            .ToHashSet();

        foreach (var slave in slaveFiles)
        {
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

            foreach (var presence in slave.Presences.ToList())
            {
                if (masterPresenceFrameworkIds.Contains(presence.MasterFrameworkEntryId))
                    _db.MasterFilePresences.Remove(presence);
                else
                    presence.MasterCanonicalFileId = masterFileId;
            }

            _db.MasterCanonicalFiles.Remove(slave);
        }

        await _db.SaveChangesAsync();
        await BuildFileDetailTableAsync(_selectedSubGroup);
        return (true, string.Empty);
    }

    public bool IsException(SubFrameworkGroup sg, string relPath, string frameworkName) =>
        _exceptions.Contains((sg, relPath, frameworkName));

    // -----------------------------------------------------------------------
    // DataTable builders
    // -----------------------------------------------------------------------

    private async Task BuildSubMatrixAsync()
    {
        var allEntries = await _db.MasterFrameworkEntries
            .Where(e => e.SubGroup != null)
            .OrderBy(e => e.SubGroup).ThenBy(e => e.SortOrder)
            .ToListAsync();

        if (!allEntries.Any())
        {
            StatusMessage = "No sub-framework entries found. Rescan in Framework Manager.";
            return;
        }

        var subGroups = Enum.GetValues<SubFrameworkGroup>();

        var dt = new DataTable();
        dt.Columns.Add("_SubGroup", typeof(string));
        dt.Columns.Add("Sub-Framework Group", typeof(string));
        foreach (var e in allEntries)
        {
            if (!dt.Columns.Contains(e.CanonicalName))
                dt.Columns.Add(e.CanonicalName, typeof(bool));
        }

        foreach (var sg in subGroups)
        {
            if (!allEntries.Any(e => e.SubGroup == sg)) continue;

            var row = dt.NewRow();
            row["_SubGroup"] = sg.ToString();
            row["Sub-Framework Group"] = sg.ToString().Replace("_", " ");
            foreach (var e in allEntries)
                row[e.CanonicalName] = e.SubGroup == sg;
            dt.Rows.Add(row);
        }

        SubMatrixTable = dt;
        StatusMessage = $"Sub-matrix ready — {allEntries.Count} frameworks across {dt.Rows.Count} sub-groups.";
    }

    private async Task BuildFileDetailTableAsync(SubFrameworkGroup subGroup)
    {
        var entries = await _db.MasterFrameworkEntries
            .Where(e => e.SubGroup == subGroup)
            .OrderBy(e => e.SortOrder)
            .ToListAsync();

        SubGroupPresentFrameworks = entries
            .Select(e => e.CanonicalName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        _frameworkNames = entries.Select(e => e.CanonicalName).ToList();

        var typeGroup = GetParentTypeGroup(subGroup);
        var entryIds = entries.Select(e => e.Id).ToList();

        var presences = await _db.MasterFilePresences
            .Where(p => entryIds.Contains(p.MasterFrameworkEntryId))
            .ToListAsync();

        // Only show files that exist in at least one framework within this sub-group
        var presentFileIds = presences.Select(p => p.MasterCanonicalFileId).ToHashSet();

        var files = await _db.MasterCanonicalFiles
            .Where(f => f.TypeGroup == typeGroup && presentFileIds.Contains(f.Id))
            .OrderBy(f => f.RelativePath)
            .ToListAsync();

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
                else if (IsException(subGroup, file.RelativePath, e.CanonicalName))
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
        FileDetailHeader = $"File Detail — {subGroup.ToString().Replace("_", " ")} ({entries.Count} frameworks, {files.Count} files)";
        StatusMessage = $"{subGroup} — {files.Count} canonical files × {entries.Count} frameworks.";

        BinaryCompareCommand.NotifyCanExecuteChanged();
    }

    private static string BuildPresentCell(
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

file sealed class SubExceptionComparer
    : IEqualityComparer<(SubFrameworkGroup sg, string path, string fw)>
{
    public static readonly SubExceptionComparer Instance = new();
    public bool Equals((SubFrameworkGroup sg, string path, string fw) x,
                       (SubFrameworkGroup sg, string path, string fw) y) =>
        x.sg == y.sg
        && string.Equals(x.path, y.path, StringComparison.OrdinalIgnoreCase)
        && string.Equals(x.fw, y.fw, StringComparison.OrdinalIgnoreCase);
    public int GetHashCode((SubFrameworkGroup sg, string path, string fw) obj) =>
        HashCode.Combine((int)obj.sg, obj.path.ToUpperInvariant(), obj.fw.ToUpperInvariant());
}
