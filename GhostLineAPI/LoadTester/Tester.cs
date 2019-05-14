using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace LoadTester
{
    public class Tester
    {
        public async Task<String> RunTest()
        {

                Console.WriteLine($"Trying ...");
                // Create a New HttpClient object and dispose it when done, so the app doesn't leak resources
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Authorization", "27bc5f2c-bed5-41c7-8a5d-aec966212146");
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Authorization", "27bc5f2c-bed5-41c7-8a5d-aec966212146");

                    // Call asynchronous network methods in a try/catch block to handle exceptions
                    try
                    {

                        HttpResponseMessage httpResponse = await client.GetAsync("http://127.0.0.1:19001/UntrainedElkDogs");
                        httpResponse.EnsureSuccessStatusCode();
                        string responseBody = await httpResponse.Content.ReadAsStringAsync();
                        // Above three lines can be replaced with new helper method below
                        // string responseBody = await client.GetStringAsync(uri);

                        Console.WriteLine(responseBody);
                    }
                    catch (HttpRequestException e)
                    {
                        Console.WriteLine("\nException Caught!");
                        Console.WriteLine("Message :{0} ", e.Message);
                    }
                }


            return "DONE";
        }

        public async Task<String> RunTestNoWait()
        {
            // Create a New HttpClient object and dispose it when done, so the app doesn't leak resources
            for (int i = 0; i < 1000; i++)
            {

                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Authorization", "27bc5f2c-bed5-41c7-8a5d-aec966212146");
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Authorization", "27bc5f2c-bed5-41c7-8a5d-aec966212146");

                    // Call asynchronous network methods in a try/catch block to handle exceptions
                    try
                    {

                        HttpResponseMessage httpResponse = await client.GetAsync("http://127.0.0.1:19001/UntrainedElkDogs");
                        httpResponse.EnsureSuccessStatusCode();
                        //string responseBody = await httpResponse.Content.ReadAsStringAsync();
                        // Above three lines can be replaced with new helper method below
                        // string responseBody = await client.GetStringAsync(uri);

                        //Console.WriteLine(responseBody);
                    }
                    catch (HttpRequestException e)
                    {
                        Console.WriteLine("\nException Caught!");
                        Console.WriteLine("Message :{0} ", e.Message);
                    }
                }

            }

            return "DONE";
        }
    }
}
