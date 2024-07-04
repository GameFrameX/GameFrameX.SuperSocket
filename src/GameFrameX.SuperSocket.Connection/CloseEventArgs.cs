namespace GameFrameX.SuperSocket.Connection;

public class CloseEventArgs : EventArgs
{
    public CloseReason Reason { get; private set; }

    public CloseEventArgs(CloseReason reason)
    {
        Reason = reason;
    }
}