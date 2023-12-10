using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Dboy;
using System.Text;

namespace Mainboi
{
    public class Cherlock
    {
        private HttpClient _httpClient;
        private List<SiteInfo> sites;
        private SemaphoreSlim _semaphore;
        private int resultCount; // Class-level field for counting results

        public Cherlock()//(int maxConcurrentRequests = 10) // Default to 10 concurrent requests
        {
            _httpClient = new HttpClient();
            sites = new List<SiteInfo>();
            _semaphore = new SemaphoreSlim(20, 20); // Initialize the semaphore, 25 concurrent tasks
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
                // Console.WriteLine($"Skipping site {site?.Name} due to missing or invalid URL format.");
                return false;
            }

            string formattedUrl = string.Format(site.Url, username);
            site.FormattedUrl = formattedUrl; // Store the formatted URL

            try
            {
                var response = await _httpClient.GetAsync(formattedUrl);
                var finalUrl = response.RequestMessage.RequestUri.ToString();
                var content = await response.Content.ReadAsStringAsync();

                // Check if the response indicates the username does not exist
                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        return false;
                    }
                    // Uncomment for specific status code debugging
                    // Console.WriteLine($"Unhandled status code {response.StatusCode} for {formattedUrl}.");
                    return false;
                }                
                // Additional checks based on site.ErrorType and site.ErrorMsg
                if (site.ErrorType == "message" && !string.IsNullOrEmpty(site.ErrorMsg))
                {
                    return !content.Contains(site.ErrorMsg);
                }

                return true; // Username exists and passes all checks
            }
            catch (Exception ex)
            {
                // Console.WriteLine($"Error checking {formattedUrl}: {ex.Message}");
                return false;
            }
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

        private async Task ProcessSiteAsyncForDiscord(SiteInfo site, string username, StringBuilder results)
        {
            await _semaphore.WaitAsync();
            try
            {
                if (await CheckUsernameAsync(site, username))
                {
                    lock (results)
                    {
                        results.AppendLine($"[+] {site.Name}: {site.FormattedUrl}");
                    }
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public class SiteInfo
        {
            public string? Name { get; set; }
            public string? Url { get; set; }
            public string? ErrorType { get; set; }
            public string? ErrorMsg { get; set; }
            public string? UrlMain { get; set; }
            public string? UsernameClaimed { get; set; }
            public string? FormattedUrl { get; set; } // Add this property
        }
    }
}
