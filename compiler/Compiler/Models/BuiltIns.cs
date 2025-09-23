namespace Compiler.Models;

public static class BuiltIns
{
    public static readonly Type Object = new() { Name = "Object", IsInheritable = true };
    public static readonly Type Void = new() { Name = "Void", IsObject = false };
    public static readonly Type Int = new() { Name = "Int", IsObject = false };
    public static readonly Type Double = new() { Name = "Double", IsObject = false };
    public static readonly Type Float = new() { Name = "Float", IsObject = false };
    public static readonly Type Bool = new() { Name = "Bool", IsObject = false };
    public static readonly Type Char = new() { Name = "Char", IsObject = false };

    public static readonly Type String = new()
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
        Subscript = new() { ReturnType = Char, IsSettable = false },
    };

    public static readonly Type StringBuilder = new() { Name = "StringBuilder", IsObject = false };

    private static readonly Type ArrayGenericPlaceholder = new()
    {
        Name = "T",
        IsGenericPlaceholder = true,
    };

    public static readonly Type Array = new()
    {
        Name = "Array",
        GenericTypes = [ArrayGenericPlaceholder],
        Methods =
        [
            new()
            {
                Name = "push",
                ArgumentTypes = [ArrayGenericPlaceholder],
                ReturnType = Void,
            },
            new()
            {
                Name = "pop",
                ArgumentTypes = [],
                ReturnType = Void,
            },
            new() { Name = "clear", ReturnType = Void },
            new() { Name = "length", ReturnType = Int },
        ],
        IsObject = false,
        Subscript = new() { ReturnType = ArrayGenericPlaceholder, IsSettable = true },
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
        Array,
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
            IsCppOperator = false,
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
            CppName = "&&",
        },
        new Operator
        {
            Name = "||",
            Fixity = OperatorFixity.Infix,
            LhsType = Bool,
            RhsType = Bool,
            ReturnType = Bool,
            IsCppOperator = true,
            CppName = "||",
        },
        new Operator
        {
            Name = "<",
            Fixity = OperatorFixity.Infix,
            LhsType = Int,
            RhsType = Int,
            ReturnType = Bool,
            IsCppOperator = true,
            CppName = "<",
        },
        new Operator
        {
            Name = "<",
            Fixity = OperatorFixity.Infix,
            LhsType = Float,
            RhsType = Float,
            ReturnType = Bool,
            IsCppOperator = true,
            CppName = "<",
        },
        new Operator
        {
            Name = "<",
            Fixity = OperatorFixity.Infix,
            LhsType = Double,
            RhsType = Double,
            ReturnType = Bool,
            IsCppOperator = true,
            CppName = "<",
        },
        new Operator
        {
            Name = "<=",
            Fixity = OperatorFixity.Infix,
            LhsType = Int,
            RhsType = Int,
            ReturnType = Bool,
            IsCppOperator = true,
            CppName = "<=",
        },
        new Operator
        {
            Name = "<=",
            Fixity = OperatorFixity.Infix,
            LhsType = Float,
            RhsType = Float,
            ReturnType = Bool,
            IsCppOperator = true,
            CppName = "<=",
        },
        new Operator
        {
            Name = "<=",
            Fixity = OperatorFixity.Infix,
            LhsType = Double,
            RhsType = Double,
            ReturnType = Bool,
            IsCppOperator = true,
            CppName = "<=",
        },
        new Operator
        {
            Name = ">",
            Fixity = OperatorFixity.Infix,
            LhsType = Int,
            RhsType = Int,
            ReturnType = Bool,
            IsCppOperator = true,
            CppName = ">",
        },
        new Operator
        {
            Name = ">",
            Fixity = OperatorFixity.Infix,
            LhsType = Float,
            RhsType = Float,
            ReturnType = Bool,
            IsCppOperator = true,
            CppName = ">",
        },
        new Operator
        {
            Name = ">",
            Fixity = OperatorFixity.Infix,
            LhsType = Double,
            RhsType = Double,
            ReturnType = Bool,
            IsCppOperator = true,
            CppName = ">",
        },
        new Operator
        {
            Name = ">=",
            Fixity = OperatorFixity.Infix,
            LhsType = Int,
            RhsType = Int,
            ReturnType = Bool,
            IsCppOperator = true,
            CppName = ">=",
        },
        new Operator
        {
            Name = ">=",
            Fixity = OperatorFixity.Infix,
            LhsType = Float,
            RhsType = Float,
            ReturnType = Bool,
            IsCppOperator = true,
            CppName = ">=",
        },
        new Operator
        {
            Name = ">=",
            Fixity = OperatorFixity.Infix,
            LhsType = Double,
            RhsType = Double,
            ReturnType = Bool,
            IsCppOperator = true,
            CppName = ">=",
        },
        new Operator
        {
            Name = "==",
            Fixity = OperatorFixity.Infix,
            LhsType = Int,
            RhsType = Int,
            ReturnType = Bool,
            IsCppOperator = true,
            CppName = "==",
        },
        new Operator
        {
            Name = "==",
            Fixity = OperatorFixity.Infix,
            LhsType = Float,
            RhsType = Float,
            ReturnType = Bool,
            IsCppOperator = true,
            CppName = "==",
        },
        new Operator
        {
            Name = "==",
            Fixity = OperatorFixity.Infix,
            LhsType = Double,
            RhsType = Double,
            ReturnType = Bool,
            IsCppOperator = true,
            CppName = "==",
        },
        new Operator
        {
            Name = "!=",
            Fixity = OperatorFixity.Infix,
            LhsType = Int,
            RhsType = Int,
            ReturnType = Bool,
            IsCppOperator = true,
            CppName = "!=",
        },
        new Operator
        {
            Name = "!=",
            Fixity = OperatorFixity.Infix,
            LhsType = Float,
            RhsType = Float,
            ReturnType = Bool,
            IsCppOperator = true,
            CppName = "!=",
        },
        new Operator
        {
            Name = "!=",
            Fixity = OperatorFixity.Infix,
            LhsType = Double,
            RhsType = Double,
            ReturnType = Bool,
            IsCppOperator = true,
            CppName = "!=",
        },
        new Operator
        {
            Name = "!",
            Fixity = OperatorFixity.Prefix,
            RhsType = Bool,
            ReturnType = Bool,
            IsCppOperator = true,
            CppName = "!",
        },
        new Operator
        {
            Name = "-",
            Fixity = OperatorFixity.Prefix,
            RhsType = Int,
            ReturnType = Int,
            IsCppOperator = true,
            CppName = "-",
        },
        new Operator
        {
            Name = "-",
            Fixity = OperatorFixity.Prefix,
            RhsType = Float,
            ReturnType = Float,
            IsCppOperator = true,
            CppName = "-",
        },
        new Operator
        {
            Name = "-",
            Fixity = OperatorFixity.Prefix,
            RhsType = Double,
            ReturnType = Double,
            IsCppOperator = true,
            CppName = "-",
        },
        new Operator
        {
            Name = "%",
            Fixity = OperatorFixity.Infix,
            LhsType = Int,
            RhsType = Int,
            ReturnType = Int,
            IsCppOperator = true,
            CppName = "%",
        },
        new Operator
        {
            Name = "==",
            Fixity = OperatorFixity.Infix,
            LhsType = Char,
            RhsType = Char,
            ReturnType = Bool,
            IsCppOperator = true,
            CppName = "==",
        },
        new Operator
        {
            Name = "!=",
            Fixity = OperatorFixity.Infix,
            LhsType = Char,
            RhsType = Char,
            ReturnType = Bool,
            IsCppOperator = true,
            CppName = "!=",
        },
    ];

    static BuiltIns()
    {
        foreach (var method in String.Methods)
        {
            method.ThisType = String;
        }

        foreach (var method in Array.Methods)
        {
            method.ThisType = Array;
        }
    }
}
