using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using StudentManagement.Models;
using System.Security.Claims;
using System.Net.Http;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Runtime.Caching;
using System.Web;

namespace StudentManagement.Controllers
{
    public class StudentController : Controller
    {
        public  readonly IConfiguration _configuration;
        public StudentController(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public async Task<IActionResult> WetherForeCast()
        {
            List<WeatherForecast> res = new List<WeatherForecast>();
            Uri baseUri = new Uri("http://localhost:5226/");
            using(var client = new HttpClient())
            {
                client.BaseAddress = baseUri;
                string bearerToken = GetJWTToken();
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + bearerToken);
                HttpResponseMessage response = await client.GetAsync(baseUri + "WeatherForecast");
                if(response != null)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();

                    res =  JsonConvert.DeserializeObject<List<WeatherForecast>>(jsonString);
                }
            }

            return View(res);
        }

        public string GetJWTToken()
        {
            bool isTokenCache = false;
            var cached = Cache.Get();
            var result = cached.GetCacheItem("CacheToken");
            if(result != null)
            {
                return result.Value.ToString();
            }
            else
            {
                var authClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name,_configuration["JWT:SecurityKey"]),
                    new Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString())
                };
                var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:SecretKey"]));
                var token = new JwtSecurityToken(
                    issuer: _configuration["JWT:ValidIssuer"],
                    audience: _configuration["JWT:ValidAudience"],
                    expires: DateTime.Now.AddHours(1),
                    claims: authClaims,
                    signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
                );

                var cacheToken = new JwtSecurityTokenHandler().WriteToken(token);
                var cache = new MemoryCache("Token");
                var cacheItem = new CacheItem("CacheToken", cacheToken);
                var cacheItemPolicy = new CacheItemPolicy
                {
                    AbsoluteExpiration = DateTimeOffset.Now.AddHours(1)
                };
                isTokenCache = cache.Add(cacheItem, cacheItemPolicy);

                return new JwtSecurityTokenHandler().WriteToken(token);
            }
            
        }
    }
}
