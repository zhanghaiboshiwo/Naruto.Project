﻿using Consul;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Fate.Commom.Consul;
using System.Net;
using Microsoft.Extensions.Options;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using Fate.Common.Repository;
using Fate.Common.Repository.UnitOfWork;

using Microsoft.EntityFrameworkCore;
using Fate.Domain.Model;
using Fate.Domain.Model.Entities;
using System.Threading.Tasks;
using Fate.Common.Repository.Interceptor;
using MySql.Data.MySqlClient;

namespace Fate.XUnitTest
{
    public class RepositoryTest
    {
        IServiceCollection services = new ServiceCollection();

        private DbContext dbContex;

        public RepositoryTest()
        {
            services.AddScoped(typeof(IRepositoryFactory), typeof(RepositoryFactory));
            //注入mysql仓储   //注入多个ef配置信息
            services.AddRepositoryServer().AddRepositoryEFOptionServer(options =>
            {
                options.ConfigureDbContext = context => context.UseMySql("Database=test;DataSource=127.0.0.1;Port=3306;UserId=root;Password=hks360;Charset=utf8;");
                options.ReadOnlyConnectionString = new string[] { "Database=test;DataSource=127.0.0.1;Port=3306;UserId=root;Password=hks360;Charset=utf8;" };
                //
                options.UseEntityFramework<MysqlDbContent>(true, 100);
                options.IsOpenMasterSlave = true;
            });
            services.AddScoped<EFCommandInterceptor>();
            services.AddScoped<EFDiagnosticListener>();
            DiagnosticListener.AllListeners.Subscribe(services.BuildServiceProvider().GetRequiredService<EFDiagnosticListener>());
        }
        [Fact]
        public void Test()
        {

            var iserverPri = services.BuildServiceProvider();

            //var u0k = iserverPri.GetRequiredService<MysqlDbContent>();


            //var re = iserverPri.GetRequiredService<IRepositoryFactory>();

            //re.dbContext = u0k;
            //dbContex = u0k;
            //u0k.Dispose();
            //var re2 = iserverPri.GetRequiredService<IRepositoryFactory>();

            var u0k = iserverPri.GetRequiredService<IUnitOfWork<MysqlDbContent>>();
            u0k.CommandTimeout = 40;
            u0k.Query<setting>().AsQueryable().OrderBy("Rule").ToList();
            u0k.Command<setting>().Add(new setting() { Id = new Random().Next(100000, 999999) });
            u0k.SaveChanges();
            var res2 = u0k.Query<setting>().AsQueryable().OrderBy("Rule").ToList();
            var res = u0k.Query<setting>().AsQueryable().FirstOrDefault();
        }
        [Fact]
        public async Task bulkAdd()
        {
            var unit = services.BuildServiceProvider().GetRequiredService<IUnitOfWork<MysqlDbContent>>();
            ConcurrentQueue<setting> settings1 = new ConcurrentQueue<setting>();

            Parallel.For(0, 10000, (item) =>
            {
                settings1.Enqueue(new setting() { Contact = "1", Description = "1", DuringTime = "1", Integral = 1, Rule = "1" });
            });
            await unit.Command<setting>().BulkAddAsync(settings1);
            await unit.SaveChangeAsync();
        }
        [Fact]
        public async Task Query()
        {
            var unit = services.BuildServiceProvider().GetRequiredService<IUnitOfWork<MysqlDbContent>>();
            //
            // await unit.ChangeReadOrWriteConnection(Common.Repository.Object.ReadWriteEnum.Read);
            // await unit.ChangeDataBase("test1");
            var str = await unit.Query<setting>().AsQueryable().AsNoTracking().ToListAsync();
        }

