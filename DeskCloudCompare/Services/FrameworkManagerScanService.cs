using DeskCloudCompare.Data;
using DeskCloudCompare.Models;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Security.Cryptography;

namespace DeskCloudCompare.Services;

public class FrameworkManagerScanService(AppDbContext db)
{
    // -----------------------------------------------------------------------
    // Framework type classification
    // -----------------------------------------------------------------------

    public static FrameworkTypeGroup ClassifyFramework(string canonicalName)
    {
        // Arabic — specific known names (including Arabic-script name)
        if (canonicalName.Equals("IFRS Full", StringComparison.OrdinalIgnoreCase) ||
            canonicalName.Equals("IFRS SME English", StringComparison.OrdinalIgnoreCase) ||
            canonicalName.Contains('\u0622') || canonicalName.Contains('\u0625') || canonicalName.Contains('\u0627'))
            return FrameworkTypeGroup.Arabic;

        // FRS — name contains FRS102, FRS102(1A), FRS105, Charity SORP, LLP SORP
        if (canonicalName.Contains("FRS102", StringComparison.OrdinalIgnoreCase) ||
            canonicalName.Contains("FRS105", StringComparison.OrdinalIgnoreCase) ||
            canonicalName.Contains("Charity SORP", StringComparison.OrdinalIgnoreCase) ||
            canonicalName.Contains("LLP SORP", StringComparison.OrdinalIgnoreCase))
            return FrameworkTypeGroup.FRS;

        // IFRS — name contains "IFRS" with a "+", or is IFRS_Company/IFRS Consoli_Company,
        //         FRS101, or ASPE (treated as IFRS for now)
        if (canonicalName.Contains("IFRS+", StringComparison.OrdinalIgnoreCase) ||
            canonicalName.Contains("IFRS SME+", StringComparison.OrdinalIgnoreCase) ||
            canonicalName.Equals("IFRS_Company", StringComparison.OrdinalIgnoreCase) ||
            canonicalName.Equals("IFRS Consoli_Company", StringComparison.OrdinalIgnoreCase) ||
            canonicalName.StartsWith("FRS101", StringComparison.OrdinalIgnoreCase) ||
            canonicalName.StartsWith("ASPE", StringComparison.OrdinalIgnoreCase))
            return FrameworkTypeGroup.IFRS;

        // Everything else is Legacy
        return FrameworkTypeGroup.Legacy;
    }

