using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FeiPos.Domain.Entities;
using FeiPos.Infrastructure.Persistence;

namespace FeiPos.Presentation.ViewModels
{
    public partial class CustomerViewModel : ObservableObject
    {
        private readonly AppDbContext _context;

        [ObservableProperty]
        private ObservableCollection<Customer> _customers = new();

        [ObservableProperty]
        private Customer? _selectedCustomer;

        public CustomerViewModel(AppDbContext context)
        {
            _context = context;
            LoadCustomers();
        }

        [RelayCommand]
        private void LoadCustomers()
        {
            var list = _context.Customers.ToList();
            Customers = new ObservableCollection<Customer>(list);
        }

        [RelayCommand]
        private void AddCustomer()
        {
            var newCust = new Customer { FullName = "Nuevo Cliente", Identification = "0" };
            _context.Customers.Add(newCust);
            _context.SaveChanges();
            LoadCustomers();
            SelectedCustomer = newCust;
        }

        [RelayCommand]
        private void SaveChanges()
        {
            _context.SaveChanges();
            LoadCustomers();
        }
    }
}
