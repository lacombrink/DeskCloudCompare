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
}
