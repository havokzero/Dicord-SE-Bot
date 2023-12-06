using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Dboy; 
using Smguy;

namespace Mainboi
{
    class Program
    {
        //private static SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

        private static dynamic _keys;
        private static SmsHandler _smsHandler;
        private static DiscordHandler _discordHandler;
        private static List<string> _messageHistory = new List<string>();        
        

        static async Task Main(string[] args)
        {
            Console.Title = "Havok will Text you";

            Console.SetBufferSize(120, 1000);
            Console.WriteLine("Let's Run these texts!!!");

            LoadKeys();

            ulong guildId = ulong.Parse(_keys.discord.guildId.ToString());
            ulong channelId = ulong.Parse(_keys.discord.channelId.ToString());

            _smsHandler = new SmsHandler(
                _keys.flowroute.accessKey.ToString(),
                _keys.flowroute.secretKey.ToString(),
                _keys.flowroute.mmsMediaUrl.ToString());

            // Reading the allowedUsers data from keys.json
            var allowedUsers = ((JObject)_keys.allowedUsers).ToObject<Dictionary<string, List<string>>>();

            _discordHandler = new DiscordHandler(
                _keys.discord.token.ToString(),
                guildId,
                channelId,
                ((JObject)_keys.discord.phoneNumberToUserId).ToObject<Dictionary<string, ulong>>(),
                _smsHandler, allowedUsers); // Passing allowedUsers to the constructor

            // Start the Discord bot in a separate task
            Task botTask = RunBot();
            Task pollingTask = StartPollingForMessages();
            
            // Handling console input in the main thread
            while (true)
            {
                await HandleConsoleInput();
            }
        }

        static async Task StartPollingForMessages()
        {
            while (true)
            {
                await HandleSmsMmsMessages(); // Call the correct method
                await Task.Delay(5000); // Poll every 5 seconds, adjust as needed
            }
        }


        static async Task RunBot()
        {
            await _discordHandler.InitializeAsync();
            while (true)
            {
                await HandleIncomingCommandsAndMessages();
                await Task.Delay(1000); // Delay to prevent tight loop
            }
        }

        static async Task HandleConsoleInput()
        {
            Console.WriteLine("\nChoose a number!:");
            Console.WriteLine("1: Send a Message");
            Console.WriteLine("2: Exit Like a Bitch");
            Console.Write("Enter option: ");
            var option = Console.ReadLine();

            switch (option)
            {
                case "1":
                    await SendSMSMMS();
                    break;
                case "2":
                    Environment.Exit(0);
                    break;
                default:
                    Console.WriteLine("Invalid option. Please try again.");
                    break;
            }
        }

        static async Task SendSMSMMS()
        {
            // Load and list Flowroute numbers
            var flowrouteNumbers = _keys.flowroute.phoneNumbers.ToObject<List<string>>();
            Console.WriteLine("Select a Flowroute number to send from:");
            for (int i = 0; i < flowrouteNumbers.Count; i++)
            {
                Console.WriteLine($"{i + 1}: {flowrouteNumbers[i]}");
            }

            Console.Write("Enter your choice: ");
            int choice = Convert.ToInt32(Console.ReadLine());
            string fromNumber = flowrouteNumbers[choice - 1];

            Console.Write("Enter recipient phone number: ");
            var phoneNumber = Console.ReadLine();

            // Prepend "1" if it's not already there
            if (!phoneNumber.StartsWith("1"))
            {
                phoneNumber = "1" + phoneNumber;
            }

            Console.Write("Enter message: ");
            var message = Console.ReadLine();

            try
            {
                await _smsHandler.SendSMSMMSAsync(fromNumber, phoneNumber, message);
                Console.WriteLine("SMS/MMS sent successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending SMS/MMS: {ex.Message}");
            }
        }

        static void LoadKeys()
        {
            try
            {
                string keysJson = File.ReadAllText("keys.json");
                _keys = JsonConvert.DeserializeObject<dynamic>(keysJson);

                if (_keys == null)
                {
                    Console.WriteLine("Error: Unable to deserialize keys from keys.json.");
                    Environment.Exit(1);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading keys: {ex.Message}");
                Environment.Exit(1);
            }
        }

        private static async Task HandleIncomingCommandsAndMessages()
        {
            await HandleSmsMmsMessages();
        }

        private static async Task HandleSmsMmsMessages()
        {
            //await semaphore.WaitAsync();
            try
            {
                var messages = await _smsHandler.GetFlowrouteMessages(1); // Adjust this call as needed 1 pulls only one message

                foreach (var message in messages)
                {
                    var messageId = message["id"]?.ToString();
                    if (!_smsHandler.IsMessageDisplayed(messageId))
                    {
                        Console.WriteLine($"Processing new message: {messageId}");

                        _smsHandler.MarkMessageAsDisplayed(messageId);
                        var formattedMessage = _smsHandler.FormatSmsMessage(message);
                        _messageHistory.Add(formattedMessage);

                        Console.WriteLine($"Received SMS/MMS: {formattedMessage}");
                        await _discordHandler.SendMessageAsync(formattedMessage, message["attributes"]["to"].ToString());
                    }
                }
            }
            finally
            {
               // semaphore.Release();
            }
        }

        private static void RedrawConsole()
        {
            Console.Clear();

            foreach (var message in _messageHistory)
            {
                Console.WriteLine(message);
            }
        }
    }
}
