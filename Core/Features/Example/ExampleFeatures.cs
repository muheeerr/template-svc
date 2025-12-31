using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Utility.EndpointController;
using Utility.Helpers.Common;

namespace Core.Features.Example;

/// <summary>
/// Example feature demonstrating the features-as-classes pattern.
/// Copy this file as a starting point for new features.
/// </summary>
public class GetExample : IFeature
{
    public record Response(string Message, DateTime Timestamp);

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(nameof(GetExample), Handle)
           .WithTags("Example")
           .WithDescription("Example GET endpoint")
           .Produces<Response>(StatusCodes.Status200OK);
    }

    private static IResult Handle()
    {
        var response = new Response("Hello from template!", DateTime.UtcNow);
        return Results.Ok(ApiResponseHelper.Success(response, "Success"));
    }
}

/// <summary>
/// Example POST feature with validation.
/// </summary>
public class CreateExample : IFeature
{
    public record Request(string Name, string Description);
    public record Response(Guid Id, string Name, string Description);

    /// <summary>
    /// Nested RequestValidator is auto-registered by the template.
    /// </summary>
    public class RequestValidator : AbstractValidator<Request>
    {
        public RequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required")
                .MaximumLength(100).WithMessage("Name cannot exceed 100 characters");
            
            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Description cannot exceed 500 characters");
        }
    }

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost(nameof(CreateExample), Handle)
           .WithTags("Example")
           .WithDescription("Example POST endpoint with validation")
           .Produces<Response>(StatusCodes.Status201Created)
           .ProducesValidationProblem();
    }

    private static IResult Handle(Request request)
    {
        // TODO: Replace with actual business logic using IUnitOfWork
        var response = new Response(Guid.NewGuid(), request.Name, request.Description);
        return Results.Created($"/example/{response.Id}", 
            ApiResponseHelper.Success(response, "Created successfully", StatusCodes.Status201Created));
    }
}
