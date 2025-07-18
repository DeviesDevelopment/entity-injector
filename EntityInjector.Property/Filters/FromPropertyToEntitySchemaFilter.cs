using EntityInjector.Property.Attributes;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EntityInjector.Property.Filters;

public class FromPropertyToEntitySchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema?.Properties == null)
        {
            return;
        }

        var skipProperties = context.Type.GetProperties().Where(t => t.GetCustomAttributes(true).OfType<FromPropertyToEntityAttribute>().Any());

        foreach (var skipProperty in skipProperties)
        {
            var propertyToSkip = schema.Properties.Keys.SingleOrDefault(x => string.Equals(x, skipProperty.Name, StringComparison.OrdinalIgnoreCase));
            if (propertyToSkip != null)
            {
                schema.Properties.Remove(propertyToSkip);
            }
        }
    }
}