using System;
using System.Windows.Input;

namespace FileEncrypt.ViewModels.Commands
{
    public abstract class BaseCommand : ICommand
    {
        #region ICommand Implementation

        event EventHandler ICommand.CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public abstract bool CanExecute(object parameter);
        public abstract void Execute(object parameter);

        #endregion
    }
}
