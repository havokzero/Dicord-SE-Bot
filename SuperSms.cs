using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Newtonsoft.Json;
using Smguy;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Dboy
{
    public class SuperSmsHandler
    {
        private readonly DiscordSocketClient _discordClient;
        private readonly SmsHandler _smsHandler;
        private readonly Dictionary<string, ulong> _phoneNumberToUserId;
        private readonly Dictionary<string, List<string>> _allowedUsers;
        private readonly ulong _guildId;
        private readonly DiscordHandler _discordHandler;

        public SuperSmsHandler(DiscordSocketClient discordClient, SmsHandler smsHandler,
            Dictionary<string, ulong> phoneNumberToUserId, Dictionary<string, List<string>> allowedUsers,
            ulong guildId, DiscordHandler discordHandler)
        {
            _discordClient = discordClient;
            _smsHandler = smsHandler;
            _phoneNumberToUserId = phoneNumberToUserId;
            _allowedUsers = allowedUsers;
            _guildId = guildId;
            _discordHandler = discordHandler;

            _discordClient.SlashCommandExecuted += OnSlashCommandExecutedAsync;
            RegisterSlashCommands();
        }

        private void RegisterSlashCommands()
        {
            var guild = _discordClient.GetGuild(_guildId);
            if (guild != null)
            {
                var commands = new List<SlashCommandBuilder>
                {
                    new SlashCommandBuilder()
                        .WithName("help")
                        .WithDescription("Shows help information"),

                    new SlashCommandBuilder()
                        .WithName("sms")
                        .WithDescription("Sends an SMS message")
                        .AddOption("phonenumber", ApplicationCommandOptionType.String, "Phone number to send the SMS", isRequired: true)
                        .AddOption("message", ApplicationCommandOptionType.String, "The message to send", isRequired: true),

                    new SlashCommandBuilder()
                        .WithName("sms-spam")
                        .WithDescription("Sends multiple SMS messages")
                        .AddOption("phonenumber", ApplicationCommandOptionType.String, "Phone number to send the SMS", isRequired: true)
                        .AddOption("message", ApplicationCommandOptionType.String, "The message to send", isRequired: true)
                        .AddOption("count", ApplicationCommandOptionType.Integer, "Number of times to send the message", isRequired: true)
                };

                foreach (var command in commands)
                {
                    guild.CreateApplicationCommandAsync(command.Build());
                }
            }
        }

        private async Task OnSlashCommandExecutedAsync(SocketSlashCommand command)
        {
            Console.WriteLine($"Command Received: {command.Data.Name}");

            try
            {
                switch (command.Data.Name)
                {
                    case "help":
                        await ProcessHelpCommandAsync(command);
                        break;
                    case "sms":
                        var phoneNumber = command.Data.Options.First(o => o.Name == "phonenumber").Value.ToString();
                        var message = command.Data.Options.First(o => o.Name == "message").Value.ToString();
                        await ProcessSmsCommandAsync(command, phoneNumber, message);
                        break;
                    case "sms-spam":
                        var phoneNumberSpam = command.Data.Options.First(o => o.Name == "phonenumber").Value.ToString();
                        var messageSpam = command.Data.Options.First(o => o.Name == "message").Value.ToString();
                        var countSpam = Convert.ToInt32(command.Data.Options.First(o => o.Name == "count").Value);
                        await ProcessSmsSpamCommandAsync(command, phoneNumberSpam, messageSpam, countSpam);
                        break;
                    default:
                        await command.RespondAsync("Invalid command.");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in OnSlashCommandExecutedAsync: {ex.Message}");
                await command.RespondAsync("An error occurred while processing your command.");
            }
        }

        private async Task ProcessHelpCommandAsync(SocketSlashCommand command)
        {
            var embed = new EmbedBuilder()
                .WithTitle("Help Information")
                .WithDescription("Here are the available commands:\n" +
                                 "- `/help`: Shows help information.\n" +
                                 "- `/sms <phonenumber> <message>`: Send an SMS to the specified phone number. Format the phone number as 1NXXNXXXXXX.\n" +
                                 "- `/sms-spam <phonenumber> <message> <count>`: Send multiple SMS messages.\n")
                .WithColor(Color.Red)
                .WithCurrentTimestamp()
                .Build();

            await command.RespondAsync(embed: embed, ephemeral: true);
        }

        private async Task ProcessSmsCommandAsync(SocketSlashCommand command, string phoneNumber, string message)
        {
            try
            {
                await command.DeferAsync(); // Acknowledge the command immediately

                if (!phoneNumber.StartsWith("1"))
                {
                    phoneNumber = "1" + phoneNumber;
                }

                string senderPhoneNumber = _discordHandler.FindPhoneNumberByUserId(command.User.Id);

                if (string.IsNullOrEmpty(senderPhoneNumber))
                {
                    await command.FollowupAsync("Your Discord account is not linked with a phone number.");
                    return;
                }

                if (string.IsNullOrWhiteSpace(message))
                {
                    await command.FollowupAsync("Cannot send an empty message.");
                    return;
                }

                var response = await _smsHandler.SendSMSMMSAsync(senderPhoneNumber, phoneNumber, message);

                if (response != null)
                {
                    var embed = new EmbedBuilder()
                        .WithTitle("SMS Sent Successfully")
                        .WithDescription($"SMS sent to: {phoneNumber} \n Body: \n {message}")
                        .WithColor(Color.Green)
                        .WithCurrentTimestamp()
                        .Build();

                    await command.FollowupAsync(embed: embed);
                    await Task.Delay(2500);
                }
                else
                {
                    await command.FollowupAsync("Failed to send SMS. Please check the phone numbers and try again.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                await command.FollowupAsync($"Failed to send SMS: {ex.Message}");
            }
        }

        private async Task ProcessSmsSpamCommandAsync(SocketSlashCommand command, string phoneNumber, string message, int count)
        {
            try
            {
                await command.DeferAsync(); // Acknowledge the command immediately

                if (!phoneNumber.StartsWith("1"))
                {
                    phoneNumber = "1" + phoneNumber;
                }

                string senderPhoneNumber = _discordHandler.FindPhoneNumberByUserId(command.User.Id);
                if (string.IsNullOrEmpty(senderPhoneNumber))
                {
                    await command.FollowupAsync("Your Discord account is not linked with a phone number.");
                    return;
                }

                if (string.IsNullOrWhiteSpace(message))
                {
                    await command.FollowupAsync("Cannot send an empty message.");
                    return;
                }

                for (int i = 0; i < count; i++)
                {
                    // Send SMS and respect the updated API limitation of 5 messages per 1 second
                    if (i > 0 && i % 5 == 0)
                    {
                        await Task.Delay(1000); // Wait for 1 second after every 5 messages
                    }

                    var response = await _smsHandler.SendSMSMMSAsync(senderPhoneNumber, phoneNumber, message);
                    if (response == null)
                    {
                        await command.FollowupAsync($"Failed to send SMS after {i + 1} attempts. Please check the phone numbers and try again.");
                        return;
                    }
                }

                var embed = new EmbedBuilder()
                    .WithTitle("SMS Spam Sent Successfully")
                    .WithDescription($"To: {phoneNumber}\nMessage: {message}\nCount: {count}")
                    .WithColor(Color.Green)
                    .WithCurrentTimestamp()
                    .Build();

                await command.FollowupAsync(embed: embed);
            }
            catch (Exception ex)
            {
                await command.FollowupAsync($"Failed to send SMS spam: {ex.Message}");
            }
        }
    }
}
