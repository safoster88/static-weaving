using System;
using System.Windows.Input;

namespace StaticWeaving
{
    public class Command : ICommand
    {
        public event EventHandler CanExecuteChanged;

        private readonly Action _doThings;

        public Command(Action doThings)
        {
            _doThings = doThings;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            _doThings();
        }
    }
}
