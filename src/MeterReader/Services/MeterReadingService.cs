using Grpc.Core;
using MeterReader.gRPC;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using static MeterReader.gRPC.MeterReaderService;

namespace MeterReader.Services
{
    [Authorize(AuthenticationSchemes = CertificateAuthenticationDefaults.AuthenticationScheme)]
    public class MeterReadingService(IReadingRepository repository, ILogger<MeterReadingService> logger, JwtTokenValidationService tokenService) : MeterReaderServiceBase
    {
        [AllowAnonymous]
        public override async Task<TokenResponse> GenerateToken(TokenRequest request, ServerCallContext context)
        {
            var creds = new CredentialModel
            {
                UserName = request.Username,
                Passcode = request.Password
            };

            var result = await tokenService.GenerateTokenModelAsync(creds);

            if (result.Success)
            {
                return new TokenResponse
                {
                    Success = true,
                    Token = result.Token!,
                    Expiration = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(result.Expiration.ToUniversalTime())
                };
            }
            else
            {
                return new TokenResponse
                {
                    Success = false
                };
            }
        }

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

            return new StatusMessage()
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

                if (msg.ReadingValue < 500)
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
