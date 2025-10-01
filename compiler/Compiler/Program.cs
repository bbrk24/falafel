using System.Collections.Specialized;
using System.Text.Json;
using System.Text.Json.Serialization;
using Compiler.Components;
using Compiler.Models;
using Compiler.Util;

try
{
    var jsonContent = File.ReadAllText(args[0]);

    var decoded =
        JsonSerializer.Deserialize<Dictionary<string, AstRoot>>(
            jsonContent,
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
            }
        ) ?? throw new Exception("Unexpected JSON null");

    if (decoded.Count > 1)
    {
        throw new NotImplementedException("Compiling multiple files together is not supported.");
    }

    var root = decoded.Values.Single();

    OrderedDictionary declarations;
    try
    {
        declarations = root.Ast.OfType<Declaration>().ToOrderedDictionary(d => d.Name, d => d);
    }
    catch (ArgumentException e)
    {
        throw new Exception("Duplicate declaration detected", e);
    }

    var typeCheckedStatements = new TypeChecker(root.LineCounts).CheckTypes(root.Ast);

    new Codegen().GenerateCode(typeCheckedStatements, args.Length > 1 ? args[1] : null);
    return 0;
}
catch (TypeCheckException tce)
{
    Console.Error.WriteLine("Type checking failed: {0}", tce.Message);
}
catch (AggregateException ae)
{
    Console.Error.WriteLine(ae.Message);
}
catch (Exception e)
{
    Console.Error.WriteLine("Unexpected exception. This likely represents a compiler bug.\n{0}", e);
}
return 1;
