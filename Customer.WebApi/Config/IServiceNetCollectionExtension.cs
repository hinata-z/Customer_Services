

﻿using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace Customer.WebApi.Config
{
    public static class IServiceNetCollectionExtension
    {
        public static void BatchRegisterServices(this IServiceCollection services, IConfiguration config)
        {

            //services.AddSingleton<ISqlSugarClient>(s =>
            //{
            //    SqlSugarScope sqlSugar = new SqlSugarScope(new ConnectionConfig()
            //    {
            //        DbType = SqlSugar.DbType.MySql,
            //        ConnectionString = config.GetConnectionString("CustomerDbConnnection"),
            //        IsAutoCloseConnection = true,
            //    },
            //   db =>
            //   {
            //       //单例参数配置，所有上下文生效
            //       db.Aop.OnLogExecuting = (sql, pars) =>
            //       {
            //           Console.WriteLine(sql);//输出sql,查看执行sql 性能无影响
            //           //获取IOC对象要求在一个上下文
            //           //var appServive = s.GetService<IHttpContextAccessor>();
            //           //var log= appServive?.HttpContext?.RequestServices.GetService<Log>();
            //       };
            //   }); ;
            //    return sqlSugar;
            //});


            // 批量注册所有Dapper DAO类为单例
            var types = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.Name.EndsWith("Repository") || t.Name.EndsWith("Service"));
            foreach (var type in types)
            {
                //var interfaceType = type.GetInterface($"IBaseRepository");

                if (type.Name.EndsWith("Service"))
                {
                    services.AddSingleton(type);
                }
                else
                {
                    if (!type.Name.StartsWith("IBase"))
                    {
                        services.AddSingleton(type);
                    }

                }


            }






        }

    }
}
