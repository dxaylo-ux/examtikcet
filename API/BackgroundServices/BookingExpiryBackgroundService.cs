using Application.Interfaces;
using Application.Services;
using Microsoft.Extensions.Options;

namespace API.BackgroundServices;

public class BookingExpiryBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly BookingSettings _settings;
    private readonly ILogger<BookingExpiryBackgroundService> _logger;

    public BookingExpiryBackgroundService(IServiceScopeFactory scopeFactory, IOptions<BookingSettings> settings,
        ILogger<BookingExpiryBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IBookingExpiryService>();
                await service.ExpireAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при истечении броней");
            }

            await Task.Delay(TimeSpan.FromSeconds(_settings.ExpiryCheckSeconds), stoppingToken);
        }
    }
}
