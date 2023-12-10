using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord.Interactions;
using Smguy;

namespace Dboy
{
    public class CustomSlashCommandsModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly DiscordSocketClient _discordClient;
        private readonly SmsHandler _smsHandler;
        private readonly DiscordHandler _discordHandler;
        private readonly IServiceProvider _serviceProvider;

        public CustomSlashCommandsModule(DiscordSocketClient discordClient, SmsHandler smsHandler, DiscordHandler discordHandler, IServiceProvider serviceProvider)
        {
            _discordClient = discordClient;
            _smsHandler = smsHandler;
            _discordHandler = discordHandler;
            _serviceProvider = serviceProvider;
            _discordClient.SelectMenuExecuted += SelectMenuHandler;
        }

        [Command("sms-test")]
        public async Task TestSmsAsync()
        {
            try
            {
                // Load allowed phone numbers from the keys.json file
                var allowedNumbers = LoadAllowedPhoneNumbersFromJson(Context.User.Id);

                if (allowedNumbers.Count == 0)
                {
                    await Context.User.SendMessageAsync("No allowed phone numbers found.");
                    return;
                }

                // Create a SelectMenuBuilder to add options
                var selectMenu = new SelectMenuBuilder()
                    .WithCustomId("select_phonenumber")
                    .WithPlaceholder("Select a phone number")
                    .WithMinValues(1)
                    .WithMaxValues(1);

                foreach (var phoneNumber in allowedNumbers)
                {
                    selectMenu.AddOption(phoneNumber, phoneNumber);
                }

                // Create an embed to provide instructions
                var embed = new EmbedBuilder()
                    .WithTitle("Send SMS")
                    .WithDescription("Choose a phone number and provide the destination number, message content, and count.")
                    .WithColor(Color.Blue)
                    .Build();

                // Send an ephemeral message with the select menu and embed
                await Context.User.SendMessageAsync("Select a phone number:", false, embed, components: new ComponentBuilder().WithSelectMenu(selectMenu).Build());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                await Context.User.SendMessageAsync("An error occurred while processing the command.");
            }
        }

        // Function to load allowed phone numbers from keys.json for a specific user
        private List<string> LoadAllowedPhoneNumbersFromJson(ulong userId)
        {
            try
            {
                var jsonString = File.ReadAllText("keys.json");
                var json = JsonConvert.DeserializeObject<Keys>(jsonString);

                if (json == null || json.allowedUsers == null || !json.allowedUsers.ContainsKey(userId.ToString()))
                {
                    return new List<string>();
                }

                return json.allowedUsers[userId.ToString()];
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new List<string>();
            }
        }

        // Select menu handler
        private async Task SelectMenuHandler(SocketMessageComponent arg)
        {
            switch (arg.Data.CustomId)
            {
                case "select_phonenumber":
                    var selectedValue = arg.Data.Values.FirstOrDefault();
                    var phoneNumber = selectedValue; // The selected phone number

                    // Now, you can continue the conversation with the user to collect other required information.
                    // Handle user responses and send SMS messages accordingly.
                    // You may want to use a dictionary or a state machine to manage the conversation with the user.

                    break;
            }
        }
    }
}
