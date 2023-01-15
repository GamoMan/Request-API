using eAGM_eRequest_API.Controllers;
using Microsoft.Extensions.Caching.Distributed;

namespace eAGM_eRequest_API.Service
{
    public class HttpClientService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _client;

        public HttpClientService(IConfiguration config, HttpClient client)
        {
            _configuration = config;
            _client = client;
            _client.BaseAddress = new Uri(_configuration["ServiceOption:base_url"]);
        }
    }
}
