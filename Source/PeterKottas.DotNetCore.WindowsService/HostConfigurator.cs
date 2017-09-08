using System;
using PeterKottas.DotNetCore.WindowsService.Configurators.Service;
using PeterKottas.DotNetCore.WindowsService.Interfaces;

namespace PeterKottas.DotNetCore.WindowsService
{
    public class HostConfigurator<SERVICE> where SERVICE : IMicroService
    {
        public HostConfiguration<SERVICE> HostConfiguration { get; }
        
        public HostConfigurator(HostConfiguration<SERVICE> hostConfiguration)
        {
            HostConfiguration = hostConfiguration;
        }

        public void Service(Action<ServiceConfigurator<SERVICE>> serviceConfigAction)
        {
            if (serviceConfigAction == null)
                throw new ArgumentNullException(nameof(serviceConfigAction));

            try
            {
                var serviceConfig = new ServiceConfigurator<SERVICE>(HostConfiguration);
                serviceConfigAction(serviceConfig);
                if (HostConfiguration.ServiceFactory == null)
                    throw new ArgumentException("It's necessary to configure action that creates the service", nameof(HostConfiguration.ServiceFactory));
                if (HostConfiguration.OnServiceStart == null)
                    throw new ArgumentException("It's necessary to configure action that is called when the service starts", nameof(HostConfiguration.OnServiceStart));
            }
            catch (Exception e)
            {
                throw new ArgumentException("Configuring the service throws an exception", e);
            }
        }
    }
}
