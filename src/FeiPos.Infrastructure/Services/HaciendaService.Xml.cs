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
            
            var doc = new XDocument(
                new XElement(ns + "FacturaElectronica",
                    new XElement(ns + "Clave", sale.HaciendaKey),
                    new XElement(ns + "CodigoActividad", "000000"), // Debe venir de config
                    new XElement(ns + "NumeroConsecutivo", sale.ConsecutiveNumber),
                    new XElement(ns + "FechaEmision", sale.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssK")),
                    
                    // Emisor (Datos del comercio)
                    new XElement(ns + "Emisor",
                        new XElement(ns + "Nombre", "Mi Comercio S.A."),
                        new XElement(ns + "Identificacion",
                            new XElement(ns + "Tipo", "02"), // Jurídica
                            new XElement(ns + "Numero", "3101123456")
                        ),
                        new XElement(ns + "Ubicacion",
                            new XElement(ns + "Provincia", "1"),
                            new XElement(ns + "Canton", "01"),
                            new XElement(ns + "Distrito", "01"),
                            new XElement(ns + "OtrasSenas", "San Jose")
                        )
                    ),

                    // Receptor (Opcional)
                    !string.IsNullOrEmpty(sale.CustomerTaxId) ? 
                    new XElement(ns + "Receptor",
                        new XElement(ns + "Nombre", sale.CustomerName),
                        new XElement(ns + "Identificacion",
                            new XElement(ns + "Tipo", sale.CustomerTaxId.Length == 9 ? "01" : "02"),
                            new XElement(ns + "Numero", sale.CustomerTaxId)
                        )
                    ) : null,

                    // Detalles de la venta
                    new XElement(ns + "DetalleServicio",
                        sale.Items.Select((item, index) => 
                            new XElement(ns + "LineaDetalle",
                                new XElement(ns + "NumeroLinea", index + 1),
                                new XElement(ns + "Cantidad", item.Quantity),
                                new XElement(ns + "UnidadMedida", "Unid"),
                                new XElement(ns + "Detalle", item.ProductName),
                                new XElement(ns + "PrecioUnitario", item.UnitPrice),
                                new XElement(ns + "MontoTotal", item.UnitPrice * item.Quantity),
                                new XElement(ns + "SubTotal", item.UnitPrice * item.Quantity),
                                new XElement(ns + "Impuesto",
                                    new XElement(ns + "Codigo", "01"), // IVA
                                    new XElement(ns + "CodigoTarifa", "08"), // 13%
                                    new XElement(ns + "Tarifa", item.TaxRate),
                                    new XElement(ns + "Monto", item.TaxAmount)
                                ),
                                new XElement(ns + "MontoTotalLinea", item.Total)
                            )
                        )
                    ),

                    // Resumen
                    new XElement(ns + "ResumenFactura",
                        new XElement(ns + "CodigoTipoMoneda",
                            new XElement(ns + "CodigoMoneda", "CRC"),
                            new XElement(ns + "TipoCambio", "1.00")
                        ),
                        new XElement(ns + "TotalServGravados", 0.00),
                        new XElement(ns + "TotalMercanciasGravadas", sale.SubTotal),
                        new XElement(ns + "TotalGravado", sale.SubTotal),
                        new XElement(ns + "TotalExento", 0.00),
                        new XElement(ns + "TotalVenta", sale.SubTotal),
                        new XElement(ns + "TotalDescuentos", 0.00),
                        new XElement(ns + "TotalVentaNeta", sale.SubTotal),
                        new XElement(ns + "TotalImpuesto", sale.TotalTax),
                        new XElement(ns + "TotalComprobante", sale.Total)
                    )
                )
            );

            return Task.FromResult(doc.ToString());
        }
    }
}
