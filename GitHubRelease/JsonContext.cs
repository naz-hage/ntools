using System.Text.Json.Serialization;

namespace GitHubRelease
{
    /// <summary>
    /// Represents a JSON context used for serialization and deserialization.
    /// The JsonContext is a part of the System.Text.Json namespace and is used for JSON serialization and deserialization. 
    /// It's a context class that provides metadata about the types that are being serialized or deserialized. 
    /// This metadata is used by the JsonSerializer to understand how to convert between JSON and the .NET types.
    /// Here's a breakdown of the JsonContext class:
    /// •	JsonSerializable(typeof(Release)) : This attribute tells the System.Text.Json source generator to generate 
    ///     serialization and deserialization metadata for the Release type.This metadata is used by the 
    ///     JsonSerializer to convert between Release objects and JSON.
    /// •	JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata) : This attribute 
    ///     controls how the System.Text.Json source generator generates the serialization and deserialization 
    ///     metadata. In this case, it's set to JsonSourceGenerationMode.Metadata, which means that the source 
    ///     generator will generate both serialization and deserialization metadata.
    /// •	internal partial class JsonContext : JsonSerializerContext: This declares JsonContext as a subclass 
    ///     of JsonSerializerContext.The JsonSerializerContext class is a base class that provides a way to specify
    ///     custom converters, property naming policies, and other settings.The partial keyword means that the JsonContext
    ///     class can be split across multiple files.The System.Text.Json source generator will generate the other part 
    ///     of the class with the serialization and deserialization metadata.
    /// In summary, the JsonContext class provides a way to customize how the JsonSerializer serializes and deserializes
    ///     Release objects.It's used in conjunction with the System.Text.Json source generator, which generates
    ///     the necessary serialization and deserialization code at compile time, improving runtime performance.
    /// </summary>
    [JsonSerializable(typeof(Release))]
    [JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
    internal partial class JsonContext : JsonSerializerContext
    {
    }
}
