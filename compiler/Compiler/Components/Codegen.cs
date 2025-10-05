using System.Collections.Frozen;
using System.Globalization;
using System.Text;
using Compiler.Models;
using Compiler.Util;
using Microsoft.Win32.SafeHandles;

namespace Compiler.Components;

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
        }.ToFrozenDictionary();

    static Codegen()
    {
        NumberFormatter.NaNSymbol = "NAN";
        NumberFormatter.NegativeInfinitySymbol = "-INFINITY";
        NumberFormatter.NumberGroupSeparator = "'";
        NumberFormatter.PositiveInfinitySymbol = "INFINITY";
    }

    private static readonly byte[] Preamble = Encoding.UTF8.GetBytes("#include <falafel.hh>\n");
    private static readonly byte[] MainEntry = Encoding.UTF8.GetBytes(
        "int main(int argc, const char** argv) {{"
    );
    private static readonly byte[] MainExit = Encoding.UTF8.GetBytes(
        "}Object::collect_cycles();return 0;}"
    );

    private readonly Dictionary<string, uint> _stringLiterals = [];
    private string _beforeMainDecls = "";
    private readonly StringBuilder _mainStatements = new();
    private string _afterMainDecls = "";
    private uint _stringCount = 0;
    private StringBuilder _currentBlock;

    public Codegen()
    {
        _currentBlock = _mainStatements;
    }

    public void GenerateCode(IEnumerable<TypeCheckedStatement> program, string? location)
    {
        GenerateCodeWithoutWriting(program);

        foreach (var (str, index) in _stringLiterals)
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

            _beforeMainDecls = $"auto {LiteralName(index)} = {strAllocation};{_beforeMainDecls}";
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
                var typeString = RcPointerWrap(v.Type);
                var statement = $"{typeString} {v.Name} = {TranslateExpression(v.Value)};";
                _currentBlock.Append(statement);
            }
            else if (node is TypeCheckedAssignment a)
            {
                string statement;
                if (a.Lhs is TypeCheckedIndexAccess ia)
                {
                    var base_ = TranslateExpression(ia.Base);
                    var index = TranslateExpression(ia.Index);
                    var rhs = TranslateExpression(a.Rhs);
                    statement =
                        $"{base_}{(ia.Base.Type.IsObject ? "->" : ".")}_indexset({index}, {rhs});";
                }
                else
                {
                    statement = $"{TranslateExpression(a.Lhs)} = {TranslateExpression(a.Rhs)};";
                }
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
            else if (node is TypeCheckedReturnStatement rs)
            {
                var value = rs.Value is null ? "" : TranslateExpression(rs.Value);
                _currentBlock.Append($"return {value};");
            }
            else if (node is TypeCheckedFunctionDeclaration fd)
            {
                var functionSignature =
                    $@"{
                        RcPointerWrap(fd.Method.ReturnType)
                    } {
                        MangleMethodName(fd.Method)
                    }({
                        string.Join(", ", fd.Arguments.Select(a => $"{RcPointerWrap(a.Type)} {a.Name}"))
                    })";

                _beforeMainDecls += functionSignature + ";";

                var oldBlock = _currentBlock;
                _currentBlock = new StringBuilder();

                GenerateCodeWithoutWriting(fd.Body);

                _afterMainDecls += $"{functionSignature}{{ {_currentBlock.ToString()} }}";
                _currentBlock = oldBlock;
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
            return $"{MangleMethodName(fc.Method)}({string.Join(", ", fc.Arguments.Select(TranslateExpression))})";
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
                return "(Int)" + valueString;
            }
        }
        else if (expr is TypedDecimalLiteral dl)
        {
            var valueString = dl.Value.ToString("g17", NumberFormatter);

            if (double.IsFinite(dl.Value))
            {
                if (!valueString.Contains('.') && !valueString.Contains('e'))
                {
                    valueString += ".0";
                }

                if (dl.Type == BuiltIns.Float)
                {
                    return valueString + 'f';
                }
            }

            return valueString;
        }
        else if (expr is TypeCheckedStringLiteral sl)
        {
            if (sl.Value == "")
            {
                return "String::empty";
            }

            if (_stringLiterals.TryAdd(sl.Value, _stringCount))
            {
                ++_stringCount;
            }

            return LiteralName(_stringLiterals[sl.Value]);
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

                string rhsString;
                if (o.Rhs is null)
                {
                    rhsString = "";
                }
                else if (o.Operator.LambdaWrapRhs)
                {
                    var oldBlock = _currentBlock;
                    var newBlock = new StringBuilder();

                    _currentBlock = newBlock;
                    var innerString = TranslateExpression(o.Rhs);
                    _currentBlock = oldBlock;

                    rhsString = $"([&] {{ {newBlock.ToString()} return {innerString}; }})";
                }
                else
                {
                    rhsString = $" ({TranslateExpression(o.Rhs)})";
                }

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
            if (si.Pieces.All(x => x is TypeCheckedStringLiteral))
            {
                var value = string.Join(
                    "",
                    si.Pieces.Cast<TypeCheckedStringLiteral>().Select(x => x.Value)
                );
                return TranslateExpression(new TypeCheckedStringLiteral { Value = value });
            }

            var sbNum = _stringCount;
            ++_stringCount;

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
        else if (expr is TypeCheckedIndexAccess ia)
        {
            var base_ = TranslateExpression(ia.Base);
            var index = TranslateExpression(ia.Index);
            return $"{base_}{(ia.Base.Type.IsObject ? "->" : ".")}_indexget({index})";
        }
        else if (expr is TypeCheckedCastExpression cast)
        {
            var inner = TranslateExpression(cast.Base);
            var typeString = RcPointerWrap(cast.Type);
            if (cast.Base.Type.IsStrictSuperclassOf(cast.Type))
            {
                // Call the `explicit` constructor on RcPointer, which calls dynamic_cast
                return $"{typeString}{{ {inner} }}";
            }
            return $"static_cast<{typeString} >({inner})";
        }
        else if (expr is TypeCheckedArrayLiteral al)
        {
            return $"{RcPointerWrap(al.Type)}({{ {string.Join(", ", al.Values.Select(TranslateExpression))} }})";
        }
        else if (expr is TypeCheckedPropertyAccess pa)
        {
            var base_ = TranslateExpression(pa.Base);
            return $"{base_}{(pa.Base.Type.IsObject ? "->" : ".")}{pa.Property.Name}";
        }
        else if (expr is TypeCheckedMethodCall mc)
        {
            var base_ = TranslateExpression(mc.Base);
            return $@"{
                base_
            }{
                (mc.Base.Type.IsObject ? "->" : ".")
            }{
                MangleMethodName(mc.Method)
            }({
                string.Join(", ", mc.Arguments.Select(TranslateExpression))
            })";
        }
        else if (expr is TypeCheckedCharLiteral cl)
        {
            return $@"u8'\x{cl.Value:x2}'";
        }
        else if (expr is TypeCheckedNullLiteral nl)
        {
            return RcPointerWrap(nl.Type) + "(nullptr)";
        }
        else
        {
            throw new Exception($"Unrecognized type {expr.GetType()}");
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

    private static string LiteralName(uint index) => $"stringLiteral{index:X}";

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

    private const ulong PrimitiveEncoding = 0b00UL;
    private const ulong ObjectEncoding = 0b01UL;
    private const ulong StructEncoding = 0b10UL;
    private const ulong GenericTypeEncoding = 0b11UL;

    private static string MangleMethodName(Method m)
    {
        ulong argumentEncoding = 1UL;

        for (int i = 0; i < m.ArgumentTypes.Length; ++i)
        {
            argumentEncoding <<= 2;
            if (m.OriginallyGenericArguments[1 << i])
            {
                argumentEncoding |= GenericTypeEncoding;
            }
            else if (m.ArgumentTypes[i].IsObject)
            {
                argumentEncoding |= ObjectEncoding;
            }
            else if (BuiltIns.PrimitiveTypes.Contains(m.ArgumentTypes[i]))
            {
                argumentEncoding |= PrimitiveEncoding;
            }
            else
            {
                argumentEncoding |= StructEncoding;
            }
        }

        return $"f_{m.Name}{MangleReturnType(m.ReturnType)}{BaseConverter.ToBase63(argumentEncoding)}";
    }

    private static string MangleReturnType(Models.Type t)
    {
        if (t.IsGenericPlaceholder)
        {
            return "t";
        }
        if (BuiltIns.PrimitiveTypes.Contains(t))
        {
            return char.ToLowerInvariant(t.Name[0]).ToString();
        }
        if (t == BuiltIns.String)
        {
            return "s";
        }
        if (t == BuiltIns.Object)
        {
            return "o";
        }
        if (t.IsInstantiationOf(BuiltIns.Array))
        {
            return 'a' + MangleReturnType(t.GenericTypes.Single());
        }
        if (t.IsInstantiationOf(BuiltIns.Optional))
        {
            return 'o' + MangleReturnType(t.GenericTypes.Single());
        }

        char firstChar;
        if (t.IsObject)
        {
            firstChar = 'O';
        }
        else
        {
            firstChar = 'S';
        }

        var result = $"{firstChar}{t.Name.Length}{t.Name}";
        if (t.GenericTypes.Count > 0)
        {
            result +=
                $"{t.GenericTypes.Count}_{string.Join("", t.GenericTypes.Select(MangleReturnType))}";
        }
        return result;
    }

    private static string RcPointerWrap(Models.Type t)
    {
        var arguments = t.GenericTypes.Select(RcPointerWrap);
        var fullName =
            t.GenericTypes.Count > 0 ? $"{t.Name}<{string.Join(", ", arguments)} >" : t.Name;
        if (t.IsObject)
        {
            return $"RcPointer<{fullName} >";
        }
        else
        {
            return fullName;
        }
    }
}
