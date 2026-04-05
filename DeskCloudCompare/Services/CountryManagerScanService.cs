using DeskCloudCompare.Data;
using DeskCloudCompare.Models;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Security.Cryptography;

namespace DeskCloudCompare.Services;

public class CountryManagerScanService(AppDbContext db)
{
    // Country codes that can appear as suffixes in framework/methodology folder names.
    // Order matters: longer/more-specific first.
    private static readonly string[] SpaceSuffixes =
    [
        " AA", " BW", " CA", " GH", " IE", " KE", " LR", " MU", " NA", " NG",
        " RW", " SA", " ZA", " SZ", " UG", " GG", " JE", " TZ", " ZM",
        " EW", " NI", " SC", " GB"
    ];

    private static readonly string[] UnderscoreSuffixes =
    [
        "_AA", "_BW", "_CA", "_GH", "_IE", "_KE", "_LR", "_MU", "_NA", "_NG",
        "_RW", "_SA", "_ZA", "_SZ", "_UG", "_GG", "_JE", "_TZ", "_ZM",
        "_EW", "_NI", "_SC", "_GB"
    ];

    // -----------------------------------------------------------------------
    // Scan — discovers all frameworks, methodologies and their files.
    // Clears all previous scan data before running.
    // -----------------------------------------------------------------------

    public async Task ScanAsync(
        string rootFolderPath,
        string? masterCountryCode,
        IProgress<string>? progress = null,
        CancellationToken ct = default)
    {
        progress?.Report("Clearing previous scan data…");

        // Clear in dependency order
        await db.Database.ExecuteSqlRawAsync("DELETE FROM CountryFilePresences", ct);
        await db.Database.ExecuteSqlRawAsync("DELETE FROM CanonicalFiles", ct);
        await db.Database.ExecuteSqlRawAsync("DELETE FROM CountryFrameworkPresences", ct);
        await db.Database.ExecuteSqlRawAsync("DELETE FROM CanonicalFrameworks", ct);
        await db.Database.ExecuteSqlRawAsync("DELETE FROM CountryEntries", ct);

        // In-memory caches so EF can build the object graph before a single SaveChangesAsync
        var frameworkCache = new Dictionary<string, CanonicalFramework>(StringComparer.OrdinalIgnoreCase);
        var fileCache = new Dictionary<string, CanonicalFile>(StringComparer.OrdinalIgnoreCase);

        var countryFolders = Directory.GetDirectories(rootFolderPath)
            .OrderBy(f => f)
            .ToList();

        int sortOrder = 0;

        await Task.Run(() =>
        {
            foreach (var countryFolder in countryFolders)
            {
                ct.ThrowIfCancellationRequested();
                var rawName = Path.GetFileName(countryFolder);
                var code = ParseCountryCode(rawName);

                progress?.Report($"Scanning {code}…");

                db.CountryEntries.Add(new CountryEntry
                {
                    Code = code,
                    RawFolderName = rawName,
                    SortOrder = sortOrder++
                });

                ScanCategory(countryFolder, code, FrameworkCategory.Framework,
                    "Frameworks", frameworkCache, fileCache, progress, ct);

                ScanCategory(countryFolder, code, FrameworkCategory.Methodology,
                    "Methodologies", frameworkCache, fileCache, progress, ct);

                ScanUpdates(countryFolder, code, frameworkCache, fileCache, ct);
            }
        }, ct);

        progress?.Report("Saving scan results…");
        await db.SaveChangesAsync(ct);

        // Persist settings
        var settings = await db.CountryManagerSettings.FirstOrDefaultAsync(ct)
                       ?? new CountryManagerSettings();
        settings.RootFolderPath = rootFolderPath;
        settings.MasterCountryCode = masterCountryCode;
        settings.LastScanned = DateTime.Now;
        if (settings.Id == 0) db.CountryManagerSettings.Add(settings);
        await db.SaveChangesAsync(ct);

        progress?.Report($"Scan complete — {frameworkCache.Count} canonical frameworks discovered.");
    }

