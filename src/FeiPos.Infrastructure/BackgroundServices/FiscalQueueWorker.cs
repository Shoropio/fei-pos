using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using FeiPos.Application.Interfaces;
using FeiPos.Domain.Entities;
using System.Linq;

namespace FeiPos.Infrastructure.BackgroundServices
{
    public class FiscalQueueWorker : BackgroundService
    {
        private readonly ILogger<FiscalQueueWorker> _logger;
        private readonly IServiceProvider _serviceProvider;

        public FiscalQueueWorker(ILogger<FiscalQueueWorker> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Iniciando trabajador de cola fiscal...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var haciendaService = scope.ServiceProvider.GetRequiredService<IHaciendaService>();
                        // Aquí iría la lógica para buscar ventas con estado 'PendingSend'
                        // y procesarlas una a una.
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error procesando cola fiscal");
                }

                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }
}
