using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Mainboi;
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
        private readonly PKeys _keys; // Add this field to store the configuration

        public SuperSmsHandler(DiscordSocketClient discordClient, SmsHandler smsHandler,
                       Dictionary<string, ulong> phoneNumberToUserId, Dictionary<string, List<string>> allowedUsers,
                       ulong guildId, DiscordHandler discordHandler, PKeys keys)
        {
            _discordClient = discordClient;
            _smsHandler = smsHandler;
            _phoneNumberToUserId = phoneNumberToUserId;
            _allowedUsers = allowedUsers;
            _guildId = guildId;
            _discordHandler = discordHandler;
            _keys = keys;


            _discordClient.SlashCommandExecuted += OnSlashCommandExecutedAsync;

            discordClient.Ready += async () =>
            {
                ulong searchResultsChannelId = ulong.Parse(_keys.discord.searchResultsChannelId);

                RegisterSlashCommands();
            }; // This should register / commands
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
                        .AddOption("count", ApplicationCommandOptionType.Integer, "Number of times to send the message", isRequired: true),

                    new SlashCommandBuilder() // Add a new command for /cherlock
                         .WithName("cherlock")
                         .WithDescription("Perform username search with Cherlock")
                        .AddOption("username", ApplicationCommandOptionType.String, "Username to search for", isRequired: true)
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
                    case "cherlock":
                        await command.DeferAsync(ephemeral: true); // Acknowledge the command
                        var username = command.Data.Options.First(o => o.Name == "username").Value.ToString();
                        await SearchUsernamesAsync(command, username); // Execute the search
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in OnSlashCommandExecutedAsync: {ex.Message}");
                await command.FollowupAsync("An error occurred while processing your command.");
            }
        }

        private async Task ProcessHelpCommandAsync(SocketSlashCommand command)
        {
            var embed = new EmbedBuilder()
                .WithTitle("SE Bot Help Information")
                .WithDescription("Choose your vector in the realm of social engineering! Here are the available commands:\n" +
                                 "- `/sms <phonenumber> <message>`: Send an SMS to the specified phone number. Format the phone number as 1NXXNXXXXXX.\n" +
                                 "- `/sms-spam <phonenumber> <message> <count>`: Send multiple SMS messages.\n" +
                                 "- `/cherlock <username>`: Search for usernames associated with the provided username on various platforms.\n")
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

        public async Task SearchUsernamesAsync(SocketSlashCommand command, string input)
        {
            try
            {
                var usernames = input.Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                var cherlock = new Cherlock(_keys);

                foreach (var username in usernames)
                {
                    Console.WriteLine($"Searching for username: {username}");
                    var results = await cherlock.SearchForUsernamesForDiscord(username);

                    if (results.Length > 2000)
                    {
                        // Save lengthy results to a temporary file
                        var tempFilePath = Path.Combine(Path.GetTempPath(), $"{username}.txt");
                        await File.WriteAllTextAsync(tempFilePath, results);

                        // Create an embed for the file and include the count of usernames found
                        var embedBuilder = new EmbedBuilder();
                        embedBuilder.AddField("Username Search Results", $"Results for '{username}' are too lengthy to display here. Please see the attached file.")
                                     .AddField("Total Results", $"{cherlock.UsernameCount}");

                        // Send the file with the embed
                        await command.Channel.SendFileAsync(tempFilePath, embeds: new Embed[] { embedBuilder.Build() });

                        // Clean up the temporary file
                        File.Delete(tempFilePath);
                    }
                    else
                    {
                        // Create an embed for shorter results, splitting into multiple fields if necessary
                        var embedBuilder = new EmbedBuilder();
                        embedBuilder.AddField("Username Search Results", $"Results for '{username}':");

                        const int maxFieldLength = 1024;
                        while (results.Length > 0)
                        {
                            string fieldContent = results.Length <= maxFieldLength ? results : results.Substring(0, maxFieldLength);
                            embedBuilder.AddField("\u200B", fieldContent); // Using a zero-width space as a field title
                            results = results.Length <= maxFieldLength ? string.Empty : results.Substring(maxFieldLength);
                        }

                        // Send the embed with the results
                        await command.Channel.SendMessageAsync(embed: embedBuilder.Build());
                    }

                    // Optionally, add a delay between processing each username
                    await Task.Delay(1000); // 1 second delay
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error searching usernames: {ex.Message}");
                await command.Channel.SendMessageAsync("An error occurred while searching for usernames.");
            }
        }
    }
}
