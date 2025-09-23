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

    private readonly HashSet<string> _stringLiterals = [];
    private string _beforeMainDecls = "";
    private readonly StringBuilder _mainStatements = new();
    private string _afterMainDecls = "";
    private int _stringBuilderCount = 0;
    private StringBuilder _currentBlock;

    public Codegen()
    {
        _currentBlock = _mainStatements;
    }

    public void GenerateCode(IEnumerable<TypeCheckedStatement> program, string? location)
    {
        GenerateCodeWithoutWriting(program);

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

        using Stream stream = location is null
            ? Console.OpenStandardOutput()
            : new FileStream(location, FileMode.Create);

        EmitCode(stream);
    }

    private void GenerateCodeWithoutWriting(IEnumerable<TypeCheckedStatement> program)
    {
        foreach (var node in program)
        {
            if (node is TypeCheckedClass)
            {
                throw new NotImplementedException();
            }
            else if (node is TypeCheckedVar v)
            {
                var typeString = v.Type.IsObject ? $"RcPointer<{v.Type} >" : v.Type.ToString();
                var statement = $"{typeString} {v.Name} = {TranslateExpression(v.Value)};";
                _currentBlock.Append(statement);
            }
            else if (node is TypeCheckedAssignment a)
            {
                var statement = $"{a.Name} = {TranslateExpression(a.Value)};";
                _currentBlock.Append(statement);
            }
            else if (node is TypeCheckedConditional c)
            {
                var condition = TranslateExpression(c.Condition);

                var oldBlock = _currentBlock;
                var newBlock = new StringBuilder();

                oldBlock.Append("if (");
                oldBlock.Append(condition);
                oldBlock.Append(") {");

                _currentBlock = newBlock;

                GenerateCodeWithoutWriting(c.TrueBlock);

                if (c.FalseBlock.Any())
                {
                    oldBlock.Append(newBlock);
                    oldBlock.Append("} else {");

                    newBlock = new StringBuilder();
                    _currentBlock = newBlock;

                    GenerateCodeWithoutWriting(c.FalseBlock);
                }

                _currentBlock = oldBlock;
                oldBlock.Append(newBlock);
                oldBlock.Append("}");
            }
            else if (node is TypeCheckedLoop l)
            {
                var condition = TranslateExpression(l.Condition);

                var oldBlock = _currentBlock;
                var newBlock = new StringBuilder();

                oldBlock.Append("while (");
                oldBlock.Append(condition);
                oldBlock.Append(") {");

                _currentBlock = newBlock;

                GenerateCodeWithoutWriting(l.Body);

                _currentBlock = oldBlock;
                oldBlock.Append(newBlock);
                oldBlock.Append("}");
            }
            else if (node is TypeCheckedExpression expr)
            {
                var statement = TranslateExpression(expr) + ";";
                _currentBlock.Append(statement);
            }
            else
            {
                throw new ArgumentException($"Invalid type {node.GetType()}", nameof(program));
            }
        }
    }

    private string TranslateExpression(TypeCheckedExpression expr)
    {
        if (expr is TypeCheckedFunctionCall fc)
        {
            return $"{MangleMethodName(fc.Method.Name, fc.Method.ArgumentTypes)}({string.Join(", ", fc.Arguments.Select(TranslateExpression))})";
        }
        else if (expr is TypedIntegerLiteral il)
        {
            var valueString = il.Value.ToString("R", NumberFormatter);
            if (il.Type == BuiltIns.Float)
            {
                return valueString + ".0f";
            }
            else if (il.Type == BuiltIns.Double)
            {
                return valueString + ".0";
            }
            else
            {
                return valueString;
            }
        }
        else if (expr is TypedDecimalLiteral dl)
        {
            var valueString = dl.Value.ToString("g17", NumberFormatter);
            if (!valueString.Contains('.') && !valueString.Contains('e'))
            {
                valueString += ".0";
            }

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
            if (sl.Value == "")
            {
                return "String::empty";
            }

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

            _currentBlock.Append($"StringBuilder sb{sbNum}({si.Pieces.Count()}U);");
            foreach (var piece in si.Pieces)
            {
                var statement = $"sb{sbNum}.add_piece({TranslateExpression(piece)});";
                _currentBlock.Append(statement);
            }

            return $"sb{sbNum}.build()";
        }
        else if (expr is TypeCheckedBooleanLiteral bl)
        {
            return bl.Value ? "true" : "false";
        }
        else if (expr is TypeCheckedIndexGet ig)
        {
            var base_ = TranslateExpression(ig.Base);
            var index = TranslateExpression(ig.Index);
            return $"{base_}{(ig.Base.Type.IsObject ? "->" : ".")}_indexget({index})";
        }
        else if (expr is TypeCheckedCastExpression cast)
        {
            var inner = TranslateExpression(cast.Base);
            var typeString = cast.Type.IsObject ? $"RcPointer<{cast.Type} >" : cast.Type.ToString();
            if (cast.Base.Type.IsStrictSuperclassOf(cast.Type))
            {
                // Call the `explicit` constructor on RcPointer, which calls dynamic_cast
                return $"{typeString}{{ {inner} }}";
            }
            return $"static_cast<{typeString} >({inner})";
        }
        else if (expr is TypeCheckedArrayLiteral al)
        {
            return $"{al.Type}({{ {string.Join(", ", al.Values.Select(TranslateExpression))} }})";
        }
        else
        {
            throw new Exception($"COMPILER BUG: Unrecognized type {expr.GetType()}");
        }
    }

    private void EmitCode(Stream stream)
    {
        stream.Write(Preamble, 0, Preamble.Length);

        var beforeMainBytes = Encoding.UTF8.GetBytes(_beforeMainDecls);
        if (beforeMainBytes.Length > 0)
        {
            stream.Write(beforeMainBytes, 0, beforeMainBytes.Length);
        }

        stream.Write(MainEntry, 0, MainEntry.Length);

        var mainBytes = Encoding.UTF8.GetBytes(_mainStatements.ToString());
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

    private static string MangleMethodName(string original, IEnumerable<Models.Type> argumentTypes)
    {
        return original
            + string.Join(
                "_",
                argumentTypes
                    .Select((t, i) => (t, i))
                    .Where((t) => t.Item1.IsObject)
                    .Select((t) => t.Item2)
            );
    }
}
