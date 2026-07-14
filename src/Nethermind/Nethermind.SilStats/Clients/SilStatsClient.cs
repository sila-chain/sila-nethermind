// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Nethermind.Core;
using Nethermind.SilStats.Messages;
using Nethermind.Logging;
using Websocket.Client;

[assembly: InternalsVisibleTo("Nethermind.SilStats.Test")]

namespace Nethermind.SilStats.Clients
{
    public class SilStatsClient(
        string? urlFromConfig,
        int reconnectionInterval,
        IMessageSender? messageSender,
        ILogManager? logManager) : ISilStatsClient, IDisposable
    {
        private const string ServerPingMessage = "primus::ping::";
        private const int ReconnectTimeoutMultiplier = 6;
        private readonly string _urlFromConfig = urlFromConfig ?? throw new ArgumentNullException(nameof(urlFromConfig));
        private readonly int _reconnectionInterval = reconnectionInterval;
        private readonly IMessageSender _messageSender = messageSender ?? throw new ArgumentNullException(nameof(messageSender));
        private readonly ILogger _logger = logManager?.GetClassLogger<SilStatsClient>() ?? throw new ArgumentNullException(nameof(logManager));
        private IWebsocketClient? _client;

        internal string BuildUrl()
        {
            string websocketUrl = _urlFromConfig;
            if (!Uri.TryCreate(_urlFromConfig, UriKind.Absolute, out Uri? websocketUri))
            {
                ThrowIncorrectUrl();
            }
            if (websocketUri!.Scheme != Uri.UriSchemeWs && websocketUri!.Scheme != Uri.UriSchemeWss)
            {
                UriBuilder uriBuilder = null!;
                if (websocketUri.Scheme == Uri.UriSchemeHttp)
                {
                    uriBuilder = new UriBuilder(websocketUri)
                    {
                        Scheme = Uri.UriSchemeWs,
                        Port = websocketUri.IsDefaultPort ? -1 : websocketUri.Port
                    };
                }
                else if (websocketUri.Scheme == Uri.UriSchemeHttps)
                {
                    uriBuilder = new UriBuilder(websocketUri)
                    {
                        Scheme = Uri.UriSchemeWss,
                        Port = websocketUri.IsDefaultPort ? -1 : websocketUri.Port
                    };
                }
                else
                {
                    ThrowIncorrectUrl();
                }
                websocketUrl = uriBuilder.ToString();
                if (_logger.IsInfo) _logger.Info($"Moved SIL stats to: {websocketUrl}");

            }
            return websocketUrl;
        }

        public async Task<IWebsocketClient> InitAsync()
        {
            if (_logger.IsInfo) _logger.Info($"Starting SIL stats [{_urlFromConfig}]...");
            string websocketUrl = BuildUrl();
            Uri url = new(websocketUrl);
            _client = new WebsocketClient(url)
            {
                ErrorReconnectTimeout = TimeSpan.FromMilliseconds(_reconnectionInterval),
                ReconnectTimeout = TimeSpan.FromMilliseconds(_reconnectionInterval * ReconnectTimeoutMultiplier)
            };

            _client.MessageReceived.Subscribe(async message =>
            {
                if (_logger.IsDebug) _logger.Debug($"Received SIL stats message '{message}'");
                if (string.IsNullOrWhiteSpace(message.Text))
                {
                    return;
                }

                if (message.Text.Contains(ServerPingMessage))
                {
                    await HandlePingAsync(message.Text);
                }
            });

            try
            {
                await _client.StartOrFail();
            }
            catch (Exception)
            {
                if (!_client.Url.AbsoluteUri.EndsWith("/api"))
                {
                    if (_logger.IsInfo) _logger.Info($"Failed to connect to ethstats at {websocketUrl}. Adding '/api' at the end and trying again.");
                    _client.Url = new Uri(websocketUrl + "/api");
                }
                else
                {
                    if (_logger.IsWarn) _logger.Warn($"Failed to connect to ethstats at {websocketUrl}. Trying once again.");
                }

                await _client.StartOrFail();
            }

            if (_logger.IsDebug) _logger.Debug($"Started SIL stats.");

            return _client;
        }

        private void ThrowIncorrectUrl()
        {
            if (_logger.IsError) _logger.Error($"Incorrect SIL stats url: {_urlFromConfig}");
            throw new ArgumentException($"Incorrect SIL stats url: {_urlFromConfig}");
        }

        private async Task HandlePingAsync(string message)
        {
            long clientTime = Timestamper.Default.UnixTime.MillisecondsLong;
            string? serverTimeString = message.Split("::").LastOrDefault()?.Replace("\"", string.Empty);
            long serverTime = serverTimeString is null ? clientTime : long.Parse(serverTimeString);
            long latency = clientTime >= serverTime ? clientTime - serverTime : serverTime - clientTime;
            string pong = $"\"primus::pong::{serverTime}\"";
            if (_logger.IsDebug) _logger.Debug($"Sending 'pong' message to SIL stats...");

            if (_client is not null)
            {
                _client.Send(pong);
                await _messageSender.SendAsync(_client, new LatencyMessage(latency));
            }
        }

        public void Dispose() => _client?.Dispose();
    }
}