    // -----------------------------------------------------------------------
    // Binary compare — hashes all comparable files for the given framework
    // across all countries and stores results.
    // -----------------------------------------------------------------------

    public async Task BinaryCompareAsync(
        int canonicalFrameworkId,
        string masterCountryCode,
        IProgress<string>? progress = null,
        CancellationToken ct = default)
    {
        var files = await db.CanonicalFiles
            .Where(f => f.CanonicalFrameworkId == canonicalFrameworkId
                     && !f.IsDxdb
                     && !f.IsFinancialData)
            .Include(f => f.Presences)
            .ToListAsync(ct);

        foreach (var file in files)
        {
            ct.ThrowIfCancellationRequested();
            foreach (var presence in file.Presences)
            {
                // Find the actual file path to hash
                var countryPresence = await db.CountryFrameworkPresences
                    .FirstOrDefaultAsync(p =>
                        p.CanonicalFrameworkId == canonicalFrameworkId &&
                        p.CountryCode == presence.CountryCode, ct);

                if (countryPresence == null) continue;

                var rootPath = await GetCountryFolderAsync(presence.CountryCode, ct);
                if (rootPath == null) continue;

                var relPath = file.RelativePath;
                string fullPath;

                if (relPath.StartsWith("#Updates\\", StringComparison.OrdinalIgnoreCase))
                {
                    var fileName = relPath["#Updates\\".Length..];
                    fullPath = Path.Combine(rootPath, "NewUserDataUpdates",
                        countryPresence.ActualFolderName, fileName);
                }
                else
                {
                    fullPath = Path.Combine(rootPath, "NewUserData",
                        countryPresence.CanonicalFramework.Category == FrameworkCategory.Framework
                            ? "Frameworks" : "Methodologies",
                        countryPresence.ActualFolderName, relPath);
                }

                if (!File.Exists(fullPath)) continue;

                progress?.Report($"Hashing {presence.CountryCode}: {file.FileName}");
                presence.BinaryHash = await ComputeHashAsync(fullPath, ct);
            }
        }

        await db.SaveChangesAsync(ct);
        progress?.Report("Binary compare complete.");
    }

    // -----------------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------------

    private void ScanCategory(
        string countryFolder,
        string countryCode,
        FrameworkCategory category,
        string subfolder,
        Dictionary<string, CanonicalFramework> frameworkCache,
        Dictionary<string, CanonicalFile> fileCache,
        IProgress<string>? progress,
        CancellationToken ct)
    {
        var baseDir = Path.Combine(countryFolder, "NewUserData", subfolder);
        if (!Directory.Exists(baseDir)) return;

        foreach (var frameworkDir in Directory.GetDirectories(baseDir))
        {
            ct.ThrowIfCancellationRequested();
            var folderName = Path.GetFileName(frameworkDir);
            var canonicalName = GetCanonicalName(folderName);

            var framework = GetOrCreate(frameworkCache, $"{category}:{canonicalName}", () =>
            {
                var f = new CanonicalFramework { Name = canonicalName, Category = category };
                db.CanonicalFrameworks.Add(f);
                return f;
            });

            db.CountryFrameworkPresences.Add(new CountryFrameworkPresence
            {
                CanonicalFramework = framework,
                CountryCode = countryCode,
                ActualFolderName = folderName
            });

            ScanFiles(frameworkDir, countryCode, framework, string.Empty, fileCache, ct);
        }
    }

