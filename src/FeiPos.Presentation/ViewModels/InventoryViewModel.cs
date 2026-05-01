using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FeiPos.Domain.Entities;
using FeiPos.Infrastructure.Persistence;

namespace FeiPos.Presentation.ViewModels
{
    public partial class InventoryViewModel : ObservableObject
    {
        private readonly AppDbContext _context;

        [ObservableProperty]
        private ObservableCollection<Product> _products = new();

        [ObservableProperty]
        private Product? _selectedProduct;

        public InventoryViewModel(AppDbContext context)
        {
            _context = context;
            LoadProducts();
        }

        [RelayCommand]
        private void LoadProducts()
        {
            var list = _context.Products.ToList();
            Products = new ObservableCollection<Product>(list);
        }

        [RelayCommand]
        private void AddProduct()
        {
            // Evitar agregar múltiples productos vacíos si ya existe uno llamado "Nuevo Producto" sin cambios
            var existingEmpty = Products.FirstOrDefault(p => p.Name == "Nuevo Producto" && p.Price == 0 && p.Stock == 0);
            if (existingEmpty != null)
            {
                SelectedProduct = existingEmpty;
                return;
            }

            var lastSku = _context.Products
                .Where(p => p.Sku.StartsWith("PROD"))
                .Select(p => p.Sku)
                .AsEnumerable()
                .Select(s => int.TryParse(s.Substring(4), out var n) ? n : 0)
                .DefaultIfEmpty(0)
                .Max();

            var newProd = new Product
            {
                Name = "Nuevo Producto",
                Sku = $"PROD{(lastSku + 1):D3}",
                GroupName = "General",
                UnitOfMeasure = "Unid",
                ColorHex = "#455A64",
                Price = 0,
                Stock = 0,
                Barcode = string.Empty,
                IsActive = true
            };
            _context.Products.Add(newProd);
            _context.SaveChanges();
            LoadProducts();
            SelectedProduct = Products.FirstOrDefault(p => p.Sku == newProd.Sku);
        }

        [RelayCommand]
        private void SaveChanges()
        {
            _context.SaveChanges();
            LoadProducts();
        }

        [RelayCommand]
        private void SetProductColor(string hex)
        {
            if (SelectedProduct != null)
            {
                SelectedProduct.ColorHex = hex;
            }
        }
    }
}
