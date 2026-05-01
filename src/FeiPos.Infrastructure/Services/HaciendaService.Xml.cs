using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using FeiPos.Domain.Entities;
using FeiPos.Application.Interfaces;

namespace FeiPos.Infrastructure.Services
{
    public partial class HaciendaService : IHaciendaService
    {
        public Task<string> GenerateXml(Sale sale)
        {
            XNamespace ns = "https://cdn.comprobanteselectronicos.go.cr/xml-schemas/v4.3/facturaElectronica";
            
            var config = _configService.Config;
            
            var doc = new XDocument(
                new XElement(ns + "FacturaElectronica",
                    new XElement(ns + "Clave", sale.HaciendaKey),
                    new XElement(ns + "CodigoActividad", config.EconomicActivity),
                    new XElement(ns + "NumeroConsecutivo", sale.ConsecutiveNumber),
                    new XElement(ns + "FechaEmision", sale.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssK")),
                    new XElement(ns + "CondicionVenta", "01"), // Contado
                    new XElement(ns + "MedioPago", "01"), // Efectivo (por defecto)
                    
                    // Emisor (Datos del comercio)
                    new XElement(ns + "Emisor",
                        new XElement(ns + "Nombre", config.CompanyName),
                        new XElement(ns + "Identificacion",
                            new XElement(ns + "Tipo", config.TaxId.Replace("-", "").Length == 10 ? "02" : "01"),
                            new XElement(ns + "Numero", config.TaxId.Replace("-", ""))
                        ),
                        new XElement(ns + "Ubicacion",
                            new XElement(ns + "Provincia", config.Province),
                            new XElement(ns + "Canton", config.Canton),
                            new XElement(ns + "Distrito", config.District),
                            new XElement(ns + "Barrio", config.Neighborhood),
                            new XElement(ns + "OtrasSenas", config.Address)
                        ),
                        new XElement(ns + "CorreoElectronico", config.Email)
                    ),

                    // Receptor (Opcional)
                    !string.IsNullOrEmpty(sale.CustomerTaxId) ? 
                    new XElement(ns + "Receptor",
                        new XElement(ns + "Nombre", sale.CustomerName),
                        new XElement(ns + "Identificacion",
                            new XElement(ns + "Tipo", sale.CustomerTaxId.Replace("-","").Length == 9 ? "01" : "02"),
                            new XElement(ns + "Numero", sale.CustomerTaxId.Replace("-",""))
                        )
                    ) : null,

                    // Detalles de la venta
                    new XElement(ns + "DetalleServicio",
                        sale.Items.Select((item, index) => 
                            new XElement(ns + "LineaDetalle",
                                new XElement(ns + "NumeroLinea", index + 1),
                                new XElement(ns + "Cantidad", item.Quantity.ToString("F3")),
                                new XElement(ns + "UnidadMedida", "Unid"),
                                new XElement(ns + "Detalle", item.ProductName),
                                new XElement(ns + "PrecioUnitario", item.UnitPrice.ToString("F5")),
                                new XElement(ns + "MontoTotal", (item.UnitPrice * item.Quantity).ToString("F5")),
                                new XElement(ns + "SubTotal", (item.UnitPrice * item.Quantity).ToString("F5")),
                                new XElement(ns + "Impuesto",
                                    new XElement(ns + "Codigo", "01"), // IVA
                                    new XElement(ns + "CodigoTarifa", "08"), // 13%
                                    new XElement(ns + "Tarifa", item.TaxRate.ToString("F2")),
                                    new XElement(ns + "Monto", item.TaxAmount.ToString("F5"))
                                ),
                                new XElement(ns + "MontoTotalLinea", item.Total.ToString("F5"))
                            )
                        )
                    ),

                    // Resumen
                    new XElement(ns + "ResumenFactura",
                        new XElement(ns + "CodigoTipoMoneda",
                            new XElement(ns + "CodigoMoneda", "CRC"),
                            new XElement(ns + "TipoCambio", "1.00000")
                        ),
                        new XElement(ns + "TotalServGravados", 0.00),
                        new XElement(ns + "TotalMercanciasGravadas", sale.SubTotal.ToString("F5")),
                        new XElement(ns + "TotalGravado", sale.SubTotal.ToString("F5")),
                        new XElement(ns + "TotalExento", 0.00),
                        new XElement(ns + "TotalVenta", sale.SubTotal.ToString("F5")),
                        new XElement(ns + "TotalDescuentos", 0.00),
                        new XElement(ns + "TotalVentaNeta", sale.SubTotal.ToString("F5")),
                        new XElement(ns + "TotalImpuesto", sale.TotalTax.ToString("F5")),
                        new XElement(ns + "TotalComprobante", sale.Total.ToString("F5"))
                    )
                )
            );

            return Task.FromResult(doc.ToString());
        }
    }
}
