using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
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
                Type = "string",
                Enum = [new OpenApiString(type)],
            };
        }

        private OpenApiSchema ValueSchema(string description)
        {
            return new()
            {
                Description = description,
            };
        }

        private static OpenApiSchema GetOrRegister(SchemaFilterContext context, Type type)
        {
            if (!context.SchemaRepository.TryLookupByType(type, out var schema))
            {
                var schemaId = type.Name;
                schema = context.SchemaGenerator.GenerateSchema(type, context.SchemaRepository);
                schema = context.SchemaRepository.AddDefinition(schemaId, schema);
                context.SchemaRepository.RegisterType(type, schemaId);
            }

            return schema;
        }

        /// <inheritdoc/>
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            var jsonPointer = GetOrRegister(context, typeof(JsonPointer));

            var add = context.SchemaRepository.AddDefinition("JsonPatchAddOperation", new()
            {
                Type = "object",
                AdditionalPropertiesAllowed = false,
                Required = new HashSet<string> { OP, PATH, VALUE },
                Properties = new Dictionary<string, OpenApiSchema>
                {
                    { OP, OpSchema("add") },
                    { PATH, jsonPointer },
                    { VALUE, ValueSchema("The value to add.") },
                },
            });

            var copy = context.SchemaRepository.AddDefinition("JsonPatchCopyOperation", new()
            {
                Type = "object",
                AdditionalPropertiesAllowed = false,
                Required = new HashSet<string> { OP, FROM, PATH },
                Properties = new Dictionary<string, OpenApiSchema>
                {
                    { OP, OpSchema("copy") },
                    { FROM, jsonPointer },
                    { PATH, jsonPointer },
                },
            });

            var move = context.SchemaRepository.AddDefinition("JsonPatchMoveOperation", new()
            {
                Type = "object",
                AdditionalPropertiesAllowed = false,
                Required = new HashSet<string> { OP, FROM, PATH },
                Properties = new Dictionary<string, OpenApiSchema>
                {
                    { OP, OpSchema("move") },
                    { FROM, jsonPointer },
                    { PATH, jsonPointer },
                },
            });

            var remove = context.SchemaRepository.AddDefinition("JsonPatchRemoveOperation", new()
            {
                Type = "object",
                AdditionalPropertiesAllowed = false,
                Required = new HashSet<string> { OP, PATH },
                Properties = new Dictionary<string, OpenApiSchema>
                {
                    { OP, OpSchema("remove") },
                    { PATH, jsonPointer },
                },
            });

            var replace = context.SchemaRepository.AddDefinition("JsonPatchReplaceOperation", new()
            {
                Type = "object",
                AdditionalPropertiesAllowed = false,
                Required = new HashSet<string> { OP, PATH, VALUE },
                Properties = new Dictionary<string, OpenApiSchema>
                {
                    { OP, OpSchema("replace") },
                    { PATH, jsonPointer },
                    { VALUE, ValueSchema("The value to replace with.") },
                },
            });

            var test = context.SchemaRepository.AddDefinition("JsonPatchTestOperation", new()
            {
                Type = "object",
                AdditionalPropertiesAllowed = false,
                Required = new HashSet<string> { OP, PATH, VALUE },
                Properties = new Dictionary<string, OpenApiSchema>
                {
                    { OP, OpSchema("test") },
                    { PATH, jsonPointer },
                    { VALUE, ValueSchema("The value to match.") },
                },
            });

            schema.Properties.Clear();
            schema.Discriminator = new() 
            { 
                PropertyName = "op",
                Mapping = new Dictionary<string, string>
                {
                    { "add", add.Reference.ReferenceV3 },
                    { "copy", copy.Reference.ReferenceV3 },
                    { "move", move.Reference.ReferenceV3 },
                    { "remove", remove.Reference.ReferenceV3 },
                    { "replace", replace.Reference.ReferenceV3 },
                    { "test", test.Reference.ReferenceV3 },
                },
            };
            schema.OneOf = [
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
