using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

public sealed class PaymentHeadersFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var path = context.ApiDescription.RelativePath ?? "";
        if (!path.StartsWith("v1/payments", StringComparison.Ordinal)) return;
        operation.Parameters ??= new List<IOpenApiParameter>();
        operation.Parameters.Add(new OpenApiParameter { Name = "X-Api-Key", In = ParameterLocation.Header, Required = true, Description = "Server API key of the shop", Schema = new OpenApiSchema { Type = JsonSchemaType.String } });
        if (context.ApiDescription.HttpMethod == "POST")
            operation.Parameters.Add(new OpenApiParameter { Name = "Idempotency-Key", In = ParameterLocation.Header, Required = true, Description = "Unique per shop and create_payment operation", Schema = new OpenApiSchema { Type = JsonSchemaType.String } });
    }
}