        [Fact]
        public async Task ChangeDataBase()
        {
            var unit = services.BuildServiceProvider().GetRequiredService<IUnitOfWork<MysqlDbContent>>();
            unit.CommandTimeout = 40;
            await unit.ChangeDataBase("test1");

            var str = await unit.Query<setting>().AsQueryable().ToListAsync();
            await unit.Command<setting>().AddAsync(new setting() { Contact = "1", Description = "1", DuringTime = "1", Integral = 1, Rule = "1" });
            await unit.SaveChangeAsync();
            str = await unit.Query<setting>().AsQueryable().ToListAsync();
            await unit.ChangeDataBase("test");
            str = await unit.Query<setting>().AsQueryable().ToListAsync();

        }

        [Fact]
        public async Task WriteRead()
        {
            var unit = services.BuildServiceProvider().GetRequiredService<IUnitOfWork<MysqlDbContent>>();
            var str = await unit.Query<setting>().AsQueryable().AsNoTracking().ToListAsync();
            str = await unit.Query<setting>().AsQueryable().AsNoTracking().ToListAsync();
            await unit.Command<setting>().AddAsync(new setting() { Contact = "1", Description = "1", DuringTime = "1", Integral = 1, Rule = "1" });
            await unit.SaveChangeAsync();
            str = await unit.Query<setting>().AsQueryable().ToListAsync();

        }
        [Fact]
        public async Task Tran()
        {
            var unit = services.BuildServiceProvider().GetRequiredService<IUnitOfWork<MysqlDbContent>>();

            unit.BeginTransaction();
            await unit.ChangeDataBase("test1");
            var str = await unit.Query<setting>().AsQueryable().ToListAsync();
            await unit.Command<setting>().AddAsync(new setting() { Contact = "1", Description = "1", DuringTime = "1", Integral = 1, Rule = "1" });
            await unit.SaveChangeAsync();
            //str = await unit.Query<setting>().AsQueryable().ToListAsync();
            //await unit.ChangeDataBase("test");
            //str = await unit.Query<setting>().AsQueryable().ToListAsync();
            unit.CommitTransaction();
        }

        [Fact]
        public async Task Tran2()
        {
            var unit = services.BuildServiceProvider().GetRequiredService<IUnitOfWork<MysqlDbContent>>();

            await unit.BeginTransactionAsync();

            await unit.Command<setting>().AddAsync(new setting() { Contact = "1", Description = "1", DuringTime = "1", Integral = 1, Rule = "1" });

            await unit.SaveChangeAsync();
            unit.CommitTransaction();
        }

        [Fact]
        public void DataTableTest()
        {
            var unit = services.BuildServiceProvider().GetRequiredService<IUnitOfWork<MysqlDbContent>>();
            var dt = unit.SqlQuery().ExecuteSqlQuery("select  * from setting");

        }
        [Fact]
        public async Task DataTableAsyncTest()
        {
            var unit = services.BuildServiceProvider().GetRequiredService<IUnitOfWork<MysqlDbContent>>();
            await unit.ChangeDataBase("test1");
            var dt = await unit.SqlQuery().ExecuteSqlQueryAsync("select  * from setting");

        }

        [Fact]
        public async Task ExecSqlTest()
        {
            var unit = services.BuildServiceProvider().GetRequiredService<IUnitOfWork<MysqlDbContent>>();
            unit.BeginTransaction();
            var res = await unit.SqlCommand().ExecuteNonQueryAsync("delete from setting");
            unit.CommitTransaction();
        }
        [Fact]
        public async Task ExecuteScalarAsync()
        {
            var unit = services.BuildServiceProvider().GetRequiredService<IUnitOfWork<MysqlDbContent>>();
            var query = unit.SqlQuery();
            var res = await query.ExecuteScalarAsync<int>("select Id from setting where Id=@id and Rule=@rule", new MySqlParameter[] { new MySqlParameter("id", "1"), new MySqlParameter("rule", "2") });
        }
    }


    public interface IRepositoryFactory
    {
        DbContext dbContext { get; set; }
    }
    public class RepositoryFactory : IRepositoryFactory
    {
        public DbContext dbContext { get; set; }
    }

}
