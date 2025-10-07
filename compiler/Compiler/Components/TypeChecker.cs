using Compiler.Models;

namespace Compiler.Components;

public class TypeChecker
{
    private List<Models.Type> _knownTypes;
    private List<Variable> _knownVariables;
    private List<Method> _knownFunctions;
    private readonly int _scopeStart;

    private readonly List<ulong> _lineCounts;

    public TypeChecker(List<ulong> lineCounts)
    {
        _lineCounts = lineCounts;
        _knownTypes = BuiltIns.Types.ToList();
        _knownVariables = [];
        _knownFunctions = BuiltIns.Methods.ToList();
        _scopeStart = 0;
    }

    public TypeChecker(TypeChecker other)
    {
        _lineCounts = other._lineCounts;
        _knownTypes = [.. other._knownTypes];
        _knownVariables = [.. other._knownVariables];
        _knownFunctions = [.. other._knownFunctions];
        _scopeStart = _knownVariables.Count;
    }

    private int GetLineNumber(Location loc)
    {
        var maybeLineNumber = _lineCounts.FindIndex(x => x >= loc.Pos);
        return maybeLineNumber < 0 ? _lineCounts.Count + 1 : maybeLineNumber + 1;
    }

    private int? GetLineNumber(HasLocation node)
    {
        if (node.Loc is not null)
        {
            return GetLineNumber(node.Loc);
        }
        return null;
    }

