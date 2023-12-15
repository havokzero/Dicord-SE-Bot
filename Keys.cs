namespace Dboy
{
    public class PKeys
    {
        public DiscordKeys? discord { get; set; }
        public FlowrouteKeys? flowroute { get; set; }
        public Dictionary<string, List<string>> allowedUsers { get; set; }
    }

    public class DiscordKeys
    {
        public string? token { get; set; }
        public string? guildId { get; set; }
        public string? channelId { get; set; }
        public Dictionary<string, ulong>? phoneNumberToUserId { get; set; }
        public string? webhookUrl { get; set; }
        public string? searchResultsChannelId { get; set; }
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
        
    }

    public class SiteInfo
    {
        public string? searchResultsChannelId { get; set; }
        public string? Name { get; set; }
        public string? Url { get; set; }
        public string? ErrorType { get; set; }
        public string? ErrorMsg { get; set; }
        public string? UrlMain { get; set; }
        public string? UsernameClaimed { get; set; }
        public string? FormattedUrl { get; set; }
    }
}
