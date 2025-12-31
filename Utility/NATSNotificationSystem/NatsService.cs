/*public class NatsService : INatsService
{
    private readonly NatsConnectionManager _connectionManager;

    public NatsService(NatsConnectionManager connectionManager)
    {
        _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
    }

    public async Task PublishAsync(string streamName, string streamSubject, string message)
    {
        try
        {
            var streamConfig = new StreamConfig(streamName, subjects: new[] { streamSubject });
            var js = _connectionManager.GetJetStreamContext();
            await js.CreateStreamAsync(streamConfig);
            var ack = await js.PublishAsync(subject: streamSubject, data: System.Text.Encoding.UTF8.GetBytes(message));
            ack.EnsureSuccess();
            Console.WriteLine($"Message published to {streamSubject}.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error publishing message: {ex.Message}");
           
        }
    }

    public async Task SubscribeAsync(string streamName, string consumerName, Func<string, Task> messageHandler)
    {
        try
        {
            var js = _connectionManager.GetJetStreamContext();

            // Create or update the consumer
            //var consumerConfig = ConsumerConfig.Builder()
            //                                    .WithDurable(consumerName)
            //                                    .Build();
            var consumer = await js.CreateOrUpdateConsumerAsync(streamName, new ConsumerConfig(consumerName));

            await foreach (var jsMsg in consumer.ConsumeAsync<string>())
            {
                Console.WriteLine($"Processed: {jsMsg.Data}");
                await jsMsg.AckAsync();
            }


            Console.WriteLine($"Subscribed to {streamName} with consumer {consumerName}.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error subscribing to messages: {ex.Message}");
            throw;
        }
    }

    public NatsJSContext GetJetStreamContext()
    {
        return _connectionManager.GetJetStreamContext();
    }

}
*/
//public class NatsService : INatsService
//{
//    private readonly NatsConnectionManager _connectionManager;

//    public NatsService(NatsConnectionManager connectionManager)
//    {
//        _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
//    }

//    public async Task PublishAsync(string streamName, string streamSubject, string message)
//    {
//        try
//        {
//            var js = _connectionManager.GetJetStreamContext();
//            var ack = await js.PublishAsync(subject: streamSubject, data: message);
//            ack.EnsureSuccess();
//            Console.WriteLine($"Message published to {streamSubject}.");
//        }
//        catch (Exception ex)
//        {
//            Console.WriteLine($"Error publishing message: {ex.Message}");
//            throw;
//        }
//    }

//    public async Task SubscribeAsync(string streamName, string consumerName, Func<string, Task> messageHandler)
//    {
//        try
//        {
//            var js = _connectionManager.GetJetStreamContext();

//            // Create or update the consumer
//            await js.CreateOrUpdateConsumerAsync(streamName, new ConsumerConfig(consumerName));

//            // Get the consumer
//            var consumer = await js.GetConsumerAsync(streamName, consumerName);

//            // Consume messages
//            await foreach (var msg in consumer.ConsumeAsync<string>())
//            {
//                await messageHandler(msg.Data);
//                await msg.AckAsync();
//            }
//        }
//        catch (Exception ex)
//        {
//            Console.WriteLine($"Error subscribing to messages: {ex.Message}");
//            throw;
//        }
//    }

//    public NatsJSContext GetJetStreamContext()
//    {
//        return _connectionManager.GetJetStreamContext();
//    }
//}







//public class NatsService : INatsService
//{
//    private readonly NatsConnectionManager _connectionManager;

//    public NatsService(NatsConnectionManager connectionManager)
//    {
//        _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
//    }

//    public async Task PublishAsync(string streamName, string streamSubject, string message)
//    {
//        try
//        {
//            await  _connectionManager.ConnectAsync();
//            var js = _connectionManager.GetJetStreamContext();

//            var ack = await js.PublishAsync(subject: streamSubject, data: message);
//            ack.EnsureSuccess();
//            Console.WriteLine($"Message published to {streamSubject}.");
//        }
//        catch (Exception ex)
//        {
//            Console.WriteLine($"Error publishing message: {ex.Message}");
//            throw;
//        }
//    }

//    public async Task SubscribeAsync(string streamName, string consumerName, Func<string, Task> messageHandler)
//    {
//        try
//        {
//            await _connectionManager.ConnectAsync();
//            var js = _connectionManager.GetJetStreamContext();

//            // Create or update the consumer
//            await js.CreateOrUpdateConsumerAsync(streamName, new ConsumerConfig(consumerName));

//            // Get the consumer
//            var consumer = await js.GetConsumerAsync(streamName, consumerName);

//            // Consume messages
//            await foreach (var msg in consumer.ConsumeAsync<string>())
//            {
//                await messageHandler(msg.Data);
//                await msg.AckAsync();
//            }
//        }
//        catch (Exception ex)
//        {
//            Console.WriteLine($"Error subscribing to messages: {ex.Message}");
//            throw;
//        }
//    }
//}
