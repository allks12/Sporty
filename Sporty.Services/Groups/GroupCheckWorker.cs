using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Sporty.Services.Groups;

public class GroupCheckWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<GroupCheckWorker> _logger;
    private readonly TimeSpan _checkInterval;

    public GroupCheckWorker(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<GroupCheckWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        
        var seconds = configuration.GetValue<int?>("GroupCheck:IntervalSeconds") ?? 60;
        _checkInterval = TimeSpan.FromSeconds(Math.Max(10, seconds));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_checkInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Неочаквана грешка при изпълнение на фоновата проверка на групи.");
            }

            try
            {
                await timer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    internal async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var checkService = scope.ServiceProvider.GetRequiredService<IGroupCheckService>();
        var report = await checkService.RunAsync(cancellationToken);

        if (report.ConfirmedCount > 0 
            || report.CancelledCount > 0 
            || report.CompletedCount > 0)
        {
            _logger.LogInformation(
                "GroupCheck приключи: потвърдени={Confirmed}, отменени={Cancelled}, приключили={Completed}.",
                report.ConfirmedCount,
                report.CancelledCount,
                report.CompletedCount);
        }
    }
}