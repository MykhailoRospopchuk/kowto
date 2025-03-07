using System.Net.Http.Json;
using ScrapperHttpFunction.ResultContainer;

namespace ScrapperHttpFunction.Wrappers;

public class ClientWrapper
{
    private HttpClient _client;

    public ClientWrapper(HttpClient client)
    {
        _client = client;
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

            var result = new ContainerResult<TReturn>();

            if(response.Content.Headers.ContentType.MediaType != "application/json")
            {
                var content = await response.Content.ReadAsStringAsync();
                result.Value = (TReturn)Convert.ChangeType(content, typeof(TReturn));
                result.Success = true;
            }
            else
            {
                var responceValue = await response.Content.ReadFromJsonAsync<TReturn>();
                result.Value = responceValue;
                result.Success = true;
            }

            return result;            
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
}
