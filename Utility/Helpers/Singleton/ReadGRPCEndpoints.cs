using System;
using System.Collections.Generic;
using Grpc.Core;
using Grpc.Net.Client;

namespace Helpers.Singletons
{
    public class GRPCEndpointsModel
    {
        public required int Port { get; set; }
        public required string Host { get; set; }
        public required string Password { get; set; }
    }

    public class ReadGRPCEndpoints
    {
        private static readonly object LockObject = new object();
        private static ReadGRPCEndpoints? _instance;

        private readonly Dictionary<string, GRPCEndpointsModel> _grpcEndpoints;

        private ReadGRPCEndpoints()
        {
            _grpcEndpoints = InitializeEndpoints();
        }

        private Dictionary<string, GRPCEndpointsModel> InitializeEndpoints()
        {
            Console.WriteLine($"GRPC endpoints loading in api");
            var endpointsDictionary = new Dictionary<string, GRPCEndpointsModel>();
            var envVariable = Environment.GetEnvironmentVariable("GRPCEndpoints")?? "parser,ai-srv.qbscocloud.net,32089,password123|UserManagement,ai-srv.qbscocloud.net,32090,password123|";

            if (string.IsNullOrEmpty(envVariable))
            {
                return endpointsDictionary;
            }

            var endpoints = envVariable.Split('|');
            
            foreach (var endpoint in endpoints)
            {
                Console.WriteLine($"{endpoint} loaded");
                var parts = endpoint.Split(',');

                if (parts.Length != 4)
                {
                    continue;
                }

                var grpcApiName = parts[0];
                var host = parts[1];
                if (!int.TryParse(parts[2], out var port))
                {
                    throw new FormatException("Invalid port number in GRPC endpoint format.");
                }
                var password = parts[3];

                var grpcConfig = new GRPCEndpointsModel
                {
                    Host = host,
                    Port = port,
                    Password = password
                };

                endpointsDictionary.TryAdd(grpcApiName, grpcConfig);
            }

            return endpointsDictionary;
        }

        public GRPCEndpointsModel Get(string key)
        {
            if (_grpcEndpoints.TryGetValue(key, out var value))
            {
                return value;
            }

            throw new KeyNotFoundException($"Invalid GRPC server key: {key}");
        }

        /// <summary>
        /// Creates a gRPC client for the specified service contract.
        /// </summary>
        /// <typeparam name="TClient">The type of the gRPC client (e.g., UserManagementGRPCServiceContractClient).</typeparam>
        /// <param name="serviceName">The name of the gRPC service (e.g., "UserManagement").</param>
        /// <param name="headers">Optional metadata to include in the gRPC calls.</param>
        /// <returns>An instance of the gRPC client.</returns>
        public TClient CreateGrpcClient<TClient>(string serviceName,string source, Metadata headers = null) where TClient : class
        {
            //var grpcCreds = Get(serviceName);
            
            // Create the gRPC channel
            var grpcChannel = GrpcChannel.ForAddress(source);

            // Dynamically create the gRPC client using reflection
            var clientType = typeof(TClient);
            var constructor = clientType.GetConstructor(new[] { typeof(GrpcChannel) });

            if (constructor == null)
            {
                throw new InvalidOperationException($"No constructor found for {clientType.Name} that accepts a GrpcChannel.");
            }

            var grpcClient = constructor.Invoke(new object[] { grpcChannel }) as TClient;

            if (grpcClient == null)
            {
                throw new InvalidOperationException($"Failed to create an instance of {clientType.Name}.");
            }

            // Optionally add metadata (headers) to the client call
            if (headers != null)
            {
                // If you need to add metadata to every call, you can wrap the client in a proxy class.
                // This is more advanced and depends on your use case.
            }

            return grpcClient;
        }

        public static ReadGRPCEndpoints Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (LockObject)
                    {
                        if (_instance == null)
                        {
                            _instance = new ReadGRPCEndpoints();
                        }
                    }
                }

                return _instance;
            }
        }
    }
}