using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

namespace eAGM_eRequest_API.Middleware
{
    public class JwtMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;
        private readonly IDistributedCache _cache;
        //private readonly IUserService _userService;

        public JwtMiddleware(RequestDelegate next, IConfiguration configuration, IDistributedCache cache)
        {
            _next = next;
            _configuration = configuration;
            _cache = cache;
            //_userService = userService;
        }

        public async Task Invoke(HttpContext context)
        {
            var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            if (token != null)
                attachAccountToContext(context, token);
            await _next(context);
        }

        private async void attachAccountToContext(HttpContext context, string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                string strkey = _configuration["Jwt:Key"];
                var key = Encoding.ASCII.GetBytes(strkey);
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidAudience = _configuration["Jwt:Issuer"],
                    ValidIssuer = _configuration["Jwt:Audience"],
                    // set clockskew to zero so tokens expire exactly at token expiration time (instead of 5 minutes later)
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                var sessionId = jwtToken.Claims.First(x => x.Type == "sub").Value;
                //var cachetoken = await _cache.GetStringAsync(sessionId);
                // attach account to context on successful jwt validation
                context.Items["SessionID"] = sessionId;
            }
            catch
            {
                // do nothing if jwt validation fails
                // account is not attached to context so request won't have access to secure routes
                //context.Items["SessionID"] = "";
            }
        }
    }
}
