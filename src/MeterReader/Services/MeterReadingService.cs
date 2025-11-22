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
                    return new StatusMessage
                    {
                        Message = "Readings successfully saved.",
                        Success = ReadingStatus.Success
                    };
                }
            }

            return  new StatusMessage()
            {
                Message = "Failed to save readings.",
                Success = ReadingStatus.Failure
            };
        }
    }
}
