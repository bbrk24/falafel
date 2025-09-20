using System.Text.Json.Serialization;
using Compiler.Util;

namespace Compiler.Models;

[JsonConverter(typeof(AstJsonConverter))]
public interface AstNode
{
    string Type { get; set; }
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
    public string Base { get; set; }
    public IEnumerable<Declaration> Body { get; set; }
}

public class VarDeclaration : Declaration
{
    public string Type { get; set; }
    public string Name { get; set; }
    public AstType DeclaredType { get; set; }
    public Expression Value { get; set; }
}

public class AstType
{
    public string Name { get; set; }
    public IEnumerable<AstType> Arguments { get; set; }
}

[JsonConverter(typeof(AstJsonConverter))]
public interface Expression : AstNode { }

public class FunctionCall : Expression
{
    public string Type { get; set; }
    public string Function { get; set; }
    public IEnumerable<Expression> Arguments { get; set; }
}

public class NumberLiteral : Expression
{
    public string Type { get; set; }
    public decimal Value { get; set; }
}

public class StringLiteral : Expression
{
    public string Type { get; set; }
    public string Value { get; set; }
}

public class BinaryExpression : Expression
{
    public string Type { get; set; }
    public Expression Lhs { get; set; }
    public Expression Rhs { get; set; }
    public string Operator { get; set; }
}
