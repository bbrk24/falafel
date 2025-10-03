using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;

namespace Compiler.Models;

public class Type : IEquatable<Type>
{
    public string Name { get; set; }
    public ICollection<Property> Properties { get; set; } = [];
    public ICollection<Method> Methods { get; set; } = [];
    public ICollection<Type> GenericTypes { get; set; } = [];
    public Type? BaseType { get; set; }
    public Subscript? Subscript { get; set; }

    private bool _isObject;
    public bool IsObject
    {
        get => _isObject;
        set
        {
            _isObject = value;
            if (!value)
            {
                _isInheritable = false;
            }
        }
    }

    private bool _isInheritable;
    public bool IsInheritable
    {
        get => _isInheritable;
        set
        {
            _isInheritable = value;
            if (value)
            {
                _isObject = true;
                _isGenericPlaceholder = false;
            }
        }
    }

    private bool _isGenericPlaceholder = false;
    public bool IsGenericPlaceholder
    {
        get => _isGenericPlaceholder;
        set
        {
            if (GenericTypes.Count > 0)
            {
                throw new InvalidOperationException("Generic placeholder cannot be generic");
            }

            _isGenericPlaceholder = value;
            if (value)
            {
                _isInheritable = false;
            }
        }
    }

    public override string ToString()
    {
        if (GenericTypes.Count > 0)
        {
            return $"{Name}<{string.Join(", ", GenericTypes)} >";
        }
        else
        {
            return Name;
        }
    }

    public bool IsFullyInstantiated() =>
        !_isGenericPlaceholder && GenericTypes.All(t => t.IsFullyInstantiated());

    internal Type PartiallyInstantiate(Dictionary<Type, Type> parts)
    {
        if (parts.TryGetValue(this, out var result))
        {
            return result;
        }

        if (GenericTypes.Count == 0)
        {
            return this;
        }

        return new Type
        {
            Name = Name,
            GenericTypes =
            [
                .. GenericTypes.Select(t =>
                {
                    if (parts.TryGetValue(t, out var value))
                    {
                        return value;
                    }
                    return t;
                }),
            ],
            IsObject = _isObject,
            IsInheritable = _isInheritable,
            BaseType = BaseType?.PartiallyInstantiate(parts),
            Properties =
            [
                .. Properties.Select(p =>
                {
                    return new Property
                    {
                        Name = p.Name,
                        Value = p.Value,
                        Type = p.Type.PartiallyInstantiate(parts),
                    };
                }),
            ],
            Methods =
            [
                .. Methods.Select(m => new Method
                {
                    Name = m.Name,
                    ThisType = this,
                    ArgumentTypes = [.. m.ArgumentTypes.Select(t => t.PartiallyInstantiate(parts))],
                    ReturnType = m.ReturnType.PartiallyInstantiate(parts),
                    OriginallyGenericArguments = m.OriginallyGenericArguments,
                }),
            ],
            Subscript = Subscript is null
                ? null
                : new Subscript
                {
                    ReturnType = Subscript.ReturnType.PartiallyInstantiate(parts),
                    IndexType = Subscript.IndexType.PartiallyInstantiate(parts),
                    IsSettable = Subscript.IsSettable,
                },
        };
    }

    public Type Instantiate(IEnumerable<Type> types)
    {
        var typeList = types.ToList();

        if (typeList.Count != (_isGenericPlaceholder ? 1 : GenericTypes.Count))
        {
            throw new ArgumentException(
                "Incorrect number of generic type arguments",
                nameof(types)
            );
        }

        if (_isGenericPlaceholder)
        {
            return typeList[0];
        }

        var parts = Enumerable.Zip(GenericTypes, types).ToDictionary(t => t.Item1, t => t.Item2);

        return PartiallyInstantiate(parts);
    }

    public bool IsImplicitlyConvertibleFrom(Type other)
    {
        if (other == this)
        {
            return true;
        }
        if (
            IsInstantiationOf(BuiltIns.Optional)
            && GenericTypes.Single().IsImplicitlyConvertibleFrom(other)
        )
        {
            return true;
        }
        if (other.BaseType is null)
        {
            return false;
        }
        return IsImplicitlyConvertibleFrom(other.BaseType);
    }

    public bool IsCastableFrom(Type other)
    {
        if (other == this)
        {
            return true;
        }

        var builtInNumerics = new List<Type> { BuiltIns.Int, BuiltIns.Float, BuiltIns.Double };
        if (builtInNumerics.Contains(this) && builtInNumerics.Contains(other))
        {
            return true;
        }

        return this.IsImplicitlyConvertibleFrom(other) || other.IsImplicitlyConvertibleFrom(this);
    }

