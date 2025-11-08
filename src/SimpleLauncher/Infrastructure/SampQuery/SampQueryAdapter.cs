using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.Logging;
using SimpleLauncher.Domain.Abstractions;
using SimpleLauncher.Domain.Models;
using SimpleLauncher.Infrastructure.SampQuery.Models;
using SimpleLauncher.Infrastructure.SampQuery.Mappers;
using System.Diagnostics;
/* This code is based on SAMonitor GitHub project's samp query.
 * Author: markski1
 * See original here: https://github.com/markski1/SAMonitor/blob/main/API/Utils/SampQuery.cs
 */
namespace SimpleLauncher.Infrastructure.SampQuery
{
    public class SampQueryAdapter : ISampQueryAdapter
    {
        private readonly ILogger<SampQueryAdapter> _logger;
        private const ushort DefaultServerPort = 7777;
        private const int ReceiveArraySize = 2048;
        private const int TimeoutMilliseconds = 1000;

        public SampQueryAdapter(ILogger<SampQueryAdapter> logger)
        {
            _logger = logger;
        }

        private static IPAddress GetServerIpAddress(string ip)
            => GetServerIpAddress(ip.Split(':')[0], GetPortFromStringOrDefault(ip));
        private static IPAddress GetServerIpAddress(string host, ushort port)
        {
            IPAddress serverIp;
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            // if the given 'host' cannot be parsed as an IP Address, it might be a domain/hostname.
            if (!IPAddress.TryParse(host, out var getAddr))
                serverIp = Dns.GetHostEntry(host).AddressList.First(a => a.AddressFamily == AddressFamily.InterNetwork);
            else
                serverIp = getAddr;
            return serverIp;
        }
        private static IPEndPoint GetServerEndpoint(IPAddress ip, ushort port)
            => new IPEndPoint(ip, port);
        private static IPEndPoint GetServerEndpoint(string ip, ushort port)
            => GetServerEndpoint(GetServerIpAddress(ip), port);
        private static char[] GenerateSocketHeader()
            => "SAMP".ToCharArray();

        private static ushort GetPortFromStringOrDefault(string ip)
        {
            var parts = ip.Split(':');
            return parts.Length > 1 ? 
                string.IsNullOrWhiteSpace(parts[1]) ? 
                DefaultServerPort : ushort.Parse(parts[1]) : DefaultServerPort;
        }

