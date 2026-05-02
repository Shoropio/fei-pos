# Documentación Técnica de Fei POS

Esta guía contiene la información necesaria para administradores de sistemas, contadores y desarrolladores que necesitan configurar el comportamiento profundo de Fei POS.

---

## 🏗️ 1. Arquitectura y Base de Datos

Fei POS es una aplicación local completamente autónoma. 
- **Tecnología:** .NET 8 (WPF para UI)
- **Base de Datos:** SQLite. Los datos se almacenan de forma local en el archivo `feipos.db`, que se genera automáticamente la primera vez que abres la aplicación.
- **ORM:** Entity Framework Core (EF Core). Las migraciones se aplican automáticamente en el arranque, lo que significa que el sistema siempre estará actualizado sin necesidad de scripts SQL.

---

## 🌐 2. Pruebas y Facturación (Modo Sandbox de Hacienda)

Fei POS incluye soporte oficial para el **entorno de pruebas (Sandbox)** del Ministerio de Hacienda de Costa Rica. Esto te permite generar facturas y hacer pruebas de transmisión sin afectar tu contabilidad real.

### ¿Cómo activar las pruebas?
1. Ingresa a la aplicación y navega al menú **Ajustes**.
2. Desplázate hacia abajo hasta la sección **Hacienda (ATV)**.
3. Activa el interruptor **Modo Sandbox (Pruebas)** (Se pondrá verde).
4. Configura el certificado de pruebas: Usa el botón **Buscar...** bajo *Firma Digital* para cargar tu archivo `.p12` de *Staging* proporcionado por el Ministerio.
5. Digita el PIN del certificado y los credenciales API (Usuario y Contraseña) que sacaste del portal ATV de pruebas.
6. Dale clic al botón **Guardar**.

### ¿Cómo funciona la transmisión?
El sistema trabaja asincrónicamente mediante el `HaciendaQueueWorker`. Al facturar, el tiquete se imprime instantáneamente y se encola. Cada 30 segundos, el worker:
1. Revisa si hay facturas pendientes.
2. Extrae el Token OpenID de Hacienda (según estés en Prod o Sandbox).
3. Transforma la venta en XML, lo firma localmente con tu `.p12` y lo envía.
4. Puedes ver el estado de cada envío (y la respuesta de Hacienda) en la pestaña **Facturas**.

Si una factura falla (ej. Hacienda está caído o internet falló), puedes ir a la vista de **Facturas**, seleccionar el registro y usar el botón **Reenviar a Hacienda**.

---

## 🖨️ 3. Integración de Hardware (Impresoras y Gavetas)

El sistema soporta comandos nativos `ESC/POS` de Epson, ampliamente estandarizados en el mercado de impresoras térmicas (Xprinter, Epson, Bematech, etc.).

### Configuración de la Impresora
1. Instala los controladores (drivers) de tu impresora en Windows y asegúrate de que aparezca en tu lista de "Dispositivos e Impresoras" de Windows y puedas imprimir una página de prueba genérica.
2. Abre Fei POS, ve a **Ajustes** -> **Impresora**.
3. Selecciona tu impresora en la lista desplegable.
4. Haz clic en **Prueba de Impresión**. Si la impresora reacciona e imprime el logo de prueba, la conexión es exitosa.

### Activación de Gaveta de Dinero (Caja Registradora)
Fei POS abre automáticamente la gaveta conectada a la impresora en dos escenarios (siempre que esté activado el *check* "Abrir gaveta al cobrar" en los Ajustes):
1. Cuando se finaliza una venta e imprimes un tiquete.
2. Cuando se registra un "Retiro de efectivo" en el menú de Control de Caja (Cash Drawer).

El comando en crudo enviado es el estándar decimal `[27, 112, 0, 25, 250]`.

---

## 💻 4. Guía para Desarrolladores (Compilar desde el código)

Si quieres clonar este repositorio y compilar el código fuente tú mismo:

1. Instala el [SDK de .NET 8](https://dotnet.microsoft.com/download/dotnet/8.0).
2. Clona el repositorio:
   ```bash
   git clone https://github.com/Shoropio/fei-pos.git
   ```
3. Restaura las dependencias e inicia el proyecto:
   ```bash
   cd fei-pos
   dotnet restore
   dotnet run --project src/FeiPos.Presentation/FeiPos.Presentation.csproj
   ```

Las credenciales administrativas para el entorno de desarrollo también serán `admin` / `admin123`.

*Nota:* Asegúrate de mantener ignorados los archivos generados localmente como `feipos.db` y configuraciones privadas mediante el `.gitignore` existente.
