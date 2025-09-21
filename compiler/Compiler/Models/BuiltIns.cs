namespace Compiler.Models;

public static class BuiltIns
{
    public static readonly ConcreteType Object = new() { Name = "Object", IsInheritable = true };
    public static readonly ConcreteType Void = new() { Name = "Void", IsObject = false };
    public static readonly ConcreteType Int = new() { Name = "Int", IsObject = false };
    public static readonly ConcreteType Double = new() { Name = "Double", IsObject = false };
    public static readonly ConcreteType Float = new() { Name = "Float", IsObject = false };
    public static readonly ConcreteType Bool = new () { Name = "Bool", IsObject = false };

    public static readonly ConcreteType String = new()
    {
        Name = "String",
        IsObject = true,
        IsInheritable = false,
        BaseType = Object,
        Methods =
        [
            new()
            {
                Name = "length",
                ArgumentTypes = [],
                ReturnType = Int,
            },
        ],
    };

    public static readonly ConcreteType StringBuilder = new()
    {
        Name = "StringBuilder",
        IsObject = false,
    };

    public static readonly IReadOnlyCollection<Type> Types =
    [
        Int,
        Bool,
        Double,
        Float,
        Void,
        Object,
        String,
        StringBuilder,
    ];

    public static readonly IReadOnlyCollection<Method> Methods =
    [
        new Method
        {
            Name = "print",
            ArgumentTypes = [String],
            ReturnType = Void,
        },
    ];

