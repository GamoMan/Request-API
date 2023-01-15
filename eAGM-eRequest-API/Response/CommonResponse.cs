namespace eAGM_eRequest_API.Response
{
    public class CaptchaResponse
    {
        public string sessionid { get; set; }
        public byte[] image { get; set; }

    }
    public class ValidateCaptchaResponse
    {
        public string token { get; set; }
    }

    public class FileUploadResponse
    {
        public string message { get; set; }
    }

    public class GetOTPResponse
    {
        public string data { get; set; }
    }
}
