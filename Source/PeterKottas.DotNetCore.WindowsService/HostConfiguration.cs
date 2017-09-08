using System;
using System.Collections.Generic;
using PeterKottas.DotNetCore.WindowsService.Enums;
using PeterKottas.DotNetCore.WindowsService.Interfaces;
using System.Diagnostics;

namespace PeterKottas.DotNetCore.WindowsService
{
    public class HostConfiguration<SERVICE> where SERVICE : IMicroService
    {
        public HostConfiguration(TraceSource trace)
        {
            OnServiceStart = (service, arguments) => { trace.TraceEvent(TraceEventType.Information, 1, $"OnServiceStart service {this.Name}"); };
            OnServiceStop = service => { trace.TraceEvent(TraceEventType.Information, 1, $"OnServiceStop service {this.Name}"); };
            OnServiceError = e =>
            {
                trace.TraceEvent(TraceEventType.Error, 1, e.ToString());
            };
        }

        public ActionEnum Action { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string DisplayName { get; set; }

        public SERVICE Service { get; set; }

        public Func<List<string>, IMicroServiceController, SERVICE> ServiceFactory { get; set; }

        public Action<SERVICE, List<string>> OnServiceStart { get; set; }

        public Action<SERVICE> OnServiceStop { get; set; }

        public Action<Exception> OnServiceError { get; set; }

        public List<string> ExtraArguments { get; set; }
    }
}
