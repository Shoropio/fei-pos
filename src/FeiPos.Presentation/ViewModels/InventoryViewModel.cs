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
            var next = _context.Products.Count() + 1;
            var newProd = new Product
            {
                Name = "Nuevo Producto",
                Sku = $"NEW{next:000}",
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
            SelectedProduct = newProd;
        }

        [RelayCommand]
        private void SaveChanges()
        {
            _context.SaveChanges();
            LoadProducts();
        }
    }
}
