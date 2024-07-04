using GameFrameX.SuperSocket.Server.Abstractions.Session;

namespace GameFrameX.SuperSocket.Server
{
    class DefaultSessionFactory : ISessionFactory
    {
        public Type SessionType => typeof(AppSession);

        public IAppSession Create()
        {
            return new AppSession();
        }
    }
}