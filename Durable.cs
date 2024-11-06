using Microsoft.Azure.Functions.Worker;
using Microsoft.AspNetCore.Http;
using Microsoft.DurableTask.Client;
using Microsoft.DurableTask;
using Microsoft.Azure.Functions.Worker.Http;

namespace AzFuncMonteCarlo
{

    public class Durable
    {
        [Function("durable")]
        public static async Task<HttpResponseData> Starter([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req,[DurableClient] DurableTaskClient client)
        {
            Config? config = await req.ReadFromJsonAsync<Config>();
            if (config is null) return req.CreateResponse(System.Net.HttpStatusCode.BadRequest);

            var instanceId = await client.ScheduleNewOrchestrationInstanceAsync(nameof(Orchestration), config);
            return await client.CreateCheckStatusResponseAsync(req, instanceId);
        }

        [Function(nameof(Orchestration))] 
        public static async Task<Response> Orchestration([OrchestrationTrigger] TaskOrchestrationContext context)
        {
            Config config = context.GetInput<Config>()!;
            var response = new Response();
            var startTime = context.CurrentUtcDateTime;

            List<Task<double>> parallelTasks = new List<Task<double>>();
            for (int i = 0; i < config.TotalIterations; i++)
            {
                Task<double> task = context.CallActivityAsync<double>(nameof(IterationActivity), config.SamplingPerIteration);
                parallelTasks.Add(task);
            }

           var iterations = (await Task.WhenAll(parallelTasks)).ToList();
            response.Iterations = iterations;

            var endTime = context.CurrentUtcDateTime;
            var duration = (endTime - startTime).TotalSeconds;
            response.DurationSecond = duration;
            response.SimulatedMedianValue = response.Iterations.Median();
            response.SimulatedAverageValue = response.Iterations.Average();
            response.SimulatedModeValue = response.Iterations.Mode();

            return response;
        }

        [Function(nameof(IterationActivity))] 
        public static double IterationActivity([ActivityTrigger] double samplingPerIteration)
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