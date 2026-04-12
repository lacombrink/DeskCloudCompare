using System.IO;
using System.Security.Cryptography;

namespace DeskCloudCompare.Services;

public record BinaryCompareResult(bool AllIdentical, Dictionary<string, string?> HashBySlot);

public class BinaryCompareService
{
    private const int BufferSize = 64 * 1024;

    public async Task<BinaryCompareResult> CompareAsync(
        IReadOnlyDictionary<string, string?> slotPaths,
        CancellationToken ct = default)
    {
        var hashBySlot = new Dictionary<string, string?>();

        await Task.Run(async () =>
        {
            foreach (var (label, path) in slotPaths)
            {
                if (string.IsNullOrEmpty(path) || !File.Exists(path))
                {
                    hashBySlot[label] = null;
                    continue;
                }

                using var sha = SHA256.Create();
                using var stream = new FileStream(
                    path, FileMode.Open, FileAccess.Read, FileShare.Read,
                    BufferSize, FileOptions.SequentialScan);

                var hash = await sha.ComputeHashAsync(stream, ct);
                hashBySlot[label] = Convert.ToHexString(hash);
            }
        }, ct);

        var nonNullHashes = hashBySlot.Values.Where(h => h != null).Distinct().ToList();
        var allIdentical = nonNullHashes.Count == 1;

        return new BinaryCompareResult(allIdentical, hashBySlot);
    }

    /// <summary>
    /// Hashes <paramref name="masterPath"/> then compares it against each path in
    /// <paramref name="copyPaths"/>.  Returns "All identical", "N/M identical", or an
    /// error string when the master file cannot be read.
    /// </summary>
    public async Task<string> CompareOneToManyAsync(
        string masterPath,
        IReadOnlyList<string> copyPaths,
        CancellationToken ct = default)
    {
        string? masterHash = null;

        await Task.Run(async () =>
        {
            if (!File.Exists(masterPath)) return;
            using var sha = SHA256.Create();
            await using var stream = new FileStream(
                masterPath, FileMode.Open, FileAccess.Read, FileShare.Read,
                BufferSize, FileOptions.SequentialScan);
            masterHash = Convert.ToHexString(await sha.ComputeHashAsync(stream, ct));
        }, ct);

        if (masterHash == null) return "Master file not found";

        int total = 0, identical = 0;

        await Task.Run(async () =>
        {
            foreach (var path in copyPaths)
            {
                ct.ThrowIfCancellationRequested();
                if (!File.Exists(path)) continue;
                total++;
                using var sha = SHA256.Create();
                await using var stream = new FileStream(
                    path, FileMode.Open, FileAccess.Read, FileShare.Read,
                    BufferSize, FileOptions.SequentialScan);
                if (Convert.ToHexString(await sha.ComputeHashAsync(stream, ct)) == masterHash)
                    identical++;
            }
        }, ct);

        if (total == 0) return "No cloud copies found";
        return identical == total ? "All identical" : $"{identical}/{total} identical";
    }
}
