using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MeterReader.gRPC;
using static MeterReader.gRPC.MeterReaderService;

namespace MeterReader.Services
{
    public class MeterReadingService(IReadingRepository repository, ILogger<MeterReadingService> logger) : MeterReaderServiceBase
    {
        public override async Task<StatusMessage> AddReading(ReadingPacket request, ServerCallContext context)
        {
            if (request.Succesful == ReadingStatus.Success)
            {
                foreach (var reading in request.Readings)
                {
                    var readingValue = new MeterReading()
                    {
                        CustomerId = reading.CustomerId,
                        Value = reading.ReadingValue,
                        ReadingDate = reading.ReadingTime.ToDateTime()
                    };

                    repository.AddEntity(readingValue);
                }
                if (await repository.SaveAllAsync())
                {
                    logger.LogInformation("Successfully saved {count} readings.", request.Readings.Count);
                    return new StatusMessage
                    {
                        Message = "Readings successfully saved.",
                        Status = ReadingStatus.Success
                    };
                }
            }

            return  new StatusMessage()
            {
                Message = "Failed to save readings.",
                Status = ReadingStatus.Failure
            };
        }

        public override async Task AddReadingStream(
            IAsyncStreamReader<ReadingMessage> requestStream,
            IServerStreamWriter<ErrorMessage> responseStream,
            ServerCallContext context)
        {
            while (await requestStream.MoveNext())
            {
                var msg = requestStream.Current;

                if(msg.ReadingValue < 500)
                {
                    await responseStream.WriteAsync(new ErrorMessage
                    {
                        Message = $"Reading value {msg.ReadingValue} is below the minimum threshold."
                    });
                }
                var readingValue = new MeterReading()
                {
                    CustomerId = msg.CustomerId,
                    Value = msg.ReadingValue,
                    ReadingDate = msg.ReadingTime.ToDateTime()
                };
                logger.LogInformation("Received reading for CustomerId: {customerId} with Value: {value}", msg.CustomerId, msg.ReadingValue);
                repository.AddEntity(readingValue);
                await repository.SaveAllAsync();
            }
        }
    }
}
