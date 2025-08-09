// RelayCommand.cs
using System;
using System.Windows.Input;

namespace DeepBIM.ViewModels
{
    public class RelayCommand : ICommand
    {
        private readonly Action _executeWithoutParam;
        private readonly Action<object> _executeWithParam;
        private readonly Func<bool> _canExecuteWithoutParam;
        private readonly Predicate<object> _canExecuteWithParam;
        private readonly bool _useParameter;

        // Dùng cho lệnh không cần tham số: DuplicateCommand
        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _executeWithoutParam = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecuteWithoutParam = canExecute;
            _useParameter = false;
        }

        // Dùng cho lệnh có tham số: DeleteRowCommand
        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            _executeWithParam = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecuteWithParam = canExecute;
            _useParameter = true;
        }

        public bool CanExecute(object parameter)
        {
            return _useParameter
                ? _canExecuteWithParam?.Invoke(parameter) != false
                : _canExecuteWithoutParam?.Invoke() != false;
        }

        public void Execute(object parameter)
        {
            if (_useParameter)
                _executeWithParam(parameter);
            else
                _executeWithoutParam();
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}