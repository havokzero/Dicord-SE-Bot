Good SuperSms.cs no errors 

using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using Smguy;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dboy
{
    public class SuperSmsHandler
    {
        private readonly DiscordSocketClient _discordClient;
        private readonly Dictionary<ulong, SmsInteractionState> _interactionStates;
        private readonly SmsHandler _smsHandler;
        private readonly Dictionary<string, ulong> _phoneNumberToUserId;
        private readonly Dictionary<string, List<string>> _allowedUsers;
        private readonly ulong _guildId;  // Add this line
        private readonly DiscordHandler _discordHandler;
        private readonly Dictionary<ulong, (string SelectedNumber, int? SendTimes, string RecipientNumber)> _userChoices;


        public SuperSmsHandler(DiscordSocketClient discordClient, SmsHandler smsHandler,
                       Dictionary<string, ulong> phoneNumberToUserId,
                       Dictionary<string, List<string>> allowedUsers, ulong guildId,
                       DiscordHandler discordHandler) // Add DiscordHandler here

        {
            _discordClient = discordClient;
            _smsHandler = smsHandler;
            _phoneNumberToUserId = phoneNumberToUserId;
            _allowedUsers = allowedUsers;
            _guildId = guildId;
            _interactionStates = new Dictionary<ulong, SmsInteractionState>();
            _discordHandler = discordHandler; // Assign it here
            _userChoices = new Dictionary<ulong, (string, int?, string)>();
            _discordClient.MessageReceived += OnMessageReceived;


            // Subscribe to necessary events
            _discordClient.SlashCommandExecuted += OnSlashCommandExecutedAsync;
            //_discordClient.ComponentInteractionCreated += OnComponentInteractionAsync;


        }

        private void RegisterSlashCommands()
        {
            var guild = _discordClient.GetGuild(_guildId);
            if (guild != null)
            {
                guild.CreateApplicationCommandAsync(new SlashCommandBuilder()
                    .WithName("help")
                    .WithDescription("Shows help information.")
                    .Build());

                guild.CreateApplicationCommandAsync(new SlashCommandBuilder()
                    .WithName("sms")
                    .WithDescription("Sends an SMS message.")
                    .AddOption("phonenumber", ApplicationCommandOptionType.String, "Phone number to send the SMS", isRequired: true)
                    .AddOption("message", ApplicationCommandOptionType.String, "The message to send", isRequired: true)
                    .Build());

                guild.CreateApplicationCommandAsync(new SlashCommandBuilder()
                    .WithName("sms-spam")
                    .WithDescription("Sends multiple SMS messages.")
                    .AddOption("phonenumber", ApplicationCommandOptionType.String, "Phone number to send the SMS", isRequired: true)
                    .AddOption("message", ApplicationCommandOptionType.String, "The message to send", isRequired: true)
                    .AddOption("count", ApplicationCommandOptionType.Integer, "Number of times to send the message", isRequired: true)
                    .Build());
                guild.CreateApplicationCommandAsync(new SlashCommandBuilder()
                    .WithName("super-sms")
                    //.WithDescription("Sends multiple SMS messages from achosen number.")
                    //.AddOption("from number", ApplicationCommandOptionType.String, "Phone number to send the SMS from", isRequired: true)
                    // .AddOption("to number", ApplicationCommandOptionType.String, "Phone number to send the SMS to", isRequired: true)
                    // .AddOption("message", ApplicationCommandOptionType.String, "The message to send", isRequired: true)
                    // .AddOption("count", ApplicationCommandOptionType.Integer, "Number of times to send the message", isRequired: true)
                    .Build());
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
                    case "super-sms":
                        //  await ProcessSuperSmsCommandAsync(command);
                        break;
                        /*     var fromPhoneNumberOption = command.Data.Options.FirstOrDefault(o => o.Name == "fromnumber")?.Value?.ToString();
                               var toPhoneNumberOption = command.Data.Options.FirstOrDefault(o => o.Name == "tonumber")?.Value?.ToString();
                               var superMessageOption = command.Data.Options.FirstOrDefault(o => o.Name == "message")?.Value?.ToString(); // Renamed variable
                               var countOption = command.Data.Options.FirstOrDefault(o => o.Name == "count")?.Value;

                               if (fromPhoneNumberOption == null || toPhoneNumberOption == null || superMessageOption == null || countOption == null)
                               {
                                   await command.RespondAsync("Missing required options. Please provide all necessary details.", ephemeral: true);
                                   return;
                               }

                               string fromPhoneNumber = fromPhoneNumberOption;
                               string toPhoneNumber = toPhoneNumberOption;
                               string superMessage = superMessageOption; // Using renamed variable
                               int count = Convert.ToInt32(countOption);

                               await ProcessSuperSmsCommandAsync(command, fromPhoneNumber, toPhoneNumber, superMessage, count);
                               break;
                       }*/
                        Console.WriteLine($"Command2 Received: {command.Data.Name}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in OnSlashCommandExecutedAsync: {ex.Message}");
                // Consider sending a follow-up message to the user to indicate an error occurred.
                await command.RespondAsync("no message here");
                await command.FollowupAsync("An error occurred while processing your command.");
            }
        }

        private async Task ProcessHelpCommandAsync(SocketSlashCommand command)
        {
            var embed = new EmbedBuilder()
                .WithTitle("Help Information")
                .WithDescription("Here are the available commands:\n" +
                                 "- `/help`: Shows help information.\n" +
                                 "- `/sms <phonenumber> <message>`: Send an SMS to the specified phone number. Format the phone number as 1NXXNXXXXXX.\n" +
                                 "- `/sms-spam <phonenumber> <message> <count>`: Send multiple SMS messages.\n +" +
                                 "- `/super-sms <From number> <phonenumber> <message> <count>`: Send multiple SMS messages from a number Havok owns.\n")


                .WithColor(Color.Red)
                .WithCurrentTimestamp()
                .Build();

            await command.RespondAsync(embed: embed);
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
                        .WithDescription($"SMS sent to {phoneNumber}: {message}")
                        .WithColor(Color.Green)
                        .WithCurrentTimestamp()
                        .Build();

                    await command.FollowupAsync(embed: embed);
                }
                else
                {
                    await command.FollowupAsync("Failed to send SMS. Please check the phone numbers and try again.");
                }
            }
            catch (Exception ex)
            {
                await command.FollowupAsync($"Failed to send SMS: {ex.Message}");
            }
        }

        /*  private string FindPhoneNumberByUserId(ulong userId) //this is handled in the DiscordHandler.cs
          {
              // Implement the logic to find the phone number by user ID like this //string senderPhoneNumber = _discordHandler.FindPhoneNumberByUserId(command.User.Id);
              // This can be similar to what you have in your DiscordHandler class
          }*/


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
                    // Send SMS and respect the updated API limitation of 5 messages per 1 seconds
                    if (i > 0 && i % 3 == 0)
                    {
                        await Task.Delay(2200); // Wait for 2.2 seconds after every 3 messages
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

        private async Task OnComponentInteractionAsync(SocketMessageComponent component)
        {
            var userId = component.User.Id;

            // Check if the interaction is for the 'select_number' dropdown
            if (component.Data.CustomId == "select_number")
            {
                // Retrieve the selected phone number
                var selectedNumber = component.Data.Values.First();

                // Initialize or update the interaction state for this user
                if (!_interactionStates.ContainsKey(userId))
                {
                    _interactionStates[userId] = new SmsInteractionState();
                }

                _interactionStates[userId].SelectedNumber = selectedNumber;
                _interactionStates[userId].AwaitingRecipientPhoneNumber = true;

                // Prompt the user to enter the recipient's phone number
                await component.FollowupAsync("Please enter the recipient's phone number:", ephemeral: true);
            }
            else if (_interactionStates.TryGetValue(userId, out var state))
            {
                if (state.AwaitingRecipientPhoneNumber)
                {
                    state.RecipientPhoneNumber = component.Data.CustomId; // Assuming customId contains the phone number
                    state.AwaitingRecipientPhoneNumber = false;
                    state.AwaitingMessageContent = true;

                    await component.FollowupAsync("Please enter the message content:", ephemeral: true);
                }
                else if (state.AwaitingMessageContent)
                {
                    state.MessageContent = component.Data.CustomId; // Assuming customId contains the message
                    state.AwaitingMessageContent = false;
                    state.AwaitingCount = true;

                    await component.FollowupAsync("Please enter the number of times to send the message:", ephemeral: true);
                }
                else if (state.AwaitingCount)
                {
                    if (int.TryParse(component.Data.CustomId, out int count)) // Assuming customId contains the count
                    {
                        state.Count = count;
                        state.AwaitingCount = false;

                        // Call the method to send the SMS
                        await SendSms(state, userId);

                        // Clear the interaction state
                        _interactionStates.Remove(userId);
                    }
                    else
                    {
                        await component.FollowupAsync("Invalid count. Please enter a valid number.", ephemeral: true);
                    }
                }
            }
        }

        private async Task SendSms(SmsInteractionState state, ulong userId)
        {
            var user = _discordClient.GetUser(userId);
            if (user == null)
            {
                Console.WriteLine("User not found.");
                return;
            }

            try
            {
                int sentCount = 0;
                for (int i = 0; i < state.Count; i++)
                {
                    var sendResult = await _smsHandler.SendSMSMMSAsync(state.SelectedNumber, state.RecipientPhoneNumber, state.MessageContent);
                    if (sendResult != null)
                    {
                        sentCount++;
                        // Delay every 2 messages to comply with the rate limit
                        if (sentCount % 2 == 0 && sentCount < state.Count)
                        {
                            await Task.Delay(2000); // Wait for 2 seconds
                        }
                    }
                    else
                    {
                        // Handle the case where sending an individual message fails
                        Console.WriteLine($"Failed to send message {i + 1}");
                    }
                }

                await user.SendMessageAsync($"SMS sent successfully to {state.RecipientPhoneNumber} {state.Count} times.");
            }
            catch (Exception ex)
            {
                await user.SendMessageAsync($"Failed to send SMS: {ex.Message}");
            }

            // Clear the state after sending the SMS
            _interactionStates.Remove(userId);
        }

        private async Task ProcessSuperSmsCommandAsync(SocketSlashCommand command)
        {
            ulong userId = command.User.Id;

            // Convert userId to string before using it as a key
            string userIdString = userId.ToString();

            if (!_allowedUsers.ContainsKey(userIdString))
            {
                await command.RespondAsync("You are not authorized to use this command.", ephemeral: true);
                return;
            }

            var userNumbers = _allowedUsers[userIdString];
            var selectMenu = new SelectMenuBuilder()
                .WithCustomId("select_number")
                .WithPlaceholder("Select a number")
                .WithMinValues(1)
                .WithMaxValues(1);

            foreach (var number in userNumbers)
            {
                selectMenu.AddOption($"Number: {number}", number);
            }

            var component = new ComponentBuilder()
                .WithSelectMenu(selectMenu)
                .Build();

            await command.RespondAsync("Choose a number to send SMS from:", components: component, ephemeral: true);

            // Initialize or update the interaction state for this user
            if (!_interactionStates.ContainsKey(userId))
            {
                _interactionStates[userId] = new SmsInteractionState();
            }
            _interactionStates[userId].AwaitingNumberChoice = true;
        }

        private List<string> GetAllowedNumbersForUser(ulong userId)
        {
            var jsonString = File.ReadAllText("keys.json");
            var json = JsonConvert.DeserializeObject<dynamic>(jsonString);

            // Extracting allowed numbers for the user
            List<string> allowedNumbers = new List<string>();
            foreach (var item in json.allowedUsers[userId.ToString()])
            {
                allowedNumbers.Add((string)item);
            }

            return allowedNumbers;
        }
        private async Task OnMessageReceived(SocketMessage message)
        {
            if (message.Author.IsBot || !(message is SocketUserMessage userMessage))
                return;

            var userId = userMessage.Author.Id;

            // Check if the user has an interaction state
            if (_interactionStates.TryGetValue(userId, out var state))
            {
                if (state.AwaitingNumberChoice)
                {
                    if (int.TryParse(message.Content, out int choice) && choice >= 1 && choice <= state.UserNumbers.Count)
                    {
                        state.SelectedNumber = state.UserNumbers[choice - 1];
                        state.AwaitingNumberChoice = false;

                        await message.Channel.SendMessageAsync("Please enter the recipient's phone number:");
                        state.AwaitingRecipientPhoneNumber = true;
                        Console.WriteLine($"State updated: AwaitingRecipientPhoneNumber = {state.AwaitingRecipientPhoneNumber}");
                    }
                    else
                    {
                        await message.Channel.SendMessageAsync("Invalid choice. Please enter a valid number corresponding to the phone numbers listed.");
                    }
                }

                else if (state.AwaitingRecipientPhoneNumber)

                {
                    state.RecipientPhoneNumber = message.Content;
                    state.AwaitingRecipientPhoneNumber = false;
                    await message.Channel.SendMessageAsync("Please enter the message content:");
                    state.AwaitingMessageContent = true;
                }
                else if (state.AwaitingMessageContent)
                {
                    state.MessageContent = message.Content;
                    state.AwaitingMessageContent = false;
                    await message.Channel.SendMessageAsync("Please enter the number of times to send the message:");
                    state.AwaitingCount = true;
                }
                else if (state.AwaitingCount)
                {
                    if (int.TryParse(message.Content, out int count) && count > 0)
                    {
                        state.Count = count;
                        state.AwaitingCount = false;
                        await SendSms(state, userId);
                    }
                    else
                    {
                        await message.Channel.SendMessageAsync("Invalid count. Please enter a valid number.");
                        Console.WriteLine($"State updated: AwaitingRecipientPhoneNumber3 = {state.AwaitingRecipientPhoneNumber}");
                    }
                }
            }
        }

        private async Task PromptForSmsDetails(ISocketMessageChannel channel, ulong userId, string selectedNumber)
        {
            await channel.SendMessageAsync($"You have selected the number: {selectedNumber}. Please enter the recipient's phone number:");

            // Update the interaction state to await the recipient's phone number and count
            _interactionStates[userId].AwaitingRecipientPhoneNumber = true;
            // Reset or initialize other necessary fields in SmsInteractionState as needed


              
    }

    }
}
