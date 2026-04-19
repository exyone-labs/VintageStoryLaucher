using System;
using System.Linq;
using System.Reflection;
using Wpf.Ui.Controls;

Console.WriteLine("SnackbarPresenter members:");
foreach (var p in typeof(SnackbarPresenter).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
{
    Console.WriteLine($"- {p.Name} : {p.PropertyType.FullName}");
}
