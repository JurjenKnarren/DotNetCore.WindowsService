using DasMulli.Win32.ServiceUtils;
using System;
using PeterKottas.DotNetCore.WindowsService.Enums;
using System.Diagnostics;
using System.ServiceProcess;
using PeterKottas.DotNetCore.CmdArgParser;
using System.Collections.Generic;
using System.Linq;
using PeterKottas.DotNetCore.WindowsService.Interfaces;
using Microsoft.Extensions.PlatformAbstractions;
using System.IO;
using System.Threading.Tasks;

namespace PeterKottas.DotNetCore.WindowsService
{
    public static class ServiceRunner<SERVICE> where SERVICE : IMicroService
    {
        static TraceSource _trace = new TraceSource(typeof(SERVICE).FullName, SourceLevels.All);

        public static int Run(Action<HostConfigurator<SERVICE>> runAction)
        {
            var innerConfiguration = new HostConfiguration<SERVICE>(_trace)
            {
                Action = ActionEnum.RunInteractive,
                Name = typeof(SERVICE).FullName
            };

            innerConfiguration.ExtraArguments = Parser.Parse(config =>
            {
                config.AddParameter(new CmdArgParam
                {
                    Key = "username",
                    Description = "Username for the service account",
                    Value = val =>
                    {
                        innerConfiguration.Username = val;
                    }
                });
                config.AddParameter(new CmdArgParam
                {
                    Key = "password",
                    Description = "Password for the service account",
                    Value = val =>
                    {
                        innerConfiguration.Password = val;
                    }
                });
                config.AddParameter(new CmdArgParam
                {
                    Key = "name",
                    Description = "Service name",
                    Value = val =>
                    {
                        innerConfiguration.Name = val;
                    }
                });
                config.AddParameter(new CmdArgParam
                {
                    Key = "description",
                    Description = "Service description",
                    Value = val =>
                    {
                        innerConfiguration.Description = val;
                    }
                });
                config.AddParameter(new CmdArgParam
                {
                    Key = "displayName",
                    Description = "Service display name",
                    Value = val =>
                    {
                        innerConfiguration.DisplayName = val;
                    }
                });
                config.AddParameter(new CmdArgParam
                {
                    Key = "action",
                    Description = "Installs the service. It's run like console application otherwise",
                    Value = val =>
                    {
                        switch (val)
                        {
                            case "install":
                                innerConfiguration.Action = ActionEnum.Install;
                                break;
                            case "start":
                                innerConfiguration.Action = ActionEnum.Start;
                                break;
                            case "stop":
                                innerConfiguration.Action = ActionEnum.Stop;
                                break;
                            case "uninstall":
                                innerConfiguration.Action = ActionEnum.Uninstall;
                                break;
                            case "run":
                                innerConfiguration.Action = ActionEnum.Run;
                                break;
                            case "run-interactive":
                                innerConfiguration.Action = ActionEnum.RunInteractive;
                                break;
                            default:
                                Console.WriteLine("{0} is unrecognized, will run the app as console application instead");
                                _trace.TraceEvent(TraceEventType.Warning, 2, "{0} is unrecognized, will run the app as console application instead");
                                innerConfiguration.Action = ActionEnum.RunInteractive;
                                break;
                        }
                    }
                });

                config.UseDefaultHelp();
                config.UseAppDescription("Sample microservice application");
            });

            if (string.IsNullOrEmpty(innerConfiguration.Name))
            {
                innerConfiguration.Name = typeof(SERVICE).FullName;
            }

            if (string.IsNullOrEmpty(innerConfiguration.DisplayName))
            {
                innerConfiguration.DisplayName = innerConfiguration.Name;
            }

            if (string.IsNullOrEmpty(innerConfiguration.Description))
            {
                innerConfiguration.Description = "No description";
            }

            var hostConfiguration = new HostConfigurator<SERVICE>(innerConfiguration);

            try
            {
                runAction?.Invoke(hostConfiguration);
                if (innerConfiguration.Action == ActionEnum.Run || innerConfiguration.Action == ActionEnum.RunInteractive)
                {
                    var controller = new MicroServiceController(
                        () =>
                        {
                            var task = Task.Factory.StartNew(() =>
                            {
                                UsingServiceController(innerConfiguration, (sc, cfg) => StopService(cfg, sc));
                            });
                            //task.Wait();
                        }
                    );
                    innerConfiguration.Service = innerConfiguration.ServiceFactory(innerConfiguration.ExtraArguments, controller);
                }
                ConfigureService(innerConfiguration);
                return 0;
            }
            catch (Exception e)
            {
                Error(innerConfiguration, e);
                return -1;
            }
        }

        private static string GetServiceCommand(List<string> extraArguments)
        {
            var host = Process.GetCurrentProcess().MainModule.FileName;
            if (host.EndsWith("dotnet.exe", StringComparison.OrdinalIgnoreCase))
            {
                var appPath = Path.Combine(PlatformServices.Default.Application.ApplicationBasePath,
                    PlatformServices.Default.Application.ApplicationName + ".dll");
                host = $"{host} \"{appPath}\"";
            }
            if (!host.EndsWith("dotnet.exe", StringComparison.OrdinalIgnoreCase))
            {
                //For self-contained apps, skip the dll path
                extraArguments = extraArguments.Skip(1).ToList();
            }

            return $"{host} {string.Join(" ", extraArguments)} {"action:run"}";
        }

