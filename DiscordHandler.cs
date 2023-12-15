using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Smguy;

namespace Dboy
{
    public class DiscordHandler
    {
        private readonly DiscordSocketClient _discordClient;
        private readonly string _token;
        private readonly ulong _guildId;
        private readonly ulong _channelId;
        private readonly Dictionary<string, ulong> _phoneNumberToUserId;
        private readonly SmsHandler _smsHandler;
        private bool _connected = false;
        private readonly Dictionary<string, List<string>> _allowedUsers;
        private readonly Dictionary<ulong, SmsInteractionState> _interactionStates;
        private static SuperSmsHandler _superSmsHandler;

        public DiscordHandler(string token, ulong guildId, ulong channelId,
                      Dictionary<string, ulong> phoneNumberToUserId,
                      SmsHandler smsHandler,
                      Dictionary<string, List<string>> allowedUsers,
                      Dboy.PKeys keys)

        {
            _token = token;
            _guildId = guildId;
            _channelId = channelId;
            _phoneNumberToUserId = phoneNumberToUserId;
            _smsHandler = smsHandler;
            

            _discordClient = new DiscordSocketClient(new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.DirectMessages
            });

            _discordClient.Log += LogAsync;
            _discordClient.Ready += OnClientReady;

            _allowedUsers = allowedUsers;
            _interactionStates = new Dictionary<ulong, SmsInteractionState>();
            _superSmsHandler = new SuperSmsHandler(
                _discordClient, _smsHandler, phoneNumberToUserId, allowedUsers, guildId, this, keys); // Pass the 'keys' parameter here
        }
    

        public string? FindPhoneNumberByUserId(ulong userId)
        {
            foreach (var pair in _phoneNumberToUserId)
            {
                if (pair.Value == userId)
                {
                    return pair.Key;
                }
            }
            return null;
        }

        private Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }

        public async Task InitializeAsync()
        {
            await _discordClient.LoginAsync(TokenType.Bot, _token);
            await _discordClient.StartAsync();
        }

        private Task OnClientReady()
        {
            _connected = true;
            //RegisterSlashCommands();
            return Task.CompletedTask;
        }

        private string ExtractPhoneNumberFromReply(SocketMessage message)
        {
            if (message.Content.StartsWith("From: "))
            {
                var parts = message.Content.Split('\n');
                foreach (var part in parts)
                {
                    if (part.StartsWith("From: "))
                    {
                        var phoneNumber = part.Substring("From: ".Length).Trim();
                        return phoneNumber.Length == 10 ? "1" + phoneNumber : phoneNumber;
                    }
                }
            }
            return string.Empty;
        }

        public async Task SendMessageAsync(string message, string toPhoneNumber)
        {
            if (!_connected) await WaitForConnectionAsync();

            var guild = _discordClient.GetGuild(_guildId);
            var channel = guild?.GetTextChannel(_channelId);

            if (channel != null && _phoneNumberToUserId.TryGetValue(toPhoneNumber, out var userId))
            {
                var userMention = $"<@{userId}>"; // Mention format
                var embed = new EmbedBuilder()
                    .WithDescription(message)
                    .WithColor(Color.Blue)
                    .WithCurrentTimestamp()
                    .Build();

                await channel.SendMessageAsync(userMention, embed: embed);
            }
        }

        private async Task WaitForConnectionAsync()
        {
            while (!_connected)
            {
                await Task.Delay(1000);
            }
        }
    }
}