    public static readonly IReadOnlyCollection<Operator> Operators =
    [
        new Operator
        {
            Name = "+",
            Fixity = OperatorFixity.Infix,
            LhsType = Int,
            RhsType = Int,
            ReturnType = Int,
            IsCppOperator = true,
            CppName = "+",
        },
        new Operator
        {
            Name = "+",
            Fixity = OperatorFixity.Infix,
            LhsType = Float,
            RhsType = Float,
            ReturnType = Float,
            IsCppOperator = true,
            CppName = "+",
        },
        new Operator
        {
            Name = "+",
            Fixity = OperatorFixity.Infix,
            LhsType = Double,
            RhsType = Double,
            ReturnType = Double,
            IsCppOperator = true,
            CppName = "+",
        },
        new Operator
        {
            Name = "-",
            Fixity = OperatorFixity.Infix,
            LhsType = Int,
            RhsType = Int,
            ReturnType = Int,
            IsCppOperator = true,
            CppName = "-",
        },
        new Operator
        {
            Name = "-",
            Fixity = OperatorFixity.Infix,
            LhsType = Float,
            RhsType = Float,
            ReturnType = Float,
            IsCppOperator = true,
            CppName = "-",
        },
        new Operator
        {
            Name = "-",
            Fixity = OperatorFixity.Infix,
            LhsType = Double,
            RhsType = Double,
            ReturnType = Double,
            IsCppOperator = true,
            CppName = "-",
        },
        new Operator
        {
            Name = "*",
            Fixity = OperatorFixity.Infix,
            LhsType = Int,
            RhsType = Int,
            ReturnType = Int,
            IsCppOperator = true,
            CppName = "*",
        },
        new Operator
        {
            Name = "*",
            Fixity = OperatorFixity.Infix,
            LhsType = Float,
            RhsType = Float,
            ReturnType = Float,
            IsCppOperator = true,
            CppName = "*",
        },
        new Operator
        {
            Name = "*",
            Fixity = OperatorFixity.Infix,
            LhsType = Double,
            RhsType = Double,
            ReturnType = Double,
            IsCppOperator = true,
            CppName = "*",
        },
        new Operator
        {
            Name = "/",
            Fixity = OperatorFixity.Infix,
            LhsType = Int,
            RhsType = Int,
            ReturnType = Int,
            IsCppOperator = true,
            CppName = "/",
        },
        new Operator
        {
            Name = "/",
            Fixity = OperatorFixity.Infix,
            LhsType = Float,
            RhsType = Float,
            ReturnType = Float,
            IsCppOperator = true,
            CppName = "/",
        },
        new Operator
        {
            Name = "/",
            Fixity = OperatorFixity.Infix,
            LhsType = Double,
            RhsType = Double,
            ReturnType = Double,
            IsCppOperator = true,
            CppName = "/",
        },
        new Operator
        {
            Name = "**",
            Fixity = OperatorFixity.Infix,
            LhsType = Float,
            RhsType = Float,
            ReturnType = Float,
            IsCppOperator = false,
            CppName = "powf",
        },
        new Operator
        {
            Name = "**",
            Fixity = OperatorFixity.Infix,
            LhsType = Double,
            RhsType = Double,
            ReturnType = Double,
            IsCppOperator = true,
            CppName = "pow",
        },
        new Operator
        {
            Name = "+",
            Fixity = OperatorFixity.Infix,
            LhsType = String,
            RhsType = String,
            ReturnType = String,
            IsCppOperator = true,
            CppName = "->add",
        },
        new Operator
        {
            Name = "&&",
            Fixity = OperatorFixity.Infix,
            LhsType = Bool,
            RhsType = Bool,
            ReturnType = Bool,
            IsCppOperator = true,
            CppName = "&&"
        },
        new Operator
        {
            Name = "||",
            Fixity = OperatorFixity.Infix,
            LhsType = Bool,
            RhsType = Bool,
            ReturnType = Bool,
            IsCppOperator = true,
            CppName = "||"
        },
        new Operator
        {
            Name = "<",
            Fixity = OperatorFixity.Infix,
            LhsType = Int,
            RhsType = Int,
            ReturnType = Bool,
            IsCppOperator = true,
            CppName = "<"
        },
        new Operator
        {
            Name = "<",
            Fixity = OperatorFixity.Infix,
            LhsType = Float,
            RhsType = Float,
            ReturnType = Bool,
            IsCppOperator = true,
            CppName = "<"
        },
        new Operator
        {
            Name = "<",
            Fixity = OperatorFixity.Infix,
            LhsType = Double,
            RhsType = Double,
            ReturnType = Bool,
            IsCppOperator = true,
            CppName = "<"
        },
        new Operator
        {
            Name = "<=",
            Fixity = OperatorFixity.Infix,
            LhsType = Int,
            RhsType = Int,
            ReturnType = Bool,
            IsCppOperator = true,
            CppName = "<="
        },
        new Operator
        {
            Name = "<=",
            Fixity = OperatorFixity.Infix,
            LhsType = Float,
            RhsType = Float,
            ReturnType = Bool,
            IsCppOperator = true,
            CppName = "<="
        },
        new Operator
        {
            Name = "<=",
            Fixity = OperatorFixity.Infix,
            LhsType = Double,
            RhsType = Double,
            ReturnType = Bool,
            IsCppOperator = true,
            CppName = "<="
        },
        new Operator
        {
            Name = ">",
            Fixity = OperatorFixity.Infix,
            LhsType = Int,
            RhsType = Int,
            ReturnType = Bool,
            IsCppOperator = true,
            CppName = ">"
        },
        new Operator
        {
            Name = ">",
            Fixity = OperatorFixity.Infix,
            LhsType = Float,
            RhsType = Float,
            ReturnType = Bool,
            IsCppOperator = true,
            CppName = ">"
        },
        new Operator
        {
            Name = ">",
            Fixity = OperatorFixity.Infix,
            LhsType = Double,
            RhsType = Double,
            ReturnType = Bool,
            IsCppOperator = true,
            CppName = ">"
        },
        new Operator
        {
            Name = ">=",
            Fixity = OperatorFixity.Infix,
            LhsType = Int,
            RhsType = Int,
            ReturnType = Bool,
            IsCppOperator = true,
            CppName = ">="
        },
        new Operator
        {
            Name = ">=",
            Fixity = OperatorFixity.Infix,
            LhsType = Float,
            RhsType = Float,
            ReturnType = Bool,
            IsCppOperator = true,
            CppName = ">="
        },
        new Operator
        {
            Name = ">=",
            Fixity = OperatorFixity.Infix,
            LhsType = Double,
            RhsType = Double,
            ReturnType = Bool,
            IsCppOperator = true,
            CppName = ">="
        },
        new Operator
        {
            Name = "==",
            Fixity = OperatorFixity.Infix,
            LhsType = Int,
            RhsType = Int,
            ReturnType = Bool,
            IsCppOperator = true,
            CppName = "=="
        },
        new Operator
        {
            Name = "==",
            Fixity = OperatorFixity.Infix,
            LhsType = Float,
            RhsType = Float,
            ReturnType = Bool,
            IsCppOperator = true,
            CppName = "=="
        },
        new Operator
        {
            Name = "==",
            Fixity = OperatorFixity.Infix,
            LhsType = Double,
            RhsType = Double,
            ReturnType = Bool,
            IsCppOperator = true,
            CppName = "=="
        },
        new Operator
        {
            Name = "!=",
            Fixity = OperatorFixity.Infix,
            LhsType = Int,
            RhsType = Int,
            ReturnType = Bool,
            IsCppOperator = true,
            CppName = "!="
        },
        new Operator
        {
            Name = "!=",
            Fixity = OperatorFixity.Infix,
            LhsType = Float,
            RhsType = Float,
            ReturnType = Bool,
            IsCppOperator = true,
            CppName = "!="
        },
        new Operator
        {
            Name = "!=",
            Fixity = OperatorFixity.Infix,
            LhsType = Double,
            RhsType = Double,
            ReturnType = Bool,
            IsCppOperator = true,
            CppName = "!="
        },
        new Operator
        {
            Name = "!",
            Fixity = OperatorFixity.Prefix,
            RhsType = Bool,
            ReturnType = Bool,
            IsCppOperator = true,
            CppName = "!"
        },
        new Operator
        {
            Name = "-",
            Fixity = OperatorFixity.Prefix,
            RhsType = Int,
            ReturnType = Int,
            IsCppOperator = true,
            CppName = "-"
        },
        new Operator
        {
            Name = "-",
            Fixity = OperatorFixity.Prefix,
            RhsType = Float,
            ReturnType = Float,
            IsCppOperator = true,
            CppName = "-"
        },
        new Operator
        {
            Name = "-",
            Fixity = OperatorFixity.Prefix,
            RhsType = Double,
            ReturnType = Double,
            IsCppOperator = true,
            CppName = "-"
        },
    ];

    static BuiltIns()
    {
        foreach (var method in String.Methods)
        {
            method.ThisType = String;
        }
    }
}
