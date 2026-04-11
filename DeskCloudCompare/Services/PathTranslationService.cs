using DeskCloudCompare.Data;
using DeskCloudCompare.Models;
using Microsoft.EntityFrameworkCore;

namespace DeskCloudCompare.Services;

public class PathTranslationService(AppDbContext db)
{
    public Task<List<PathTranslationRule>> GetAllAsync() =>
        db.PathTranslationRules
          .Include(r => r.FromType)
          .Include(r => r.ToType)
          .OrderBy(r => r.FromTypeId)
          .ThenBy(r => r.SortOrder)
          .ToListAsync();

    public async Task AddAsync(PathTranslationRule rule)
    {
        db.PathTranslationRules.Add(rule);
        await db.SaveChangesAsync();
    }

    public Task UpdateAsync() => db.SaveChangesAsync();

    public async Task DeleteAsync(int id)
    {
        var rule = await db.PathTranslationRules.FindAsync(id)
            ?? throw new KeyNotFoundException();
        db.PathTranslationRules.Remove(rule);
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Normalizes a relative file path from the given folder type toward the canonical form.
    /// Applies all rules where FromTypeId matches, in SortOrder order.
    /// This is a pure method — pass the pre-loaded rules list to avoid DB calls per file.
    /// </summary>
    public static string Normalize(
        string relativeFilePath,
        int fromTypeId,
        IEnumerable<PathTranslationRule> allRules)
    {
        var applicable = allRules
            .Where(r => r.FromTypeId == fromTypeId)
            .OrderBy(r => r.SortOrder);

        var path = relativeFilePath;
        foreach (var rule in applicable)
            path = path.Replace(rule.FindText, rule.ReplaceText, StringComparison.OrdinalIgnoreCase);

        return path;
    }

    // -----------------------------------------------------------------------
    // Reverse translation: Canonical → Desktop
    // -----------------------------------------------------------------------

    // Country code (as it appears in the canonical root folder) → Desktop root folder.
    // Non-GB countries map one-to-one; GB sub-nations are handled separately below.
    private static readonly Dictionary<string, string> _countryToDesktopRoot =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["ZZ"] = @"000.AA",  // canonical ZZ ← desktop 000.AA folder
            ["BW"] = @"072.BW",
            ["CA"] = @"124.CA",
            ["GH"] = @"288.GH",
            ["IE"] = @"372.IE",
            ["KE"] = @"404.KE",
            ["LR"] = @"430.LR",
            ["MU"] = @"480.MU",
            ["NA"] = @"516.NA",
            ["NG"] = @"566.NG",
            ["RW"] = @"646.RW",
            ["SA"] = @"682.SA",
            ["ZA"] = @"710.ZA",
            ["SZ"] = @"748.SZ",
            ["UG"] = @"800.UG",
            ["GG"] = @"831.GG",
            ["JE"] = @"832.JE",
            ["TZ"] = @"834.TZ",
            ["ZM"] = @"894.ZM",
        };

