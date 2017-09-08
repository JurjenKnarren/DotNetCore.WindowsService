using PeterKottas.DotNetCore.WindowsService.Interfaces;
using System;
using System.IO;
using System.Diagnostics;
using Microsoft.Extensions.PlatformAbstractions;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace PeterKottas.DotNetCore.WindowsService.Example
{
    public class ExampleService : IMicroService
    {
        private readonly IMicroServiceController _controller;
        private readonly TraceSource _trace;
        private readonly bool _isConsoleContext;

        public ExampleService() : this(null, null)
        {
        }

        public ExampleService(IMicroServiceController controller, TraceSource trace, bool isConsoleContext = false)
        {
            _controller = controller;
            _trace = trace;
            _isConsoleContext = isConsoleContext;
        }

        public void Start()
        {
            _trace.TraceEvent(TraceEventType.Information, 1, $"Start");
            if (_isConsoleContext)
            {
                // Wait for the user to quit the program.
                Console.WriteLine("Press \'q\' to quit:");
                while (Console.Read() != 'q') ;
                Stop();
            }
        }

        public void Stop()
        {
            _trace.TraceEvent(TraceEventType.Information, 1, $"Stop");
        }
    }
}
