using System.Security.Cryptography;
using System.Text;

namespace ElectricityOffNotifier.AppHost.Auth;

public static class AuthHelper
{
    private const int HmacSizeBytes = 32; // 256 bits

    public static string ToHmacSha256Base64String(this string input, ReadOnlySpan<byte> secretKeyBytes)
    {
        ReadOnlySpan<char> inputSpan = input.AsSpan();
        Span<byte> inputBytesSpan = stackalloc byte[Encoding.ASCII.GetMaxByteCount(inputSpan.Length)];
        int inputBytesWritten = Encoding.ASCII.GetBytes(inputSpan, inputBytesSpan);

        Span<byte> hashBytesSpan = stackalloc byte[HmacSizeBytes];
        int hashBytesWritten = HMACSHA256.HashData(secretKeyBytes, inputBytesSpan[..inputBytesWritten], hashBytesSpan);

        string hashBase64 = Convert.ToBase64String(hashBytesSpan[..hashBytesWritten]);
        return hashBase64;
    }

    public static string ToHmacSha256Base64String(this string input, string secretKeyBase64)
    {
        Span<byte> secretKeyBytes = stackalloc byte[GetOriginalLengthInBytes(secretKeyBase64)];
        if (!Convert.TryFromBase64String(secretKeyBase64, secretKeyBytes, out int bytesWritten))
            throw new InvalidOperationException("Invalid base64 input string");

        return input.ToHmacSha256Base64String(secretKeyBytes[..bytesWritten]);
    }

    public static byte[] ToHmacSha256ByteArray(this string input, ReadOnlySpan<byte> secretKeyBytes)
    {
        ReadOnlySpan<char> inputSpan = input.AsSpan();
        Span<byte> inputBytesSpan = stackalloc byte[Encoding.ASCII.GetMaxByteCount(inputSpan.Length)];
        int inputBytesWritten = Encoding.ASCII.GetBytes(inputSpan, inputBytesSpan);

        byte[] hashBytes = HMACSHA256.HashData(secretKeyBytes, inputBytesSpan[..inputBytesWritten]);
        return hashBytes;
    }

    public static byte[] ToHmacSha256ByteArray(this string input, string secretKeyBase64)
    {
        Span<byte> secretKeyBytes = stackalloc byte[GetOriginalLengthInBytes(secretKeyBase64)];
        if (!Convert.TryFromBase64String(secretKeyBase64, secretKeyBytes, out int bytesWritten))
            throw new InvalidOperationException("Invalid base64 input string");

        return input.ToHmacSha256ByteArray(secretKeyBytes[..bytesWritten]);
    }

    private static int GetOriginalLengthInBytes(string base64String)
    {
        if (string.IsNullOrEmpty(base64String))
            return 0;

        ReadOnlySpan<char> base64CharArray = base64String.AsSpan();
        int characterCount = base64CharArray.Length;
        ReadOnlySpan<char> paddingSlice = base64CharArray.Slice(characterCount - 2, 2);

        var paddingCount = 0;
        foreach (ref readonly char c in paddingSlice)
            if (c == '=')
                paddingCount++;

        return 3 * (characterCount / 4) - paddingCount;
    }
}