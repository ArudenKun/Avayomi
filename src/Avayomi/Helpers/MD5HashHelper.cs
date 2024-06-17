using System;
using System.Security.Cryptography;
using System.Text;

namespace Avayomi.Helpers;

// ReSharper disable once InconsistentNaming
public static class MD5HashHelper
{
    [ThreadStatic]
    private static MD5? _threadInstance;

    private static MD5 HashAlgorithm => _threadInstance ??= MD5.Create();

    public static unsafe string ComputeHash(ReadOnlySpan<char> value)
    {
        var encoding = Encoding.UTF8;
        var bytesRequired = encoding.GetByteCount(value);
        Span<byte> bytes = stackalloc byte[bytesRequired];
        encoding.GetBytes(value, bytes);

        Span<byte> hashBytes = stackalloc byte[16];
        HashAlgorithm.TryComputeHash(bytes, hashBytes, out _);

        const int charArrayLength = 32;
        var charArrayPtr = stackalloc char[charArrayLength];

        var charPtr = charArrayPtr;
        for (var i = 0; i < 16; i++)
        {
            var hashByte = hashBytes[i];
            *charPtr++ = GetHexValue(hashByte >> 4);
            *charPtr++ = GetHexValue(hashByte & 0xF);
        }

        return new string(charArrayPtr, 0, charArrayLength);
    }

    //Based on byte conversion implementation in BitConverter (but with the dash stripped)
    //https://github.com/dotnet/coreclr/blob/fbc11ea6afdaa2fe7b9377446d6bb0bd447d5cb5/src/mscorlib/shared/System/BitConverter.cs#L409-L440
    private static char GetHexValue(int i)
    {
        if (i < 10)
        {
            return (char)(i + '0');
        }

        return (char)(i - 10 + 'A');
    }
}
