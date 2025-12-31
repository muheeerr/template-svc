using Microsoft.AspNetCore.Routing;

namespace Utility.EndpointController;

public interface IFeature
{
    void Map(IEndpointRouteBuilder app);
}