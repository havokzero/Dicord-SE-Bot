using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Text;

namespace Smguy
{
    public class SmsHandler
    {
        private readonly string _accessKey;
        private readonly string _secretKey;
        private readonly string _mmsMediaUrl;
        private static readonly HttpClient _httpClient = new HttpClient();
        private readonly HashSet<string> _displayedMessageIds = new HashSet<string>();
        // private object _offset;
        private DateTime _lastMessageTimestamp = DateTime.MinValue;

        public SmsHandler(string accessKey, string secretKey, string mmsMediaUrl)
        {
            _accessKey = accessKey;
            _secretKey = secretKey;
            _mmsMediaUrl = mmsMediaUrl;

            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{accessKey}:{secretKey}"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        }

        public async Task<JArray> GetFlowrouteMessages(int limit = 50)
        {
            string startDate = _lastMessageTimestamp == DateTime.MinValue
                ? DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-ddTHH:mm:ssZ")
                : _lastMessageTimestamp.AddSeconds(1).ToString("yyyy-MM-ddTHH:mm:ssZ");

            var url = $"https://api.flowroute.com/v2.1/messages?start_date={startDate}&limit={limit}";

            try
            {
                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseObject = JObject.Parse(responseContent);
                    JArray messages = responseObject["data"]?.ToObject<JArray>() ?? new JArray();

                    // Update _lastMessageTimestamp if new messages are found
                    if (messages.Count > 0)
                    {
                        var latestMessage = messages.Last;
                        if (DateTime.TryParse(latestMessage["attributes"]["timestamp"]?.ToString(), out var newTimestamp))
                        {
                            _lastMessageTimestamp = newTimestamp;
                        }
                    }

                    return messages;
                }
                else
                {
                    Console.WriteLine($"Error retrieving messages from Flowroute: {response.StatusCode}");
                    return new JArray();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving messages from Flowroute: {ex.Message}");
                return new JArray();
            }
        }

        public bool IsMessageDisplayed(string messageId)
        {
            return _displayedMessageIds.Contains(messageId);
        }

        public void MarkMessageAsDisplayed(string messageId)
        {
            Console.WriteLine($"Marking message as displayed: {messageId}");
            _displayedMessageIds.Add(messageId);
        }

        public string FormatSmsMessage(JToken message)
        {
            var attributes = message["attributes"];
            var body = attributes?["body"]?.ToString();
            var from = attributes?["from"]?.ToString();
            var to = attributes?["to"]?.ToString();
            var timestamp = attributes?["timestamp"]?.ToString();
            var isMms = (bool)(attributes?["is_mms"] ?? false);
            var formattedTimestamp = ParseTimestamp(timestamp);

            string formattedMessage = $"From: {from}\nTo: {to}\nTimestamp: {formattedTimestamp}\nBody: {body}\n";

            if (isMms && message["relationships"]?["media"]?["data"] != null)
            {
                JArray mediaData = (JArray)message["relationships"]["media"]["data"];
                foreach (var media in mediaData)
                {
                    string mediaId = media["id"].ToString();
                    string mediaUrl = $"{_mmsMediaUrl}{mediaId}";
                    formattedMessage += $"Media URL: {mediaUrl}\n";
                }
            }

            return formattedMessage;
        }

        private string ParseTimestamp(string timestamp)
        {
            if (DateTime.TryParse(timestamp, null, System.Globalization.DateTimeStyles.AssumeUniversal, out var dateTime))
            {
                dateTime = dateTime.ToLocalTime();
                return dateTime.ToString("yyyy-MM-dd hh:mm:ss tt");
            }

            return timestamp;
        }

        private SemaphoreSlim _rateLimitSemaphore = new SemaphoreSlim(5, 5); // Limit to 5 messages per second

        public async Task<string> SendSMSMMSAsync(string fromDid, string toPhoneNumber, string messageContent)
        {
            try
            {
                Console.WriteLine("Sending SMS/MMS...");
                Console.WriteLine($"From: {fromDid}, To: {toPhoneNumber}, Message: {messageContent}");

                // Acquire a semaphore slot for rate limiting
                await _rateLimitSemaphore.WaitAsync();

                var smsMessage = new
                {
                    from = fromDid,
                    to = toPhoneNumber,
                    body = messageContent
                };

                return await SendSMSViaFlowroute(smsMessage);
            }
            finally
            {
                // Release the semaphore slot after sending the message
                _rateLimitSemaphore.Release();
            }
        }

        private async Task<string> SendSMSViaFlowroute(object smsMessage)
        {
            try
            {
                string apiUrl = "https://api.flowroute.com/v2.1/messages";
                string jsonContent = Newtonsoft.Json.JsonConvert.SerializeObject(smsMessage);

                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(apiUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    return responseContent;
                }
                else
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error response from Flowroute: {errorResponse}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending SMS via Flowroute: {ex.Message}");
                return null;
            }
        }

        internal Task SendSMSMMSAsync(ulong userPhoneNumber, string phoneNumber, string message)
        {
            throw new NotImplementedException();
        }
    }
}
