namespace Compiler.Models;

public abstract class Type
{
    public string Name { get; set; }
    public abstract bool IsInheritable { get; set; }

    public override string ToString() => Name;
}

public class GenericPlaceholder : Type
{
    public override bool IsInheritable
    {
        get => false;
        set => throw new NotSupportedException();
    }
}

public class ConcreteType : Type
{
    public Dictionary<string, Property> Properties { get; set; } = [];
    public Dictionary<string, Method> Methods { get; set; } = [];
    public Type? BaseType { get; set; }

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
    public override bool IsInheritable
    {
        get => _isInheritable;
        set
        {
            _isInheritable = value;
            if (value)
            {
                _isObject = true;
            }
        }
    }
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
    public ICollection<GenericPlaceholder> GenericTypes { get; set; } = [];
    public ICollection<Type> ArgumentTypes { get; set; } = [];
    public Type ReturnType { get; set; }

    public override string ToString() =>
        $@"func {
            (ThisType == BuiltIns.Void ? "" : ThisType.ToString() + '.')
        }{
            Name
        }({
            string.Join(", ", ArgumentTypes.Select(t => t.ToString()))
        }) -> {
            ReturnType
        }";
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

    public override string ToString() => $"{Fixity} operator {Name} returning {ReturnType}";
}

public class Variable
{
    public string Name { get; set; }
    public Type Type { get; set; }
}
