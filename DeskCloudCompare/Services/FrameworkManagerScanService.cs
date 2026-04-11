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

        // IFRS — any framework whose canonical name contains "+" (IFRS+, IFRS SME+, ASPE+,
        //         Body Corp+, CC+, Monthly+ Manpack, NPC+, NPO+, Partnership+, School+,
        //         Sole Prop+, Trust+, …), plus the plain-name IFRS company folders,
        //         FRS101, and plain ASPE (treated as IFRS-family)
        if (canonicalName.Contains("+", StringComparison.Ordinal) ||
            canonicalName.Equals("IFRS_Company", StringComparison.OrdinalIgnoreCase) ||
            canonicalName.Equals("IFRS Consoli_Company", StringComparison.OrdinalIgnoreCase) ||
            canonicalName.StartsWith("FRS101", StringComparison.OrdinalIgnoreCase) ||
            canonicalName.StartsWith("ASPE", StringComparison.OrdinalIgnoreCase))
            return FrameworkTypeGroup.IFRS;

        // Everything else is Legacy
        return FrameworkTypeGroup.Legacy;
    }

    public static SubFrameworkGroup? ClassifySubFramework(string canonicalName)
    {
        // FRS102(1A) — must check before FRS102 (substring match order matters)
        if (canonicalName.Contains("FRS102(1A)", StringComparison.OrdinalIgnoreCase))
            return SubFrameworkGroup.FRS102_1A;

        // FRS105
        if (canonicalName.Contains("FRS105", StringComparison.OrdinalIgnoreCase))
            return SubFrameworkGroup.FRS105;

        // FRS102 (plain — after 1A check)
        if (canonicalName.Contains("FRS102", StringComparison.OrdinalIgnoreCase))
            return SubFrameworkGroup.FRS102;

        // LLP SORP(1A) → FRS102_1A (must check before plain LLP SORP)
        if (canonicalName.Contains("LLP SORP(1A)", StringComparison.OrdinalIgnoreCase))
            return SubFrameworkGroup.FRS102_1A;

        // LLP SORP (plain) → FRS102
        if (canonicalName.Contains("LLP SORP", StringComparison.OrdinalIgnoreCase))
            return SubFrameworkGroup.FRS102;

        // Charity SORP → dedicated Charity sub-group
        if (canonicalName.Contains("Charity SORP", StringComparison.OrdinalIgnoreCase))
            return SubFrameworkGroup.Charity;

        // IFRS SME+ group — IFRS SME+, IFRS SME+ Consoli, and all entity variants whose
        //   names do NOT contain "IFRS SME+" (Body Corp+, CC+, Monthly+ Manpack, NPC+,
        //   NPO+, Partnership+, School+, Sole Prop+, Trust+).
        //   Must be checked BEFORE the IFRS_Plus catch-all, which would otherwise absorb
        //   any framework name containing "+" that doesn't start with "IFRS SME+".
        if (canonicalName.StartsWith("IFRS SME+", StringComparison.OrdinalIgnoreCase) ||
            canonicalName.Contains("IFRS SME+", StringComparison.OrdinalIgnoreCase) ||
            canonicalName.Equals("Body Corp+", StringComparison.OrdinalIgnoreCase) ||
            canonicalName.Equals("CC+", StringComparison.OrdinalIgnoreCase) ||
            canonicalName.Equals("NPC+", StringComparison.OrdinalIgnoreCase) ||
            canonicalName.Equals("NPO+", StringComparison.OrdinalIgnoreCase) ||
            canonicalName.Equals("Partnership+", StringComparison.OrdinalIgnoreCase) ||
            canonicalName.Equals("School+", StringComparison.OrdinalIgnoreCase) ||
            canonicalName.StartsWith("Sole Prop+", StringComparison.OrdinalIgnoreCase) ||
            canonicalName.Equals("Trust+", StringComparison.OrdinalIgnoreCase))
            return SubFrameworkGroup.IFRS_SME;

        // ASPE+ — dedicated sub-group
        if (canonicalName.StartsWith("ASPE", StringComparison.OrdinalIgnoreCase))
            return SubFrameworkGroup.ASPE_Plus;

        // IFRS+ group — IFRS+, IFRS+ Consoli, IFRS_Company, IFRS Consoli_Company, FRS101,
        //   and any remaining "+" frameworks (after IFRS SME and ASPE are excluded above).
        if (canonicalName.Equals("IFRS+", StringComparison.OrdinalIgnoreCase) ||
            canonicalName.Equals("IFRS+ Consoli", StringComparison.OrdinalIgnoreCase) ||
            canonicalName.Equals("IFRS_Company", StringComparison.OrdinalIgnoreCase) ||
            canonicalName.Equals("IFRS Consoli_Company", StringComparison.OrdinalIgnoreCase) ||
            canonicalName.StartsWith("FRS101", StringComparison.OrdinalIgnoreCase) ||
            canonicalName.Contains("+", StringComparison.Ordinal))
            return SubFrameworkGroup.IFRS_Plus;

        // Everything else excluded (Legacy, Arabic, plain IFRS SME, etc.)
        return null;
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
        // Normalize path separators so Windows '\' and '/' variants both match.
        var aliases = await db.MasterFileAliases.ToListAsync(ct);
        var aliasLookup = aliases.ToDictionary(
            a => (NormSep(a.FolderPath), a.ActualFileName.ToUpperInvariant()),
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
                    SubGroup = ClassifySubFramework(canonicalName),
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
        // Track canonical paths already seen in this folder to skip alias duplicates
        // (e.g. both Director Certificates.xlsx and Partner Certificates.xlsx in the same folder
        //  both resolve to the same canonical file — only the first should create a presence).
        var seenInThisFolder = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var filePath in Directory.EnumerateFiles(fwDir, "*", SearchOption.AllDirectories))
        {
            ct.ThrowIfCancellationRequested();

            var relativePath = Path.GetRelativePath(fwDir, filePath);
            var fileName = Path.GetFileName(filePath);
            var folderRel = Path.GetDirectoryName(relativePath) ?? string.Empty;

            // Resolve alias: does this file name map to a canonical name?
            var aliasKey = (NormSep(folderRel), fileName.ToUpperInvariant());
            var canonicalFileName = aliasLookup.TryGetValue(aliasKey, out var mapped)
                ? mapped
                : fileName;

            // Rebuild canonical relative path using canonical file name
            var canonicalRelPath = string.IsNullOrEmpty(folderRel)
                ? canonicalFileName
                : folderRel + Path.DirectorySeparatorChar + canonicalFileName;

            // Skip if this canonical path already has a presence for this framework folder
            if (!seenInThisFolder.Add(canonicalRelPath.ToUpperInvariant()))
                continue;

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

    /// <summary>Normalize path separators to OS separator and uppercase, for alias key comparison.</summary>
    private static string NormSep(string path) =>
        path.Replace('/', Path.DirectorySeparatorChar).ToUpperInvariant();
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
