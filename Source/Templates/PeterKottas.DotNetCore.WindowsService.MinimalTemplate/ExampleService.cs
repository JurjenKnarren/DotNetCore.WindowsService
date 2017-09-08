using Microsoft.Extensions.PlatformAbstractions;
using PeterKottas.DotNetCore.WindowsService.Interfaces;
using System;
using System.IO;

namespace PeterKottas.DotNetCore.WindowsService.MinimalTemplate
{
    public class ExampleService : IMicroService
    {
        private IMicroServiceController _controller;

        public ExampleService() : this(null)
        {
        }

        public ExampleService(IMicroServiceController controller)
        {
            _controller = controller;
        }

        private string fileName = Path.Combine(PlatformServices.Default.Application.ApplicationBasePath, "log.txt");
        public void Start()
        {
            Console.WriteLine("I started");
            Console.WriteLine(fileName);
            File.AppendAllText(fileName, "Started\n");
            if (_controller != null)
            {
                _controller.Stop();
            }
        }

        public void Stop()
        {
            File.AppendAllText(fileName, "Stopped\n");
            Console.WriteLine("I stopped");
        }
    }
}
