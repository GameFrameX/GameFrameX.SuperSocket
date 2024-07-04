using System;
using Microsoft.Extensions.Logging;
using GameFrameX.SuperSocket.ProtoBase;
using System.Threading.Tasks;

namespace GameFrameX.SuperSocket.Server.Abstractions.Connections
{
    public interface IConnectionRegister
    {
        Task RegisterConnection(object connection);
    }
}