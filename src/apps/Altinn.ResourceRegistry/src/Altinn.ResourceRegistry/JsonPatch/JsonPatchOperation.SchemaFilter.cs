using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Altinn.ResourceRegistry.JsonPatch;

public sealed partial record class JsonPatchOperation
{
    private sealed class SchemaFilter : ISchemaFilter
    {
        private const string OP = "op";
        private const string PATH = "path";
        private const string VALUE = "value";
        private const string FROM = "from";

        private OpenApiSchema OpSchema(string type)
        {
            return new()
            {
                Description = "The operation to perform",
                Type = JsonSchemaType.String,
                Enum = [(JsonNode)type],
            };
        }

        private OpenApiSchema ValueSchema(string description)
        {
            return new()
            {
                Description = description,
            };
        }

        private static OpenApiSchemaReference GetOrRegister(SchemaFilterContext context, Type type)
        {
            if (!context.SchemaRepository.TryLookupByType(type, out var schema))
            {
                var schemaId = type.Name;
                var generated = context.SchemaGenerator.GenerateSchema(type, context.SchemaRepository);
                schema = context.SchemaRepository.AddDefinition(schemaId, (OpenApiSchema)generated);
                context.SchemaRepository.RegisterType(type, schemaId);
            }

            return schema;
        }

        /// <inheritdoc/>
        public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
        {
            if (schema is not OpenApiSchema openApiSchema)
            {
                return;
            }

            var jsonPointer = GetOrRegister(context, typeof(JsonPointer));

            var add = context.SchemaRepository.AddDefinition("JsonPatchAddOperation", new()
            {
                Type = JsonSchemaType.Object,
                AdditionalPropertiesAllowed = false,
                Required = new HashSet<string> { OP, PATH, VALUE },
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    { OP, OpSchema("add") },
                    { PATH, jsonPointer },
                    { VALUE, ValueSchema("The value to add.") },
                },
            });

            var copy = context.SchemaRepository.AddDefinition("JsonPatchCopyOperation", new()
            {
                Type = JsonSchemaType.Object,
                AdditionalPropertiesAllowed = false,
                Required = new HashSet<string> { OP, FROM, PATH },
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    { OP, OpSchema("copy") },
                    { FROM, jsonPointer },
                    { PATH, jsonPointer },
                },
            });

            var move = context.SchemaRepository.AddDefinition("JsonPatchMoveOperation", new()
            {
                Type = JsonSchemaType.Object,
                AdditionalPropertiesAllowed = false,
                Required = new HashSet<string> { OP, FROM, PATH },
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    { OP, OpSchema("move") },
                    { FROM, jsonPointer },
                    { PATH, jsonPointer },
                },
            });

            var remove = context.SchemaRepository.AddDefinition("JsonPatchRemoveOperation", new()
            {
                Type = JsonSchemaType.Object,
                AdditionalPropertiesAllowed = false,
                Required = new HashSet<string> { OP, PATH },
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    { OP, OpSchema("remove") },
                    { PATH, jsonPointer },
                },
            });

            var replace = context.SchemaRepository.AddDefinition("JsonPatchReplaceOperation", new()
            {
                Type = JsonSchemaType.Object,
                AdditionalPropertiesAllowed = false,
                Required = new HashSet<string> { OP, PATH, VALUE },
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    { OP, OpSchema("replace") },
                    { PATH, jsonPointer },
                    { VALUE, ValueSchema("The value to replace with.") },
                },
            });

            var test = context.SchemaRepository.AddDefinition("JsonPatchTestOperation", new()
            {
                Type = JsonSchemaType.Object,
                AdditionalPropertiesAllowed = false,
                Required = new HashSet<string> { OP, PATH, VALUE },
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    { OP, OpSchema("test") },
                    { PATH, jsonPointer },
                    { VALUE, ValueSchema("The value to match.") },
                },
            });

            openApiSchema.Properties?.Clear();
            openApiSchema.Discriminator = new()
            {
                PropertyName = "op",
                Mapping = new Dictionary<string, OpenApiSchemaReference>
                {
                    { "add", add },
                    { "copy", copy },
                    { "move", move },
                    { "remove", remove },
                    { "replace", replace },
                    { "test", test },
                },
            };
            openApiSchema.OneOf = [
                add,
                copy,
                move,
                remove,
                replace,
                test,
            ];
        }
    }
}
