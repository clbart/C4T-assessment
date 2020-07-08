using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C4T_assessment.Controllers
{
    public class QueueConnector
    {
        private readonly Lazy<Task<QueueClient>> asyncClient;
        private readonly QueueClient queueClient;
        public QueueConnector(string connectionString, string queueName)
        {
            asyncClient = new Lazy<Task<QueueClient>>(async () =>
            {
                var managementClient = new ManagementClient(connectionString);

                var allQueues = await managementClient.GetQueuesAsync();

                var foundQueue = allQueues.Where(q => q.Path == queueName.ToLower()).SingleOrDefault();

                if (foundQueue == null)
                {
                    await managementClient.CreateQueueAsync(queueName);//add queue desciption properties
                }


                return new QueueClient(connectionString, queueName);
            });

            queueClient = asyncClient.Value.Result;
        }

        public async Task SendMessagesAsync(List<CountryModel> messages)
        {
            try
            {
                foreach (var country in messages.Select((value, index) => new { value, index }))
                {
                    // Create a new message to send to the queue.
                    string countryString = JsonConvert.SerializeObject(country.value);
                    var message = new Message(Encoding.UTF8.GetBytes(countryString));

                    // Write the body of the message to the console.
                    Console.WriteLine($"Sending message {country.index} : {countryString}");

                    // Send the message to the queue.

                    await queueClient.SendAsync(message);
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine($"{DateTime.Now} :: Exception: {exception.Message}");
            }


        }
    }
}