    public bool IsInstantiationOf(Type other) =>
        other._isGenericPlaceholder
        || (
            IsObject == other.IsObject
            && GenericTypes.Count > 0
            && GenericTypes.Count == other.GenericTypes.Count
            && Name == other.Name
            && (
                BaseType == other.BaseType
                || (
                    BaseType is not null
                    && other.BaseType is not null
                    && BaseType.IsInstantiationOf(other.BaseType)
                )
            )
        );

    public Dictionary<Type, Type>? DeriveInstantiation(Type other)
    {
        if (GenericTypes.Count == 0 && !_isGenericPlaceholder)
        {
            return other == this ? [] : null;
        }
        if (_isGenericPlaceholder)
        {
            return new() { { this, other } };
        }
        if (!other.IsInstantiationOf(this))
        {
            return null;
        }
        return Enumerable
            .Zip(this.GenericTypes, other.GenericTypes)
            .ToDictionary(t => t.Item1, t => t.Item2);
    }

    public bool IsStrictSuperclassOf(Type other)
    {
        if (other == this)
        {
            return false;
        }
        if (!this.IsObject || !other.IsObject)
        {
            return false;
        }
        if (this == other.BaseType)
        {
            return true;
        }
        if (other.BaseType is null)
        {
            return false;
        }
        return IsStrictSuperclassOf(other.BaseType);
    }

    public bool Equals([NotNullWhen(true)] Type? other)
    {
        if (other is null)
        {
            return false;
        }

        if (object.ReferenceEquals(this, other))
        {
            return true;
        }

        return Name == other.Name
            && IsGenericPlaceholder == other.IsGenericPlaceholder
            && Enumerable.SequenceEqual(GenericTypes, other.GenericTypes)
            && IsObject == other.IsObject
            && BaseType == other.BaseType;
    }

    public override bool Equals([NotNullWhen(true)] object? other) =>
        other is Type t && t.Equals(this);

    public override int GetHashCode() =>
        (Name, IsGenericPlaceholder, GenericTypes.Count, IsObject, BaseType).GetHashCode();

    public static bool operator ==(Type? lhs, Type? rhs) =>
        lhs is null ? rhs is null : lhs.Equals(rhs);

    public static bool operator !=(Type? lhs, Type? rhs) => !(lhs == rhs);
}

public class Property
{
    public string Name { get; set; }
    public Type Type { get; set; }
    public Expression Value { get; set; }
}

public class Method
{
    public string Name { get; set; }
    public Type ThisType { get; set; } = BuiltIns.Void;

    private Type[] _argumentTypes = [];
    public Type[] ArgumentTypes
    {
        get => _argumentTypes;
        set
        {
            if (value.Length > 31)
            {
                throw new TypeCheckException("A function may only have up to 31 arguments");
            }
            _argumentTypes = value;
        }
    }

    public Type ReturnType { get; set; }

    public BitVector32 OriginallyGenericArguments { get; set; } = new(0);

    public override string ToString() =>
        $@"{
            (ThisType == BuiltIns.Void ? "" : ThisType.ToString() + '.')
        }{
            Name
        }({
            string.Join(", ", ArgumentTypes.Select(t => t.ToString()))
        }) -> {
            ReturnType
        }";
}

public class Subscript
{
    public Type ReturnType { get; set; }
    public Type IndexType { get; set; } = BuiltIns.Int;
    public bool IsSettable { get; set; }
}

public enum OperatorFixity
{
    Prefix,
    Postfix,
    Infix,
}

public class Operator
{
    public string Name { get; set; }
    public OperatorFixity Fixity { get; set; }
    public Type? LhsType { get; set; }
    public Type? RhsType { get; set; }
    public Type ReturnType { get; set; }
    public bool IsCppOperator { get; set; }
    public string CppName { get; set; }

    public ICollection<Type> GenericTypes { get; set; } = [];

    public Operator Instantiate(Dictionary<Type, Type> parts)
    {
        return new()
        {
            Name = Name,
            Fixity = Fixity,
            LhsType = LhsType?.PartiallyInstantiate(parts),
            RhsType = RhsType?.PartiallyInstantiate(parts),
            ReturnType = ReturnType.PartiallyInstantiate(parts),
            IsCppOperator = IsCppOperator,
            CppName = CppName,
            GenericTypes =
            [
                .. GenericTypes.Select(t =>
                {
                    if (parts.TryGetValue(t, out var value))
                    {
                        return value;
                    }
                    return t;
                }),
            ],
        };
    }

    public override string ToString() => $"{Fixity} operator {Name} returning {ReturnType}";
}

public class Variable
{
    public string Name { get; set; }
    public Type Type { get; set; }
}
