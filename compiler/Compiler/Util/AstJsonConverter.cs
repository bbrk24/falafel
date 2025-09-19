using Compiler.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Compiler.Util;

public class AstJsonConverter: JsonConverter<AstNode>
{
    public override bool CanConvert(Type typeToConvert) =>
        typeof(AstNode).IsAssignableFrom(typeToConvert);

    public override AstNode? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        using var document = JsonDocument.ParseValue(ref reader);

        var element = document.RootElement;
        var typeElement = element.GetProperty("type");
        var typeString = typeElement.GetString()!;

        var actualType = Type.GetType($"Compiler.Models.{typeString}");
        if (actualType is null || !typeToConvert.IsAssignableFrom(actualType))
        {
            throw new Exception($"Invalid type field: {typeElement.GetRawText()}");
        }

        return (AstNode)JsonSerializer.Deserialize(document, actualType, options);
    }

    public override void Write(
        Utf8JsonWriter writer,
        AstNode value,
        JsonSerializerOptions options
    )
    {
        throw new NotImplementedException();
    }
}
