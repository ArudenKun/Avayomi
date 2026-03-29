using Avayomi.Core.Extensions;

namespace Avayomi.Core;

internal sealed class Unbaser
{
    private readonly int _value;

    public Unbaser(int value)
    {
        _value = value;
    }

    private readonly Dictionary<int, string> _alphabet = new()
    {
        [52] = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOP",
        [54] = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQR",
        [62] = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ",
        [95] =
            " !\\\"#\\$%&\\\\'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\\\]^_`abcdefghijklmnopqrstuvwxyz{|}~",
    };

    public int Unbase(string value)
    {
        if (_value is >= 2 and <= 36)
            return value.ToIntOrNull(_value) ?? 0;

        var selector = _value switch
        {
            > 62 => 95,
            > 54 => 62,
            > 52 => 54,
            _ => 52,
        };

        var dict = _alphabet[selector].ToCharArray();

        var returnVal = 0;

        var valArray = value.ToCharArray().AsEnumerable().Reverse().ToArray();
        for (var i = 0; i < valArray.Length; i++)
        {
            var cipher = valArray[i];
            returnVal += (int)(Math.Pow(_value, i) * (dict?[cipher] ?? 0));
        }

        return returnVal;
    }
}
