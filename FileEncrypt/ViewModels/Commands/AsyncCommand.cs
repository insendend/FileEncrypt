using System;

namespace FileEncrypt.ViewModels.Commands
{
    public class AsyncCommand : BaseCommand
    {
        #region Fields

        private Action<object> execute;
        private AsyncCallback callback;
        private Predicate<object> canExecute;

        #endregion

        #region Ctor

        public AsyncCommand(Action<object> execute, AsyncCallback callback = null, Predicate<object> canExecute = null)
        {
            this.execute = execute ?? throw new ArgumentException(nameof(execute));
            this.callback = callback;
            this.canExecute = canExecute;
        }

        #endregion

        #region BaseCommand implementation

        public override bool CanExecute(object parameter)
        {
            return canExecute == null || canExecute(parameter);
        }

        public override void Execute(object parameter)
        {
            execute.BeginInvoke(parameter, callback, null);
        }

        #endregion
    }
}
