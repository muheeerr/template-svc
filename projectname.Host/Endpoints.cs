using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;
using Utility.Helpers.Common.Auth;

namespace projectname.Host;

public static class Endpoints
{
    private static readonly OpenApiSecurityScheme securityScheme = new()
    {
        Type = SecuritySchemeType.Http,
        Name = JwtBearerDefaults.AuthenticationScheme,
        Scheme = JwtBearerDefaults.AuthenticationScheme,
        //Reference = new()
        //{
        //    Type = ReferenceType.SecurityScheme,
        //    Id = JwtBearerDefaults.AuthenticationScheme
        //}
    };

    //public static void MapEndpoints(this WebApplication app)
    //{
    //    var endpoints = app.MapGroup(KConstant.ApiName)
    //        .AddEndpointFilter<RequestLoggingFilter>();

    //    endpoints.MapAuthEndpoints();
    //    endpoints.MapRoleManagementEndpoints();
    //    endpoints.MapReportEndpoints();
    //    endpoints.MapActionsManagementEndpoints();
    //    endpoints.MapUserManagement();
    //    endpoints.MapAdminDashboardEndpoints();
    //    endpoints.MapNotificationManagementEndpoints();
    //}

    private static RouteGroupBuilder MapPublicGroup(this IEndpointRouteBuilder app, string? prefix = null)
    {
        return app.MapGroup(prefix ?? string.Empty)
            .AllowAnonymous();
    }

    private static RouteGroupBuilder MapAuthorizedGroup(this IEndpointRouteBuilder app, string? prefix = null)
    {
        return app.MapGroup(prefix ?? string.Empty)
            .RequireAuthorization(KPolicyDescriptor.CustomPolicy);
            //.WithOpenApi(x => new(x)
            //{
            //    Security = [new() { [securityScheme] = [] }],
            //});
    }

    //private static IEndpointRouteBuilder MapEndpoint<TEndpoint>(this IEndpointRouteBuilder app) where TEndpoint : IFeature
    //{
    //    TEndpoint.Map(app);
    //    return app;
    //}
}
internal sealed class ApiVersionPathTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var paths = document.Paths.ToList();
        document.Paths.Clear();

        foreach (var path in paths)
        {
            var newKey = path.Key
                .Replace("{version:apiVersion}", "1")
                .Replace("{version}", "1");
            document.Paths[newKey] = path.Value;
        }

        return Task.CompletedTask;
    }
}

internal sealed class BearerSecuritySchemeTransformer(Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider authenticationSchemeProvider) : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        try
        {
            var authenticationSchemes = await authenticationSchemeProvider.GetAllSchemesAsync();
            if (!authenticationSchemes.Any(authScheme => authScheme.Name == "Bearer"))
            {
                return;
            }

            // Ensure Components exists
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, OpenApiSecurityScheme>();
            
            // Only add/update if not already present to avoid overwriting
            if (!document.Components.SecuritySchemes.ContainsKey("Bearer"))
            {
                var bearerScheme = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\""
                };
                
                document.Components.SecuritySchemes["Bearer"] = bearerScheme;
            }

            // Process each operation separately to avoid any shared state issues
            //var operations = document.Paths.Values
            //    .SelectMany(path => path.Operations)
            //    .Where(op => op.Value.Security == null || op.Value.Security.Count == 0)
            //    .ToList(); // Materialize to avoid issues during iteration

            //foreach (var operation in operations)
            //{
            //    try
            //    {
            //        // Initialize security list if needed
            //        operation.Value.Security ??= new List<OpenApiSecurityRequirement>();
                    
            //        // Create a fresh security reference for each operation to avoid any serialization issues
            //        var securityReference = new OpenApiSecurityScheme
            //        {
            //            Reference = new OpenApiReference
            //            {
            //                Id = "Bearer",
            //                Type = ReferenceType.SecurityScheme
            //            }
            //        };
                    
            //        // Create a new security requirement with empty scopes
            //        var securityRequirement = new OpenApiSecurityRequirement();
            //        securityRequirement.Add(securityReference, new List<string>());
                    
            //        operation.Value.Security.Add(securityRequirement);
            //    }
            //    catch (Exception ex)
            //    {
            //        // Log but continue processing other operations
            //        // This prevents one bad operation from breaking the entire document
            //        System.Diagnostics.Debug.WriteLine($"Error adding security to operation: {ex.Message}");
            //    }
            //}
        }
        catch (Exception ex)
        {
            // Log the error but don't throw - allow OpenAPI generation to continue
            // This prevents the transformer from breaking the entire OpenAPI generation
            System.Diagnostics.Debug.WriteLine($"Error in BearerSecuritySchemeTransformer: {ex.Message}");
        }
    }
}
