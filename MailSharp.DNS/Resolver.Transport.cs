using System.Net;
using System.Net.Sockets;

namespace MailSharp.DNS;

public partial class Resolver
{
	private async Task<Response> GetResponseAsync(IPEndPoint server, Request request)
	{
		request.Header.ID = (ushort)Interlocked.Increment(ref uniqueId);
		request.Header.RD = recursion;

		return transportType switch
		{
			TransportType.Udp => await UdpRequestAsync(server, request),
			TransportType.Tcp => await TcpRequestAsync(server, request),
			_ => new Response { ErrorMessage = "Unknown TransportType" }
		};
	}

	private async Task<Response> UdpRequestAsync(IPEndPoint server, Request request)
	{
		var buffer = new byte[512];

		for (int attempt = 0; attempt < retries; attempt++)
		{
			using var socket = new Socket(server.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
			socket.ReceiveTimeout = timeoutSeconds * 1000;

			try
			{
				await socket.SendToAsync(request.Data.ToArray(), SocketFlags.None, server);
				var received = await socket.ReceiveAsync(buffer, SocketFlags.None);
				var data = buffer[..received];
				var response = new Response(data);
				AddToCache(response);
				return response;
			}
			catch (SocketException) { /* next server/attempt */ }

		}

		return new Response { ErrorMessage = "Timeout Error" };
	}

	private async Task<Response> TcpRequestAsync(IPEndPoint server, Request request)
	{
		Memory<byte> data = request.Data;

		for (int attempt = 0; attempt < retries; attempt++)
		{
			using var client = new TcpClient(server.AddressFamily);
			client.ReceiveTimeout = timeoutSeconds * 1000;
			client.SendTimeout = timeoutSeconds * 1000;

			try
			{
				await client.ConnectAsync(server.Address, server.Port);
				await using var stream = client.GetStream();

				// 2-byte length prefix (big-endian) – zonder WriteByteAsync
				byte[] lengthPrefix = [(byte)(data.Length >> 8), (byte)(data.Length & 0xFF)];
				await stream.WriteAsync(lengthPrefix);
				await stream.WriteAsync(data.ToArray());
				await stream.FlushAsync();

				// lees 2-byte length prefix
				byte[] lengthBuffer = new byte[2];
				await stream.ReadExactlyAsync(lengthBuffer);
				int length = (lengthBuffer[0] << 8) | lengthBuffer[1];

				// lees het echte DNS-bericht
				byte[] responseData = new byte[length];
				await stream.ReadExactlyAsync(responseData);

				Response response = new(responseData);
				AddToCache(response);
				return response;
			}
			catch (Exception)
			{
				// volgende server / poging
			}
		}

		return new Response { ErrorMessage = "Timeout Error" };
	}

}