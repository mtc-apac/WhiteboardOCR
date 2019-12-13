using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OcrFunctionApp
{
    public static class ParseRecognizeTextJsonResponse
    {
        [FunctionName("ParseRecognizeTextJsonResponse")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            //get data from request body
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            //get array of lines object
            JArray linesData = data["recognitionResult"]["lines"];

            //Append each read line to a final output string
            var returnString = string.Empty;
            foreach (JObject item in linesData)
            {
                var text = (string)item["text"];
                returnString = returnString + text + "\n";
            }

            //Check for correctly formatted query before returning result
            return new OkObjectResult(returnString);
        }
    }
}
