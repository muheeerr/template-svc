using Grpc.Core;
using Helpers.Singletons;

namespace Utility.Helpers.Singleton.NotificationSender
{
    public class NotificationSenderModel
    {
        public string Message { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Tag { get; set; }
        public string TargetNamespace { get; set; }
        public string RecieverUserType { get; set; }
        public string RecieverUserId { get; set; }
    }

    

    public sealed class NotificationSingleton
    {
        private static readonly Lazy<NotificationSingleton> _instance = new(() => new NotificationSingleton());

        
        private ReadGRPCEndpoints? _grpcEndpoints;

        private NotificationSingleton() { }

        public static NotificationSingleton Instance => _instance.Value;

        public void Initialize( ReadGRPCEndpoints grpcEndpoints)
        {
            
            _grpcEndpoints = grpcEndpoints ?? throw new ArgumentNullException(nameof(grpcEndpoints));
        }

        //public async Task<bool> SendAndSaveNotificationAsync(NotificationSenderModel sender, string senderUserType, string senderUserId, string topicName, CancellationToken cancellationToken,ICustomLogger _logger)
        //{
            

        //    try
        //    {
        //        var (grpcClient, request) = GetSenderObject(sender, senderUserType, senderUserId, topicName);

        //        await grpcClient.SendAndSaveNotificationAsync(request, deadline: DateTime.UtcNow.AddMinutes(1), cancellationToken: cancellationToken);
        //        return true;
        //    }
        //    catch (Exception e)
        //    {
        //        _logger?.LogError(e, "Error in SendAndSaveNotification: {Message}", e.Message);
        //        return false;
        //    }
        //}

        //public async Task<bool> SendOnlyNotificationAsync(NotificationSenderModel sender, string senderUserType, string senderUserId, string topicName, CancellationToken cancellationToken, ICustomLogger _logger)
        //{
            

        //    try
        //    {
        //        var (grpcClient, request) = GetSenderObject(sender, senderUserType, senderUserId, topicName);
        //        await grpcClient.SendNotificationAsync(request, deadline: DateTime.UtcNow.AddMinutes(1), cancellationToken: cancellationToken);
        //        return true;
        //    }
        //    catch (Exception e)
        //    {
        //        _logger?.LogError(e, "Error in SendOnlyNotification: {Message}", e.Message);
        //        return false;
        //    }
        //}

        //private (NotificationServiceGrpc.NotificationServiceGrpcClient grpcClient, RequestSendNotification request) GetSenderObject(NotificationSenderModel sender, string senderUserType, string senderUserId, string topicName)
        //{

        //    var key = "NotificationService";
        //    var grpcCreds = _grpcEndpoints.Get(key);

        //    var metadata = new Metadata { { "password", grpcCreds.Password } };
        //    string source = $"http://{grpcCreds.Host}:{grpcCreds.Port}";

        //    var grpcClient = _grpcEndpoints.CreateGrpcClient<NotificationServiceGrpc.NotificationServiceGrpcClient>(key, source, metadata);

        //    var request = new RequestSendNotification
        //    {
        //        SenderUserId = senderUserId,
        //        SenderUserType = senderUserType,
        //        At = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.UtcNow),
        //        Description = sender.Description,
        //        Message = sender.Message,
        //        RecieverUserId = sender.RecieverUserId,
        //        RecieverUserType = sender.RecieverUserType,
        //        Tag = sender.Tag,
        //        TargetNamespace = sender.TargetNamespace,
        //        Title = sender.Title,
        //        TopicName = topicName
        //    };

        //    return (grpcClient, request);
        //}
    }
}
