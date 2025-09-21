namespace Compiler.Models;

public interface TypeCheckedStatement { }

public class TypeCheckedClass : TypeCheckedStatement
{
    public Type Type { get; set; }
    public IEnumerable<TypeCheckedStatement> Body { get; set; }
}

public class TypeCheckedVar : TypeCheckedStatement
{
    public string Name { get; set; }
    public Type Type { get; set; }
    public TypeCheckedExpression Value { get; set; }
}

public class TypeCheckedAssignment : TypeCheckedStatement
{
    public string Name { get; set; }
    public TypeCheckedExpression Value { get; set; }
}

public class TypeCheckedConditional : TypeCheckedStatement
{
    public TypeCheckedExpression Condition { get; set; }
    public IEnumerable<TypeCheckedStatement> TrueBlock { get; set; }
    public IEnumerable<TypeCheckedStatement> FalseBlock { get; set; }
}

public class TypeCheckedLoop : TypeCheckedStatement
{
    public TypeCheckedExpression Condition { get; set; }
    public IEnumerable<TypeCheckedStatement> Body { get; set; }
}

public interface TypeCheckedExpression : TypeCheckedStatement { }

public class TypedIntegerLiteral : TypeCheckedExpression
{
    public Type Type { get; set; }
    public long Value { get; set; }
}

public class TypedDecimalLiteral : TypeCheckedExpression
{
    public Type Type { get; set; }
    public double Value { get; set; }
}

public class TypeCheckedStringLiteral : TypeCheckedExpression
{
    public string Value { get; set; }
}

public class TypeCheckedIdentifier : TypeCheckedExpression
{
    public string Name { get; set; }
}

public class TypeCheckedFunctionCall : TypeCheckedExpression
{
    public Method Method { get; set; }
    public IEnumerable<TypeCheckedExpression> Arguments { get; set; }
}

public class TypeCheckedStringInterpolation : TypeCheckedExpression
{
    public IEnumerable<TypeCheckedExpression> Pieces { get; set; }
}

public class TypeCheckedOperatorCall : TypeCheckedExpression
{
    public Operator Operator { get; set; }
    public TypeCheckedExpression? Lhs { get; set; }
    public TypeCheckedExpression? Rhs { get; set; }
}

public class TypeCheckedBooleanLiteral : TypeCheckedExpression
{
    public bool Value { get; set; }
}
