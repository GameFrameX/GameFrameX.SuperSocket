using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GameFrameX.SuperSocket.Server.Host
{
    /// <summary>
    /// Provides a builder for configuring and building a SuperSocket web application.
    /// </summary>
    public class SuperSocketWebApplicationBuilder : IHostApplicationBuilder
    {
        private readonly IHostApplicationBuilder _hostApplicationBuilder;

        /// <summary>
        /// Gets the properties associated with the application builder.
        /// </summary>
        public IDictionary<object, object> Properties => _hostApplicationBuilder.Properties;

        /// <summary>
        /// Gets the configuration manager for the application.
        /// </summary>
        public IConfigurationManager Configuration => _hostApplicationBuilder.Configuration;

        /// <summary>
        /// Gets the hosting environment for the application.
        /// </summary>
        public IHostEnvironment Environment => _hostApplicationBuilder.Environment;

        /// <summary>
        /// Gets the logging builder for configuring logging services.
        /// </summary>
        public ILoggingBuilder Logging => _hostApplicationBuilder.Logging;

        /// <summary>
        /// Gets the metrics builder for configuring metrics services.
        /// </summary>
        public IMetricsBuilder Metrics => _hostApplicationBuilder.Metrics;

        /// <summary>
        /// Gets the service collection for the application.
        /// </summary>
        public IServiceCollection Services => _hostApplicationBuilder.Services;

        /// <summary>
        /// Initializes a new instance of the <see cref="SuperSocketWebApplicationBuilder"/> class.
        /// </summary>
        /// <param name="hostApplicationBuilder">The underlying host application builder.</param>
        internal SuperSocketWebApplicationBuilder(IHostApplicationBuilder hostApplicationBuilder)
        {
            _hostApplicationBuilder = hostApplicationBuilder;
        }

        /// <summary>
        /// Gets the host builder associated with the application.
        /// </summary>
        public IHostBuilder Host
        {
            get
            {
                return _hostApplicationBuilder.GetType()
                    .GetProperty("Host", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                    ?.GetValue(_hostApplicationBuilder) as IHostBuilder;
            }
        }

        /// <summary>
        /// Builds the host for the application.
        /// </summary>
        /// <returns>The built host.</returns>
        public IHost Build()
        {
            var host = _hostApplicationBuilder
                .GetType()
                .GetMethod(nameof(Build))
                ?.Invoke(_hostApplicationBuilder, Array.Empty<object>()) as IHost;

            host?.Services.GetService<MultipleServerHostBuilder>()?.AdaptMultipleServerHost(host.Services);

            return host;
        }

        /// <summary>
        /// Configures the container for the application.
        /// </summary>
        /// <typeparam name="TContainerBuilder">The type of the container builder.</typeparam>
        /// <param name="factory">The service provider factory to use.</param>
        /// <param name="configure">The action to configure the container.</param>
        void IHostApplicationBuilder.ConfigureContainer<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory, Action<TContainerBuilder> configure)
        {
            _hostApplicationBuilder.ConfigureContainer<TContainerBuilder>(factory, configure);
        }
    }
}