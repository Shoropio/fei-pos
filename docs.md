# Documentación Técnica de Fei POS

Esta guía está orientada a administradores, personal contable y desarrolladores que necesitan configurar o entender el funcionamiento interno de Fei POS.

---

## 1. Arquitectura y persistencia

Fei POS es una aplicación de escritorio autónoma.

* Plataforma: .NET 8 (WPF)
* Base de datos: SQLite
* Archivo de datos: `feipos.db` (generado automáticamente en el primer inicio)
* Acceso a datos: Entity Framework Core

Las migraciones se ejecutan automáticamente al iniciar la aplicación, por lo que no se requieren scripts manuales para mantener la estructura de la base de datos actualizada.

---

## 2. Facturación electrónica (Sandbox de Hacienda)

El sistema soporta el entorno de pruebas (sandbox) del Ministerio de Hacienda de Costa Rica, permitiendo validar la integración sin afectar datos reales.

### Activación

1. Ir a **Ajustes**
2. Sección **Hacienda (ATV)**
3. Activar **Modo Sandbox**
4. Cargar certificado `.p12` de pruebas
5. Ingresar PIN y credenciales del entorno ATV
6. Guardar configuración

### Proceso de envío

La transmisión se realiza de forma asincrónica mediante un proceso interno:

* Las facturas se generan e imprimen inmediatamente
* Se colocan en una cola de envío
* Un proceso en segundo plano:

  * Obtiene token de Hacienda
  * Genera el XML
  * Firma el documento con el certificado
  * Envía la información

El estado de cada documento puede consultarse en la sección **Facturas**.

Si ocurre un error, el sistema permite reenviar manualmente el documento.

---

## 3. Integración con hardware

Fei POS utiliza el estándar ESC/POS para comunicación con impresoras térmicas.

### Impresoras

1. Instalar drivers en Windows
2. Verificar impresión desde el sistema operativo
3. Configurar en **Ajustes > Impresora**
4. Ejecutar prueba de impresión

### Gaveta de dinero

La apertura de gaveta se ejecuta automáticamente en:

* Finalización de venta
* Registro de retiro de efectivo

Comando utilizado:

`[27, 112, 0, 25, 250]`

---

## 4. Compilación desde código fuente

Para ejecutar el proyecto desde código:

1. Instalar SDK de .NET 8
2. Clonar repositorio:

   ```bash
   git clone https://github.com/Shoropio/fei-pos.git
   ```
3. Restaurar dependencias:

   ```bash
   dotnet restore
   ```
4. Ejecutar:

   ```bash
   dotnet run --project src/FeiPos.Presentation/FeiPos.Presentation.csproj
   ```

Credenciales por defecto en entorno de desarrollo:

* Usuario: `admin`
* Contraseña: `admin123`

---

## Notas

* El archivo `feipos.db` debe permanecer fuera del control de versiones
* Las configuraciones locales deben respetar el `.gitignore`