        private async Task<byte[]> SendSocketToServerAsync(char packetType, IPEndPoint serverEndPoint)
        {
            const int SocketTimeoutException = 10060;

            ushort port;
            if (!ushort.TryParse(serverEndPoint.Port.ToString(), out port)) // Be aware of parsing port correctly!
            {
                _logger.LogError("Wrong server port in address: {ENDPORT}",
                    serverEndPoint.Port.ToString());
                throw new ArgumentException("Wrong port value");
            }
            using var serverSocket = new Socket(serverEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

            using var stream = new MemoryStream();
            await using var writer = new BinaryWriter(stream);
            _logger.LogTrace("Endpoint address: {ENDPOINT}:{PORT}",
                serverEndPoint.Address.ToString(),
                serverEndPoint.Port.ToString());

            var serverIpString = serverEndPoint.Address.ToString();
            string[] splitIp = serverIpString.Split('.');

            writer.Write(GenerateSocketHeader());

            for (sbyte i = 0; i < splitIp.Length; i++)
            {
                writer.Write(Convert.ToByte(Convert.ToInt16(splitIp[i])));
            }
            
            writer.Write(port);
            writer.Write(packetType);

            await serverSocket.SendToAsync(stream.ToArray(), SocketFlags.None, serverEndPoint);
            EndPoint rawPoint = serverEndPoint;
            var data = new byte[ReceiveArraySize];

            var task = serverSocket.ReceiveFromAsync(data, SocketFlags.None, rawPoint);

            if (await Task.WhenAny(task, Task.Delay(TimeoutMilliseconds)) != task)
            {
                serverSocket.Close();
                throw new SocketException(SocketTimeoutException); // Operation timed out
            }
            serverSocket.Close();
            return data;
        }

        private void SendSocketToServer(char packetType, IPEndPoint serverEndPoint)
        {
            var serverSocket = new Socket(serverEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp)
            {
                SendTimeout = TimeoutMilliseconds,
                ReceiveTimeout = TimeoutMilliseconds
            };

            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            string[] splitIp = serverEndPoint.Address.ToString().Split('.'); // ??? idk will it work

            writer.Write(GenerateSocketHeader());

            for (sbyte i = 0; i < splitIp.Length; i++)
            {
                writer.Write(Convert.ToByte(Convert.ToInt16(splitIp[i])));
            }

            writer.Write(serverEndPoint.Port);
            writer.Write(packetType);

            serverSocket.SendTo(stream.ToArray(), SocketFlags.None, serverEndPoint);

            EndPoint rawPoint = serverEndPoint;
            var szReceive = new byte[ReceiveArraySize];
            serverSocket.ReceiveFrom(szReceive, SocketFlags.None, ref rawPoint);
            serverSocket.Close();
        }

        /// <summary>
        /// Get server players. Attempts either player list or client list.
        /// </summary>
        /// <returns>An asynchronous task that completes with the collection of ServerPlayer instances</returns>
        /// <exception cref="SocketException">Thrown when operation timed out</exception>
        private async Task<List<QueryServerPlayer>> GetServerPlayersAsync(IPEndPoint endpoint,
            CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                throw new OperationCanceledException();
            byte[] data;
            try
            {
                data = await SendSocketToServerAsync('d', endpoint);
                return CollectServerPlayersInfoFromByteArray(data, 'd');
            }
            catch
            {
                data = await SendSocketToServerAsync('c', endpoint);
                return CollectServerPlayersInfoFromByteArray(data, 'c');
            }
        }
        public async Task<List<PlayerMeta>> GetServerPlayersAsync(string ipAddress, 
            ushort port, 
            CancellationToken cancellationToken)
            => (await GetServerPlayersAsync(GetServerEndpoint(ipAddress, port), 
                cancellationToken))
                .Select(player => player.ToApplicationModel())
                .ToList();
        public async Task<List<PlayerMeta>> GetServerPlayersAsync(string ipAddressAndPort,
            CancellationToken cancellationToken)
            => await GetServerPlayersAsync(ipAddressAndPort.Split(':')[0], 
                GetPortFromStringOrDefault(ipAddressAndPort), 
                cancellationToken);

        private async Task<QueryServerInfo> GetServerInfoAsync(IPEndPoint endpoint, 
            CancellationToken? cancellationToken)
        {
            cancellationToken?.ThrowIfCancellationRequested();
            byte[] data = await SendSocketToServerAsync('i', endpoint);
            cancellationToken?.ThrowIfCancellationRequested();
            return CollectServerInfoFromByteArray(data, Stopwatch.StartNew());
        }
        /// <summary>
        /// Get information about server
        /// </summary>
        /// <returns>An asynchronous task that completes with an instance of ServerPlayer</returns>
        /// <exception cref="SocketException">Thrown when operation timed out</exception>
        public async Task<ServerMeta> GetServerInfoAsync(string ipAddress, 
            ushort port,
            CancellationToken cancellationToken)
            => (await GetServerInfoAsync(GetServerEndpoint(ipAddress, port), 
                cancellationToken))
            .ToApplicationModel(ipAddress, port);
        public async Task<ServerMeta> GetServerInfoAsync(string ipAddressAndPort, 
            CancellationToken cancellationToken)
            => await GetServerInfoAsync(ipAddressAndPort.Split(':')[0], 
                GetPortFromStringOrDefault(ipAddressAndPort),
                cancellationToken);

        /// <summary>
        /// Get whether the server software is open.mp or not
        /// </summary>
        /// <returns>An asynchronous task that completes with an instance of Bool</returns>
        public bool GetServerIsOmp(string ipAddress, ushort port, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                SendSocketToServer('o', GetServerEndpoint(ipAddress, port));
                return true;
            }
            catch
            {
                // a timeout means the server is not open.mp
                return false;
            }
        }

        /// <summary>
        /// Get server rules
        /// </summary>
        /// <returns>An asynchronous task that completes with an instance of ServerRules</returns>
        /// <exception cref="SocketException">Thrown when operation timed out</exception>
        public async Task<ServerMeta> GetServerRulesAsync(string ipAddress, 
            ushort port, 
            CancellationToken cancellationToken)
            => (await GetServerRulesAsync(GetServerEndpoint(ipAddress, port), 
                cancellationToken))
            .ToApplicationModel();
        private async Task<QueryServerRules> GetServerRulesAsync(IPEndPoint endpoint, 
            CancellationToken? cancellationToken)
        {
            cancellationToken?.ThrowIfCancellationRequested();
            byte[] data = await SendSocketToServerAsync('r', endpoint);
            cancellationToken?.ThrowIfCancellationRequested();
            return CollectServerRulesFromByteArray(data);
        }