    private void ScanUpdates(
        string countryFolder,
        string countryCode,
        Dictionary<string, CanonicalFramework> frameworkCache,
        Dictionary<string, CanonicalFile> fileCache,
        CancellationToken ct)
    {
        var updatesDir = Path.Combine(countryFolder, "NewUserDataUpdates");
        if (!Directory.Exists(updatesDir)) return;

        // Files directly in NewUserDataUpdates (FindReplace.dxdb, LeadTemp.xlsx, etc.)
        // are handled as a special case — we skip them here (they're SpecialFileRules).

        foreach (var updateFolder in Directory.GetDirectories(updatesDir))
        {
            ct.ThrowIfCancellationRequested();
            var folderName = Path.GetFileName(updateFolder);
            var canonicalName = GetCanonicalName(folderName);

            // Find the matching canonical framework (must already exist in cache)
            var key = $"{FrameworkCategory.Framework}:{canonicalName}";
            if (!frameworkCache.TryGetValue(key, out var framework)) continue;

            foreach (var filePath in Directory.EnumerateFiles(updateFolder))
            {
                ct.ThrowIfCancellationRequested();
                var fileName = Path.GetFileName(filePath);
                var relPath = "#Updates\\" + fileName;

                var file = GetOrCreate(fileCache, $"{framework.GetHashCode()}:{relPath}", () =>
                {
                    var isDxdb = fileName.EndsWith(".dxdb", StringComparison.OrdinalIgnoreCase);
                    var f = new CanonicalFile
                    {
                        CanonicalFramework = framework,
                        RelativePath = relPath,
                        FileName = fileName,
                        IsDxdb = isDxdb,
                        IsFinancialData = false
                    };
                    db.CanonicalFiles.Add(f);
                    return f;
                });

                db.CountryFilePresences.Add(new CountryFilePresence
                {
                    CanonicalFile = file,
                    CountryCode = countryCode
                });
            }
        }
    }

    private void ScanFiles(
        string frameworkDir,
        string countryCode,
        CanonicalFramework framework,
        string relativePrefix,
        Dictionary<string, CanonicalFile> fileCache,
        CancellationToken ct)
    {
        foreach (var entry in Directory.EnumerateFileSystemEntries(frameworkDir))
        {
            ct.ThrowIfCancellationRequested();
            var name = Path.GetFileName(entry);

            if (Directory.Exists(entry))
            {
                ScanFiles(entry, countryCode, framework,
                    string.IsNullOrEmpty(relativePrefix) ? name : relativePrefix + "\\" + name,
                    fileCache, ct);
            }
            else
            {
                var relPath = string.IsNullOrEmpty(relativePrefix)
                    ? name
                    : relativePrefix + "\\" + name;

                var cacheKey = $"{framework.GetHashCode()}:{relPath}";

                var file = GetOrCreate(fileCache, cacheKey, () =>
                {
                    var isDxdb = name.EndsWith(".dxdb", StringComparison.OrdinalIgnoreCase);
                    var isFinancial = relPath.Contains("Financial Data",
                        StringComparison.OrdinalIgnoreCase);
                    var f = new CanonicalFile
                    {
                        CanonicalFramework = framework,
                        RelativePath = relPath,
                        FileName = name,
                        IsDxdb = isDxdb,
                        IsFinancialData = isFinancial
                    };
                    db.CanonicalFiles.Add(f);
                    return f;
                });

                db.CountryFilePresences.Add(new CountryFilePresence
                {
                    CanonicalFile = file,
                    CountryCode = countryCode
                });
            }
        }
    }

    private async Task<string?> GetCountryFolderAsync(string countryCode, CancellationToken ct)
    {
        var entry = await db.CountryEntries.FindAsync([countryCode], ct);
        if (entry == null) return null;
        var settings = await db.CountryManagerSettings.FirstOrDefaultAsync(ct);
        if (settings == null) return null;
        return Path.Combine(settings.RootFolderPath, entry.RawFolderName);
    }

    private static string ParseCountryCode(string rawFolderName)
    {
        var dot = rawFolderName.IndexOf('.');
        return dot >= 0 ? rawFolderName[(dot + 1)..] : rawFolderName;
    }

    internal static string GetCanonicalName(string folderName)
    {
        foreach (var suffix in UnderscoreSuffixes)
            if (folderName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                return folderName[..(folderName.Length - suffix.Length)];

        foreach (var suffix in SpaceSuffixes)
            if (folderName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                return folderName[..(folderName.Length - suffix.Length)];

        return folderName;
    }

    private static TValue GetOrCreate<TKey, TValue>(
        Dictionary<TKey, TValue> cache, TKey key, Func<TValue> factory)
        where TKey : notnull
    {
        if (cache.TryGetValue(key, out var v)) return v;
        v = factory();
        cache[key] = v;
        return v;
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
