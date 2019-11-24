﻿using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Fate.Infrastructure.Redis;
using Fate.Infrastructure.Redis.IRedisManage;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Fate.Domain.Model.Entities;
using System.Linq;

namespace Fate.XUnitTest
{
    public class RedisTest
    {
        IServiceCollection services = new ServiceCollection();

        private readonly IRedisOperationHelp redis;
        public RedisTest()
        {
            //注入redis仓储服务
            services.AddRedisRepository(options =>
            {
                options.Connection = "127.0.0.1:6379";
                options.RedisPrefix = new Fate.Infrastructure.Redis.RedisConfig.RedisPrefixKey();
            });
            redis = services.BuildServiceProvider().GetService<IRedisOperationHelp>();
        }
        [Fact]
        public void test()
        {
            var redis = ConnectionMultiplexer.Connect("127.0.0.1");
            ISubscriber subscriber = redis.GetSubscriber();
            //订阅
            subscriber.Subscribe("push", (chanel, msg) =>
            {
                Console.WriteLine(chanel);
                Console.WriteLine(msg);
            });
            //发布
            subscriber.Publish("push", "你好");
            Console.WriteLine("1");
        }
        [Fact]
        public async Task RedisTest1()
        {

            ConcurrentQueue<setting> settings1 = new ConcurrentQueue<setting>();

            Parallel.For(0, 1000000, (item) =>
            {
                settings1.Enqueue(new setting() { Contact = "1", Description = "1", DuringTime = "1", Integral = 1, Rule = "1" });
            });

            await redis.ListSetAsync<setting>("test", settings1.ToList());
        }

        [Fact]
        public async Task Pub()
        {

            await redis.SubscribeAsync("test", (msg, value) =>
             {
                 Console.WriteLine(value);
             });
        }

        [Fact]
        public async Task StringTest()
        {
            await redis.StringSetAsync("1", "1");
            await redis.StringGetAsync("1");
        }
        [Fact]
        public void Store()
        {
            redis.Store(new setting() { Description = "1" });
            //List<setting> list = new List<setting>();
            //for (int i = 0; i < 100000; i++)
            //{
            //    list.Add(new setting() { Id = i });
            //}
            //redis.StoreAll(list);
            // redis.DeleteAll<setting>();
        }
        [Fact]
        public void Remove()
        {
            redis.KeyRemove(new List<string>() { "test2", "zhang" });
        }
    }
}
