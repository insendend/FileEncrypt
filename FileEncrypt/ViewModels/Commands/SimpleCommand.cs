using System;

namespace FileEncrypt.ViewModels.Commands
{
    public class SimpleCommand : BaseCommand
    {
        #region Fields

        private Action execute;
        private Predicate<object> predicate;

        #endregion

        #region Ctor

        public SimpleCommand(Action execute, Predicate<object> predicate = null)
        {
            this.execute = execute ?? throw new ArgumentException(nameof(execute));
            this.predicate = predicate;
        }

        #endregion

        #region BaseCommand implementation

        public override bool CanExecute(object parameter)
        {
            return predicate?.Invoke(parameter) ?? true;
        }

        public override void Execute(object parameter)
        {
            execute();
        }

        #endregion
    }
}
