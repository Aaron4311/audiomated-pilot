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
        return Task.Run(async () =>
        {
            try
            {
                // Watch mode only reacts to *future* default-device-changed
                // events — it never checks the device Windows already picked
                // at boot. Without this, a device that's already wrong when
                // the watcher starts (e.g. Windows/a driver picked something
                // other than config.json before this service even started)
                // stays wrong forever, since nothing ever "changes" again to
                // trigger a revert. Apply config.json once at startup first,
                // mirroring `Audio.CLI --startup`'s delay so other audio
                // drivers (Sonar included) have time to finish their own
                // startup routine before we set anything.
                await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);
                try
                {
                    var applied = _manager.ApplyConfig();
                    _logger.LogInformation("Startup apply complete: Playback={Playback}, Recording={Recording}, Communications={Communications}", applied.Playback, applied.Recording, applied.Communications);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Startup apply failed; continuing to watch mode anyway.");
                }

                _manager.RunWatchMode(msg => _logger.LogInformation("{Message}", msg), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // normal shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Watch mode terminated unexpectedly.");
            }
        }, stoppingToken);
    }
}
