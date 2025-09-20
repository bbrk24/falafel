using System.Collections.Specialized;
using System.Text.Json;
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

    new TypeChecker().CheckTypes(statements);
}
catch (Exception e)
{
    Console.Error.WriteLine(e.Message);
}
