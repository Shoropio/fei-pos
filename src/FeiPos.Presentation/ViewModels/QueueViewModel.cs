using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FeiPos.Domain.Entities;
using FeiPos.Infrastructure.Persistence;

namespace FeiPos.Presentation.ViewModels
{
    public partial class QueueViewModel : ObservableObject
    {
        private readonly AppDbContext _context;

        [ObservableProperty]
        private ObservableCollection<HaciendaResponse> _responses = new();

        public QueueViewModel(AppDbContext context)
        {
            _context = context;
            LoadData();
        }

        [RelayCommand]
        private void LoadData()
        {
            var list = _context.HaciendaResponses
                              .OrderByDescending(r => r.ReceivedAt)
                              .Take(100)
                              .ToList();
            Responses = new ObservableCollection<HaciendaResponse>(list);
        }
    }
}