        private static void Install(HostConfiguration<SERVICE> config, ServiceController sc, int counter = 0)
        {
            var cred = Win32ServiceCredentials.LocalSystem;
            if (!string.IsNullOrEmpty(config.Username))
            {
                cred = new Win32ServiceCredentials(config.Username, config.Password);
            }
            try
            {
                new Win32ServiceManager().CreateService(
                    config.Name,
                    config.DisplayName,
                    config.Description,
                    GetServiceCommand(config.ExtraArguments),
                    cred,
                    autoStart: true,
                    startImmediately: true,
                    errorSeverity: ErrorSeverity.Normal);
                _trace.TraceEvent(TraceEventType.Warning, 2, $@"Successfully registered and started service ""{config.Name}"" (""{config.Description}"")");
            }
            catch (Exception e)
            {
                if (e.Message.Contains("already exists"))
                {
                    _trace.TraceEvent(TraceEventType.Warning, 2, $@"Service ""{config.Name}"" (""{config.Description}"") was already installed. Reinstalling...");
                    Reinstall(config, sc);
                }
                else if (e.Message.Contains("The specified service has been marked for deletion"))
                {
                    if (counter < 10)
                    {
                        System.Threading.Thread.Sleep(500);
                        counter++;
                        var suffix = "th";
                        switch (counter)
                        {
                            case 1:
                                suffix = "st";
                                break;
                            case 2:
                                suffix = "nd";
                                break;
                            case 3:
                                suffix = "rd";
                                break;
                        }

                        _trace.TraceEvent(TraceEventType.Warning, 2, $"The specified service has been marked for deletion. Retrying {counter}{suffix} time");
                        Install(config, sc, counter);
                    }
                    else
                    {
                        throw;
                    }
                }
                else
                {
                    throw;
                }
            }
        }

        private static void Uninstall(HostConfiguration<SERVICE> config, ServiceController sc)
        {
            try
            {
                if (!(sc.Status == ServiceControllerStatus.Stopped || sc.Status == ServiceControllerStatus.StopPending))
                {
                    StopService(config, sc);
                }
                new Win32ServiceManager().DeleteService(config.Name);
                _trace.TraceEvent(TraceEventType.Warning, 2, $@"Successfully unregistered service ""{config.Name}"" (""{config.Description}"")");
            }
            catch (Exception e)
            {
                if (!e.Message.Contains("does not exist"))
                {
                    throw;
                }
                _trace.TraceEvent(TraceEventType.Warning, 2, $@"Service ""{config.Name}"" (""{config.Description}"") does not exist. No action taken.");
            }
        }

        private static void StopService(HostConfiguration<SERVICE> config, ServiceController sc)
        {
            if (!(sc.Status == ServiceControllerStatus.Stopped | sc.Status == ServiceControllerStatus.StopPending))
            {
                sc.Stop();
                sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromMilliseconds(1000));
                _trace.TraceEvent(TraceEventType.Warning, 2, $@"Successfully stopped service ""{config.Name}"" (""{config.Description}"")");
            }
            else
            {
                _trace.TraceEvent(TraceEventType.Warning, 2, $@"Service ""{config.Name}"" (""{config.Description}"") is already stopped or stop is pending.");
            }
        }

        private static void StartService(HostConfiguration<SERVICE> config, ServiceController sc)
        {
            if (!(sc.Status == ServiceControllerStatus.StartPending | sc.Status == ServiceControllerStatus.Running))
            {
                sc.Start();
                sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromMilliseconds(1000));
                _trace.TraceEvent(TraceEventType.Warning, 2, $@"Successfully started service ""{config.Name}"" (""{config.Description}"")");
            }
            else
            {
                _trace.TraceEvent(TraceEventType.Warning, 2, $@"Service ""{config.Name}"" (""{config.Description}"") is already running or start is pending.");
            }
        }

        private static void Reinstall(HostConfiguration<SERVICE> config, ServiceController sc)
        {
            StopService(config, sc);
            Uninstall(config, sc);
            Install(config, sc);
        }

        private static void ConfigureService(HostConfiguration<SERVICE> config)
        {
            switch (config.Action)
            {
                case ActionEnum.Install:
                    UsingServiceController(config, (sc, cfg) => Install(cfg, sc));
                    break;
                case ActionEnum.Uninstall:
                    UsingServiceController(config, (sc, cfg) => Uninstall(cfg, sc));
                    break;
                case ActionEnum.Run:
                    var testService = new InnerService(config.Name, () => Start(config), () => Stop(config));
                    var serviceHost = new Win32ServiceHost(testService);
                    serviceHost.Run();
                    break;
                case ActionEnum.RunInteractive:
                    Start(config);
                    break;
                case ActionEnum.Stop:
                    UsingServiceController(config, (sc, cfg) => StopService(cfg, sc));
                    break;
                case ActionEnum.Start:
                    UsingServiceController(config, (sc, cfg) => StartService(cfg, sc));
                    break;
            }
        }

        private static void UsingServiceController(HostConfiguration<SERVICE> config, Action<ServiceController, HostConfiguration<SERVICE>> action)
        {
            using (var sc = new ServiceController(config.Name))
            {
                action?.Invoke(sc, config);
            }
        }

        private static void Start(HostConfiguration<SERVICE> config)
        {
            try
            {
                config.OnServiceStart(config.Service, config.ExtraArguments);
            }
            catch (Exception e)
            {
                Error(config, e);
            }
        }

        private static void Stop(HostConfiguration<SERVICE> config)
        {
            try
            {
                config.OnServiceStop(config.Service);
            }
            catch (Exception e)
            {
                Error(config, e);
            }
        }

        private static void Error(HostConfiguration<SERVICE> config, Exception e = null)
        {
            config.OnServiceError(e);
        }
    }
}
