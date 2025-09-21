using System.Globalization;
using System.Text;
using Compiler.Models;
using Microsoft.Win32.SafeHandles;

namespace Compiler.Util;

public class Codegen
{
    private const int MaxShortStringLength = 7;
    private static readonly NumberFormatInfo NumberFormatter = new();
    private static readonly IReadOnlyDictionary<char, string> SpecialEscapeSequences =
        new Dictionary<char, string>
        {
            { '\n', @"\n" },
            { '\r', @"\r" },
            { '\t', @"\t" },
            { '\v', @"\v" },
            { '\\', @"\\" },
            { '"', "\\\"" },
            { '\x07', @"\a" },
            { '\x08', @"\b" },
            { '\x7f', @"\177" },
        };

    static Codegen()
    {
        NumberFormatter.NaNSymbol = "NAN";
        NumberFormatter.NegativeInfinitySymbol = "-INFINITY";
        NumberFormatter.NumberGroupSeparator = "'";
        NumberFormatter.PositiveInfinitySymbol = "INFINITY";
    }

    private static readonly byte[] Preamble = Encoding.UTF8.GetBytes("#include <runtime.hh>\n");
    private static readonly byte[] MainEntry = Encoding.UTF8.GetBytes(
        "int main(int argc, const char** argv) {"
    );
    private static readonly byte[] MainExit = Encoding.UTF8.GetBytes("return 0;}");

    private HashSet<string> _stringLiterals = [];
    private string _beforeMainDecls = "";
    private string _mainStatements = "";
    private string _afterMainDecls = "";
    private int _stringBuilderCount = 0;

    public void GenerateCode(IEnumerable<TypeCheckedStatement> program, string? location)
    {
        using var stream = location is null
            ? new FileStream(new SafeFileHandle(1, false), FileAccess.Write)
            : new FileStream(location, FileMode.Create);

        foreach (var node in program)
        {
            if (node is TypeCheckedClass)
            {
                throw new NotImplementedException();
            }
            else if (node is TypeCheckedVar v)
            {
                var typeString = v.Type.IsObject ? $"RcPointer<{v.Type}>" : v.Type.ToString();
                var statement = $"{typeString} {v.Name} = {TranslateExpression(v.Value)};";
                _mainStatements += statement;
            }
            else if (node is TypeCheckedExpression expr)
            {
                var statement = TranslateExpression(expr) + ";";
                _mainStatements += statement;
            }
            else
            {
                throw new ArgumentException($"Invalid type {node.GetType()}", nameof(program));
            }
        }

        foreach (var str in _stringLiterals)
        {
            string strAllocation;

            var utf8Length = Encoding.UTF8.GetByteCount(str);
            var escaped = EscapeLiteralUtf8(str);
            if (utf8Length < MaxShortStringLength)
            {
                strAllocation = $"String::allocate_small_utf8(u8\"{escaped}\")";
            }
            else
            {
                strAllocation = $"String::allocate_immortal_utf8(u8\"{escaped}\")";
            }

            _beforeMainDecls = $"auto {LiteralName(str)} = {strAllocation};{_beforeMainDecls}";
        }

        EmitCode(stream);
    }

    private string TranslateExpression(TypeCheckedExpression expr)
    {
        if (expr is TypeCheckedFunctionCall fc)
        {
            return $"{fc.Method.Name}({string.Join(", ", fc.Arguments.Select(TranslateExpression))})";
        }
        else if (expr is TypedIntegerLiteral il)
        {
            var valueString = il.Value.ToString("R", NumberFormatter);
            if (il.Type == BuiltIns.Float)
            {
                return valueString + 'f';
            }
            else if (il.Type == BuiltIns.Double)
            {
                return "(double)" + valueString;
            }
            else
            {
                return valueString;
            }
        }
        else if (expr is TypedDecimalLiteral dl)
        {
            var valueString = dl.Value.ToString("g17", NumberFormatter);
            if (dl.Type == BuiltIns.Float)
            {
                return valueString + 'f';
            }
            else
            {
                return valueString;
            }
        }
        else if (expr is TypeCheckedStringLiteral sl)
        {
            _stringLiterals.Add(sl.Value);
            return LiteralName(sl.Value);
        }
        else if (expr is TypeCheckedIdentifier i)
        {
            return i.Name;
        }
        else if (expr is TypeCheckedOperatorCall o)
        {
            if (o.Operator.IsCppOperator)
            {
                var lhsString = o.Lhs is null ? "" : $"({TranslateExpression(o.Lhs)}) ";
                var rhsString = o.Rhs is null ? "" : $" ({TranslateExpression(o.Rhs)})";
                return lhsString + o.Operator.CppName + rhsString;
            }
            else
            {
                var arguments = new List<TypeCheckedExpression?> { o.Lhs, o.Rhs }
                    .Where(x => x is not null)
                    .Cast<TypeCheckedExpression>();
                return $"{o.Operator.CppName}({string.Join(", ", arguments.Select(TranslateExpression))})";
            }
        }
        else if (expr is TypeCheckedStringInterpolation si)
        {
            var sbNum = _stringBuilderCount;
            ++_stringBuilderCount;

            _mainStatements += $"StringBuilder sb{sbNum}({si.Pieces.Count()});";
            foreach (var piece in si.Pieces)
            {
                var statement = $"sb{sbNum}.add_piece({TranslateExpression(piece)});";
                _mainStatements += statement;
            }

            return $"sb{sbNum}.build()";
        }
        else if (expr is TypeCheckedBooleanLiteral bl)
        {
            return bl.Value ? "true" : "false";
        }
        else
        {
            throw new Exception($"COMPILER BUG: Unrecognized type {expr.GetType()}");
        }
    }

    private void EmitCode(FileStream stream)
    {
        stream.Write(Preamble, 0, Preamble.Length);

        var beforeMainBytes = Encoding.UTF8.GetBytes(_beforeMainDecls);
        if (beforeMainBytes.Length > 0)
        {
            stream.Write(beforeMainBytes, 0, beforeMainBytes.Length);
        }

        stream.Write(MainEntry, 0, MainEntry.Length);

        var mainBytes = Encoding.UTF8.GetBytes(_mainStatements);
        if (mainBytes.Length > 0)
        {
            stream.Write(mainBytes, 0, mainBytes.Length);
        }

        stream.Write(MainExit, 0, MainExit.Length);

        var afterMainBytes = Encoding.UTF8.GetBytes(_afterMainDecls);
        if (afterMainBytes.Length > 0)
        {
            stream.Write(afterMainBytes, 0, afterMainBytes.Length);
        }

        stream.WriteByte(10);
    }

    private static string LiteralName(string s) => $"stringLiteral_{((uint)s.GetHashCode()):X8}";

    private static string EscapeLiteralUtf8(string s)
    {
        var builder = new StringBuilder(s.Length);

        for (var i = 0; i < s.Length; ++i)
        {
            var c = s[i];
            if (SpecialEscapeSequences.TryGetValue(c, out var escape))
            {
                builder.Append(escape);
            }
            else if (c < ' ')
            {
                builder.Append($"\\x{((int)c):x2}");
            }
            else
            {
                builder.Append(c);
            }
        }

        return builder.ToString();
    }
}
