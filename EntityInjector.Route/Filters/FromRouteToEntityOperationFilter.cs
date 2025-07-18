using System.Reflection;
using EntityInjector.Core.Exceptions;
using EntityInjector.Route.Attributes;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EntityInjector.Route.Filters;

public class FromRouteToEntityOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Remove parameters decorated with FromRouteToEntityAttribute
        var parametersToHide = context.ApiDescription.ParameterDescriptions
            .Where(desc =>
                desc.ModelMetadata is DefaultModelMetadata metadata &&
                metadata.Attributes.ParameterAttributes?.OfType<FromRouteToEntityAttribute>().Any() == true)
            .ToList();

        foreach (var parameterToHide in parametersToHide)
        {
            var parameter = operation.Parameters
                .FirstOrDefault(p => string.Equals(p.Name, parameterToHide.Name, StringComparison.Ordinal));
            if (parameter != null)
            {
                operation.Parameters.Remove(parameter);
            }
        }

        // Add OpenAPI responses for known EntityBindingException types
        var types = typeof(EntityBindingException).Assembly
            .GetTypes()
            .Where(t => typeof(EntityBindingException).IsAssignableFrom(t) &&
                        t.IsSealed &&
                        t.IsClass &&
                        t.GetInterfaces().Contains(typeof(IExceptionMetadata)))
            .ToList();
        
        foreach (var type in types)
        {
            if (Activator.CreateInstance(type) is not IExceptionMetadata instance)
                continue;

            var key = instance.StatusCode.ToString();
            if (!operation.Responses.ContainsKey(key))
            {
                operation.Responses.Add(key, new OpenApiResponse { Description = instance.DefaultDescription });
            }
        }


    }
}