    // GB sub-nation suffix (retained in canonical framework folder) → Desktop root folder.
    private static readonly Dictionary<string, string> _gbNationSuffixToDesktopRoot =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["EW"] = @"826.EW",
            ["NI"] = @"826.NI",
            ["SC"] = @"826.SC",
            ["GB"] = @"826.GB",
        };

    // Countries whose framework folder uses an underscore country suffix on Desktop
    // (e.g. FRS102_Trust_GG).  All others use a space suffix (e.g. FRS102_Trust ZA).
    private static readonly HashSet<string> _underscoreSuffixCountries =
        new(StringComparer.OrdinalIgnoreCase) { "GG", "JE", "IE" };

    // First-level subfolders whose Desktop contents live inside a "DraftworX Files"
    // sub-folder that the Desktop→Canonical rule 31 strips.  When reversing, we must
    // re-insert \DraftworX Files\ after these folder names.
    private static readonly HashSet<string> _draftworxParentFolders =
        new(StringComparer.OrdinalIgnoreCase)
        { "Financial Data", "Lead Schedules", "Documents", "Audit Programs" };

    // Files that sit directly at the framework root on Desktop under NewUserDataUpdates\
    // rather than NewUserData\Frameworks\.  The Desktop rule at SortOrder 29 maps
    // \NewUserDataUpdates\ → \Frameworks\, so the canonical path looks the same as a
    // normal framework file — we distinguish them by filename.
    private static readonly HashSet<string> _updatesRootFiles =
        new(StringComparer.OrdinalIgnoreCase) { "Detail.xlsx", "Switch.xlsx" };

    /// <summary>
    /// Derives the Desktop-relative path from a canonical relative path, reversing the
    /// Desktop→Canonical rules.  Returns <c>null</c> when the path cannot be translated
    /// (e.g. the canonical root country is unrecognised, or the path does not follow the
    /// expected <c>COUNTRY\Frameworks\FRAMEWORK\…</c> structure).
    /// </summary>
    /// <remarks>
    /// The reconstruction logic:
    /// <list type="number">
    ///   <item>Identifies the country root (first path segment) and looks up the Desktop
    ///         numeric prefix folder (e.g. ZA → 710.ZA).</item>
    ///   <item>For GB, inspects the framework folder suffix (_EW/_NI/_SC/_GB) to determine
    ///         the correct nation root (826.EW / 826.NI / 826.SC / 826.GB).</item>
    ///   <item>Adds the country code back as a suffix on the framework folder name
    ///         (underscore for GG/JE/IE, space for all other non-GB countries).</item>
    ///   <item>Inserts <c>\NewUserData\</c> between the country root and <c>\Frameworks\</c>.
    ///         Note: <c>\DraftworX Files\</c> sub-folders are NOT recreated — files copied
    ///         without that sub-folder are still matched correctly on the next scan because
    ///         the Desktop→Canonical rule strips it.</item>
    /// </list>
    /// </remarks>
    public static string? DeriveDesktopRelativePath(string canonicalRelPath)
    {
        // Normalise separators and strip leading backslash
        var path = canonicalRelPath.Replace('/', '\\').TrimStart('\\');

        // Expect at least: COUNTRY\Frameworks\FRAMEWORK[\rest]
        var segments = path.Split('\\');
        if (segments.Length < 3) return null;
        if (!string.Equals(segments[1], "Frameworks", StringComparison.OrdinalIgnoreCase))
            return null;

        var countryRoot    = segments[0];
        var frameworkFolder = segments[2];
        var rest           = segments.Length > 3
                             ? string.Join('\\', segments[3..])
                             : string.Empty;

        string desktopRoot;
        string frameworkDesktop;

        if (string.Equals(countryRoot, "GB", StringComparison.OrdinalIgnoreCase))
        {
            // The framework folder retains the nation suffix in canonical (_EW/_NI/_SC/_GB).
            // Use that suffix to pick the right 826.XX desktop root.
            string? nation = null;
            foreach (var code in new[] { "EW", "NI", "SC", "GB" })
            {
                if (frameworkFolder.EndsWith($"_{code}", StringComparison.OrdinalIgnoreCase))
                {
                    nation = code;
                    break;
                }
            }
            if (nation == null || !_gbNationSuffixToDesktopRoot.TryGetValue(nation, out desktopRoot!))
                return null;

            // Framework folder suffix is already present in canonical — no change needed.
            frameworkDesktop = frameworkFolder;
        }
        else
        {
            if (!_countryToDesktopRoot.TryGetValue(countryRoot, out desktopRoot!))
                return null;

            // Determine the Desktop country suffix format for this country.
            // ZZ maps to canonical "ZZ" but Desktop uses "AA" as the suffix.
            var suffixCode = string.Equals(countryRoot, "ZZ", StringComparison.OrdinalIgnoreCase)
                             ? "AA"
                             : countryRoot;
            var suffix = _underscoreSuffixCountries.Contains(countryRoot)
                         ? $"_{suffixCode}"
                         : $" {suffixCode}";

            // Only append the suffix if it is not already present (guard against double-add).
            frameworkDesktop =
                frameworkFolder.EndsWith($"_{suffixCode}", StringComparison.OrdinalIgnoreCase) ||
                frameworkFolder.EndsWith($" {suffixCode}", StringComparison.OrdinalIgnoreCase)
                ? frameworkFolder
                : frameworkFolder + suffix;
        }

        // ── Fix 1: re-insert \DraftworX Files\ ─────────────────────────────────
        // Desktop rule 31 strips "\DraftworX Files" from paths like
        //   Financial Data\DraftworX Files\file.xlsx → Financial Data\file.xlsx
        // Reverse: if "rest" starts with a known parent folder, insert the sub-folder back.
        if (!string.IsNullOrEmpty(rest))
        {
            var restParts = rest.Split('\\');
            if (restParts.Length >= 1 && _draftworxParentFolders.Contains(restParts[0]))
                rest = restParts[0] + @"\DraftworX Files\" + string.Join('\\', restParts[1..]);
        }

        // ── Fix 2: NewUserDataUpdates for Detail.xlsx / Switch.xlsx ─────────────
        // Desktop rule 29 maps \NewUserDataUpdates\ → \Frameworks\ so these files
        // appear at canonical COUNTRY\Frameworks\FW\Detail.xlsx.  On Desktop they live
        // at NUMERIC.COUNTRY\NewUserDataUpdates\FW_SUFFIX\Detail.xlsx — no "Frameworks"
        // subfolder and no "NewUserData" parent.
        var isUpdatesFile = !string.IsNullOrEmpty(rest) && _updatesRootFiles.Contains(rest);

        // Reconstruct Desktop-relative path:
        //   Updates:  {desktopRoot}\NewUserDataUpdates\{frameworkDesktop}\{file}
        //   Regular:  {desktopRoot}\NewUserData\Frameworks\{frameworkDesktop}[\rest]
        if (isUpdatesFile)
            return $@"{desktopRoot}\NewUserDataUpdates\{frameworkDesktop}\{rest}";

        return string.IsNullOrEmpty(rest)
            ? $@"{desktopRoot}\NewUserData\Frameworks\{frameworkDesktop}"
            : $@"{desktopRoot}\NewUserData\Frameworks\{frameworkDesktop}\{rest}";
    }
}
