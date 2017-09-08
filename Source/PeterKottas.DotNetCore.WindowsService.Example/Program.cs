using Microsoft.Extensions.PlatformAbstractions;
using System;
using System.Diagnostics;
using System.IO;

namespace PeterKottas.DotNetCore.WindowsService.Example
{
    public class Program
    {
        public static void Main(string[] args)
        {
            ServiceRunner<ExampleService>.Run(config =>
            {
                const string svcName = nameof(ExampleService);
                config.HostConfiguration.Name = svcName;
                var trace = new TraceSource(typeof(ExampleService).FullName, SourceLevels.All);
                config.Service(serviceConfig =>
                {
                    serviceConfig.ServiceFactory((extraArguments, controller) =>
                    {
                        return new ExampleService(controller, trace, config.HostConfiguration.Action == Enums.ActionEnum.RunInteractive);
                    });

                    serviceConfig.OnStart((service, extraParams) =>
                    {
                        trace.TraceEvent(TraceEventType.Information, 1, $"Service {svcName} started");
                        service.Start();
                    });

                    serviceConfig.OnStop(service =>
                    {
                        trace.TraceEvent(TraceEventType.Information, 2, $"Service {svcName} stopped");
                        service.Stop();
                    });

                    serviceConfig.OnError(e =>
                    {
                        trace.TraceEvent(TraceEventType.Error, 3, $"Service {svcName} errored with exception:\n{e.ToString()}");
                    });
                });
            });
        }
    }
}
