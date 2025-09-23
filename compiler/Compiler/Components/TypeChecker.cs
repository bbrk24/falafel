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
            else if (node is Assignment a)
            {
                var variable =
                    _knownVariables.LastOrDefault(v => v.Name == a.Name)
                    ?? throw new Exception($"Unrecognized identifier {a.Name}");

                var value = CheckExpressionType(a.Value, variable.Type);

                yield return new TypeCheckedAssignment { Name = variable.Name, Value = value };
            }
            else if (node is ConditionalStatement cs)
            {
                if (!cs.TrueBlock.Any())
                {
                    Console.Error.WriteLine("Warning: empty if statement");
                }
                if (cs.FalseBlock?.Count() == 0)
                {
                    Console.Error.WriteLine("Warning: Extraneous else block");
                }

                if (
                    cs.TrueBlock.Any(x => x is ClassDefinition)
                    || (cs.FalseBlock is not null && cs.FalseBlock.Any(x => x is ClassDefinition))
                )
                {
                    throw new Exception("Conditional class definitions are forbidden");
                }

                var condition = CheckExpressionType(cs.Condition, BuiltIns.Bool);
                var trueBlock = new TypeChecker(this).CheckTypes(cs.TrueBlock);
                IEnumerable<TypeCheckedStatement> falseBlock = [];
                if (cs.FalseBlock is not null)
                {
                    falseBlock = new TypeChecker(this).CheckTypes(cs.FalseBlock);
                }

                yield return new TypeCheckedConditional
                {
                    Condition = condition,
                    TrueBlock = trueBlock,
                    FalseBlock = falseBlock,
                };
            }
            else if (node is LoopStatement ls)
            {
                if (!ls.Body.Any())
                {
                    Console.Error.WriteLine("Warning: empty loop");
                }

                if (ls.Body.Any(x => x is ClassDefinition))
                {
                    throw new Exception("Conditional class definitions are forbidden");
                }

                var condition = CheckExpressionType(ls.Condition, BuiltIns.Bool);
                var body = new TypeChecker(this).CheckTypes(ls.Body);

                yield return new TypeCheckedLoop { Condition = condition, Body = body };
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
                candidateFunctions = candidateFunctions.Where(f =>
                    expectedType.IsImplicitlyConvertibleFrom(f.ReturnType)
                );

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
                && expectedType != BuiltIns.Float
                && expectedType != BuiltIns.Double
                && !expectedType.IsImplicitlyConvertibleFrom(BuiltIns.Int)
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
            if (
                expectedType is not null
                && !expectedType.IsImplicitlyConvertibleFrom(BuiltIns.String)
            )
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
            if (
                expectedType is not null
                && !expectedType.IsImplicitlyConvertibleFrom(BuiltIns.String)
            )
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
                ops = ops.Where(op => expectedType.IsImplicitlyConvertibleFrom(op.ReturnType));

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
                _knownVariables.LastOrDefault(v => v.Name == i.Name)
                ?? throw new Exception($"Unrecognized identifier {i.Name}");

            if (
                expectedType is not null
                && !expectedType.IsImplicitlyConvertibleFrom(variable.Type)
            )
            {
                throw new Exception(
                    $"Type mismatch: {variable.Name} is {variable.Type}; expected {expectedType}"
                );
            }

            return new TypeCheckedIdentifier { Name = variable.Name, Type = variable.Type };
        }
        else if (expr is BooleanLiteral bl)
        {
            if (
                expectedType is not null
                && !expectedType.IsImplicitlyConvertibleFrom(BuiltIns.Bool)
            )
            {
                throw new Exception("Only Bool instances can be represented by boolean literals");
            }

            return new TypeCheckedBooleanLiteral { Value = bl.Value };
        }
        else if (expr is PrefixExpression pe)
        {
            var ops = BuiltIns.Operators.Where(op =>
                op.Fixity == OperatorFixity.Prefix && op.Name == pe.Operator
            );

            if (expectedType is not null)
            {
                ops = ops.Where(op => expectedType.IsImplicitlyConvertibleFrom(op.ReturnType));

                if (!ops.Any())
                {
                    throw new Exception($"Operator {pe.Operator} cannot produce {expectedType}");
                }
            }

            return ExpectOneSuccess(
                ops,
                (op) =>
                {
                    var rhs = CheckExpressionType(pe.Operand, op.RhsType);
                    return new TypeCheckedOperatorCall
                    {
                        Operator = op,
                        Lhs = null,
                        Rhs = rhs,
                    };
                }
            );
        }
        else if (expr is CastExpression ce)
        {
            var targetType =
                LookupType(ce.DeclaredType)
                ?? throw new Exception($"Unrecognized type {ce.DeclaredType}");

            if (expectedType is not null && expectedType != targetType)
            {
                throw new Exception(
                    $"Cast type {ce.DeclaredType} does not match expected type {expectedType}"
                );
            }

            try
            {
                return CheckExpressionType(ce.Value, targetType);
            }
            catch
            {
                var untypedResult = CheckExpressionType(ce.Value, null);
                if (!targetType.IsCastableFrom(untypedResult.Type))
                {
                    throw new Exception($"{untypedResult.Type} is not castable to {targetType}");
                }
                return new TypeCheckedCastExpression { Base = untypedResult, Type = targetType };
            }
        }
        else if (expr is IndexExpression ie)
        {
            var base_ = CheckExpressionType(ie.Base, null);
            if (base_.Type.Subscript is null)
            {
                throw new Exception($"Type {base_.Type} has no subscript");
            }

            if (
                expectedType is not null
                && !expectedType.IsImplicitlyConvertibleFrom(base_.Type.Subscript.ReturnType)
            )
            {
                throw new Exception(
                    $"Subscript on {base_.Type} returns {base_.Type.Subscript.ReturnType}, not {expectedType}"
                );
            }

            var index = CheckExpressionType(ie.Index, base_.Type.Subscript.IndexType);

            return new TypeCheckedIndexGet { Base = base_, Index = index };
        }
        else if (expr is ArrayLiteral al)
        {
            if (expectedType is null)
            {
                throw new Exception("Array literals must have an explicit type");
            }
            if (expectedType.Name != "Array")
            {
                throw new Exception("Only Arrays can be represented by array literals");
            }

            return new TypeCheckedArrayLiteral
            {
                Type = expectedType,
                Values = al.Values.Select(el =>
                    CheckExpressionType(el, expectedType.GenericTypes.Single())
                ),
            };
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

        var thisType = new Models.Type
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
        var found = _knownTypes.SingleOrDefault(t => t.Name == type.Name);
        if (found is null)
        {
            return null;
        }

        if (type.Arguments.Any())
        {
            return found.Instantiate(
                type.Arguments.Select(t =>
                    LookupType(t) ?? throw new Exception($"Unrecognized type {t}")
                )
            );
        }
        else if (found.GenericTypes.Count > 0)
        {
            throw new Exception($"Missing generic type arguments for type {type.Name}");
        }

        return found;
    }
}
