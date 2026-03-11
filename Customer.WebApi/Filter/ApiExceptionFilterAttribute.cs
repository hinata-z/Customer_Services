using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SunShine.Filter
{
    /// <summary>
    /// api异常统一处理过滤器
    /// </summary>
    public class ApiExceptionFilterAttribute : ExceptionFilterAttribute
    {

        private readonly ILogger<ApiExceptionFilterAttribute> _logger;

        public ApiExceptionFilterAttribute(ILogger<ApiExceptionFilterAttribute> loggerHelper)
        {
            _logger = loggerHelper;
        }

        //public override void OnException(ExceptionContext context)
        //{
            
        //    // 如果异常没有被处理则进行处理
        //    if (context.ExceptionHandled == false)
        //    {
        //        BadRequest baseResponse = null;
        //        int code = StatusCodes.Status500InternalServerError;
        //        if (context.Exception is MyException)
        //        {
        //            MyException exception = (MyException)context.Exception;
        //            baseResponse = exception.response;
        //        }
        //        else
        //        {
        //            baseResponse = BadRequest(context.Exception.Message);
        //        }
        //        //定义返回信息

        //        //写入日志
        //        _logger.LogError("error:{}:", context.Exception);
        //        context.Result = new ContentResult
        //        {
        //            // 返回状态码设置为200，表示成功
        //            StatusCode = baseResponse.code,
        //            // 设置返回格式
        //            ContentType = "application/json;charset=utf-8",
                    
        //            Content = JsonSerializer.Serialize(baseResponse)
        //        };
        //    }
        //    // 设置为true，表示异常已经被处理了
        //    context.ExceptionHandled = true;

        //}

    }
}
