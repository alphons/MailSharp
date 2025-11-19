using System.Net.Sockets;

namespace MailSharp.Common;

public sealed record ServerContext(TcpListener Listener, SecurityEnum Security);

