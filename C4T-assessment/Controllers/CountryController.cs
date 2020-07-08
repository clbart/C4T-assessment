using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace C4T_assessment.Controllers
{
    [ApiController]
    [Route("api")]
    public class CountryController : ControllerBase
    {
        private readonly ILogger<CountryController> _logger;
        private readonly HttpClient httpClient;
        private readonly Lazy<Task<QueueClient>> asyncClient;
        private  readonly QueueClient queueClient;


        const string ServiceBusConnectionString = "Endpoint=sb://bartcleeren.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=cWHxKBuzL10uwXpabfzLRh8+StVppAT5EtaUzV/Rk1c=";
        const string QueueName = "myQueue";

        public CountryController(ILogger<CountryController> logger)
        {
            _logger = logger;
            httpClient = new HttpClient();
            
            asyncClient = new Lazy<Task<QueueClient>>(async () =>
            {
                var managementClient = new ManagementClient(ServiceBusConnectionString);

                var allQueues = await managementClient.GetQueuesAsync();

                var foundQueue = allQueues.Where(q => q.Path == QueueName.ToLower()).SingleOrDefault();

                if (foundQueue == null)
                {
                    await managementClient.CreateQueueAsync(QueueName);//add queue desciption properties
                }


                return new QueueClient(ServiceBusConnectionString, QueueName);
            });

            queueClient = asyncClient.Value.Result;
        }




        private  async Task SendMessagesAsync(List<CountryModel> messages)
        {
            try
            {
                foreach (var country in messages.Select((value, index) => new { value, index }))
                {
                    // Create a new message to send to the queue.
                    string messageBody = $"Message {country.index}";
                    string countryString = JsonConvert.SerializeObject(country.value);
                    var message = new Message(Encoding.UTF8.GetBytes(countryString));

                    // Write the body of the message to the console.
                    Console.WriteLine($"Sending message: {countryString}");

                    // Send the message to the queue.
                    await queueClient.SendAsync(message);
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine($"{DateTime.Now} :: Exception: {exception.Message}");
            }


        }



        //Function to recieve country name and start next function to contact restcountries and recieve data from there, and then write this away to the 
        [HttpPost("enquiries")]
        public async Task<List<CountryModel>> Post([FromBody] CountryRequest country)
        {
            var userAgent = Request.Headers["User-Agent"];
            if ( country == null|| country.Name == null || country.Name.Length == 0 ||(country.Name.EndsWith(" ") && country.Name.StartsWith(" ")))
            {
                throw new ArgumentNullException("Argument is not defined"); 
            };
            var result = GetCountryData(country.Name).Result;
            await SendMessagesAsync(result);
            return result;
        }


        [HttpGet]
        public string Get()
        {
            return "succes";
        }

        private async Task<List<CountryModel>> GetCountryData(string name)
        {
            //list to add the results of the query to restcountries api
            List<CountryModel> countries = new List<CountryModel>();

            //get timestamp for when request is executed
            DateTime timestamp = DateTime.UtcNow;

            //make request to restcountries api
            var response = await httpClient.GetStringAsync("https://restcountries.eu/rest/v2/name/" + name + "?fields=translations;alpha2Code;regionalBlocs;name");

            //parse string response to an JArray, because response is in form of an array
            JArray joResponse = JArray.Parse(response);

            //Map date from response to country, in foreach loop, because possible to have multiple responses
            foreach (JObject result in joResponse)
            {
                CountryModel country = new CountryModel();
               
                // get and set translated name
                JObject translations = (JObject)result["translations"];
                if (translations.ContainsKey("nl"))
                {
                    country.Name = translations["nl"].ToString();
                }
                else
                {
                    country.Name = result["name"].ToString();
                }

                //get alpha2code
                country.Code = result["alpha2Code"].ToString();

                //browsername 
                country.BrowserName = Request.Headers["User-Agent"];
                //timestamp
                country.TimeStamp = timestamp;

                //regional blocs
                country.RegionalBlocs = new List<RegionalBloc>();
                JArray regionalBlocs = (JArray)result["regionalBlocs"];
                foreach (var bloc in regionalBlocs)
                {
                    RegionalBloc region = new RegionalBloc();
                    region.Name = bloc["name"].ToString();
                    region.Code = bloc["acronym"].ToString();
                    region.Countries = getCountriesOfRegionalBloc(region.Code).Result;
                    country.RegionalBlocs.Add(region);
                }

                countries.Add(country);

            }
            
           
            return countries;


        }

        //function to map the names of the countries belonging to a regional block
        private async Task<List<string>> getCountriesOfRegionalBloc(string acronym)
        {
            List<string> countriesReg = new List<string>();
            var response = await httpClient.GetStringAsync("https://restcountries.eu/rest/v2/regionalbloc/" + acronym + "?fields=translations;name");
            JArray joResponse = JArray.Parse(response);
            foreach (JObject result in joResponse)
            {
                JObject translations = (JObject)result["translations"];
                if (translations.ContainsKey("nl"))
                {
                    countriesReg.Add(translations["nl"].ToString());
                }
                else
                {
                    countriesReg.Add(result["name"].ToString());
                }
            }

            return countriesReg;
        }
        
    }
}
