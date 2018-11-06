using System;
using System.Windows.Input;

namespace JqGridCodeGenerator.Commands
{
    class CustomCommand : ICommand
    {
        private readonly Action<object> _executeMethod;

        public event EventHandler CanExecuteChanged;

        public CustomCommand(Action<object> executeMethod)
        {
            _executeMethod = executeMethod;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            _executeMethod(parameter);
        }
    }
}
