using Utility.EndpointController;

namespace Core.Features
{
    internal interface IEmployee : IFeature;
    internal interface IBarier : IFeature;
    internal interface IGuard : IFeature;
    internal interface IVisitor : IFeature;
    internal interface IQrCode : IFeature;
    internal interface IShift : IFeature;
    internal interface IAccess : IFeature;
    internal interface IProximity : IFeature;
    internal interface IDashboard : IFeature;
    internal interface IMasterData : IFeature;
    internal interface IFileUpload : IFeature;
}
