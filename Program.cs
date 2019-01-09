using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SyntheaImporter
{
    class Program
    {
        private static IConfiguration _configuration;
        private static AuthenticationContext _authContext = null;

        static void Main(string[] args)
        {
            Task.Run(() => MainAsync(args)).Wait();
        }

        static async Task MainAsync(string[] args)
        {
            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            _configuration = configurationBuilder.AddCommandLine(args).AddEnvironmentVariables().Build();

            Ensure.That(_configuration["DataPath"]).IsNotNullOrWhiteSpace();
            Ensure.That(_configuration["FhirServerUrl"]).IsNotNullOrWhiteSpace();
            Ensure.That(_configuration["Authority"]).IsNotNullOrWhiteSpace();
            Ensure.That(_configuration["Audience"]).IsNotNullOrWhiteSpace();
            Ensure.That(_configuration["ClientId"]).IsNotNullOrWhiteSpace();
            Ensure.That(_configuration["ClientSecret"]).IsNotNullOrWhiteSpace();

            _authContext = new AuthenticationContext(_configuration["Authority"]);
            ClientCredential clientCredential = new ClientCredential(_configuration["ClientId"], _configuration["ClientSecret"]); ;
            var client = new HttpClient();
            client.BaseAddress = new Uri(_configuration["FhirServerUrl"]);

            AuthenticationResult authResult = null;
            try
            {
                authResult = _authContext.AcquireTokenAsync(_configuration["Audience"], clientCredential).Result;
            }
            catch (Exception ee)
            {
                Console.WriteLine(
                    String.Format("An error occurred while acquiring a token\nTime: {0}\nError: {1}\n",
                    DateTime.Now.ToString(),
                    ee.ToString()));
                return;
            }

            DirectoryInfo dir = new DirectoryInfo(_configuration["DataPath"]);
            FileInfo[] files = dir.GetFiles("*.json");

            foreach (FileInfo f in files)
            {
                Console.WriteLine($"Processing file: {f.Name}");
                using (StreamReader reader = File.OpenText(f.FullName))
                {
                    JObject bundle = (JObject)JToken.ReadFrom(new JsonTextReader(reader));
                    bundle = (JObject)SyntheaConverter.ConvertUUIDs(bundle);

                    JArray entries = (JArray)bundle["entry"];
                    for (int i = 0; i < entries.Count; i++)
                    {
                        var entry_json = ((JObject)entries[i])["resource"].ToString();
                        string resource_type = (string)((JObject)entries[i])["resource"]["resourceType"];
                        string id = (string)((JObject)entries[i])["resource"]["id"];

                        // If we already have a token, we should get the cached one, otherwise, refresh
                        authResult = _authContext.AcquireTokenAsync(_configuration["Audience"], clientCredential).Result;
                        client.DefaultRequestHeaders.Clear();
                        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + authResult.AccessToken);
                        StringContent content = new StringContent(entry_json, Encoding.UTF8, "application/json");

                        HttpResponseMessage uploadResult = null;
                        
                        if (String.IsNullOrEmpty(id)) 
                        {
                            uploadResult = await client.PostAsync($"/{resource_type}", content);
                        } 
                        else 
                        {
                            uploadResult = await client.PutAsync($"/{resource_type}/{id}", content);
                        }

                        if (!uploadResult.IsSuccessStatusCode)
                        {
                            string resultContent = await uploadResult.Content.ReadAsStringAsync();
                            Console.WriteLine(resultContent);
                        }
                    }
                }
            }
        }
    }
}
