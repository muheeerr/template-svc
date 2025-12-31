using NATS.Client.JetStream;

public class NatsConnectionManager
{
    private readonly NatsJSContext _jetStreamContext;

    public NatsConnectionManager(NatsJSContext jetStreamContext)
    {
        _jetStreamContext = jetStreamContext ?? throw new ArgumentNullException(nameof(jetStreamContext));
    }

    public NatsJSContext GetJetStreamContext()
    {
        if (_jetStreamContext == null)
        {
            throw new InvalidOperationException("Not connected to NATS JetStream.");
        }

        return _jetStreamContext;
    }
}


//public class NatsConnectionManager
//{
//    private static NatsJSContext _jetStreamContext;

//    public async Task ConnectAsync()
//    {
//        try
//        {
//            await using var nats = new NatsConnection();
//            _jetStreamContext = new NatsJSContext(nats);
//            Console.WriteLine("Connected to NATS JetStream.");
//        }
//        catch (Exception ex)
//        {
//            Console.WriteLine($"Error connecting to NATS JetStream: {ex.Message}");
//            throw;
//        }
//    }

//    public NatsJSContext GetJetStreamContext()
//    {
//        if (_jetStreamContext == null)
//        {
//            throw new InvalidOperationException("Not connected to NATS JetStream.");
//        }

//        return _jetStreamContext;
//    }
//}