        private static List<QueryServerPlayer> CollectServerPlayersInfoFromByteArray(byte[] data, char packetType)
        {
            List<QueryServerPlayer> returnData = [];

            using MemoryStream stream = new(data);
            using BinaryReader read = new(stream);
            read.ReadBytes(10);
            read.ReadChar();

            for (int i = 0, iTotalPlayers = read.ReadInt16(); i < iTotalPlayers; i++)
            {
                if (packetType == 'd') // if the packet type is 'd', we got a full player list.
                {
                    returnData.Add(new QueryServerPlayer
                    {
                        PlayerId = Convert.ToByte(read.ReadByte()),
                        PlayerName = new string(read.ReadChars(read.ReadByte())),
                        PlayerScore = read.ReadInt32(),
                        PlayerPing = read.ReadInt32()
                    });
                }
                else // Otherwise we got a 'client' list, which might be incomplete, as per https://open.mp/docs/tutorials/QueryMechanism
                {
                    returnData.Add(new QueryServerPlayer
                    {
                        PlayerId = 0,
                        PlayerName = new string(read.ReadChars(read.ReadByte())),
                        PlayerScore = read.ReadInt32(),
                        PlayerPing = 0
                    });
                }
            }

            return returnData;
        }

        public async Task<ServerMeta> GetFullServerInfoAsync(string ipAddress, 
            ushort port, 
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var endpoint = GetServerEndpoint(ipAddress, port);
            var serverInfoMain = await GetServerInfoAsync(endpoint, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            var serverInfoRules = await GetServerRulesAsync(endpoint, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            var serverPlayers = (await GetServerPlayersAsync(endpoint, cancellationToken))
                .Select(player => player.PlayerName)
                .ToList();
            cancellationToken.ThrowIfCancellationRequested();
            return serverInfoMain.ToApplicationModel(ipAddress, port, serverInfoRules, serverPlayers);
        }

        private QueryServerInfo CollectServerInfoFromByteArray(byte[] data, Stopwatch transmitMs)
        {
            _logger.LogTrace("Collecting server info from byte array...");
            using MemoryStream stream = new(data);
            using BinaryReader read = new(stream, Encoding.GetEncoding(1251));
            read.ReadBytes(10);
            read.ReadChar();
            transmitMs.Stop();
            int ping;
            if (!int.TryParse(transmitMs.ElapsedMilliseconds.ToString(), out ping))
            {
                _logger.LogWarning("Cannot calculate server ping");
            }
            _logger.LogTrace("Server info byte array collected successfully.");

            return new QueryServerInfo
            {
                Password = Convert.ToBoolean(read.ReadByte()),
                Players = read.ReadUInt16(),
                MaxPlayers = read.ReadUInt16(),

                HostName = new string(read.ReadChars(read.ReadInt32())),
                GameMode = new string(read.ReadChars(read.ReadInt32())),
                Language = new string(read.ReadChars(read.ReadInt32())),

                ServerPing = ping,
            };
        }

        private static QueryServerRules CollectServerRulesFromByteArray(byte[] data)
        {
            var sampServerRulesData = new QueryServerRules();

            using MemoryStream stream = new(data);
            using BinaryReader read = new(stream, Encoding.GetEncoding(1251));
            read.ReadBytes(10);
            read.ReadChar();

            for (int i = 0, iRules = read.ReadInt16(); i < iRules; i++)
            {
                PropertyInfo? property = sampServerRulesData.GetType()
                    .GetProperty(new string(read.ReadChars(read.ReadByte()))
                    .Replace(' ', '_'), 
                    BindingFlags.IgnoreCase | 
                    BindingFlags.Public | 
                    BindingFlags.Instance);
                var value = new string(read.ReadChars(read.ReadByte()));

                if (property == null) continue;

                object val;
                if (property.PropertyType == typeof(bool)) val = value == "On";
                else if (property.PropertyType == typeof(Uri)) val = SqHelpers.ParseWebUrl(value);
                else if (property.PropertyType == typeof(DateTime)) val = SqHelpers.ParseTime(value);
                else val = SqHelpers.TryParseByte(value, property);

                property.SetValue(sampServerRulesData, val);
            }
            return sampServerRulesData;
        }
    }


    internal static class SqHelpers
    {
        public static Uri ParseWebUrl(string value)
        {
            if (Uri.TryCreate(value, UriKind.RelativeOrAbsolute, out var parsedUri)) return parsedUri;
            return Uri.TryCreate(value, UriKind.Absolute, out parsedUri) ? parsedUri : new Uri("https://sa-mp.mp/", UriKind.Absolute);
        }

        public static DateTime ParseTime(string value)
        {
            if (!TimeSpan.TryParse(value, new CultureInfo("en-US"), out var parsedTime)) parsedTime = TimeSpan.FromHours(0);
            return DateTime.Today.Add(parsedTime);
        }

        public static object TryParseByte(string value, PropertyInfo property)
        {
            try
            {
                return Convert.ChangeType(value, property.PropertyType, CultureInfo.InvariantCulture);
            }
            catch
            {
                // the value could not be parsed, try to return anything at all instead of crashing.
                return Convert.ChangeType("0", property.PropertyType, CultureInfo.InvariantCulture);
            }
        }
    }
}
