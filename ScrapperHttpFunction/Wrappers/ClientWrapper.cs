namespace ScrapperHttpFunction.Wrappers;

using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using ResultContainer;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

public class ClientWrapper
{
    private HttpClient _client;
    private readonly ILogger<ClientWrapper> _logger;
    private ResiliencePipeline _pipeline;
    private bool _withPipeline;

    public ClientWrapper(HttpClient client, ILogger<ClientWrapper> logger)
    {
        _client = client;
        _logger = logger;
        BuildResiliencePipeline();
    }

    public bool WithPipeline
    {
        set => _withPipeline = value;
    }

    private void BuildResiliencePipeline()
    {
        if (_pipeline == null)
        {
            _pipeline = new ResiliencePipelineBuilder()
                .AddRetry(new RetryStrategyOptions
                {
                    Delay = TimeSpan.FromSeconds(2),
                    MaxRetryAttempts = 3,
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                    ShouldHandle = args => args.Outcome switch
                    {
                        { Exception: TimeoutRejectedException } => PredicateResult.True(),
                        // { Exception: HttpRequestException } => PredicateResult.True(),
                        { Result: HttpResponseMessage response } when
                            response.StatusCode == HttpStatusCode.RequestTimeout => PredicateResult.True(),
                        { Result: HttpResponseMessage response } when
                            response.StatusCode == HttpStatusCode.TooManyRequests => PredicateResult.True(),
                        { Result: HttpResponseMessage response } when
                            (int)response.StatusCode >= 500 &&
                            (int)response.StatusCode <= 599 &&
                            response.StatusCode != HttpStatusCode.ServiceUnavailable => PredicateResult.True(),
                        { Exception: OperationCanceledException } => PredicateResult.False(),
                        _ => PredicateResult.False()
                    },
                    OnRetry = (args) =>
                    {
                        _logger.LogWarning($"Retry {args.AttemptNumber} encountered an error. Delaying for {args.Duration} seconds.");
                        return ValueTask.CompletedTask;
                    }
                })
                .AddCircuitBreaker(new CircuitBreakerStrategyOptions
                {
                    FailureRatio = 0.1,
                    SamplingDuration = TimeSpan.FromSeconds(5),
                    BreakDuration = TimeSpan.FromSeconds(5),
                    OnOpened = (args) =>
                    {
                        _logger.LogWarning($"Circuit breaker triggered OPENED. Breaking for {args.BreakDuration} seconds.");
                        return ValueTask.CompletedTask;
                    },
                    OnClosed = (args) =>
                    {
                        _logger.LogInformation("Circuit breaker CLOSED.");
                        return ValueTask.CompletedTask;
                    },
                    OnHalfOpened = (args) =>
                    {
                        _logger.LogInformation("Circuit breaker HALF-OPENED.");
                        return ValueTask.CompletedTask;
                    }
                })
                .AddTimeout(new TimeoutStrategyOptions
                {
                    Timeout = TimeSpan.FromSeconds(30),
                    OnTimeout = (args) =>
                    {
                        _logger.LogWarning($"Timeout after {args.Timeout} seconds.");
                        return ValueTask.CompletedTask;
                    }
                })
                .Build();
        }
    }

    public async Task<ContainerResult> PostAsync(string uri, HttpContent httpContent, CancellationToken cancellationToken)
    {
        var response = await ExecuteRequest(
            async (ct) => await _client.PostAsync(uri, httpContent, ct),
            cancellationToken);

        return response.sucess ?
            ProcessHttpResponse(response.message) : 
            new ContainerResult
            {
                Success = false,
            };
    }

    public async Task<ContainerResult<TReturn>> PostAsync<TReturn>(string uri, HttpContent httpContent, CancellationToken cancellationToken)
    {
        var response = await ExecuteRequest(
            async (ct) => await _client.PostAsync(uri, httpContent, ct),
            cancellationToken);

        return response.sucess ?
            await ProcessHttpResponse<TReturn>(response.message) : 
            new ContainerResult<TReturn>
            {
                Success = false,
            };
    }

    public async Task<ContainerResult<TReturn>> GetAsync<TReturn>(string uri, CancellationToken cancellationToken)
    {
        var response = await ExecuteRequest(
            async (ct) => await _client.GetAsync(uri, ct),
            cancellationToken);

        return response.sucess ?
            await ProcessHttpResponse<TReturn>(response.message) : 
            new ContainerResult<TReturn>
            {
                Success = false,
            };
    }

    private ContainerResult ProcessHttpResponse(HttpResponseMessage response)
    {
        try
        {
            response.EnsureSuccessStatusCode();

            return new ContainerResult()
            {
                Success = true
            };
        }
        catch(Exception e)
        {
            _logger.LogError(e.Message);
            return new ContainerResult()
            {
                Success = false,
            };
        }
    }

    private async Task<ContainerResult<TReturn>> ProcessHttpResponse<TReturn>(HttpResponseMessage response)
    {
        try
        {
            response.EnsureSuccessStatusCode();

            var result = new ContainerResult<TReturn>();

            if(response.Content.Headers.ContentType?.MediaType != "application/json")
            {
                var content = await response.Content.ReadAsStringAsync();
                result.Value = (TReturn)Convert.ChangeType(content, typeof(TReturn));
                result.Success = true;
            }
            else
            {
                var responseValue = await response.Content.ReadFromJsonAsync<TReturn>();
                result.Value = responseValue;
                result.Success = true;
            }

            return result;
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
            return new ContainerResult<TReturn>()
            {
                Success = false,
            };
        }
    }

    private async ValueTask<(bool sucess, HttpResponseMessage message)> ExecuteRequest(
        Func<CancellationToken, ValueTask<HttpResponseMessage>> action,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = _pipeline != null && _withPipeline
                ? await _pipeline.ExecuteAsync(action, cancellationToken)
                : await action(cancellationToken);
            return (true, result);
        }
        catch (Exception e) when (e is ExecutionRejectedException)
        {
            _logger.LogError(e.Message, "Request failed too many times please try again later.");
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
        }

        return (false, null);
    }
}
