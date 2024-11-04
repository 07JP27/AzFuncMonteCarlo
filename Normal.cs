using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AzFuncMonteCarlo
{
    public class Normal
    {
        [Function("normal")]
        public async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var config = JsonSerializer.Deserialize<Config>(requestBody);
            if (config == null) return new BadRequestObjectResult("Please pass iteration and iteration count in the request body");

            var samplingPerIteration = config.SamplingPerIteration;
            var totalIterations = config.TotalIterations;

            var response = new Response();
            var startTime = DateTime.Now;

            for(int i = 0; i < totalIterations; i++)
            {

                var inCircleCount = 0;

                for (int j = 0; j < samplingPerIteration; j++)
                {
                    var (x, y) = Utility.GenerateRandomPoint();
                    if (Utility.InCircle(x, y))
                    {
                        inCircleCount++;
                    }
                }

                // 整数除算を避けるために、doubleにキャスト
                var pi = 4 * ((double)inCircleCount / (double)samplingPerIteration);
                response.Iterations.Add(pi);
            }

            var endTime = DateTime.Now;
            var duration = (endTime - startTime).TotalSeconds;
            response.DurationSecond = duration;
            response.SimulatedMedianValue = response.Iterations.Median();
            response.SimulatedAverageValue = response.Iterations.Average();
            response.SimulatedModeValue = response.Iterations.Mode();

            return new OkObjectResult(response);
        }
    }
}
