using Compiler.Models;

namespace Compiler.Util;

public class TypeChecker
{
    private List<Models.Type> _knownTypes;
    private List<Variable> _knownVariables;
    private List<Method> _knownFunctions = BuiltIns.Methods.ToList();

    public TypeChecker()
    {
        _knownTypes = BuiltIns.Types.ToList();
        _knownVariables = [];
    }

    public TypeChecker(TypeChecker other)
    {
        _knownTypes = [.. other._knownTypes];
        _knownVariables = [.. other._knownVariables];
    }

    public void CheckTypes(IEnumerable<AstNode> program)
    {
        foreach (var cd in program.OfType<ClassDefinition>())
        {
            CheckClassDefinition(cd);
        }

        foreach (var node in program.Where(node => node is not ClassDefinition))
        {
            if (node is VarDeclaration vd)
            {
                var declType =
                    LookupType(vd.DeclaredType)
                    ?? throw new Exception($"Unrecognized type {vd.DeclaredType}");
                CheckExpressionType(vd.Value, declType);
                var variable = new Variable { Name = vd.Name, Type = declType };
                _knownVariables.Add(variable);
            }
            else if (node is Expression e)
            {
                CheckExpressionType(e, null);
            }
            else
            {
                throw new ArgumentException($"Unrecognized type {node.GetType()}");
            }
        }
    }

    private static void ExpectOneSuccess<T>(IEnumerable<T> options, Action<T> action)
        where T : notnull
    {
        var exceptions = new List<Exception>();
        var successes = new List<T>();
        foreach (var opt in options)
        {
            try
            {
                action(opt);
                successes.Add(opt);
            }
            catch (Exception e)
            {
                exceptions.Add(e);
            }
        }

        switch (successes.Count)
        {
            case 0:
                throw new AggregateException(exceptions);
            case 1:
                return;
            default:
                throw new Exception(
                    "Multiple matches found: "
                        + string.Join(", ", successes.Select(s => s.ToString()))
                );
        }
    }

    public void CheckExpressionType(Expression expr, Models.Type? expectedType)
    {
        if (expr is FunctionCall fc)
        {
            var candidateFunctions = _knownFunctions.Where(f =>
                f.ThisType == BuiltIns.Void && f.Name == fc.Function
            );

            if (!candidateFunctions.Any())
            {
                throw new Exception($"No known functions match name {fc.Function}");
            }

            if (expectedType is not null)
            {
                candidateFunctions = candidateFunctions.Where(f => f.ReturnType == expectedType);

                if (!candidateFunctions.Any())
                {
                    throw new Exception($"No overloads of {fc.Function} return {expectedType}");
                }
            }

            candidateFunctions = candidateFunctions.Where(f =>
                fc.Arguments.Count() == f.ArgumentTypes.Count
            );
            if (!candidateFunctions.Any())
            {
                throw new Exception(
                    $"No overloads of {fc.Function} take {fc.Arguments.Count()} arguments"
                );
            }

            ExpectOneSuccess(
                candidateFunctions,
                (f) =>
                {
                    foreach (
                        var (argExpr, argType) in Enumerable.Zip(fc.Arguments, f.ArgumentTypes)
                    )
                    {
                        CheckExpressionType(argExpr, argType);
                    }
                }
            );
        }
        else if (expr is NumberLiteral nl)
        {
            if (expectedType is null)
            {
                throw new Exception("A number literal should not be a statement by itself.");
            }

            if (
                expectedType != BuiltIns.Int
                && expectedType != BuiltIns.Float
                && expectedType != BuiltIns.Double
            )
            {
                throw new Exception($"{expectedType} cannot be expressed by number literal");
            }

            if (nl.IsDecimal && expectedType == BuiltIns.Int)
            {
                throw new Exception("Int cannot be represented by decimal literal");
            }
        }
        else if (expr is StringLiteral)
        {
            if (expectedType != BuiltIns.String)
            {
                throw new Exception("Only String instances can be represented by string literals");
            }
        }
        else if (expr is BinaryExpression be)
        {
            var ops = BuiltIns.Operators.Where(op =>
                op.Fixity == OperatorFixity.Infix && op.Name == be.Operator
            );

            if (expectedType is not null)
            {
                ops = ops.Where(op => op.ReturnType == expectedType);

                if (!ops.Any())
                {
                    throw new Exception($"Operator {be.Operator} cannot produce {expectedType}");
                }
            }

            ExpectOneSuccess(
                ops,
                (op) =>
                {
                    CheckExpressionType(be.Lhs, op.LhsType);
                    CheckExpressionType(be.Rhs, op.RhsType);
                }
            );
        }
        else if (expr is Identifier i)
        {
            var variable =
                _knownVariables.SingleOrDefault(v => v.Name == i.Name)
                ?? throw new Exception($"Unrecognized identifier {i.Name}");

            if (expectedType is not null && variable.Type != expectedType)
            {
                throw new Exception(
                    $"Type mismatch: {variable.Name} is {variable.Type}; expected {expectedType}"
                );
            }
        }
        else
        {
            throw new ArgumentException("Unrecognized expression type", nameof(expr));
        }
    }

    private void CheckClassDefinition(ClassDefinition cd)
    {
        Models.Type? baseType;
        if (cd.Base is null)
        {
            baseType = BuiltIns.Object;
        }
        else
        {
            baseType = LookupType(cd.Base) ?? throw new Exception($"Unrecognized type {cd.Base}");
            if (!baseType.IsInheritable)
            {
                throw new Exception($"{cd.Base} is not inheritable");
            }
        }

        var thisType = new ConcreteType
        {
            Name = cd.Name,
            BaseType = baseType,
            IsObject = true,
            IsInheritable = !cd.Final,
        };
        _knownTypes.Add(thisType);

        // TODO: add variables declared in here to thisType.Properties
        new TypeChecker(this).CheckTypes(cd.Body);
    }

    public Models.Type? LookupType(AstType type)
    {
        if (type.Arguments.Any())
        {
            // TODO
            return null;
        }

        return _knownTypes.SingleOrDefault(t => t.Name == type.Name);
    }
}
