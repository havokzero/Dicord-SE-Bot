using Dboy;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Mainboi
{
    public class Cherlock
    {
        private readonly HttpClient _httpClient;
        private List<SiteInfo> sites;
        private readonly SemaphoreSlim _semaphore;
        private int resultCount; // Class-level field for counting results
        private PKeys _keys;

        public Cherlock(PKeys keys) // Pass the _keys configuration object as a parameter
        {
            _keys = keys; // Store the configuration object
            _httpClient = new HttpClient();
            sites = new List<SiteInfo>();
            _semaphore = new SemaphoreSlim(20, 20); // Initialize the semaphore, 20 concurrent tasks
        }

        private string ConvertToString(dynamic value)
        {
            if (value is JArray arrayValue)
            {
                return string.Join(", ", arrayValue.ToObject<string[]>());
            }
            else if (value is string strValue)
            {
                // Replace '{}' with '{0}' for C# string formatting
                return strValue.Replace("{}", "{0}");
            }
            return value?.ToString();
        }

        public async Task GetSitesAsync()
        {
            string filePath = "resources.json"; // Ensure this file is in the correct location
            string jsonResponse = File.ReadAllText(filePath);
            var sitesDictionary = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(jsonResponse);

            sites = new List<SiteInfo>();
            foreach (var entry in sitesDictionary)
            {
                var site = new SiteInfo
                {
                    Name = entry.Key,
                    Url = entry.Value.url.ToString().Replace("{}", "{0}"),
                    ErrorType = ConvertToString(entry.Value.errorType),
                    ErrorMsg = ConvertToString(entry.Value.errorMsg),
                    UsernameClaimed = ConvertToString(entry.Value.username_claimed)
                };
                sites.Add(site);
            }
        }

        public async Task SearchForUsernames(string input)
        {
            var usernames = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (usernames.Length == 0)
            {
                Console.WriteLine("No usernames provided.");
                return;
            }

            if (sites == null || sites.Count == 0)
            {
                await GetSitesAsync();
            }

            foreach (var username in usernames)
            {
                Console.WriteLine($"\nSearching for username: {username}");

                var tasks = new List<Task<bool>>();
                foreach (var site in sites)
                {
                    tasks.Add(ProcessSiteAsync(site, username));
                }

                var results = await Task.WhenAll(tasks);
                int resultCount = results.Count(r => r);

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[*] Search completed for '{username}' with {resultCount} results");
                Console.ResetColor();
            }
        }

        private async Task<bool> ProcessSiteAsync(SiteInfo site, string username)
        {
            await _semaphore.WaitAsync();
            try
            {
                if (await CheckUsernameAsync(site, username))
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("[+] ");
                    Console.Write($"{site.Name}: ");
                    Console.ResetColor();
                    Console.WriteLine(site.FormattedUrl);
                    return true;
                }
                return false;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task<bool> CheckUsernameAsync(SiteInfo site, string username)
        {
            if (site == null || string.IsNullOrEmpty(site.Url) || !site.Url.Contains("{0}"))
            {
                // Uncomment for debugging
                // Console.WriteLine($"Skipping site {site?.Name} due to missing or invalid URL format.");
                return false;
            }

            string formattedUrl = string.Format(site.Url, username);
            site.FormattedUrl = formattedUrl; // Store the formatted URL for potential use

            try
            {
                var response = await _httpClient.GetAsync(formattedUrl);
                var finalUrl = response.RequestMessage.RequestUri.ToString();
                var content = await response.Content.ReadAsStringAsync();

                // Check if the final URL does not contain the username
                if (!finalUrl.Contains(username))
                {
                    return false;
                }

                switch (response.StatusCode)
                {
                    case System.Net.HttpStatusCode.OK:
                        if (site.ErrorType == "message" && !string.IsNullOrEmpty(site.ErrorMsg))
                        {
                            return !Regex.IsMatch(content, site.ErrorMsg);
                        }
                        return true;

                    case System.Net.HttpStatusCode.NotFound:
                    case System.Net.HttpStatusCode.Unauthorized:
                    case System.Net.HttpStatusCode.InternalServerError: // Silently ignore internal server errors
                    case System.Net.HttpStatusCode.BadRequest:
                    case System.Net.HttpStatusCode.Forbidden:
                    case System.Net.HttpStatusCode.NotAcceptable:
                    case System.Net.HttpStatusCode.Gone:
                    case System.Net.HttpStatusCode.Redirect:
                        return false;

                        // Uncomment for specific status code debugging
                        // default:
                        //     Console.WriteLine($"Unhandled status code {response.StatusCode} for {formattedUrl}.");
                        //     break;
                }
            }
            catch (Exception ex)
            {
                // Uncomment for debugging
                // Console.WriteLine($"Error checking {formattedUrl}: {ex.Message}");
                return false;
            }

            // Default return for unhandled cases
            return false;
        }

        private bool IsExpectedUrlFormat(string url, string username)
        {
            return url.Contains(username);
        }

        public async Task<string> SearchForUsernamesForDiscord(string input)
        {
            var usernames = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (usernames.Length == 0)
            {
                return "No usernames provided.";
            }

            if (sites == null || sites.Count == 0)
            {
                await GetSitesAsync();
            }

            StringBuilder results = new StringBuilder();

            foreach (var username in usernames)
            {
                var tasks = sites.Select(site => ProcessSiteAsyncForDiscord(site, username, results)).ToList();
                await Task.WhenAll(tasks);
            }

            return results.ToString();
        }

        public int UsernameCount { get; private set; } = 0;

        private async Task ProcessSiteAsyncForDiscord(SiteInfo site, string username, StringBuilder results)
        {
            await _semaphore.WaitAsync();
            try
            {
                if (await CheckUsernameAsync(site, username))
                {
                    lock (results)
                    {
                        results.AppendLine($"[+] {site.Name}: <{site.FormattedUrl}>");
                        UsernameCount++; // Increment the counter
                    }
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
