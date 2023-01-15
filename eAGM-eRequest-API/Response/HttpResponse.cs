using System.Net;

namespace Models.JsonResponse
{
    public class HttpResponse<T>
    {
        public T? data { get; set; }

        public HttpStatusCode statusCode { get; set; }
        public string? statusMessage { get; set; }
    }
}
