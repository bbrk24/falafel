namespace Compiler.Models;

public class Type
{
    public string Name { get; set; }
}

public class GenericPlaceholder : Type;

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
    public bool IsInheritable
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
    public ICollection<GenericPlaceholder> GenericTypes { get; set; } = [];
    public ICollection<Type> ArgumentTypes { get; set; } = [];
    public Type ReturnType { get; set; }
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
}
