using Dobropay.Contracts.Constants;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Dobropay.Payments.Api;

public sealed class PaymentHeadersFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var path = context.ApiDescription.RelativePath ?? string.Empty;
        if (!path.StartsWith(PublicRouteNames.PaymentsOpenApiPrefix, StringComparison.Ordinal) &&
            !path.StartsWith(PublicRouteNames.RefundsOpenApiPrefix, StringComparison.Ordinal))
        {
            return;
        }

        operation.Parameters ??= new List<IOpenApiParameter>();
        operation.Parameters.Add(
            new OpenApiParameter
            {
                Name = HttpHeaderNames.ApiKey,
                In = ParameterLocation.Header,
                Required = true,
                Description = "Server API key of the shop",
                Schema = new OpenApiSchema { Type = JsonSchemaType.String }
            });

        if (context.ApiDescription.HttpMethod == HttpMethods.Post)
        {
            operation.Parameters.Add(
                new OpenApiParameter
                {
                    Name = HttpHeaderNames.IdempotencyKey,
                    In = ParameterLocation.Header,
                    Required = true,
                    Description = "Unique per shop and create_payment operation",
                    Schema = new OpenApiSchema { Type = JsonSchemaType.String }
                });
        }
    }
}
