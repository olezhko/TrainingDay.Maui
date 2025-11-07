using System.Windows.Input;

namespace TrainingDay.Maui.Controls
{
    public class DoOnceCommand : ICommand
    {
        private readonly Func<Task>? _executeAsync;
        private readonly Func<bool>? _canExecute;
        private bool _isRunning;

        public DoOnceCommand(Func<Task> executeAsync, Func<bool>? canExecute = null)
        {
            _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter)
        {
            if (_isRunning)
                return false;

            return _canExecute?.Invoke() ?? true;
        }

        public async void Execute(object? parameter)
        {
            if (!CanExecute(parameter))
                return;

            _isRunning = true;
            RaiseCanExecuteChanged();

            try
            {
                if (_executeAsync is not null)
                    await _executeAsync();
                else
                    throw new Exception("Use async command");
            }
            finally
            {
                _isRunning = false;
                RaiseCanExecuteChanged();
            }
        }

        public void RaiseCanExecuteChanged() =>
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    public class DoOnceCommand<T> : ICommand
    {
        private readonly Func<T, Task>? _executeAsync;
        private readonly Func<T, bool>? _canExecute;
        private bool _isRunning;

        public DoOnceCommand(Func<T, Task> executeAsync, Func<T, bool>? canExecute = null)
        {
            _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter)
        {
            if (_isRunning)
                return false;

            if (_canExecute == null)
                return true;

            return parameter is T t && _canExecute(t);
        }

        public async void Execute(object? parameter)
        {
            if (!CanExecute(parameter))
                return;

            _isRunning = true;
            RaiseCanExecuteChanged();

            try
            {
                if (parameter is T t)
                {
                    if (_executeAsync is not null)
                        await _executeAsync(t);
                    else
                        throw new Exception("Use async command");
                }
            }
            finally
            {
                _isRunning = false;
                RaiseCanExecuteChanged();
            }
        }

        public void RaiseCanExecuteChanged() =>
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
