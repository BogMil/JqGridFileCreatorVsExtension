using JqGridCodeGenerator.View.Pages;
using System;
using System.Windows.Input;

namespace JqGridCodeGenerator.Commands
{
    class GoToPreviousPageCommand : ICommand
    {
        public JqGridCodeGeneratorWindow _baseWindowInstance;
        public event EventHandler CanExecuteChanged;

        public GoToPreviousPageCommand(JqGridCodeGeneratorWindow baseWindowInstance)
        {
            _baseWindowInstance = baseWindowInstance;
        }


        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            var frame = _baseWindowInstance.PageFrame;
            var currentPageType = frame.Content.GetType();

            var currentPage = (IBasePage)Activator.CreateInstance(currentPageType);

            currentPage.GoToPreviousPage(_baseWindowInstance);
        }
    }
}
