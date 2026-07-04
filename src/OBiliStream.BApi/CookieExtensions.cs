using System.Net;
using System.Text.Json;

namespace OBiliStream.BApi;

public static class CookieExtensions
{
    extension(CookieCollection collection)
    {
        public string CSRF => collection.FirstOrDefault(i => i.Name is "bili_jct")?.Value ?? throw new BiliApiException("Cannot found bili_jct in cookies.");
        public string Buvid3 => collection.FirstOrDefault(i => i.Name is "buvid3")?.Value ?? throw new BiliApiException("Cannot found buvid3 in cookies.");

        public async Task SaveToAsync(Stream stream, CancellationToken cancellationToken = default)
            => await JsonSerializer.SerializeAsync(
                stream,
                collection,
                OBiliStreamJsonSerializerContext.Default.CookieCollection,
                cancellationToken);

        public static async Task<CookieCollection> LoadFromAsync(Stream stream, CancellationToken cancellationToken = default)
            => await JsonSerializer.DeserializeAsync(
                stream,
                OBiliStreamJsonSerializerContext.Default.CookieCollection,
                cancellationToken)
                ?? throw new InvalidOperationException();
    }

    extension(CookieContainer container)
    {
        public string CSRF => container.GetCookies(new("https://live.bilibili.com")).CSRF;
        public string Buvid3 => container.GetCookies(new("https://live.bilibili.com")).Buvid3;

        public async Task SaveToAsync(Stream stream, CancellationToken cancellationToken = default)
            => await container.GetAllCookies().SaveToAsync(stream, cancellationToken);

        public async Task LoadFromAsync(Stream stream, CancellationToken cancellationToken = default)
            => container.Add(await CookieCollection.LoadFromAsync(stream, cancellationToken));
    }
}