using System.Collections.Specialized;
using System.Text.Json;
using Compiler.Components;
using Compiler.Models;
using Compiler.Util;

try
{
    var jsonContent = File.ReadAllText(args[0]);

    var decoded =
        JsonSerializer.Deserialize<Dictionary<string, IEnumerable<AstNode>>>(
            jsonContent,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
        ) ?? throw new Exception("Unexpected JSON null");

    if (decoded.Count > 1)
    {
        throw new NotImplementedException("Compiling multiple files together is not supported.");
    }

    var statements = decoded.Values.Single();

    OrderedDictionary declarations;
    try
    {
        declarations = statements.OfType<Declaration>().ToOrderedDictionary(d => d.Name, d => d);
    }
    catch (ArgumentException e)
    {
        throw new Exception("Duplicate declaration detected", e);
    }

    var typeCheckedStatements = new TypeChecker().CheckTypes(statements);

    new Codegen().GenerateCode(typeCheckedStatements, args.Length > 1 ? args[1] : null);
    return 0;
}
catch (Exception e)
{
    Console.Error.WriteLine(e.Message);
    return 1;
}
