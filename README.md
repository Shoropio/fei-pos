# Fei POS - Sistema de Punto de Venta Moderno

Fei POS es una aplicación de escritorio robusta y moderna diseñada para el mercado costarricense, ofreciendo integración completa con la Facturación Electrónica de Hacienda (v4.3) y soporte para hardware de punto de venta.

## 🚀 Características Principales

- **Facturación Electrónica CR (v4.3):** Integración nativa con Hacienda, manejo automático de colas de envío, reintentos y almacenamiento de XML (Generado, Firmado, Respuesta).
- **Gestión de Inventario:** Control de stock, categorías, generación automática de SKU y selector visual de productos.
- **Ventas y Facturación:** Interfaz optimizada para POS, manejo de múltiples métodos de pago (Efectivo, Tarjeta, Cheque, Crédito, SINPE).
- **Seguridad:** Sistema de usuarios con roles, contraseñas seguras (Hashing) y ventana de inicio de sesión profesional.
- **Hardware POS:** Soporte para impresoras térmicas ESC/POS y apertura automática de gaveta de dinero.
- **Cierre de Caja:** Movimientos de entrada/salida (chica) y reportes de cierre diario con cuadre de efectivo.
- **Diseño Premium:** Interfaz oscura (Dark Mode) con estética industrial, bordes cuadrados y alineación precisa.

## 🛠️ Tecnologías

- **Framework:** .NET Core (WPF)
- **Base de Datos:** SQLite con Entity Framework Core
- **UI:** ModernWPF (Windows UI styles)
- **Facturación:** Hacienda CR v4.3 (Integration Services)
- **Impresión:** ESC/POS nativo

## 📋 Requisitos

- Windows 10/11
- .NET Desktop Runtime
- Certificado de Firma Digital (.p12) y credenciales de ATV (para facturación real).

## 📄 Licencia

Este proyecto es propiedad de Shoropio. Reservados todos los derechos.
