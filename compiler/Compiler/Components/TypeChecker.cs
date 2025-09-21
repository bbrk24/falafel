using Compiler.Models;

namespace Compiler.Components;

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

    public IEnumerable<TypeCheckedStatement> CheckTypes(IEnumerable<AstNode> program)
    {
        foreach (var cd in program.OfType<ClassDefinition>())
        {
            yield return CheckClassDefinition(cd);
        }

        foreach (var node in program.Where(node => node is not ClassDefinition))
        {
            if (node is VarDeclaration vd)
            {
                var declType =
                    LookupType(vd.DeclaredType)
                    ?? throw new Exception($"Unrecognized type {vd.DeclaredType}");
                var checkedValue = CheckExpressionType(vd.Value, declType);
                var variable = new Variable { Name = vd.Name, Type = declType };
                _knownVariables.Add(variable);
                yield return new TypeCheckedVar
                {
                    Name = vd.Name,
                    Type = declType,
                    Value = checkedValue,
                };
            }
            else if (node is Expression e)
            {
                yield return CheckExpressionType(e, null);
            }
            else
            {
                throw new ArgumentException($"Unrecognized type {node.GetType()}");
            }
        }
    }

    private static TReturn ExpectOneSuccess<TOption, TReturn>(
        IEnumerable<TOption> options,
        Func<TOption, TReturn> transform
    )
        where TOption : notnull
    {
        var exceptions = new List<Exception>();
        var successes = new List<(TOption, TReturn)>();
        foreach (var opt in options)
        {
            try
            {
                successes.Add((opt, transform(opt)));
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
                return successes[0].Item2;
            default:
                throw new Exception(
                    "Multiple matches found: "
                        + string.Join(", ", successes.Select(s => s.Item1.ToString()))
                );
        }
    }

    public TypeCheckedExpression CheckExpressionType(Expression expr, Models.Type? expectedType)
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

            return ExpectOneSuccess(
                candidateFunctions,
                (f) =>
                {
                    var arguments = Enumerable
                        .Zip(fc.Arguments, f.ArgumentTypes)
                        .Select(t => CheckExpressionType(t.Item1, t.Item2));
                    return new TypeCheckedFunctionCall { Method = f, Arguments = arguments };
                }
            );
        }
        else if (expr is DecimalLiteral dl)
        {
            if (expectedType == BuiltIns.Float)
            {
                if (Math.Abs(dl.Value) > (double)float.MaxValue)
                {
                    throw new Exception($"Decimal literal is too large to store as Float");
                }
            }
            else if (expectedType is not null && expectedType != BuiltIns.Double)
            {
                throw new Exception($"{expectedType} cannot be expressed by decimal literal");
            }

            return new TypedDecimalLiteral
            {
                Value = dl.Value,
                Type = expectedType ?? BuiltIns.Double,
            };
        }
        else if (expr is IntegerLiteral il)
        {
            if (
                expectedType is not null
                && expectedType != BuiltIns.Int
                && expectedType != BuiltIns.Float
                && expectedType != BuiltIns.Double
            )
            {
                throw new Exception($"{expectedType} cannot be expressed by integer literal");
            }

            return new TypedIntegerLiteral
            {
                Value = il.Value,
                Type = expectedType ?? BuiltIns.Int,
            };
        }
        else if (expr is StringInterpolation si)
        {
            if (expectedType is not null && expectedType != BuiltIns.String)
            {
                throw new Exception(
                    "Only String instances can be represented by string interpolations"
                );
            }

            return new TypeCheckedStringInterpolation
            {
                Pieces = si.Pieces.Select(e => CheckExpressionType(e, null)),
            };
        }
        else if (expr is StringLiteral sl)
        {
            if (expectedType is not null && expectedType != BuiltIns.String)
            {
                throw new Exception("Only String instances can be represented by string literals");
            }

            return new TypeCheckedStringLiteral { Value = sl.Value };
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

            return ExpectOneSuccess(
                ops,
                (op) =>
                {
                    var lhs = CheckExpressionType(be.Lhs, op.LhsType);
                    var rhs = CheckExpressionType(be.Rhs, op.RhsType);
                    return new TypeCheckedOperatorCall
                    {
                        Operator = op,
                        Lhs = lhs,
                        Rhs = rhs,
                    };
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

            return new TypeCheckedIdentifier { Name = i.Name };
        }
        else
        {
            throw new ArgumentException("Unrecognized expression type", nameof(expr));
        }
    }

    private TypeCheckedClass CheckClassDefinition(ClassDefinition cd)
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
        var body = new TypeChecker(this).CheckTypes(cd.Body);

        return new() { Type = thisType, Body = body };
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
