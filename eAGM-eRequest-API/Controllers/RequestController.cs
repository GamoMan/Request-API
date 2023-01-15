using eAGM_eRequest_API.Middleware;
using eAGM_eRequest_API.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.IdentityModel.Tokens;
using Models.Constants;
using Models.DBContext;
using System.Drawing;
using System.Drawing.Imaging;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;

namespace eAGM_eRequest_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RequestController : ControllerBase
    {
        private readonly ILogger<RequestController> _logger;
        private readonly IWebHostEnvironment _env;

        private readonly IConfiguration _configuration;
        private readonly IDistributedCache _cache;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly HttpClient _client,_client2;


        private const int CaptchaLength = 6;
        private const int CaptchaWidth = 130;
        private const int CaptchaHeight = 50;
        private const int NoiseCount = 50;
        private const int NoiseSize = 15;
        private readonly Random _random = new Random();


        private readonly eAGM_RequestContext _context;

        public RequestController(IWebHostEnvironment env,ILogger<RequestController> logger, IDistributedCache cache, IConfiguration config, IHttpClientFactory httpClientFactory, eAGM_RequestContext context)
        {
            _logger = logger;
            _env = env;
            _cache = cache;
            _configuration = config;
            _context = context;
            _httpClientFactory = httpClientFactory;
            _client = _httpClientFactory.CreateClient("externalapi");
            _client2 = _httpClientFactory.CreateClient("internalapi");
        }

        [HttpGet]
        [Route("/captcha")]

        public async Task<IActionResult> GetCaptcha()
        {
            #region Logging

            _logger.LogInformation($"[{this.GetType().Name}] GetCaptcha");

            #endregion            
            
            // generate the captcha text

            try
            {
                var captchaText = new string(Enumerable.Repeat("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789", CaptchaLength)
                    .Select(s => s[_random.Next(s.Length)]).ToArray());               
                var image = GenerateCaptchaImage(captchaText);
                // store the captcha text in the cache
                string sessionid = Guid.NewGuid().ToString();

                await _cache.SetStringAsync(sessionid, captchaText, new DistributedCacheEntryOptions() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5), SlidingExpiration = TimeSpan.FromMinutes(Convert.ToDouble(_configuration["Expired:Captcha"])) });
                return Ok(ResponseModel<CaptchaResponse>.Success(new CaptchaResponse() { sessionid = sessionid, image = image }, code: ResponseDesc.RES_CODE_SUCCESS, description: ResponseDesc.RES_DESC_SUCCESS));  //new CaptchaResponse { sessionid = sessionid, Image = image };
            }
            catch (Exception ex)
            {
                var message = $"[{this.GetType().Name}]  GetCaptcha exception " + ex.Message;
                _logger.LogError(ex, message);

                if (_env.IsDevelopment())
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, ResponseModel<dynamic>.Error(ResponseDesc.RES_CODE_GENERAL_ERROR, ResponseDesc.RES_DESC_GENERAL_ERROR));
                }

                return StatusCode(StatusCodes.Status500InternalServerError, ResponseModel<dynamic>.Error(ResponseDesc.RES_CODE_GENERAL_ERROR));
            }
        }

        [HttpGet]
        [Route("/captcha/{session-id}/renew")]

        public async Task<IActionResult> RenewCaptcha([FromRoute(Name = "session-id")] string sessionid)
        {
            #region Logging

            _logger.LogInformation($"[{this.GetType().Name}] RenewCaptcha");

            #endregion

            // generate the captcha text

            try
            {
                if (sessionid.Length <= 40)
                {
                    var captchaText = new string(Enumerable.Repeat("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789", CaptchaLength)
                        .Select(s => s[_random.Next(s.Length)]).ToArray());
                    var image = GenerateCaptchaImage(captchaText);
                    // store the captcha text in the cache
                    //string sessionid = Guid.NewGuid().ToString();
                    await _cache.RemoveAsync(sessionid);
                    await _cache.SetStringAsync(sessionid, captchaText, new DistributedCacheEntryOptions() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5), SlidingExpiration = TimeSpan.FromMinutes(5) });
                    return Ok(ResponseModel<CaptchaResponse>.Success(new CaptchaResponse() { sessionid = sessionid, image = image }, code: ResponseDesc.RES_CODE_SUCCESS, description: ResponseDesc.RES_DESC_SUCCESS));  //new CaptchaResponse { sessionid = sessionid, Image = image };
                } else
                {
                    if (_env.IsDevelopment())
                    {
                        return StatusCode(StatusCodes.Status401Unauthorized, ResponseModel<dynamic>.Error(code: ResponseDesc.RES_CODE_MODEL_INVALID, description: ResponseDesc.RES_DESC_MODEL_INVALID));
                    }

                    return StatusCode(StatusCodes.Status401Unauthorized, ResponseModel<dynamic>.Error(code: ResponseDesc.RES_CODE_MODEL_INVALID));
                }
            }
            catch (Exception ex)
            {
                var message = $"[{this.GetType().Name}]  GetCaptcha exception " + ex.Message;
                _logger.LogError(ex, message);

                if (_env.IsDevelopment())
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, ResponseModel<dynamic>.Error(ResponseDesc.RES_CODE_GENERAL_ERROR, ResponseDesc.RES_DESC_GENERAL_ERROR));
                }

                return StatusCode(StatusCodes.Status500InternalServerError, ResponseModel<dynamic>.Error(ResponseDesc.RES_CODE_GENERAL_ERROR));
            }
        }


        [HttpPost]
        [Route("/captcha/{session-id}/{captcha-code}/validate")]
        public async Task<IActionResult> ValidateCaptcha([FromRoute(Name = "session-id")] string sessionid, [FromRoute(Name = "captcha-code")] string captchacode)
        {
            #region Logging

            _logger.LogInformation($"[{this.GetType().Name}] ValidateCaptcha Session={0} Captcha Code ={1}",sessionid,captchacode);

            #endregion       
            try
            {
                // retrieve the captcha text from the cache
                var cachedCaptcha = await _cache.GetStringAsync(sessionid);
                if (cachedCaptcha != null && cachedCaptcha == captchacode)
                {
                        //generate JWT
                        await _cache.RemoveAsync(sessionid);
                        string token = GenerateJWT(sessionid);
                        await _cache.SetStringAsync(sessionid, token, new DistributedCacheEntryOptions() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(Convert.ToDouble(_configuration["Expired:Token"])), SlidingExpiration = TimeSpan.FromMinutes(Convert.ToDouble(_configuration["Expired:Token"])) });

                        return Ok(ResponseModel<ValidateCaptchaResponse>.Success(new ValidateCaptchaResponse() { token = token }, code: ResponseDesc.RES_CODE_SUCCESS, description: ResponseDesc.RES_DESC_SUCCESS));
                }
                else
                {
                    _logger.LogTrace($"[{this.GetType().Name}] ValidateCaptcha: code {ResponseDesc.RES_CODE_CAPTCHACODE_EMPTY}, description: {ResponseDesc.RES_DESC_CAPTCHACODE_EMPTY}");

                    if (_env.IsDevelopment())
                    {
                        return StatusCode(StatusCodes.Status401Unauthorized, ResponseModel<dynamic>.Error(code: ResponseDesc.RES_CODE_CAPTCHACODE_EMPTY, description: ResponseDesc.RES_DESC_CAPTCHACODE_EMPTY));
                    }

                    return StatusCode(StatusCodes.Status401Unauthorized, ResponseModel<dynamic>.Error(code: ResponseDesc.RES_CODE_CAPTCHACODE_EMPTY));
                }
            }
            catch(Exception ex)
            {
                var message = $"[{this.GetType().Name}]  ValidateCaptcha exception " + ex.Message;
                _logger.LogError(ex, message);

                if (_env.IsDevelopment())
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, ResponseModel<dynamic>.Error(ResponseDesc.RES_CODE_GENERAL_ERROR, ResponseDesc.RES_DESC_GENERAL_ERROR));
                }

                return StatusCode(StatusCodes.Status500InternalServerError, ResponseModel<dynamic>.Error(ResponseDesc.RES_CODE_GENERAL_ERROR));
            }
        }


        [Authorize(Roles = "Requester")]
        [AuthorizeToken]
        [HttpPost]
        [Route("/otp/{mobile-no}")]
        public async Task<IActionResult> GetOTP([FromRoute(Name = "mobile-no")] string mobileno)
        {
            Regex rg = new Regex("^(0[689]{1})+([0-9]{8})+$");
            // full validation and check before saving
            //...
            try
            {
                if (rg.IsMatch(mobileno))
                {
                    string url = _configuration["ServiceOption:route:otp_request"];
                    //HttpClient client = new HttpClient();
                    //client.BaseAddress = new Uri(_configuration["ServiceOption:base_url"]);
                    HttpResponseMessage response = await _client.GetAsync(url + $"/?Mobile={mobileno}");
                    response.EnsureSuccessStatusCode();
                    var data = await response.Content.ReadAsStringAsync();

                    return Ok(ResponseModel<GetOTPResponse>.Success(new GetOTPResponse()
                    { data = data }, code: ResponseDesc.RES_CODE_SUCCESS, description: ResponseDesc.RES_DESC_SUCCESS));
                }
                else
                {
                    if (_env.IsDevelopment())
                    {
                        return StatusCode(StatusCodes.Status401Unauthorized, ResponseModel<dynamic>.Error(code: ResponseDesc.RES_CODE_MODEL_INVALID, description: ResponseDesc.RES_DESC_MODEL_INVALID));
                    }

                    return StatusCode(StatusCodes.Status401Unauthorized, ResponseModel<dynamic>.Error(code: ResponseDesc.RES_CODE_MODEL_INVALID));
                }
            }
            catch (Exception ex)
            {
                var message = $"[{this.GetType().Name}]  GetOTP exception " + ex.Message;
                _logger.LogError(ex, message);

                if (_env.IsDevelopment())
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, ResponseModel<dynamic>.Error(ResponseDesc.RES_CODE_EX_ERROR, ResponseDesc.RES_DESC_EX_ERROR));
                }

                return StatusCode(StatusCodes.Status500InternalServerError, ResponseModel<dynamic>.Error(ResponseDesc.RES_CODE_EX_ERROR));
            }
        }

        [Authorize(Roles = "Requester")]
        [AuthorizeToken]
        [HttpPost]
        [Route("/otp2/{mobile-no}")]
        public async Task<IActionResult> GetOTP2([FromRoute(Name = "mobile-no")] string mobileno)
        {
            Regex rg = new Regex("^(0[689]{1})+([0-9]{8})+$");
            // full validation and check before saving
            //...
            try
            {
                if (rg.IsMatch(mobileno))
                {
                    string url = _configuration["ServiceOption:route:otp_request"];
                    //HttpClient client = new HttpClient();
                    //client.BaseAddress = new Uri(_configuration["ServiceOption:base_url"]);
                    HttpResponseMessage response = await _client2.GetAsync(url + $"?Mobile={mobileno}");
                    response.EnsureSuccessStatusCode();
                    var data = await response.Content.ReadAsStringAsync();

                    return Ok(ResponseModel<GetOTPResponse>.Success(new GetOTPResponse()
                    { data = data }, code: ResponseDesc.RES_CODE_SUCCESS, description: ResponseDesc.RES_DESC_SUCCESS));
                }
                else
                {
                    if (_env.IsDevelopment())
                    {
                        return StatusCode(StatusCodes.Status401Unauthorized, ResponseModel<dynamic>.Error(code: ResponseDesc.RES_CODE_MODEL_INVALID, description: ResponseDesc.RES_DESC_MODEL_INVALID));
                    }

                    return StatusCode(StatusCodes.Status401Unauthorized, ResponseModel<dynamic>.Error(code: ResponseDesc.RES_CODE_MODEL_INVALID));
                }
            }
            catch (Exception ex)
            {
                var message = $"[{this.GetType().Name}]  GetOTP exception " + ex.Message;
                _logger.LogError(ex, message);

                if (_env.IsDevelopment())
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, ResponseModel<dynamic>.Error(ResponseDesc.RES_CODE_EX_ERROR, ResponseDesc.RES_DESC_EX_ERROR));
                }

                return StatusCode(StatusCodes.Status500InternalServerError, ResponseModel<dynamic>.Error(ResponseDesc.RES_CODE_EX_ERROR));
            }
        }

        [Authorize(Roles = "Requester")]
        [AuthorizeToken]
        [HttpPost]
        [Route("/otp/{mobile-no}/verify")]
        public async Task<IActionResult> VerifyOTP([FromRoute(Name = "mobile-no")] string mobileno)
        {
            Regex rg = new Regex("^(0[689]{1})+([0-9]{8})+$");
            // full validation and check before saving
            //...
            try
            {
                if (rg.IsMatch(mobileno))
                {
                    string url = _configuration["ServiceOption:route:otp_verify"];
                    //HttpClient client = new HttpClient();
                    //client.BaseAddress = new Uri(_configuration["ServiceOption:base_url"]);
                    HttpResponseMessage response = await _client.GetAsync(url + $"?Mobile={mobileno}");
                    response.EnsureSuccessStatusCode();
                    var data = await response.Content.ReadAsStringAsync();

                    return Ok(ResponseModel<GetOTPResponse>.Success(new GetOTPResponse()
                    { data = data }, code: ResponseDesc.RES_CODE_SUCCESS, description: ResponseDesc.RES_DESC_SUCCESS));
                }
                else
                {
                    if (_env.IsDevelopment())
                    {
                        return StatusCode(StatusCodes.Status401Unauthorized, ResponseModel<dynamic>.Error(code: ResponseDesc.RES_CODE_MODEL_INVALID, description: ResponseDesc.RES_DESC_MODEL_INVALID));
                    }

                    return StatusCode(StatusCodes.Status401Unauthorized, ResponseModel<dynamic>.Error(code: ResponseDesc.RES_CODE_MODEL_INVALID));
                }
            }
            catch (Exception ex)
            {
                var message = $"[{this.GetType().Name}]  GetOTP exception " + ex.Message;
                _logger.LogError(ex, message);

                if (_env.IsDevelopment())
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, ResponseModel<dynamic>.Error(ResponseDesc.RES_CODE_EX_ERROR, ResponseDesc.RES_DESC_EX_ERROR));
                }

                return StatusCode(StatusCodes.Status500InternalServerError, ResponseModel<dynamic>.Error(ResponseDesc.RES_CODE_EX_ERROR));
            }
        }

        [Authorize(Roles = "Requester")]
        [AuthorizeToken]
        [HttpPost]
        [Route("/upload-files")]
        public async Task<IActionResult> UploadFiles(IFormFile[] files)
        {
            if (files == null || files.Length == 0)
            {
                if (_env.IsDevelopment())
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, ResponseModel<dynamic>.Error(code: ResponseDesc.RES_CODE_MODEL_INVALID, description: ResponseDesc.RES_DESC_MODEL_INVALID));
                }

                return StatusCode(StatusCodes.Status401Unauthorized, ResponseModel<dynamic>.Error(code: ResponseDesc.RES_CODE_MODEL_INVALID));
            }
            using (IDbContextTransaction transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    foreach (var file in files)
                    {
                        // Ensure the file is not empty
                        if (file.Length == 0)
                        {
                            if (_env.IsDevelopment())
                            {
                                return StatusCode(StatusCodes.Status401Unauthorized, ResponseModel<dynamic>.Error(code: ResponseDesc.RES_CODE_FILE_EMPTY, description: ResponseDesc.RES_DESC_FILE_EMPTY));
                            }

                            return StatusCode(StatusCodes.Status401Unauthorized, ResponseModel<dynamic>.Error(code: ResponseDesc.RES_CODE_FILE_EMPTY));
                        }

                        // Ensure the file is less than 1 MB
                        if (file.Length > 1 * 1024 * 1024)
                        {
                            if (_env.IsDevelopment())
                            {
                                return StatusCode(StatusCodes.Status401Unauthorized, ResponseModel<dynamic>.Error(code: ResponseDesc.RES_CODE_FILESIZE_INVALID, description: ResponseDesc.RES_DESC_FILESIZE_INVALID));
                            }

                            return StatusCode(StatusCodes.Status401Unauthorized, ResponseModel<dynamic>.Error(code: ResponseDesc.RES_CODE_FILESIZE_INVALID));
                        }

                        // Read the file content into a byte array
                        byte[] fileContent;
                        using (var stream = new MemoryStream())
                        {
                            await file.CopyToAsync(stream);
                            fileContent = stream.ToArray();
                        }

                        if (!VerifyFileHeader(fileContent))
                        {
                            if (_env.IsDevelopment())
                            {
                                return StatusCode(StatusCodes.Status401Unauthorized, ResponseModel<dynamic>.Error(code: ResponseDesc.RES_CODE_FILETYPE_INVALID, description: ResponseDesc.RES_DESC_FILETYPE_INVALID));
                            }

                            return StatusCode(StatusCodes.Status401Unauthorized, ResponseModel<dynamic>.Error(code: ResponseDesc.RES_CODE_FILETYPE_INVALID));
                        }
                        // Create a new FileModel object and set its properties
                        var fileModel = new UploadFile();
                        fileModel.ID = Guid.NewGuid();
                        fileModel.FileName = file.FileName;
                        fileModel.ContentType = file.ContentType;
                        fileModel.CreatedDate = DateTime.Now;
                        fileModel.FileContent = fileContent;
                        // Add the FileModel object to the context
                        _context.UploadFile.Add(fileModel);
                        // Save the changes to the database
                    }
                    await _context.SaveChangesAsync();
                    transaction.Commit();

                    return Ok(ResponseModel<FileUploadResponse>.Success(new FileUploadResponse() { message = "Files uploaded successfully" }, code: ResponseDesc.RES_CODE_SUCCESS, description: ResponseDesc.RES_DESC_SUCCESS)); ;
                } 
                catch (Exception ex)
                {
                    transaction.Rollback();
                    var message = $"[{this.GetType().Name}]  UploadFiles exception " + ex.Message;
                    _logger.LogError(ex, message);

                    if (_env.IsDevelopment())
                    {
                        return StatusCode(StatusCodes.Status500InternalServerError, ResponseModel<dynamic>.Error(ResponseDesc.RES_CODE_GENERAL_ERROR, ResponseDesc.RES_DESC_GENERAL_ERROR));
                    }

                    return StatusCode(StatusCodes.Status500InternalServerError, ResponseModel<dynamic>.Error(ResponseDesc.RES_CODE_GENERAL_ERROR));
                }
            }
        }
        [Authorize(Roles = "Requester")]
        [AuthorizeToken]
        [HttpGet]
        [Route("/test/{message}")]
        public IActionResult GetAdminData(string message)
        {
            // Protected code for admin
            return Ok($"Here is my {message}");
        }

        #region Library
        private byte[] GenerateCaptchaImage(string captchaText)
        {
            // generate the captcha image
            using (var bitmap = new Bitmap(CaptchaWidth, CaptchaHeight))
            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.White);

                int low = 180, high = 255;
                var nRend = _random.Next(high) % (high - low) + low;
                var nGreen = _random.Next(high) % (high - low) + low;
                var nBlue = _random.Next(high) % (high - low) + low;
                var backColor = new SolidBrush(Color.FromArgb((byte)nRend, (byte)nGreen, (byte)nBlue));
                //var background = new SolidBrush(Color.FromArgb(_random.Next(255), _random.Next(255), _random.Next(255)));
                g.FillRectangle(backColor, 0, 0, bitmap.Width, bitmap.Height);

                // draw each character with a random color
                var x = 0f;
                var y = CaptchaHeight/2-15;
                foreach (var c in captchaText)
                {
                    int lo = 0, hi = 180;
                    var nRed = _random.Next(hi) % (hi - lo) + lo;
                    var nGrn = _random.Next(hi) % (hi - lo) + lo;
                    var nBlu = _random.Next(hi) % (hi - lo) + lo;
                    var brush = new SolidBrush(Color.FromArgb((byte)nRed, (byte)nGrn, (byte)nBlu));
                    //var brush = new SolidBrush(Color.FromArgb(_random.Next(255), _random.Next(255), _random.Next(255)));
                    g.DrawString(c.ToString(), new Font("Arial", 20, FontStyle.Bold), brush, x, y);
                    x += 20;
                }
                // add noise to the image
                for (int i = 0; i < NoiseCount; i++)
                {
                    var pen = new Pen(Color.FromArgb(_random.Next(160), _random.Next(100), _random.Next(160)));
                    var x1 = _random.Next(CaptchaWidth);
                    var y1 = _random.Next(CaptchaHeight);
                    var x2 = x1 + _random.Next(NoiseSize) - (NoiseSize / 2);
                    var y2 = y1 + _random.Next(NoiseSize) - (NoiseSize / 2);
                    g.DrawLine(pen, x1, y1, x2, y2);
                }
                using (var stream = new MemoryStream())
                {
                    bitmap.Save(stream, ImageFormat.Png);
                    return stream.ToArray();
                }
            }
        }
        private string GenerateJWT(string sessionid)
        {
            // Define the JWT security key
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            // Create the JWT token
            var header = new JwtHeader(credentials);

            // Claims
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, sessionid),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role, "Requester")

            };

            // Payload
            var payload = new JwtPayload
            (
                issuer: _configuration["Jwt:issuer"],
                audience: _configuration["Jwt:audience"],
                claims: claims,
                notBefore: DateTime.Now,
                expires: DateTime.Now.AddMinutes(Convert.ToDouble(_configuration["Expired:Token"]))
            );

            // Create the JWT
            var jwt = new JwtSecurityToken(header, payload);

            // Serialize the JWT
            var jwtHandler = new JwtSecurityTokenHandler();
            return jwtHandler.WriteToken(jwt);
        }

        private bool VerifyFileHeader(byte[] fileContent)
        {
            // check the file header
            if (fileContent[0] == 0xFF && fileContent[1] == 0xD8 && fileContent[2] == 0xFF)
            {
                // file is a JPEG
                return true;
            }
            else if (fileContent[0] == 0x89 && fileContent[1] == 0x50 && fileContent[2] == 0x4E && fileContent[3] == 0x47 && fileContent[4] == 0x0D && fileContent[5] == 0x0A && fileContent[6] == 0x1A && fileContent[7] == 0x0A)
            {
                // file is a PNG
                return true;
            }
            else if (fileContent[0] == 0x25 && fileContent[1] == 0x50 && fileContent[2] == 0x44 && fileContent[3] == 0x46 && fileContent[4] == 0x2D)
            {
                // file is a PDF
                return true;
            }
            return false;
        }
        #endregion Library
    }
}
