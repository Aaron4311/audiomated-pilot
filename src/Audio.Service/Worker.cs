using Audio.Core;

namespace Audio.Service;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly AudioManager _manager = new();

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(() =>
        {
            try
            {
                _manager.RunWatchMode(msg => _logger.LogInformation("{Message}", msg), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Watch mode terminated unexpectedly.");
            }
        }, stoppingToken);
    }
}