    public IEnumerable<TypeCheckedStatement> CheckTypes(
        IEnumerable<AstNode> program,
        Models.Type? returnType = null
    )
    {
        foreach (var cd in program.OfType<ClassDefinition>())
        {
            yield return CheckClassDefinition(cd);
        }

        foreach (var fd in program.OfType<FunctionDeclaration>())
        {
            CheckFunctionTypeFirstPass(fd);
        }

        foreach (var node in program.Where(node => node is not ClassDefinition))
        {
            if (node is ReturnStatement rs)
            {
                if (returnType is null)
                {
                    throw new TypeCheckException(
                        "Top-level return statements are not allowed",
                        GetLineNumber(node)
                    );
                }
                if (rs.Value is null && returnType != BuiltIns.Void)
                {
                    throw new TypeCheckException(
                        "Return statement in non-Void function must return value",
                        GetLineNumber(node)
                    );
                }

                TypeCheckedExpression? value = null;
                if (rs.Value is not null)
                {
                    value = CheckExpressionType(rs.Value, returnType);
                }
                yield return new TypeCheckedReturnStatement { Value = value };
            }
            else if (node is VarDeclaration vd)
            {
                if (VariableNameExistsInLocalScope(vd.Name))
                {
                    throw new TypeCheckException(
                        $"Invalid redeclaration of variable {vd.Name}",
                        GetLineNumber(vd)
                    );
                }

                var declType =
                    LookupType(vd.DeclaredType)
                    ?? throw new TypeCheckException(
                        $"Unrecognized type {vd.DeclaredType}",
                        GetLineNumber(vd.DeclaredType)
                    );
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
                if (a.Lhs is CastExpression)
                {
                    throw new TypeCheckException(
                        "The member chain on the left of an assignment cannot end in a cast",
                        GetLineNumber(a.Lhs)
                    );
                }
                var lhs = CheckExpressionType(a.Lhs, null);
                if (lhs is TypeCheckedMethodCall)
                {
                    throw new TypeCheckException(
                        "The member chain on the left of an assignment cannot end in a method call",
                        GetLineNumber(a.Lhs)
                    );
                }
                else if (lhs is TypeCheckedIndexAccess ia && !ia.Subscript.IsSettable)
                {
                    throw new TypeCheckException(
                        $"The subscript on {ia.Base.Type} is not settable",
                        GetLineNumber(a.Lhs)
                    );
                }

                var rhs = CheckExpressionType(a.Rhs, lhs.Type);

                yield return new TypeCheckedAssignment { Lhs = lhs, Rhs = rhs };
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
                    var lineNumber = Enumerable
                        .Concat(cs.TrueBlock, cs.FalseBlock ?? [])
                        .OfType<ClassDefinition>()
                        .Select(x => GetLineNumber(x))
                        .FirstOrDefault(x => x is not null);

                    throw new TypeCheckException(
                        "Conditional class definitions are forbidden",
                        lineNumber
                    );
                }

                var condition = CheckExpressionType(cs.Condition, BuiltIns.Bool);
                var trueBlock = new TypeChecker(this).CheckTypes(cs.TrueBlock, returnType);
                IEnumerable<TypeCheckedStatement> falseBlock = [];
                if (cs.FalseBlock is not null)
                {
                    falseBlock = new TypeChecker(this).CheckTypes(cs.FalseBlock, returnType);
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
                    var message = "Warning: empty loop detected";
                    var lineNumber = GetLineNumber(ls);
                    if (lineNumber is not null)
                    {
                        message += $" on line {lineNumber}";
                    }

                    Console.Error.WriteLine(message);
                }

                if (ls.Body.Any(x => x is ClassDefinition))
                {
                    var lineNumber = ls
                        .Body.OfType<ClassDefinition>()
                        .Select(x => GetLineNumber(x))
                        .FirstOrDefault(x => x is not null);

                    throw new TypeCheckException(
                        "Conditional class definitions are forbidden",
                        lineNumber
                    );
                }

                var condition = CheckExpressionType(ls.Condition, BuiltIns.Bool);
                var body = new TypeChecker(this).CheckTypes(ls.Body, returnType);

                yield return new TypeCheckedLoop { Condition = condition, Body = body };
            }
            else if (node is FunctionDeclaration fd)
            {
                yield return CheckFunctionTypeSecondPass(fd);
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

    private bool VariableNameExistsInLocalScope(string name)
    {
        if (_knownVariables.Count <= _scopeStart)
        {
            return false;
        }
        return _knownVariables[_scopeStart..].Any(v => v.Name == name);
    }

    private void CheckFunctionTypeFirstPass(FunctionDeclaration fd)
    {
        var argumentNames = new HashSet<string>();
        foreach (var arg in fd.Arguments)
        {
            if (!argumentNames.Add(arg.Name))
            {
                throw new TypeCheckException(
                    $"Duplicate argument name {arg.Name}",
                    GetLineNumber(fd)
                );
            }
        }

        var argumentTypes = fd
            .Arguments.Select(a =>
                LookupType(a.Type)
                ?? throw new TypeCheckException(
                    $"Unrecognized type {a.Type}",
                    GetLineNumber(a.Type)
                )
            )
            .ToArray();

        var returnType = fd.ReturnType is null
            ? BuiltIns.Void
            : LookupType(fd.ReturnType)
                ?? throw new TypeCheckException(
                    $"Unrecognized type {fd.ReturnType}",
                    GetLineNumber(fd.ReturnType)
                );

        var method = new Method
        {
            Name = fd.Name,
            ArgumentTypes = argumentTypes,
            ReturnType = returnType,
            Declaration = fd,
        };

        var overlaps = _knownFunctions.Where(m => m.OverlapsWith(method));
        if (overlaps.Any())
        {
            throw new TypeCheckException(
                $"Possible duplicate declaration: {method} is too similar to: {string.Join("; ", overlaps)}",
                GetLineNumber(fd)
            );
        }

        _knownFunctions.Add(method);
    }

    private TypeCheckedFunctionDeclaration CheckFunctionTypeSecondPass(FunctionDeclaration fd)
    {
        var method = _knownFunctions.Single(m => object.ReferenceEquals(fd, m.Declaration));

        var innerChecker = new TypeChecker(this);
        innerChecker._knownVariables.AddRange(
            Enumerable
                .Zip(fd.Arguments, method.ArgumentTypes)
                .Select(t => new Variable { Name = t.Item1.Name, Type = t.Item2 })
        );

        var body = innerChecker.CheckTypes(fd.Body, method.ReturnType).ToList();

        if (method.ReturnType != BuiltIns.Void && !body.Any(x => x.IsReturn))
        {
            throw new TypeCheckException(
                "Non-Void function must have a return statement",
                GetLineNumber(fd)
            );
        }

        return new()
        {
            Method = method,
            Body = body,
            Arguments = Enumerable
                .Zip(fd.Arguments, method.ArgumentTypes)
                .Select(t => new TypeCheckedFunctionArgument
                {
                    Name = t.Item1.Name,
                    Type = t.Item2,
                }),
        };
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
                throw new TypeCheckException(
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
                throw new TypeCheckException(
                    $"No known functions match name {fc.Function}",
                    GetLineNumber(fc)
                );
            }

            if (expectedType is not null)
            {
                candidateFunctions = candidateFunctions.Where(f =>
                    expectedType.IsImplicitlyConvertibleFrom(f.ReturnType)
                );

                if (!candidateFunctions.Any())
                {
                    throw new TypeCheckException(
                        $"No overloads of {fc.Function} return {expectedType}",
                        GetLineNumber(fc)
                    );
                }
            }

            candidateFunctions = candidateFunctions.Where(f =>
                fc.Arguments.Count() == f.ArgumentTypes.Length
            );
            if (!candidateFunctions.Any())
            {
                throw new TypeCheckException(
                    $"No overloads of {fc.Function} take {fc.Arguments.Count()} arguments",
                    GetLineNumber(fc)
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
                if (double.IsFinite(dl.Value) && Math.Abs(dl.Value) > (double)float.MaxValue)
                {
                    throw new TypeCheckException(
                        $"Decimal literal is too large to store as Float",
                        GetLineNumber(dl)
                    );
                }
            }
            else if (
                expectedType is not null
                && !expectedType.IsImplicitlyConvertibleFrom(BuiltIns.Double)
            )
            {
                throw new TypeCheckException(
                    $"{expectedType} cannot be expressed by decimal literal",
                    GetLineNumber(dl)
                );
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
                throw new TypeCheckException(
                    $"{expectedType} cannot be expressed by integer literal",
                    GetLineNumber(il)
                );
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
                throw new TypeCheckException(
                    "Only String instances can be represented by string interpolations",
                    GetLineNumber(si)
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
                throw new TypeCheckException(
                    "Only String instances can be represented by string literals",
                    GetLineNumber(sl)
                );
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
                ops = ops.SelectMany<Operator, Operator>(op =>
                {
                    if (expectedType.IsImplicitlyConvertibleFrom(op.ReturnType))
                    {
                        return [op];
                    }
                    if (op.GenericTypes.Count == 0)
                    {
                        return [];
                    }
                    var derivedInstantiation = op.ReturnType.DeriveInstantiation(expectedType);
                    if (derivedInstantiation is null)
                    {
                        return [];
                    }
                    return [op.Instantiate(derivedInstantiation)];
                });

                if (!ops.Any())
                {
                    throw new TypeCheckException(
                        $"Operator {be.Operator} cannot produce {expectedType}",
                        GetLineNumber(be)
                    );
                }
            }

            return ExpectOneSuccess(
                ops,
                (op) =>
                {
                    TypeCheckedExpression lhs,
                        rhs;
                    if (op.GenericTypes.All(t => t.IsFullyInstantiated()))
                    {
                        lhs = CheckExpressionType(be.Lhs, op.LhsType);
                        rhs = CheckExpressionType(be.Rhs, op.RhsType);
                    }
                    else
                    {
                        lhs = CheckExpressionType(
                            be.Lhs,
                            op.LhsType!.IsFullyInstantiated() ? op.LhsType : null
                        );
                        rhs = CheckExpressionType(
                            be.Rhs,
                            op.RhsType!.IsFullyInstantiated() ? op.RhsType : null
                        );

                        var derivedInstantiation = op.LhsType.DeriveInstantiation(lhs.Type);
                        var di2 = op.RhsType.DeriveInstantiation(rhs.Type);

                        if (derivedInstantiation is null || di2 is null)
                        {
                            throw new TypeCheckException(
                                $"Unable to determine generic type for operator {op.Name}",
                                GetLineNumber(be)
                            );
                        }

                        foreach (var kvp in di2)
                        {
                            if (derivedInstantiation.TryGetValue(kvp.Key, out var value))
                            {
                                if (value != kvp.Value)
                                {
                                    throw new TypeCheckException(
                                        $"Unable to determine generic type for operator {op.Name}",
                                        GetLineNumber(be)
                                    );
                                }
                            }
                            else
                            {
                                derivedInstantiation.Add(kvp.Key, kvp.Value);
                            }
                        }

                        op = op.Instantiate(derivedInstantiation);
                    }

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
                ?? throw new TypeCheckException(
                    $"Unrecognized identifier {i.Name}",
                    GetLineNumber(i)
                );

            if (
                expectedType is not null
                && !expectedType.IsImplicitlyConvertibleFrom(variable.Type)
            )
            {
                throw new TypeCheckException(
                    $"Type mismatch: {variable.Name} is {variable.Type}; expected {expectedType}",
                    GetLineNumber(i)
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
                throw new TypeCheckException(
                    "Only Bool instances can be represented by boolean literals",
                    GetLineNumber(bl)
                );
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
                    throw new TypeCheckException(
                        $"Operator {pe.Operator} cannot produce {expectedType}",
                        GetLineNumber(pe)
                    );
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
                ?? throw new TypeCheckException(
                    $"Unrecognized type {ce.DeclaredType}",
                    GetLineNumber(ce)
                );

            if (expectedType is not null && expectedType != targetType)
            {
                throw new TypeCheckException(
                    $"Cast type {ce.DeclaredType} does not match expected type {expectedType}",
                    GetLineNumber(ce)
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
                    throw new TypeCheckException(
                        $"{untypedResult.Type} is not castable to {targetType}",
                        GetLineNumber(ce)
                    );
                }
                return new TypeCheckedCastExpression { Base = untypedResult, Type = targetType };
            }
        }
        else if (expr is IndexExpression ie)
        {
            var base_ = CheckExpressionType(ie.Base, null);
            if (base_.Type.Subscript is null)
            {
                throw new TypeCheckException(
                    $"Type {base_.Type} has no subscript",
                    GetLineNumber(ie)
                );
            }

            if (
                expectedType is not null
                && !expectedType.IsImplicitlyConvertibleFrom(base_.Type.Subscript.ReturnType)
            )
            {
                throw new TypeCheckException(
                    $"Subscript on {base_.Type} returns {base_.Type.Subscript.ReturnType}, not {expectedType}",
                    GetLineNumber(ie)
                );
            }

            var index = CheckExpressionType(ie.Index, base_.Type.Subscript.IndexType);

            return new TypeCheckedIndexAccess { Base = base_, Index = index };
        }
        else if (expr is ArrayLiteral al)
        {
            if (expectedType is null)
            {
                throw new TypeCheckException(
                    "Array literals must have an explicit type",
                    GetLineNumber(al)
                );
            }
            if (!expectedType.IsInstantiationOf(BuiltIns.Array))
            {
                throw new TypeCheckException(
                    "Only Arrays can be represented by array literals",
                    GetLineNumber(al)
                );
            }

            return new TypeCheckedArrayLiteral
            {
                Type = expectedType,
                Values = al.Values.Select(el =>
                    CheckExpressionType(el, expectedType.GenericTypes.Single())
                ),
            };
        }
        else if (expr is NullLiteral nl)
        {
            if (expectedType is null)
            {
                throw new TypeCheckException(
                    "Null literals must have an explicit type",
                    GetLineNumber(nl)
                );
            }
            if (!expectedType.IsInstantiationOf(BuiltIns.Optional))
            {
                throw new TypeCheckException(
                    "Only Optionals can be represented by null literals",
                    GetLineNumber(nl)
                );
            }

            return new TypeCheckedNullLiteral { Type = expectedType };
        }
        else if (expr is CharLiteral cl)
        {
            if (
                expectedType is not null
                && !expectedType.IsImplicitlyConvertibleFrom(BuiltIns.Char)
            )
            {
                throw new TypeCheckException(
                    "Only Char instances can be represented by character literals",
                    GetLineNumber(cl)
                );
            }

            return new TypeCheckedCharLiteral { Value = (byte)cl.Value };
        }
        else if (expr is MemberAccessExpression mae)
        {
            var base_ = CheckExpressionType(mae.Base, null);
            if (mae.Member is Identifier i0)
            {
                var prop =
                    base_.Type.Properties.SingleOrDefault(p => p.Name == i0.Name)
                    ?? throw new TypeCheckException(
                        $"Type {base_.Type} has no member {i0.Name}",
                        GetLineNumber(i0)
                    );

                if (
                    expectedType is not null
                    && !expectedType.IsImplicitlyConvertibleFrom(prop.Type)
                )
                {
                    throw new TypeCheckException(
                        $"{base_.Type}.{prop.Name} is of type {prop.Type} ({expectedType} expected)",
                        GetLineNumber(i0)
                    );
                }

                return new TypeCheckedPropertyAccess { Base = base_, Property = prop };
            }
            else if (mae.Member is FunctionCall fc0)
            {
                var methods = base_.Type.Methods.Where(m => m.Name == fc0.Function);

                if (!methods.Any())
                {
                    throw new TypeCheckException(
                        $"Type {base_.Type} has no method named {fc0.Function}",
                        GetLineNumber(fc0)
                    );
                }

                if (expectedType is not null)
                {
                    methods = methods.Where(m =>
                        expectedType.IsImplicitlyConvertibleFrom(m.ReturnType)
                    );

                    if (!methods.Any())
                    {
                        throw new TypeCheckException(
                            $"Method {base_.Type}.{fc0.Function} has no overloads that return {expectedType}",
                            GetLineNumber(fc0)
                        );
                    }
                }

                methods = methods.Where(m => m.ArgumentTypes.Length == fc0.Arguments.Count());

                return ExpectOneSuccess(
                    methods,
                    m =>
                    {
                        var arguments = Enumerable
                            .Zip(fc0.Arguments, m.ArgumentTypes)
                            .Select(t => CheckExpressionType(t.Item1, t.Item2));
                        return new TypeCheckedMethodCall
                        {
                            Base = base_,
                            Method = m,
                            Arguments = arguments,
                        };
                    }
                );
            }
            else
            {
                throw new ArgumentException(
                    $"Unrecognized member access type '{expr.Type}'",
                    nameof(expr)
                );
            }
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
            baseType =
                LookupType(cd.Base)
                ?? throw new TypeCheckException($"Unrecognized type {cd.Base}", GetLineNumber(cd));
            if (!baseType.IsInheritable)
            {
                throw new TypeCheckException($"{cd.Base} is not inheritable", GetLineNumber(cd));
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
                    LookupType(t)
                    ?? throw new TypeCheckException($"Unrecognized type {t}", GetLineNumber(type))
                )
            );
        }
        else if (found.GenericTypes.Count > 0)
        {
            throw new TypeCheckException(
                $"Missing generic type arguments for type {type.Name}",
                GetLineNumber(type)
            );
        }

        return found;
    }
}
