namespace Compiler.Models;

public interface TypeCheckedStatement
{
    public bool IsReturn => false;
}

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
    public TypeCheckedExpression Lhs { get; set; }
    public TypeCheckedExpression Rhs { get; set; }
}

public class TypeCheckedConditional : TypeCheckedStatement
{
    public TypeCheckedExpression Condition { get; set; }
    public IEnumerable<TypeCheckedStatement> TrueBlock { get; set; }
    public IEnumerable<TypeCheckedStatement> FalseBlock { get; set; }
    public bool IsReturn => TrueBlock.Any(x => x.IsReturn) && FalseBlock.Any(x => x.IsReturn);
}

public class TypeCheckedLoop : TypeCheckedStatement
{
    public TypeCheckedExpression Condition { get; set; }
    public IEnumerable<TypeCheckedStatement> Body { get; set; }
}

public class TypeCheckedFunctionArgument
{
    public string Name { get; set; }
    public Type Type { get; set; }
}

public class TypeCheckedFunctionDeclaration : TypeCheckedStatement
{
    public Method Method { get; set; }
    public IEnumerable<TypeCheckedStatement> Body { get; set; }
    public IEnumerable<TypeCheckedFunctionArgument> Arguments { get; set; }
}

public class TypeCheckedReturnStatement : TypeCheckedStatement
{
    public TypeCheckedExpression? Value { get; set; }
    bool TypeCheckedStatement.IsReturn => true;
}

public interface TypeCheckedExpression : TypeCheckedStatement
{
    Type Type { get; }
}

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
    Type TypeCheckedExpression.Type => BuiltIns.String;
}

public class TypeCheckedIdentifier : TypeCheckedExpression
{
    public Type Type { get; set; }
    public string Name { get; set; }
}

public class TypeCheckedFunctionCall : TypeCheckedExpression
{
    public Method Method { get; set; }
    public IEnumerable<TypeCheckedExpression> Arguments { get; set; }

    Type TypeCheckedExpression.Type => Method.ReturnType;
}

public class TypeCheckedStringInterpolation : TypeCheckedExpression
{
    public IEnumerable<TypeCheckedExpression> Pieces { get; set; }
    Type TypeCheckedExpression.Type => BuiltIns.String;
}

public class TypeCheckedOperatorCall : TypeCheckedExpression
{
    public Operator Operator { get; set; }
    public TypeCheckedExpression? Lhs { get; set; }
    public TypeCheckedExpression? Rhs { get; set; }

    Type TypeCheckedExpression.Type => Operator.ReturnType;
}

public class TypeCheckedBooleanLiteral : TypeCheckedExpression
{
    public bool Value { get; set; }
    Type TypeCheckedExpression.Type => BuiltIns.Bool;
}

public class TypeCheckedIndexAccess : TypeCheckedExpression
{
    public TypeCheckedExpression Base { get; set; }
    public TypeCheckedExpression Index { get; set; }

    public Subscript Subscript => Base.Type.Subscript ?? throw new InvalidOperationException();

    Type TypeCheckedExpression.Type =>
        Base.Type.Subscript?.ReturnType ?? throw new InvalidOperationException();
}

public class TypeCheckedCastExpression : TypeCheckedExpression
{
    public TypeCheckedExpression Base { get; set; }
    public Type Type { get; set; }
}

public class TypeCheckedArrayLiteral : TypeCheckedExpression
{
    public IEnumerable<TypeCheckedExpression> Values { get; set; }
    public Type Type { get; set; }
}

public class TypeCheckedNullLiteral : TypeCheckedExpression
{
    public Type Type { get; set; }
}

public class TypeCheckedPropertyAccess : TypeCheckedExpression
{
    public TypeCheckedExpression Base { get; set; }
    public Property Property { get; set; }

    Type TypeCheckedExpression.Type => Property.Type;
}

public class TypeCheckedMethodCall : TypeCheckedExpression
{
    public TypeCheckedExpression Base { get; set; }
    public IEnumerable<TypeCheckedExpression> Arguments { get; set; }
    public Method Method { get; set; }

    Type TypeCheckedExpression.Type => Method.ReturnType;
}

public class TypeCheckedCharLiteral : TypeCheckedExpression
{
    public byte Value { get; set; }

    Type TypeCheckedExpression.Type => BuiltIns.Char;
}
