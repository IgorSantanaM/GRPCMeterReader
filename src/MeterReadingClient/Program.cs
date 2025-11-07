using MeterReadingClient;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddTransient<ReadingGenerator>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
