using System.Net.Http.Json;
using ScrapperHttpFunction.ResultContainer;

namespace ScrapperHttpFunction.Wrappers;

public class ClientWrapper : IDisposable
{
    private HttpClient _client;

    private ClientWrapper()
    {
        _client = new HttpClient();
    }

    public static ClientWrapper GetInstance()
    {
        return new ClientWrapper();
    }

    public async Task<ContainerResult> PostAsync(string uri, HttpContent httpContent)
    {
        var response = await _client.PostAsync(uri, httpContent);
        return ProcessHttpResponse(response);
    }

    public async Task<ContainerResult<TReturn>> GetAsync<TReturn>(string uri)
    {
        var response = await _client.GetAsync(uri);
        return await ProcessHttpResponse<TReturn>(response);
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
            return new ContainerResult()
            {
                Exception = e,
                Success = false,
            };
        }
    }

    private async Task<ContainerResult<TReturn>> ProcessHttpResponse<TReturn>(HttpResponseMessage response)
    {
        try
        {
            response.EnsureSuccessStatusCode();


            // TODO: this one do not work
            if(typeof(TReturn).IsClass)
            {
                var result = await response.Content.ReadFromJsonAsync<TReturn>();
                return new ContainerResult<TReturn>
                {
                    Value = result,
                    Success = true
                };
            }

            if(typeof(TReturn) == typeof(string))
            {
                var result = await response.Content.ReadAsStringAsync();
                return new ContainerResult<TReturn>
                {
                    Value = (TReturn)Convert.ChangeType(result, typeof(TReturn)),
                    Success = true,

                };
            }
            
            var message = await response.Content.ReadAsStringAsync();

            return new ContainerResult<TReturn>()
            {
                Exception = new Exception(message),
                Success = false,
            };
            
        }
        catch (Exception e)
        {
            return new ContainerResult<TReturn>()
            {
                Exception = e,
                Success = false,
            };
        }
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}
