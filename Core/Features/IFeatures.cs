using Utility.EndpointController;

namespace Core.Features;

/// <summary>
/// Marker interfaces for feature grouping. Implement a sub-interface per feature domain.
/// Routes will be grouped under /{InterfaceName} (minus the 'I' prefix).
/// Example: IUserFeature → routes grouped under /UserFeature
/// </summary>
public interface IExample : IFeature;
