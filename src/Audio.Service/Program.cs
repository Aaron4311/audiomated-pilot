using Audio.Service;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService(options => options.ServiceName = "Audiomated Pilot");
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
