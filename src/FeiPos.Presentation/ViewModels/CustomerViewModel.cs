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

        [ObservableProperty]
        private string _statusMessage = string.Empty;

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
            var existingDraft = _context.Customers
                .FirstOrDefault(c => c.FullName == "Nuevo Cliente" && c.Identification == "0" && string.IsNullOrWhiteSpace(c.Email));

            if (existingDraft != null)
            {
                SelectedCustomer = Customers.FirstOrDefault(c => c.Id == existingDraft.Id) ?? existingDraft;
                StatusMessage = "Complete el cliente nuevo antes de crear otro.";
                return;
            }

            var newCust = new Customer { FullName = "Nuevo Cliente", Identification = "0" };
            _context.Customers.Add(newCust);
            _context.SaveChanges();
            LoadCustomers();
            SelectedCustomer = newCust;
            StatusMessage = "Cliente nuevo listo para editar.";
        }

        [RelayCommand]
        private void SaveChanges()
        {
            if (SelectedCustomer == null) return;

            var identification = SelectedCustomer.Identification.Trim();
            if (string.IsNullOrWhiteSpace(SelectedCustomer.FullName) || string.IsNullOrWhiteSpace(identification))
            {
                StatusMessage = "Nombre e identificacion son requeridos.";
                return;
            }

            var duplicate = _context.Customers.Any(c =>
                c.Id != SelectedCustomer.Id &&
                c.Identification == identification);

            if (duplicate)
            {
                StatusMessage = $"Ya existe un cliente con identificacion {identification}.";
                return;
            }

            SelectedCustomer.Identification = identification;
            _context.SaveChanges();
            LoadCustomers();
            SelectedCustomer = Customers.FirstOrDefault(c => c.Id == SelectedCustomer.Id);
            StatusMessage = "Cliente guardado.";
        }
    }
}
