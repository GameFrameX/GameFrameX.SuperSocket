using GameFrameX.SuperSocket.Server.Abstractions.Session;
using Microsoft.Extensions.DependencyInjection;

namespace GameFrameX.SuperSocket.Server
{
    public class GenericSessionFactory<TSession> : ISessionFactory
        where TSession : IAppSession
    {
        public Type SessionType => typeof(TSession);

        public IServiceProvider ServiceProvider { get; private set; }

        public GenericSessionFactory(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }

        public IAppSession Create()
        {
            return ActivatorUtilities.CreateInstance<TSession>(ServiceProvider);
        }
    }
}