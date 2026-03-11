using Customer.WebApi.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json;

namespace Customer.WebApi.Filter
{
    public class AuthFilter : IActionFilter, IResultFilter
    {
        private readonly JsonSerializerOptions _jsonSerializerOptions;
        private readonly ILogger<AuthFilter> _logger;
        private readonly IConfiguration _configuration;
        public AuthFilter(JsonSerializerOptions myJsonOptions, ILogger<AuthFilter> logger, IConfiguration configuration)
        {
            _jsonSerializerOptions = myJsonOptions;
            this._logger = logger;
            _configuration = configuration; // 从配置文件中获取 API 密钥
        }
        //在方法执行之前
        public void OnActionExecuting(ActionExecutingContext context)
        {
            var RequestIpAddress = context.HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString();
            string requestId = Guid.NewGuid().ToString("N");
            context.HttpContext.Request.EnableBuffering();
            string body = context.ActionArguments != null ? System.Text.Json.JsonSerializer.Serialize(context.ActionArguments, _jsonSerializerOptions) : "";
            //context.HttpContext.Request.Body.Position = 0;
            _logger.LogInformation("request ip:{ip},body: {body}", RequestIpAddress, body);

            var modelState = context.ModelState;
            if (!modelState.IsValid)
            {
                StringBuilder errorMsg = new StringBuilder();

                foreach (var item in context.ModelState)
                {
                    foreach (var error in item.Value.Errors)
                    {
                        //errorMsg.Append(item.Key + ":" + error.ErrorMessage);
                        errorMsg.Append(error.ErrorMessage + ";");
                    }
                }
                context.Result = new BadRequestObjectResult(
                     BaseResponse.Error(StatusCodes.Status400BadRequest, errorMsg.ToString())
                );
                return;
            }
            // todo token check
            //var authHeader = context.HttpContext.Request.Headers["Authorization"].FirstOrDefault();
            //if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
            //{
            //    var token = authHeader.Substring("Bearer ".Length);
            //    var tokenHandler = new JwtSecurityTokenHandler();
            //    var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]);
            //    try
            //    {
            //        tokenHandler.ValidateToken(token, new TokenValidationParameters
            //        {
            //            ValidateIssuer = true,
            //            ValidateAudience = true,
            //            ValidateLifetime = true,
            //            ValidateIssuerSigningKey = true,
            //            ValidIssuer = _configuration["Jwt:Issuer"],
            //            ValidAudience = _configuration["Jwt:Audience"],
            //            IssuerSigningKey = new SymmetricSecurityKey(key)
            //        }, out SecurityToken validatedToken);
            //    }
            //    catch
            //    {
            //        context.Result = new UnauthorizedObjectResult(BaseResponse.Error(StatusCodes.Status401Unauthorized, "Invalid token"));
            //        return;
            //    }
            //}
            //else if (context.HttpContext.Request.Path.StartsWithSegments("/api"))  // 如果需要token但未提供
            //{
            //    context.Result = new UnauthorizedObjectResult(BaseResponse.Error(StatusCodes.Status401Unauthorized, "Token required"));
            //    return;
            //}
            BaseResponse baseResponse = CheckSign(context);
            if (baseResponse.code != BaseResponse.SuccessCode)
            {
                context.Result = new BadRequestObjectResult(baseResponse);

            }



        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
        }

        //  after action execute response
        public void OnResultExecuted(ResultExecutedContext context)
        {
            //var result = context.Result;

            //  _logger.LogInformation("response:{} ", JsonConvert.SerializeObject(result));


        }
        //  before action execute response
        public void OnResultExecuting(ResultExecutingContext context)
        {
        }

        public BaseResponse CheckSign(ActionExecutingContext actionContext)
        {

            HttpContext context = actionContext.HttpContext;
            string path = context.Request.Path.ToString();
            if (path.StartsWith("/api"))
            {
                return BaseResponse.Success();
            }
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                var timestamp = context.Request.Headers["timestamp"];
                var nonce = context.Request.Headers["nonce"];
                var signature = context.Request.Headers["sign"];
                var appid = context.Request.Headers["appid"];
                string appkey;
                //时间搓校验
                if (context.Request.Path.StartsWithSegments("/api/manger"))
                {
                    appkey = _configuration.GetValue<string>("MANAGERAPI:" + appid);
                    long nowTime = DateTimeUtils.GetUnixSecondsTime();
                    //请求时间超过10分钟有效，则无效
                    if ((nowTime - long.Parse(timestamp)) > 60 * 10)
                    {
                        _logger.LogError("api sign is expired :{}, appid:{}", path, appid);
                        return BaseResponse.Error(StatusCodes.Status400BadRequest, "api sign is expired");
                    }
                }
                else
                {
                    appkey = _configuration.GetValue<string>("OPENAPI:" + appid);
                }

                if (string.IsNullOrEmpty(appid) || string.IsNullOrEmpty(timestamp) || string.IsNullOrEmpty(nonce) || string.IsNullOrEmpty(appkey))
                {
                    _logger.LogError("error not get appid or key where path:{}, appid:{}", path, appid);
                    return BaseResponse.Error(StatusCodes.Status400BadRequest, "no header param sign");
                }
                // 将参数按照字典序排序后拼接成字符串
                var paramString = $"{timestamp}{nonce}{appid}";
                var expectedSignature = HMACSHA256Utils.CalculateSignature(appkey, paramString);

                if (signature != expectedSignature)
                {
                    _logger.LogError("sign error  where path:{}, appid:{}", path, appid);
                    return BaseResponse.Error(StatusCodes.Status401Unauthorized, "sign error");
                }

            }
            return BaseResponse.Success();

        }

    }
}
