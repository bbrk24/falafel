namespace Compiler.Util;

public static class BaseConverter
{
    private static readonly char[] IdentifierAlphabet =
        "abcdefghijklmnopqrstuvwxyz0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ_".ToCharArray();

    public static string ToBase63(ulong value)
    {
        List<char> resultBackwards = [];

        while (value != 0UL)
        {
            var index = (int)(value % (ulong)IdentifierAlphabet.Length);
            value /= (ulong)IdentifierAlphabet.Length;
            resultBackwards.Add(IdentifierAlphabet[index]);
        }

        resultBackwards.Reverse();

        return string.Join("", resultBackwards);
    }
}
