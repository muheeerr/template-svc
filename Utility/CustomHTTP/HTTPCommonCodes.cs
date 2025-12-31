namespace Utility.CustomHTTP
{
        public static class HTTPStatusCode200
        {
            public const int Accepted = 202;//HttpStatusCode.Accepted;

            public const int AlreadyReported = 208;//HttpStatusCode.AlreadyReported;

            public const int Created = 201;//HttpStatusCode.Created;

            public const int IAmUsed = 226;//HttpStatusCode.ImUsed;

            public const int NoContent = 204;//HttpStatusCode.NoContent;
            public const int ResetContent = 205;//HttpStatusCode.ResetContent;
            public const int Ok = 200;//+HttpStatusCode.Ok;

        }
        public class HTTPStatusCode400
        {
            public const int BadRequest = 400;//HttpStatusCode.BadRequest;

            public const int NotFound = 404; 
            public const int PaymentRequiredClient = 402;

            public const int Conflict = 409;//HttpStatusCode.Conflict;
            public const int Forbidden = 403;//HttpStatusCode.Forbidden;
            public const int NotAcceptable = 406;//HttpStatusCode.NotAcceptable;
            public const int UpgradeRequired = 426;//HttpStatusCode.NotAcceptable;
            public const int Unauthorized = 401;//HttpStatusCode.Unauthorized;
            public const int UnprocessableEntity = 422;//HttpStatusCode.Unauthorized;




        }
        public class HTTPStatusCode500
        {

            public const int BadGateway = 502;//HttpStatusCode.BadGateway;
            public const int InternalServerError = 500;//HttpStatusCode.InternalServerError;
            public const int GatewayTimeout = 504;//HttpStatusCode.GatewayTimeout;
            public const int ServiceUnavailable = 503;//HttpStatusCode.ServiceUnavailable;





        }
    }
