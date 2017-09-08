﻿using System;
using System.IO;
using PeterKottas.DotNetCore.WindowsService.Base;
using PeterKottas.DotNetCore.WindowsService.Interfaces;
using Microsoft.Extensions.PlatformAbstractions;

namespace PeterKottas.DotNetCore.WindowsService.Example
{
    public class ExampleServiceTimer : MicroService, IMicroService
    {
        private IMicroServiceController controller;

        public ExampleServiceTimer() : this(null)
        {
        }

        public ExampleServiceTimer(IMicroServiceController controller)
        {
            this.controller = controller;
        }

        private string fileName = Path.Combine(PlatformServices.Default.Application.ApplicationBasePath, "log.txt");
        public void Start()
        {
            StartBase();
            Timers.Start("Poller", 1000, () =>
            {
                File.AppendAllText(fileName, string.Format("Polling at {0}\n", DateTime.Now.ToString("o")));
            });
            Console.WriteLine("I started");
            File.AppendAllText(fileName, "Started\n");
        }

        public void Stop()
        {
            StopBase();
            File.AppendAllText(fileName, "Stopped\n");
            Console.WriteLine("I stopped");
        }
    }
}
