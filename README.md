Flowroute SMS and Discord Integration Bot

This bot integrates SMS/MMS functionalities with Discord, allowing users to receive and send SMS/MMS messages through Discord and a console interface.
Features

    Receive SMS/MMS Messages: The bot checks for new SMS/MMS messages and posts them on a specified Discord channel, tagging the intended user.
    Send SMS/MMS Messages: Users can send SMS/MMS messages via Discord commands or through the console application.
    Console Interface: Allows sending SMS/MMS messages directly from the console.

Prerequisites

    .NET 6.0 SDK
    Discord Bot Token and Channel/Guild IDs
    Flowroute API Access and Secret Keys
    Valid Flowroute phone numbers

Installation

    Clone the Repository

    bash

git clone https://github.com/havokzero/Flowroute-SMS-DiscordBot
cd Flowroute-SMS-DiscordBot

Set up keys.json

Replace the placeholders in keys.json with your actual Discord and Flowroute credentials.

```json

{
  "discord": {
    "token": "YOUR_DISCORD_BOT_TOKEN",
    "guildId": "YOUR_DISCORD_GUILD_ID",
    "channelId": "YOUR_DISCORD_CHANNEL_ID",
    
  },
  "flowroute": {
    "secretKey": "YOUR_FLOWROUTE_SECRET_KEY",
    "accessKey": "YOUR_FLOWROUTE_ACCESS_KEY",
    
  }
}
```

Build and Run

Run the following command in your terminal:

bash

    dotnet build
    dotnet run

Usage
Running the Bot

After executing the program, you'll be presented with the following options:

    Run Discord Bot: Start the Discord bot.
    Send SMS/MMS: Send an SMS/MMS message via the console.
    Exit: Terminate the application.

Sending SMS/MMS via Console

    Choose 2: Send SMS/MMS from the main menu.
    Follow the prompts to select a Flowroute number, enter the recipient's phone number, and type your message.
    The program will send the message and confirm its dispatch.

Sending SMS/MMS via Discord (This has been updated and now works!!)

    Use the !msg command followed by the recipient's phone number and your message.

    Example: !msg 14025551234 Hello, world!

Receiving SMS/MMS Messages

    Incoming SMS/MMS messages will automatically be posted in the configured Discord channel.

Notes

    Ensure your Flowroute numbers and Discord settings are correctly configured in keys.json.
    The bot needs appropriate permissions in your Discord guild to post messages.

Support

For any queries or support, please open an issue in the repository or contact the repository maintainer.

The Flowroute SMS/MMS will not function without setting up the proper API access.


