using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

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
            reportsAPI.MapPost("/discord/{id}/{token}", HandleDiscordReport).DisableAntiforgery();

            app.Run();
        }

        static async Task<IResult> HandleDiscordReport([FromRoute] string id, [FromRoute] string token, [FromForm] string payload_json, [FromForm] IFormFileCollection files, HttpRequest request)
        {
            try
            {
                if (request.Headers.TryGetValue("X-ZDPS-TeamId", out var teamIdStr))
                {
                    if (ulong.TryParse(teamIdStr, out var teamId))
                    {
                        var result = await DiscordWebHooks.ProcessEncounterReport(id, token, teamId, payload_json, files);
                        return result.IsSuccessStatusCode ? Results.Ok(result) : Results.BadRequest(result);
                    }
                    else
                    {
                        return Results.Ok("Nope");
                    }
                }
                else
                {
                    return Results.Ok("Nope");
                }
            }
            catch (Exception ex)
            {
                return Results.InternalServerError();
            }
        }
    }
}
