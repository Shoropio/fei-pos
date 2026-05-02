# Fei POS

Sistema de punto de venta para comercios en Costa Rica, con facturación electrónica integrada (versión 4.3 de Hacienda) y compatibilidad con hardware de caja.

Fei POS está diseñado para operar sin configuraciones complejas, permitiendo iniciar ventas en pocos minutos desde una instalación local.

---

## Qué resuelve

Fei POS cubre las operaciones básicas de un punto de venta:

* Facturación electrónica conforme a normativa vigente
* Control de inventario en tiempo real
* Registro de ventas y métodos de pago
* Gestión de caja (aperturas, movimientos y cierres)
* Integración con impresoras térmicas y gavetas de dinero

---

## Instalación

1. Descarga la última versión desde la sección de releases
2. Extrae el archivo en una carpeta local (ej. `C:\FeiPOS`)
3. Ejecuta `FeiPos.Presentation.exe`

El sistema crea automáticamente la base de datos en el primer inicio.

---

## Acceso inicial

Credenciales por defecto:

* Usuario: `admin`
* Contraseña: `admin123`

Se recomienda cambiar estas credenciales después del primer ingreso.

---

## Entorno de operación

* Aplicación de escritorio (Windows 64 bits)
* Base de datos local (sin servidor requerido)
* Operación offline con sincronización según configuración fiscal

---

## Hardware soportado

* Impresoras térmicas compatibles con ESC/POS
* Gavetas de dinero con apertura automática

---

## Configuración avanzada

La configuración de facturación electrónica, entorno sandbox de Hacienda y dispositivos se documenta en:

`docs.md`

---

## Estado del proyecto

Fei POS se encuentra en desarrollo activo. Las funcionalidades pueden ampliarse o ajustarse según requerimientos del mercado y cambios en la normativa fiscal.

---

## Contribuciones

Las contribuciones son bienvenidas.

Si deseas proponer mejoras, correcciones o nuevas funcionalidades, puedes hacerlo mediante un *pull request*. Para cambios importantes, se recomienda abrir primero un *issue* para discutir la propuesta.

Antes de contribuir, revisa la documentación y asegúrate de que tu cambio esté alineado con la arquitectura del proyecto.

---

## Documentación

La configuración avanzada, facturación electrónica, hardware y guía de desarrollo están disponibles en:

👉 [Documentación técnica](./docs.md)

---

## Licencia

© 2026 Shoropio Corporation. Todos los derechos reservados.
