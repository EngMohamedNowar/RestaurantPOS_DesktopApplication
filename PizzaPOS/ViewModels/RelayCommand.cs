// ViewModels/RelayCommand.cs
using System;
using System.Windows.Input;
namespace PizzaPOS.ViewModels
{
    public class RelayCommand : ICommand
    {
        readonly Action<object?> _exec;
        readonly Func<object?, bool>? _can;
        public RelayCommand(Action<object?> exec, Func<object?, bool>? can = null) { _exec = exec; _can = can; }
        public event EventHandler? CanExecuteChanged { add => CommandManager.RequerySuggested += value; remove => CommandManager.RequerySuggested -= value; }
        public bool CanExecute(object? p) => _can?.Invoke(p) ?? true;
        public void Execute(object? p) => _exec(p);
    }
}

