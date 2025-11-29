using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace BPSR_DeepsServ
{
    public class Program
    {
        public static DiscordWebHookManager DiscordWebHooks = new DiscordWebHookManager();

        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateSlimBuilder(args);

            builder.Services.ConfigureHttpJsonOptions(options =>
            {
                options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
            });

            var app = builder.Build();

            var reportsAPI = app.MapGroup("/report");
            reportsAPI.MapPost("/discord", HandleDiscordReport).DisableAntiforgery();

            app.Run();
        }

        static async Task<IResult> HandleDiscordReport([FromForm] string report, [FromForm] IFormFile img)
        {
            try
            {
                var reportData = JsonSerializer.Deserialize(report, AppJsonSerializerContext.Default.EncounterReport);
                if (reportData == null)
                {
                    return Results.BadRequest("No report data");
                }
                
                var reportStatus = await DiscordWebHooks.ProcessEncounterReport(reportData, img);
                if (reportStatus)
                {
                    return Results.Ok();
                }
                else
                {
                    return Results.Ok("Already reported");
                }
            }
            catch (Exception ex)
            {
                return Results.InternalServerError();
            }
        }
    }
}
