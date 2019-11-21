﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Autofac;

using Autofac.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Fate.Common.Repository.UnitOfWork;
using Fate.Common.Infrastructure;
using NLog.Extensions.Logging;
using NLog.Web;
using System.Text;
using Fate.Common.Middleware;
using Fate.Common.Repository;
using Fate.Domain.Model.Entities;
using Fate.Common.Redis;
using Fate.Common.BaseRibbitMQ;
using Fate.Common.Repository.Object;
using Fate.Common.Extensions;
using Fate.Common.Options;
using Fate.Common.Repository.Interceptor;
using System.Diagnostics;
using System.IO;
using Microsoft.AspNetCore.ResponseCompression;
using System.IO.Compression;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Fate.Domain.Model;
using Fate.Commom.Consul;
using Fate.Commom.Consul.ServiceRegister;
using Fate.Commom.Consul.ServiceDiscovery;
using Fate.Commom.Consul.KVRepository;
using System.Net;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using System.Reflection;
using Fate.Common.Configuration.Management.Dashboard;
using Fate.Common.Configuration.Management.DB;
using Fate.Common.Configuration.Management;
using Fate.Common.Redis.IRedisManage;

namespace Fate.Test
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }
        public IContainer ApplicationContainer { get; private set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {

            //注入响应压缩的服务
            //services.AddResponseCompression();
            //services.Configure<GzipCompressionProviderOptions>(options =>
            //{
            //    options.Level = CompressionLevel.Fastest;
            //});

            //注入redis仓储服务
            services.AddRedisRepository(Configuration.GetSection("AppSetting:RedisConfig"));
            //注入mysql仓储   //注入多个ef配置信息
            services.AddRepositoryServer().AddRepositoryEFOptionServer(options =>
            {
                options.ConfigureDbContext = context => context.UseMySql(Configuration.GetConnectionString("MysqlConnection"));
                options.ReadOnlyConnectionString = Configuration.GetConnectionString("ReadMysqlConnection").Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries);
                //
                options.UseEntityFramework<MysqlDbContent>();
                options.IsOpenMasterSlave = false;
            },
            configureOptions =>
            {
                configureOptions.ConfigureDbContext = context => context.UseMySql("Database=ConfigurationDB;DataSource=127.0.0.1;Port=3306;UserId=root;Password=hai123;Charset=utf8;");
                configureOptions.UseEntityFramework<ConfigurationDbContent>();
            }
            );

            //使用单号
            //services.UseOrderNo<IUnitOfWork<MysqlDbContent>>();

            //注入一个mini版的mvc 不需要包含Razor
            services.AddMvcCore(option =>
            {
                option.Filters.Add(typeof(Fate.Common.Filters.TokenAuthorizationAttribute));
            })
                           .AddConfigurationManagement(options =>
                           {
                               options.EnableDashBoard = true;
                               options.RequestOptions = new RequestOptions
                               {
                                   HttpMethod = "get",
                                   RequestPath = "/api/data",

                               };
                               options.EnableDataRoute = true;
                           })
            .AddAuthorization().AddJsonFormatters(options =>
            {
                options.DateFormatString = "yyyy-MM-dd HH:mm:ss";
            })
            .AddJsonFormatters()
            .SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
            ////注入api授权服务
            //services.AddAuthentication("Bearer").AddJwtBearer("Bearer", option =>
            //{
            //    option.Authority = "http://localhost:54717";
            //    option.RequireHttpsMetadata = false;
            //    option.Audience = "api";
            //});
            //services.AddScoped(typeof(List<>));
            //services.UseFileOptions();

            services.AddFateConfiguration();
            ////邮箱服务
            //services.AddEmailServer(Configuration.GetSection("AppSetting:EmailConfig"));
            //services.BuildServiceProvider()
            //services.AddSingleton<Domain.Event.Infrastructure.Redis.RedisStoreEventBus>();
            //替换自带的di 转换为autofac 注入程序集
            ApplicationContainer = Fate.Common.AutofacDependencyInjection.AutofacDI.ConvertToAutofac(services);
            return new AutofacServiceProvider(ApplicationContainer);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="app"></param>
        /// <param name="env"></param>
        /// <param name="loggerFactory"></param>
        /// <param name="options1"></param>
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            //var redis = app.ApplicationServices.GetRequiredService<IRedisOperationHelp>();
            //redis.Subscribe("changeConfiguration", (channel, redisvalue) =>
            //{

            //    Configuration["test"] = "2";
            //    var res = Configuration.AsEnumerable();
            //    Console.WriteLine("1");
            //});
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            //注入一场处理中间件
            app.UseMiddleware<ExceptionHandlerMiddleware>();
            // app.UseMiddleware<DashBoardMiddleware>();
            //app.UseAuthentication();

            //app.UseResponseCompression();

            //app.UseFileUpload(new Microsoft.AspNetCore.Http.PathString("/file"));
            ////配置NLog
            //Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);//这是为了防止中文乱码
            //loggerFactory.AddNLog();//添加NLog
            //env.ConfigureNLog("nlog.config");//读取Nlog配置文件

            app.UseMvc();
            app.Map("/hello", build =>
            {
                build.Run(async content =>
                {
                    var str = Encoding.UTF8.GetBytes(Configuration.GetValue<string>("test"));
                    await content.Response.Body.WriteAsync(str, 0, str.Length);
                });
            });

        }
    }
}
