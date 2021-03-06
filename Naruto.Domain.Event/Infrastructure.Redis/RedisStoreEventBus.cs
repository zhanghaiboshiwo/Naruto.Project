﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Naruto.AutofacDependencyInjection;
using Naruto.Redis.IRedisManage;
using Naruto.Redis.RedisConfig;
using Newtonsoft.Json;
using StackExchange.Redis;
using System.Reflection;
namespace Naruto.Domain.Event.Infrastructure.Redis
{
    /// <summary>
    /// 事件总线 Redis提供者
    /// </summary>
    public class RedisStoreEventBus : IEventBus
    {
        private IRedisOperationHelp redis;
        public RedisStoreEventBus(IRedisOperationHelp _redis)
        {
            redis = _redis;
        }
        /// <summary>
        /// 存放所有的事件的key
        /// </summary>
        private const string RedisEventHashKey = "RedisEventBus";
        /// <summary>
        /// 存放失败的事件
        /// </summary>
        private const string RedisEventFailHashKey = "RedisEventFailBus";
        /// <summary>
        /// 
        /// </summary>
        private static readonly object sync = new object();

        /// <summary>
        /// 事件的触发
        /// </summary>
        /// <typeparam name="TEvent"></typeparam>
        /// <param name="eventHandlerType"></param>
        /// <param name="eventData"></param>
        /// <returns></returns>
        public async Task Trigger<TEvent>(TEvent eventData) where TEvent : class, IEventData
        {
            //从缓存中读取需要触发的事件
            var type = eventData.GetType();
            var res = (await redis.RedisHash().GetAsync(RedisEventHashKey, type.ToString())).ToString().JsonToEvent() as IEventHandler<TEvent>;
            if (res != null)
            {
                ExecAction(async () =>
                {
                    //执行方法
                    await res.Handle(eventData);
                    //验证事件是否执行成功
                    if (eventData.IsFail)
                    {
                        //存储到失败的集合
                        await redis.RedisHash().AddAsync(RedisEventFailHashKey, JsonConvert.SerializeObject(eventData), res.GetType().FullName);
                    }
                    //else
                    //{
                    //    await redis.HashDeleteAsync(RedisEventHashKey, eventData.GetType().ToString());
                    //}
                });
            }
        }
        /// <summary>
        /// 执行委托事件
        /// </summary>
        /// <param name="action"></param>
        private void ExecAction(Action action)
        {
            action();
        }
        /// <summary>
        /// 取消注册事件
        /// </summary>
        /// <typeparam name="TEvent">事件实体</typeparam>
        /// <param name="handlerType">事件的动作</param>
        /// <returns></returns>
        public Task UnRegister<TEvent>() where TEvent : class, IEventData
        {
            lock (sync)
            {
                redis.RedisHash().DeleteAsync(RedisEventHashKey, typeof(TEvent).ToString());
            }
            return Task.CompletedTask;
        }
        /// <summary>
        /// 注册
        /// </summary>
        /// <typeparam name="TEvent">实体对象</typeparam>
        /// <param name="eventHandler">需要做的事情</param>
        /// <returns></returns>
        public Task Register<TEvent>(IEventHandler<TEvent> eventHandler) where TEvent : class, IEventData
        {
            lock (sync)
            {
                //将注册的事件写入redis缓存
                redis.RedisHash().AddAsync(RedisEventHashKey, typeof(TEvent).ToString(), eventHandler.EventToJson());
            }
            return Task.CompletedTask;
        }
        /// <summary>
        /// 从程序集注册所有事件 (可放在系统启动的时候 运行)
        /// </summary>
        /// <returns></returns>
        public Task RegisterAllFromAssembly()
        {
            //获取事件总线所在的程序集
            var assembly = Assembly.Load("Naruto.Domain.Event");
            //获取当前程序集的所有的类型
            if (assembly != null)
            {
                Type[] types = assembly.GetTypes();
                if (types != null && types.Count() > 0)
                {
                    //获取当前继承的接口 并且当前为class
                    foreach (var item in types.Where(a => a.GetInterface("IEventHandler") != null && a.IsClass && a.IsAbstract == false))
                    {
                        //获取方法
                        var method = item.GetMethod("Handle");
                        //获取方法的参数
                        var dataType = method.GetParameters()[0].ParameterType;
                        //获取执行的方法
                        var eventHandler = item.GetConstructor(Type.EmptyTypes).Invoke(null);
                        lock (sync)
                        {
                            //将注册的事件写入redis缓存
                            redis.RedisHash().AddAsync(RedisEventHashKey, dataType.ToString(), eventHandler.EventToJson());
                        }
                    }
                }
            }
            return Task.FromResult(0);
        }

        /// <summary>
        /// 清除所有的事件
        /// </summary>
        /// <returns></returns>
        public async Task ClearAllEvent()
        {
            if (await redis.RedisKey().ExistsAsync(RedisEventHashKey, Naruto.Redis.KeyOperatorEnum.Hash))
            {
                await redis.RedisKey().RemoveAsync(RedisEventHashKey, Naruto.Redis.KeyOperatorEnum.Hash);
            }
        }

        /// <summary>
        /// 处理失败的事件
        /// </summary>
        /// <returns></returns>
        public async Task HandleFailEvent()
        {
            //获取失败的信息
            var eventHandleList = await redis.RedisHash().GetAllAsync(RedisEventFailHashKey);
            if (eventHandleList != null && eventHandleList.Count() > 0)
            {
                foreach (var eventHandle in eventHandleList)
                {
                    ExecAction(() =>
                    {
                        //获得处理的类型
                        var handleType = Type.GetType(eventHandle.Value.ToString());
                        //获取方法
                        var method = handleType.GetMethod("Handle");
                        //获取实例
                        var obj = Activator.CreateInstance(handleType);
                        //获取方法的参数 因为方法只有一个参数所以默认取第一个
                        var param = method.GetParameters()[0];
                        //定义方法的参数
                        var oldInfo = JsonConvert.DeserializeObject(eventHandle.Key.ToString()).ToString();
                        /*
                         动态创建实例 并为当前实例 赋值 
                         注意 Activator.CreateInstance(s.ParameterType, oldInfo) 此方法的实现需要为事件实体 创建一个有参构造函数 进行动态赋值
                         */
                        object[] parameters = new object[] { Activator.CreateInstance(param.ParameterType, oldInfo) };
                        //执行方法
                        method.Invoke(obj, parameters);
                        //删除
                        redis.RedisHash().DeleteAsync(RedisEventFailHashKey, eventHandle.Key);
                    });
                }
            }
            //如果还存在的数据就递归执行
            if ((await redis.RedisHash().ValuesAsync(RedisEventFailHashKey)).Any())
            {
                await HandleFailEvent();
            }
        }
    }
}
