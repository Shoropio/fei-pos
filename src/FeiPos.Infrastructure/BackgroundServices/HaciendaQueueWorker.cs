using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using FeiPos.Domain.Entities;
using FeiPos.Infrastructure.Persistence;
using FeiPos.Application.Interfaces;
using FeiPos.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace FeiPos.Infrastructure.BackgroundServices
{
    public class HaciendaQueueWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<HaciendaQueueWorker> _logger;

        public HaciendaQueueWorker(IServiceProvider serviceProvider, ILogger<HaciendaQueueWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Hacienda Queue Worker iniciado.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessQueue(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error procesando la cola de Hacienda.");
                }

                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }

        private async Task ProcessQueue(CancellationToken ct)
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var haciendaService = scope.ServiceProvider.GetRequiredService<IHaciendaService>();
            var identityService = scope.ServiceProvider.GetRequiredService<HaciendaIdentityService>();
            var config = scope.ServiceProvider.GetRequiredService<ConfigurationService>().Config;

            // 1. Obtener ventas pendientes de envío
            var pendingSales = await db.Sales
                .Where(s => s.InvoiceStatus == ElectronicInvoiceStatus.PendingSend)
                .ToListAsync(ct);

            if (pendingSales.Any())
            {
                var token = await identityService.GetAccessTokenAsync(config.HaciendaUser, "password_placeholder"); // Necesita password real
                if (token != null)
                {
                    foreach (var sale in pendingSales)
                    {
                        // Aquí se generaría el XML real, se firmaría y se enviaría
                        // Por ahora simulamos el flujo
                        var success = await haciendaService.SendToHacienda("xml_simulado", token);
                        if (success)
                        {
                            sale.InvoiceStatus = ElectronicInvoiceStatus.Sent;
                        }
                        else
                        {
                            sale.InvoiceStatus = ElectronicInvoiceStatus.Error;
                        }
                    }
                    await db.SaveChangesAsync(ct);
                }
            }

            // 2. Consultar estado de ventas enviadas
            var sentSales = await db.Sales
                .Where(s => s.InvoiceStatus == ElectronicInvoiceStatus.Sent)
                .ToListAsync(ct);

            foreach (var sale in sentSales)
            {
                if (string.IsNullOrEmpty(sale.HaciendaKey)) continue;
                
                var status = await haciendaService.CheckStatus(sale.HaciendaKey);
                if (status != ElectronicInvoiceStatus.Sent)
                {
                    sale.InvoiceStatus = status;
                }
            }
            
            if (sentSales.Any())
            {
                await db.SaveChangesAsync(ct);
            }
        }
    }
}
