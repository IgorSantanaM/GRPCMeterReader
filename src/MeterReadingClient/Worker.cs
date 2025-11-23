using Grpc.Net.Client;
using MeterReader.gRPC;
using System.Runtime.CompilerServices;
using static MeterReader.gRPC.MeterReaderService;

namespace MeterReadingClient
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly ReadingGenerator _generator;
        private readonly int _customerId;
        private readonly string _serviceUrl;

        public Worker(ILogger<Worker> logger, ReadingGenerator generator, IConfiguration config)
        {
            _logger = logger;
            _generator = generator;
            _customerId = config.GetValue<int>("CustomerId");
            _serviceUrl = config.GetValue<string>("ServiceUrl")!;
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var channel = GrpcChannel.ForAddress(_serviceUrl);
                // Create the gRPC client - stub
                var client = new MeterReaderServiceClient(channel);

                var stream = client.AddReadingStream();

                //var packet = new ReadingPacket()
                //{
                //    Succesful = ReadingStatus.Success
                //};

                for(var x = 0; x < 6; x++)
                {
                    var reading = await _generator.GenerateAsync(_customerId);
                    //packet.Readings.Add(reading);
                    await stream.RequestStream.WriteAsync(reading);
                    await Task.Delay(500, stoppingToken);
                }

                //var status = client.AddReading(packet);

                //if(status.Status == ReadingStatus.Success)
                //{
                //    _logger.LogInformation("Successfully called GRPC");
                //}
                //else
                //{
                //    _logger.LogError("Failed to call GRPC: {Message}", status.Message); 
                //}

                await stream.RequestStream.CompleteAsync();

                //var result = await stream.ResponseAsync;

                while(await stream.ResponseStream.MoveNext(new CancellationToken()))
                {
                    _logger.LogWarning("Error from server: {Message}", stream.ResponseStream.Current.Message);
                }

                _logger.LogInformation("Fineshed calling GRPC");

                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
