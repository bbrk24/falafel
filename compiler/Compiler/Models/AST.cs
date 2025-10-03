using System.Text.Json.Serialization;
using Compiler.Util;

namespace Compiler.Models;

public class AstRoot
{
    public IEnumerable<AstNode> Ast { get; set; }
    public List<ulong> LineCounts { get; set; }
}

public class Location
{
    public ulong Pos { get; set; }
    public ulong Length { get; set; }
}

public interface HasLocation
{
    Location? Loc { get; set; }
}

[JsonConverter(typeof(AstJsonConverter))]
public interface AstNode : HasLocation
{
    string Type { get; set; }
}

public class ConditionalStatement : AstNode
{
    public string Type { get; set; }
    public Expression Condition { get; set; }
    public IEnumerable<AstNode> TrueBlock { get; set; }
    public IEnumerable<AstNode>? FalseBlock { get; set; }
    public Location? Loc { get; set; }
}

[JsonConverter(typeof(AstJsonConverter))]
public interface AssignmentLhsAllowed : Expression { }

public class Assignment : AstNode
{
    public string Type { get; set; }
    public AssignmentLhsAllowed Lhs { get; set; }
    public Expression Rhs { get; set; }
    public Location? Loc { get; set; }
}

public class LoopStatement : AstNode
{
    public string Type { get; set; }
    public Expression Condition { get; set; }
    public IEnumerable<AstNode> Body { get; set; }
    public Location? Loc { get; set; }
}

[JsonConverter(typeof(AstJsonConverter))]
public interface Declaration : AstNode
{
    string Name { get; set; }
}

public class ClassDefinition : Declaration
{
    public string Type { get; set; }
    public string Name { get; set; }
    public AstType? Base { get; set; }
    public IEnumerable<Declaration> Body { get; set; }
    public bool Final { get; set; }
    public Location? Loc { get; set; }
}

public class VarDeclaration : Declaration
{
    public string Type { get; set; }
    public string Name { get; set; }
    public AstType DeclaredType { get; set; }
    public Expression Value { get; set; }
    public Location? Loc { get; set; }
}

public class AstType : HasLocation
{
    public string Name { get; set; }
    public IEnumerable<AstType> Arguments { get; set; }
    public Location? Loc { get; set; }

    public override string ToString()
    {
        if (Arguments.Any())
        {
            return $"{Name}<{string.Join(", ", Arguments)}>";
        }
        return Name;
    }
}

[JsonConverter(typeof(AstJsonConverter))]
public interface Expression : AstNode { }

[JsonConverter(typeof(AstJsonConverter))]
public interface MemberAccessRhsAllowed : AssignmentLhsAllowed { }

public class FunctionCall : MemberAccessRhsAllowed
{
    public string Type { get; set; }
    public string Function { get; set; }
    public IEnumerable<Expression> Arguments { get; set; }
    public Location? Loc { get; set; }
}

public class IntegerLiteral : Expression
{
    public string Type { get; set; }
    public long Value { get; set; }
    public Location? Loc { get; set; }
}

public class DecimalLiteral : Expression
{
    public string Type { get; set; }
    public double Value { get; set; }
    public Location? Loc { get; set; }
}

public class StringLiteral : Expression
{
    public string Type { get; set; }
    public string Value { get; set; }
    public Location? Loc { get; set; }
}

public class StringInterpolation : Expression
{
    public string Type { get; set; }
    public IEnumerable<Expression> Pieces { get; set; }
    public Location? Loc { get; set; }
}

public class BinaryExpression : Expression
{
    public string Type { get; set; }
    public Expression Lhs { get; set; }
    public Expression Rhs { get; set; }
    public string Operator { get; set; }
    public Location? Loc { get; set; }
}

public class Identifier : MemberAccessRhsAllowed
{
    public string Type { get; set; }
    public string Name { get; set; }
    public Location? Loc { get; set; }
}

public class BooleanLiteral : Expression
{
    public string Type { get; set; }
    public bool Value { get; set; }
    public Location? Loc { get; set; }
}

public class NullLiteral : Expression
{
    public string Type { get; set; }
    public Location? Loc { get; set; }
}

public class PrefixExpression : Expression
{
    public string Type { get; set; }
    public string Operator { get; set; }
    public Expression Operand { get; set; }
    public Location? Loc { get; set; }
}

public class CastExpression : AssignmentLhsAllowed
{
    public string Type { get; set; }
    public AstType DeclaredType { get; set; }
    public Expression Value { get; set; }
    public Location? Loc { get; set; }
}

public class IndexExpression : AssignmentLhsAllowed
{
    public string Type { get; set; }
    public Expression Base { get; set; }
    public Expression Index { get; set; }
    public Location? Loc { get; set; }
}

public class ArrayLiteral : Expression
{
    public string Type { get; set; }
    public IEnumerable<Expression> Values { get; set; }
    public Location? Loc { get; set; }
}

public class MemberAccessExpression : Expression
{
    public string Type { get; set; }
    public Expression Base { get; set; }
    public MemberAccessRhsAllowed Member { get; set; }
    public Location? Loc { get; set; }
}

public class CharLiteral : Expression
{
    public string Type { get; set; }
    public char Value { get; set; }
    public Location? Loc { get; set; }
}
