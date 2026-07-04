using System.Net.Mime;
using System.Text.Json;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

using OBiliStream;
using OBiliStream.BApi;
using OBiliStream.BApi.Event;
using OBiliStream.BApi.Models;
using OBiliStream.Model;
using OBiliStream.Services;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

var builder = WebApplication.CreateBuilder(options: new()
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory,
});

// Add services to the container.
builder.Services.AddHttpLogging();
builder.Services.AddHttpContextAccessor();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, OBiliStreamJsonSerializerContext.Default);
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

builder.Services
    .AddSingleton<BiliApiClient>()
    .AddOptions<BiliApiClientOptions>()
    .BindConfiguration("BiliLive");
builder.Services.AddTransient<BiliLiveEventClientProvider>();
builder.Services.AddTransient<BiliLoginClient>();
builder.Services
    .AddTransient<BiliLiveClient>()
    .AddOptions<BiliLiveClientOptions>()
    .BindConfiguration("BiliLive");

builder.Services.AddTransient<LiveEventServices>();

builder.Services.AddExceptionHandler<ApiExceptionHandler>();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseHttpLogging();

app.UseDefaultFiles();
app.UseStaticFiles();
app.MapStaticAssets();
app.MapFallbackToFile("/index.html");

app.UseExceptionHandler(options => options.Run(async context =>
{
    context.Response.StatusCode = 500;
    context.Response.ContentType = "text/plain";

    var exception = context.Features.Get<IExceptionHandlerFeature>();
    await context.Response.WriteAsync($"Server error: {exception?.Error.Message}");
}));

var bili = app.MapGroup("/bili");

bili.MapGet("/current", async (
    [FromServices] BiliApiClient client,
    CancellationToken cancellationToken) =>
    TypedResults.Ok(await client.GetPersonDataAsync(cancellationToken)));

var login = bili.MapGroup("/login");

login.MapGet("/url", async (
    [FromServices] BiliLoginClient login,
    CancellationToken cancellationToken)
    => TypedResults.Ok(await login.GenerateQRCodeAsync(cancellationToken)));

login.MapGet("/poll", async (
    [FromQuery] string qrcodeKey,
    [FromServices] BiliLoginClient login,
    [FromServices] IConfiguration configuration,
    CancellationToken cancellationToken)
    => TypedResults.Ok(await login.LoginWithQRCodeAsync(qrcodeKey, cancellationToken)));

var live = bili.MapGroup("/live");

live.MapGet("/info", async (
    [FromQuery] int roomId,
    [FromServices] BiliLiveClient live,
    CancellationToken cancellationToken)
    => TypedResults.Ok(await live.GetRoomInfoAsync(roomId, cancellationToken)));

live.MapGet("/infoByUid", async (
    [FromQuery] long userId,
    [FromServices] BiliLiveClient live,
    CancellationToken cancellationToken)
    => TypedResults.Ok(await live.GetRoomInfoOldAsync(userId, cancellationToken)));

live.MapGet("/areas", async (
    [FromServices] BiliLiveClient live,
    CancellationToken cancellationToken)
    => TypedResults.Ok(await live.GetAreaListAsync(cancellationToken)));

live.MapPost(
    "/start",
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status403Forbidden,
        MediaTypeNames.Application.ProblemJson,
        MediaTypeNames.Application.Json)]
async Task<Results<Ok<StartLiveData>, ProblemHttpResult>> (
        [FromBody] StartLiveRequest request,
        [FromServices] BiliLiveClient live,
        CancellationToken cancellationToken) =>
    {
        try
        {
            return TypedResults.Ok(await live.StartStreamAsync(request.RoomId, request.AreaId, cancellationToken: cancellationToken));
        }
        catch (BiliApiResultException e) when (e.Code is 60024 or 60043)
        {
            var result = e.DataResult.Deserialize(OBiliStreamJsonSerializerContext.Default.StartLiveData)
                ?? throw new BiliApiException("无法反序列化为对象", e);

            return TypedResults.Problem(
                title: e.Code.ToString(),
                detail: e.RawMessage,
                statusCode: StatusCodes.Status403Forbidden,
                extensions: new Dictionary<string, object?>
                {
                    ["qr"] = result.Qr,
                    ["data"] = result,
                });
        }
    });

live.MapPost("/stop", async (
    [FromBody] StartLiveRequest request,
    [FromServices] BiliLiveClient live,
    CancellationToken cancellationToken)
    => TypedResults.Ok(await live.StopStreamAsync(request.RoomId, cancellationToken: cancellationToken)));

live.MapPost("/update", async (
    [FromBody] UpdateLiveRequest request,
    [FromServices] BiliLiveClient live,
    CancellationToken cancellationToken)
    => TypedResults.Ok(await live.UpdateStreamInfoAsync(
        request.RoomId,
        request.Platform, request.VisitId,
        request.Title, request.AreaId,
        request.AddTag, request.DelTag,
        cancellationToken)));

live.MapPost(
    "/cover",
    async Task<Results<Ok, ValidationProblem>> (
    IFormFile file,
    [FromQuery] bool? force,
    [FromServices] BiliApiClient api,
    [FromServices] BiliLiveClient live,
    CancellationToken cancellationToken) =>
{
    await using MemoryStream ms = new();
    using var image = await Image.LoadAsync(file.OpenReadStream(), cancellationToken);
    if (force is not true && (double)image.Width / image.Height != 16 / 9d)
    {
        string errmsg = "图片长宽比必须为 16:9 ！";
        return TypedResults.ValidationProblem(
            [
                KeyValuePair.Create("IMAGE_SIZE", new[]{ errmsg }),
            ],
        errmsg);
    }
    if (force is true || image.Width > 704)
    {
        // rate as 16:9, max size as 704*396.
        image.Mutate(context => context.Resize(new ResizeOptions()
        {
            Mode = ResizeMode.Pad,
            Position = AnchorPositionMode.Center,
            Size = new(704, 396),
        }));
    }
    // 不支持 Webp
    await image.SaveAsJpegAsync(ms, cancellationToken: cancellationToken);
    ms.Seek(0, SeekOrigin.Begin);

    var fileContent = FileContent.Create(MediaTypeNames.Image.Jpeg, "blob", ms);
    var res = await api.UploadImage("live", "new_room_cover", fileContent, cancellationToken);
    await live.UpdatePreLiveInfo("web", "web", 1, cover: res.Location, coverVertical: string.Empty, cancellationToken: cancellationToken);
    return TypedResults.Ok();
}).DisableAntiforgery();

var chat = live.MapGroup("/chat");
chat.MapPost("/send", async (
    [FromBody] SendChatRequest request,
    [FromServices] BiliLiveClient live,
    CancellationToken cancellationToken)
    => TypedResults.Ok(await live.SendChatAsync(request.RoomId, request.Message, cancellationToken: cancellationToken)));

chat.MapGet("/event", async (
    HttpContext context,
    [FromQuery] int roomId,
    [FromQuery] long userId,
    [FromServices] LiveEventServices liveEventServices,
    CancellationToken cancellationToken)
    => TypedResults.ServerSentEvents(liveEventServices.GetLiveEventsAsync(roomId, userId, cancellationToken)));

app.Run();
