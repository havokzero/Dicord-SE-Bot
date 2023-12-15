This bot seamlessly connects SMS/MMS capabilities with Discord, enabling users to send and receive SMS/MMS messages through Discord and a console interface.

Features:

    Receive SMS/MMS Messages: The bot checks for new SMS/MMS messages and automatically posts them in a designated Discord channel while tagging the intended user.
    Send SMS/MMS Messages: Users can send SMS/MMS messages conveniently through Discord slash commands or directly from the console application.
    Console Interface: Allows sending SMS/MMS messages directly from the console.

Prerequisites:

    .NET 6.0 SDK
    Discord Bot Token and Channel/Guild IDs
    Flowroute API Access and Secret Keys
    Valid Flowroute phone numbers

Installation:

    Clone the Repository:

    bash
```
git clone https://github.com/havokzero/Dicord-SE-Bot
cd Dicord-SE-Bot
```
Set up keys.json:

Replace the placeholders in keys.json with your actual Discord and Flowroute credentials.

```json

{
  "discord": {
    "token": "YOUR_DISCORD_BOT_TOKEN",
    "guildId": "YOUR_DISCORD_GUILD_ID",
    "channelId": "YOUR_DISCORD_CHANNEL_ID"
  },
  "flowroute": {
    "secretKey": "YOUR_FLOWROUTE_SECRET_KEY",
    "accessKey": "YOUR_FLOWROUTE_ACCESS_KEY"
  }
}```

Build and Run:

Run the following commands in your terminal:

bash

    dotnet build
    dotnet run

Usage:

After executing the program, you'll be presented with the following options:

    Run Discord Bot: Start the Discord bot.
    Send SMS/MMS: Send an SMS/MMS message via the console.
    Exit: Terminate the application.

Sending SMS/MMS:

    Choose option 2: Send SMS/MMS from the main menu.
    Follow the prompts to select a Flowroute number, enter the recipient's phone number, and type your message.
    The program will send the message and confirm its dispatch.

Sending SMS/MMS via Discord Slash Commands (Updated and Functional!):

    Use the /sms slash command followed by the recipient's phone number and your message.

    Example: /sms 14025551234 Hello, world!

Receiving SMS/MMS Messages:

    Incoming SMS/MMS messages will automatically be posted in the configured Discord channel.

Notes:

    Ensure your Flowroute numbers and Discord settings are correctly configured in keys.json.
    The bot requires appropriate permissions in your Discord guild to post messages.
