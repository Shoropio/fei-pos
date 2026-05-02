using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using FeiPos.Infrastructure.Persistence;

namespace FeiPos.Presentation.ViewModels
{
    public partial class BackupsViewModel : ObservableObject
    {
        private readonly AppDbContext _context;

        [ObservableProperty] private string _statusMessage = "Listo para crear copia de seguridad.";
        
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotWorking))]
        private bool _isWorking;
        
        [ObservableProperty] private string _lastBackupDate = "Nunca";

        public bool IsNotWorking => !IsWorking;

        public BackupsViewModel(AppDbContext context)
        {
            _context = context;
        }

        [RelayCommand]
        private async Task CreateBackup()
        {
            if (IsWorking) return;

            var myDocs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var targetFolder = Path.Combine(myDocs, "FeiPOS_Backups");
            if (!Directory.Exists(targetFolder))
            {
                Directory.CreateDirectory(targetFolder);
            }
            
            IsWorking = true;
            StatusMessage = "Creando copia de seguridad...";

            try
            {
                // Forzar checkpoint de WAL en SQLite antes de copiar
                await _context.Database.ExecuteSqlRawAsync("PRAGMA wal_checkpoint(FULL);");

                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var zipPath = Path.Combine(targetFolder, $"FeiPOS_Backup_{timestamp}.zip");

                await Task.Run(() =>
                {
                    using var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);
                    
                    void AddFile(string path)
                    {
                        if (!File.Exists(path)) return;
                        var entry = archive.CreateEntry(Path.GetFileName(path));
                        using var entryStream = entry.Open();
                        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        fs.CopyTo(entryStream);
                    }

                    AddFile("feipos.db");
                    AddFile("feipos.db-wal");
                    AddFile("feipos.db-shm");
                });

                StatusMessage = $"Copia de seguridad guardada exitosamente en:\n{zipPath}";
                LastBackupDate = DateTime.Now.ToString("g");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error al crear respaldo: {ex.Message}";
            }
            finally
            {
                IsWorking = false;
            }
        }

        [RelayCommand]
        private async Task RestoreBackup()
        {
            if (IsWorking) return;

            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Archivos ZIP (*.zip)|*.zip",
                Title = "Seleccione el archivo de respaldo a restaurar"
            };

            if (dialog.ShowDialog() != true) return;

            var zipPath = dialog.FileName;
            IsWorking = true;
            StatusMessage = "Restaurando base de datos...";

            try
            {
                await _context.Database.CloseConnectionAsync();
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

                await Task.Run(() =>
                {
                    if (File.Exists("feipos.db")) File.Delete("feipos.db");
                    if (File.Exists("feipos.db-wal")) File.Delete("feipos.db-wal");
                    if (File.Exists("feipos.db-shm")) File.Delete("feipos.db-shm");

                    using var archive = ZipFile.OpenRead(zipPath);
                    foreach (var entry in archive.Entries)
                    {
                        entry.ExtractToFile(entry.Name, overwrite: true);
                    }
                });

                System.Windows.MessageBox.Show("Restauración completada con éxito. La aplicación se cerrará ahora para aplicar los cambios. Por favor, vuelva a abrir Fei POS.", "Restauración exitosa", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                System.Windows.Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error al restaurar: {ex.Message}";
                IsWorking = false;
            }
        }
    }
}
