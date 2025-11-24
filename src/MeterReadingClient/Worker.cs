using Grpc.Net.Client;
using MeterReader.gRPC;
using System.Security.Cryptography.X509Certificates;
using static MeterReader.gRPC.MeterReaderService;

namespace MeterReadingClient
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly ReadingGenerator _generator;
        private readonly int _customerId;
        private readonly string _serviceUrl;
        private readonly IConfiguration _config;
        private string _token;
        private DateTime _expiration;

        public Worker(ILogger<Worker> logger, ReadingGenerator generator, IConfiguration config)
        {
            _logger = logger;
            _generator = generator;
            _customerId = config.GetValue<int>("CustomerId");
            _serviceUrl = config.GetValue<string>("ServiceUrl")!;
            _config = config;
            _token = "";
            _expiration = DateTime.MinValue;
        }
        bool NeedsLogin() =>
            string.IsNullOrEmpty(_token) || DateTime.UtcNow.AddMinutes(1) >= _expiration;

        async Task<bool> RequestToken()
        {
            try
            {
                var req = new TokenRequest()
                {
                    Username = _config["Settings:Username"],
                    Password = _config["Settings:Password"]
                };
                var result = await CreateClient().GenerateTokenAsync(req);

                if (result.Success)
                {
                    _token = result.Token;
                    _expiration = result.Expiration.ToDateTime();
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception getting token: {Message}", ex.Message);
            }
            return false;
        }

        MeterReaderServiceClient CreateClient()
        {
            var certificate = new X509Certificate2(_config["Settings:Certificate:Name"]!, _config["Settings:Certificate:Password"]);

            var handler = new HttpClientHandler();
            handler.ClientCertificates.Add(certificate);

            var httpClient = new HttpClient(handler);

            var options = new GrpcChannelOptions
            {
                HttpClient = httpClient
            };

            var channel = GrpcChannel.ForAddress(_serviceUrl, options);
            var client = new MeterReaderServiceClient(channel);
            return client;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {

                //if (!NeedsLogin() || await RequestToken())
                //{
                //var headers = new Metadata();
                //headers.Add("Authorization", $"Bearer {_token}");
                var stream = CreateClient().AddReadingStream(); // headers

                //var packet = new ReadingPacket()
                //{
                //    Succesful = ReadingStatus.Success
                //};

                for (var x = 0; x < 6; x++)
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

                while (await stream.ResponseStream.MoveNext(new CancellationToken()))
                {
                    _logger.LogWarning("Error from server: {Message}", stream.ResponseStream.Current.Message);
                }

                _logger.LogInformation("Fineshed calling GRPC");

                await Task.Delay(1000, stoppingToken);
                //}
                //else
                //{
                //    _logger.LogInformation("Failed to get JWT Token");
                //}
            }
        }
    }
}
