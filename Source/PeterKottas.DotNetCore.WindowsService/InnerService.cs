using DasMulli.Win32.ServiceUtils;
using System;

namespace PeterKottas.DotNetCore.WindowsService
{
    public class InnerService : IWin32Service
    {
        readonly string _serviceName;
        readonly Action _onStart;
        readonly Action _onStopped;

        public InnerService(string serviceName, Action onStart, Action onStopped)
        {
            _serviceName = serviceName;
            _onStart = onStart;
            _onStopped = onStopped;
        }

        public string ServiceName => _serviceName;

        public void Start(string[] startupArguments, ServiceStoppedCallback serviceStoppedCallback)
        {
            try
            {
                _onStart?.Invoke();
            }
            catch (Exception)
            {
                _onStopped?.Invoke();
                serviceStoppedCallback?.Invoke();
            }
        }

        public void Stop() => _onStopped?.Invoke();
    }
}