    /// <summary>Strip the country suffix (" ZZ", "_ZZ", " ZA", etc.) from a folder name.</summary>
    public static string GetCanonicalName(string folderName)
    {
        // Reuse the same suffix list as CountryManagerScanService
        foreach (var suffix in SpaceSuffixes)
            if (folderName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                return folderName[..(folderName.Length - suffix.Length)];
        foreach (var suffix in UnderscoreSuffixes)
            if (folderName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                return folderName[..(folderName.Length - suffix.Length)];
        return folderName;
    }

    private static readonly string[] SpaceSuffixes =
    [
        " AA", " BW", " CA", " GH", " IE", " KE", " LR", " MU", " NA", " NG",
        " RW", " SA", " ZA", " SZ", " UG", " GG", " JE", " TZ", " ZM",
        " EW", " NI", " SC", " GB", " ZZ"
    ];

    private static readonly string[] UnderscoreSuffixes =
    [
        "_AA", "_BW", "_CA", "_GH", "_IE", "_KE", "_LR", "_MU", "_NA", "_NG",
        "_RW", "_SA", "_ZA", "_SZ", "_UG", "_GG", "_JE", "_TZ", "_ZM",
        "_EW", "_NI", "_SC", "_GB", "_ZZ"
    ];

    // -----------------------------------------------------------------------
    // Scan
    // -----------------------------------------------------------------------

    public async Task ScanAsync(
        string masterFolderPath,
        IProgress<string>? progress = null,
        CancellationToken ct = default)
    {
        progress?.Report("Clearing previous Framework Manager scan data…");

        await db.Database.ExecuteSqlRawAsync("DELETE FROM MasterFilePresences", ct);
        await db.Database.ExecuteSqlRawAsync("DELETE FROM MasterCanonicalFiles", ct);
        await db.Database.ExecuteSqlRawAsync("DELETE FROM MasterFrameworkEntries", ct);
        db.ChangeTracker.Clear();

        // Load aliases into a lookup: (folderPath, actualFileName) → canonicalFileName
        var aliases = await db.MasterFileAliases.ToListAsync(ct);
        var aliasLookup = aliases.ToDictionary(
            a => (a.FolderPath.ToUpperInvariant(), a.ActualFileName.ToUpperInvariant()),
            a => a.CanonicalFileName);

        var frameworksDir = Path.Combine(masterFolderPath, "NewUserData", "Frameworks");
        if (!Directory.Exists(frameworksDir))
        {
            progress?.Report("ERROR: Frameworks folder not found in master path.");
            return;
        }

        var frameworkFolders = Directory.GetDirectories(frameworksDir)
            .OrderBy(f => f).ToList();

        // canonical file cache per type group:  (typeGroup, canonicalRelPath) → MasterCanonicalFile
        var fileCache = new Dictionary<(FrameworkTypeGroup, string), MasterCanonicalFile>(
            TypeGroupPathComparer.Instance);

        var settings = await db.FrameworkManagerSettings.FirstOrDefaultAsync(ct)
                       ?? new FrameworkManagerSettings();

        int sortOrder = 0;

        await Task.Run(() =>
        {
            foreach (var fwDir in frameworkFolders)
            {
                ct.ThrowIfCancellationRequested();
                var folderName = Path.GetFileName(fwDir);
                var canonicalName = GetCanonicalName(folderName);
                var typeGroup = ClassifyFramework(canonicalName);

                progress?.Report($"Scanning {canonicalName} ({typeGroup})…");

                var entry = new MasterFrameworkEntry
                {
                    CanonicalName = canonicalName,
                    FolderName = folderName,
                    TypeGroup = typeGroup,
                    SortOrder = sortOrder++
                };
                db.MasterFrameworkEntries.Add(entry);

                ScanFrameworkFiles(fwDir, entry, typeGroup, aliasLookup, fileCache, ct);
            }
        }, ct);

        progress?.Report("Saving Framework Manager scan results…");
        await db.SaveChangesAsync(ct);

        settings.MasterFolderPath = masterFolderPath;
        settings.LastScanned = DateTime.Now;
        if (settings.Id == 0) db.FrameworkManagerSettings.Add(settings);
        await db.SaveChangesAsync(ct);

        progress?.Report($"Framework Manager scan complete — {fileCache.Count} canonical files discovered.");
    }

    private void ScanFrameworkFiles(
        string fwDir,
        MasterFrameworkEntry entry,
        FrameworkTypeGroup typeGroup,
        Dictionary<(string, string), string> aliasLookup,
        Dictionary<(FrameworkTypeGroup, string), MasterCanonicalFile> fileCache,
        CancellationToken ct)
    {
        foreach (var filePath in Directory.EnumerateFiles(fwDir, "*", SearchOption.AllDirectories))
        {
            ct.ThrowIfCancellationRequested();

            var relativePath = Path.GetRelativePath(fwDir, filePath);
            var fileName = Path.GetFileName(filePath);
            var folderRel = Path.GetDirectoryName(relativePath) ?? string.Empty;

            // Resolve alias: does this file name map to a canonical name?
            var aliasKey = (folderRel.ToUpperInvariant(), fileName.ToUpperInvariant());
            var canonicalFileName = aliasLookup.TryGetValue(aliasKey, out var mapped)
                ? mapped
                : fileName;

            // Rebuild canonical relative path using canonical file name
            var canonicalRelPath = string.IsNullOrEmpty(folderRel)
                ? canonicalFileName
                : folderRel + Path.DirectorySeparatorChar + canonicalFileName;

            var cacheKey = (typeGroup, canonicalRelPath.ToUpperInvariant());

            if (!fileCache.TryGetValue(cacheKey, out var canonicalFile))
            {
                var isDxdb = fileName.EndsWith(".dxdb", StringComparison.OrdinalIgnoreCase);
                var isFinancial = relativePath.Contains("Financial Data", StringComparison.OrdinalIgnoreCase)
                    || fileName.Equals("Detail.xlsx", StringComparison.OrdinalIgnoreCase)
                    || fileName.Equals("Switch.xlsx", StringComparison.OrdinalIgnoreCase);

                canonicalFile = new MasterCanonicalFile
                {
                    TypeGroup = typeGroup,
                    RelativePath = canonicalRelPath,
                    FileName = canonicalFileName,
                    IsDxdb = isDxdb,
                    IsFinancialData = isFinancial
                };
                db.MasterCanonicalFiles.Add(canonicalFile);
                fileCache[cacheKey] = canonicalFile;
            }

            db.MasterFilePresences.Add(new MasterFilePresence
            {
                MasterCanonicalFile = canonicalFile,
                MasterFrameworkEntry = entry,
                ActualFileName = fileName
            });
        }
    }

    // -----------------------------------------------------------------------
    // Binary compare
    // -----------------------------------------------------------------------

    public async Task BinaryCompareAsync(
        FrameworkTypeGroup typeGroup,
        string referenceFrameworkName,
        IProgress<string>? progress = null,
        CancellationToken ct = default)
    {
        var settings = await db.FrameworkManagerSettings.FirstOrDefaultAsync(ct);
        if (settings == null) return;

        var frameworksDir = Path.Combine(settings.MasterFolderPath, "NewUserData", "Frameworks");

        var entries = await db.MasterFrameworkEntries
            .Where(e => e.TypeGroup == typeGroup)
            .ToListAsync(ct);
        var entryFolderMap = entries.ToDictionary(e => e.Id, e => e.FolderName);

        var files = await db.MasterCanonicalFiles
            .Where(f => f.TypeGroup == typeGroup && !f.IsDxdb && !f.IsFinancialData)
            .Include(f => f.Presences)
            .ToListAsync(ct);

        foreach (var file in files)
        {
            ct.ThrowIfCancellationRequested();
            foreach (var presence in file.Presences)
            {
                if (!entryFolderMap.TryGetValue(presence.MasterFrameworkEntryId, out var folderName))
                    continue;
                var fullPath = Path.Combine(frameworksDir, folderName, file.RelativePath);
                // Handle aliased file names
                if (!File.Exists(fullPath))
                    fullPath = Path.Combine(frameworksDir, folderName,
                        Path.GetDirectoryName(file.RelativePath) ?? string.Empty,
                        presence.ActualFileName);
                if (!File.Exists(fullPath)) continue;

                progress?.Report($"Hashing {folderName}: {file.FileName}");
                presence.BinaryHash = await ComputeHashAsync(fullPath, ct);
            }
        }

        await db.SaveChangesAsync(ct);
        progress?.Report("Binary compare complete.");
    }

    private static async Task<string> ComputeHashAsync(string filePath, CancellationToken ct)
    {
        using var sha = SHA256.Create();
        await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read,
            FileShare.Read, 65536, FileOptions.SequentialScan | FileOptions.Asynchronous);
        var hash = await sha.ComputeHashAsync(stream, ct);
        return Convert.ToHexString(hash);
    }
}

file sealed class TypeGroupPathComparer
    : IEqualityComparer<(FrameworkTypeGroup group, string path)>
{
    public static readonly TypeGroupPathComparer Instance = new();
    public bool Equals((FrameworkTypeGroup group, string path) x, (FrameworkTypeGroup group, string path) y) =>
        x.group == y.group && string.Equals(x.path, y.path, StringComparison.OrdinalIgnoreCase);
    public int GetHashCode((FrameworkTypeGroup group, string path) obj) =>
        HashCode.Combine((int)obj.group, obj.path.ToUpperInvariant());
}
