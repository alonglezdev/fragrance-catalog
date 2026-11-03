using System.Net;

namespace FragranceCatalog.Infrastructure.Tests.Suppliers.Magna;

internal sealed class StubHttpHandler : HttpMessageHandler
{
    private readonly Dictionary<string, Func<CancellationToken, Task<HttpResponseMessage>>> _responses = new(StringComparer.Ordinal);

    public List<string> RequestedUrls { get; } = [];

    public StubHttpHandler Respond(string url, string content)
    {
        _responses[url] = _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(content) });
        return this;
    }

    public StubHttpHandler Fail(string url, HttpStatusCode statusCode)
    {
        _responses[url] = _ => Task.FromResult(new HttpResponseMessage(statusCode));
        return this;
    }

    public StubHttpHandler Throw(string url, Exception exception)
    {
        _responses[url] = _ => Task.FromException<HttpResponseMessage>(exception);
        return this;
    }

    public StubHttpHandler Hang(string url)
    {
        _responses[url] = async cancellationToken =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        };
        return this;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var url = request.RequestUri!.ToString();
        RequestedUrls.Add(url);
        return _responses.TryGetValue(url, out var respond)
            ? respond(cancellationToken)
            : Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }
}
