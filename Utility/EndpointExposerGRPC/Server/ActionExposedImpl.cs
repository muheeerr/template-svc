using ActionExposedGRPC;
using Grpc.Core;
using System.Reflection;
using Utility.EndpointController;
using Utility.EndpointExposerGRPC.Exposed;

namespace Utility.EndpointExposerGRPC.Server
{
    public class ActionExposedImpl : ActionExposedServiceGRPC.ActionExposedServiceGRPCBase
    {
        readonly string _apiBaseName;
        readonly Assembly _assembly;
        readonly Type _iFeature;

        public ActionExposedImpl(string apiBaseName, Assembly assembly, Type iFeature)
        {
            _apiBaseName = apiBaseName;
            _assembly = assembly;
            _iFeature = iFeature;
        }

        public override Task<ApiResponseTemplate> GetActions(RequestGetActions request, ServerCallContext context)
        {
            var response = new ApiResponseTemplate
            {
                IsApiHandled = true,
                StatusCode = 200,
                IsRequestSuccess = true
            };

            var data = new ResponseGetActions
            {
                ApiName = _apiBaseName
            };

            try
            {
                var routes = ExposedAllEndpoints.GetAllActions(_apiBaseName, _assembly, typeof(IFeature));
                data.Routes.AddRange(routes);
                response.Data = data;
            }
            catch (Exception ex)
            {
                response.IsApiHandled = false;
                response.IsRequestSuccess = false;
                response.StatusCode = 500;
                response.Exceptions.Add(ex.Message);
            }

            return Task.FromResult(response);
        }
    }
}
