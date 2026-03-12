using System;
using System.Threading.Tasks;

namespace WorkTicketApp.Services
{
    public class ModalService
    {
        public event Action<string, string>? OnShow;
        private TaskCompletionSource<bool> _tcs = new();

        public Task<bool> Show(string title, string message)
        {
            _tcs = new TaskCompletionSource<bool>();
            OnShow?.Invoke(title, message);
            return _tcs.Task;
        }

        public void SetResult(bool result)
        {
            _tcs?.SetResult(result);
        }
    }
}