namespace Dboy
{
    public class Keys
    {
        public DiscordKeys? discord { get; set; }
        public FlowrouteKeys? flowroute { get; set; }
    }

    public class DiscordKeys
    {
        public string? token { get; set; }
        public string? guildId { get; set; }
        public string? channelId { get; set; }
        public Dictionary<string, ulong>? phoneNumberToUserId { get; set; }
        public string? webhookUrl { get; set; }
    }

    public class FlowrouteKeys
    {
        public string? secretKey { get; set; }
        public string? accessKey { get; set; }
        public string? webhookCallbackUrl { get; set; }
        public string? mmsMediaUrl { get; set; }
        public List<string>? phoneNumbers { get; set; }

    }

    public class SmsInteractionState
    {
        public string SelectedNumber { get; set; }
        public string RecipientPhoneNumber { get; set; }
        public string MessageContent { get; set; }
        public int Count { get; set; }
        public List<string> UserNumbers { get; set; }
        public bool AwaitingNumberChoice { get; set; }
        public bool AwaitingRecipientPhoneNumber { get; set; }
        public bool AwaitingMessageContent { get; set; } // Add this
        public bool AwaitingCount { get; set; } // Add this

        // Constructor
        public SmsInteractionState()
        {
            AwaitingNumberChoice = false;
            AwaitingRecipientPhoneNumber = false;
            AwaitingMessageContent = false; // Initialize this
            AwaitingCount = false; // Initialize this
                                   // Initialize other properties as needed
        }
    }
}
   

    public class SmsInteractionState
    {
        public string SelectedNumber { get; set; }
        public string RecipientPhoneNumber { get; set; }
        public string MessageContent { get; set; }
        public int Count { get; set; }
        public List<string> UserNumbers { get; set; }
        public bool AwaitingNumberChoice { get; set; }
        public bool AwaitingRecipientPhoneNumber { get; set; }
        public bool AwaitingMessageContent { get; set; } // Add this
        public bool AwaitingCount { get; set; } // Add this

        // Constructor
        public SmsInteractionState()
        {
            AwaitingNumberChoice = false;
            AwaitingRecipientPhoneNumber = false;
            AwaitingMessageContent = false; // Initialize this
            AwaitingCount = false; // Initialize this
                                   // Initialize other properties as needed
        }
    }
