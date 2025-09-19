using Compiler.Models;
using System.Text.Json;

var jsonContent = File.ReadAllText(args[0]);

var decoded = JsonSerializer.Deserialize<Dictionary<string, IEnumerable<AstNode>>>(
    jsonContent,
    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
);

foreach (var (filename, nodes) in decoded)
{
    Console.WriteLine("{0}: ", filename);

    foreach (var node in nodes)
    {
        if (node is FunctionCall callNode)
        {
            Console.WriteLine("{0}({1})", callNode.Function, callNode.Arguments.Count());
        }
        else
        {
            Console.WriteLine("Node is not a {0}, it is a {1}", typeof(FunctionCall), node.GetType());
        }
    }
}
