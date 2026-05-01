using System;
using System.Windows;
using FeiPos.Presentation;

namespace FeiPos.Presentation
{
    public class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            var app = new App();
            app.InitializeComponent();
            app.Run();
        }
    }
}
