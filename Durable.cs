using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.DurableTask.Client;
using Microsoft.DurableTask;
using Microsoft.Azure.Functions.Worker.Http;

namespace AzFuncMonteCarlo
{

    public class Durable
    {
        [Function("durable")]
        public async Task<HttpResponseData> Starter([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req,[DurableClient] DurableTaskClient client)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var config = JsonSerializer.Deserialize<Config>(requestBody);
            if (config == null)
            {
                var badRequestResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                return badRequestResponse;
            }

            var instanceId = await client.ScheduleNewOrchestrationInstanceAsync(nameof(Orchestration), config);
            var metadata = await client.WaitForInstanceCompletionAsync(instanceId, getInputsAndOutputs: true);

            var res = HttpResponseData.CreateResponse(req);
            await res.WriteAsJsonAsync(metadata.ReadOutputAs<Response>());
            return res;
        }

        [Function(nameof(Orchestration))] 
        public async Task<Response> Orchestration([OrchestrationTrigger] TaskOrchestrationContext context, Config config)
        {
            var response = new Response();
            var startTime = context.CurrentUtcDateTime;

            List<Task<double>> parallelTasks = new List<Task<double>>();
            for (int i = 0; i < config.TotalIterations; i++)
            {
                Task<double> task = context.CallActivityAsync<double>(nameof(IterationActivity), config.SamplingPerIteration);
                parallelTasks.Add(task);
            }

            await Task.WhenAll(parallelTasks);
            response.Iterations = parallelTasks.Select(x => x.Result).ToList();

            var endTime = context.CurrentUtcDateTime;
            var duration = (endTime - startTime).TotalSeconds;
            response.DurationSecond = duration;
            response.SimulatedMedianValue = response.Iterations.Median();
            response.SimulatedAverageValue = response.Iterations.Average();
            response.SimulatedModeValue = response.Iterations.Mode();

            // implementation
            return response;
        }

        [Function(nameof(IterationActivity))] 
        public double IterationActivity([ActivityTrigger] double samplingPerIteration)
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
            return pi;
        }
    }
}