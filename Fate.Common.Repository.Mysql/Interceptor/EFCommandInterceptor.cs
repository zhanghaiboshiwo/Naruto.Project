﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using System.Data;
using Microsoft.Extensions.Options;
using Fate.Common.Repository.Mysql.Base;
using System.Linq;
using Fate.Common.Redis.IRedisManage;
using System.Net.Sockets;
using System.Collections.Concurrent;

namespace Fate.Common.Repository.Mysql.Interceptor
{
    /// <summary>
    /// EF命令的跟踪
    /// </summary>
    public class EFCommandInterceptor : IObserver<KeyValuePair<string, object>>
    {
        public bool isSumbitTran = false;
        private IOptions<List<EFOptions>> options;
        private readonly object _lock = new object();


        public EFCommandInterceptor(IOptions<List<EFOptions>> _options)
        {
            options = _options;
        }
        public void OnCompleted()
        {

        }

        public void OnError(Exception error)
        {

        }

        public void OnNext(KeyValuePair<string, object> value)
        {
            lock (_lock)
            {
                //如果是主动开启事务 
                if (value.Key == RelationalEventId.TransactionStarted.Name && isSumbitTran == false)
                {

                    #region old zhanghaibo 2019-08-05
                    ////获取当前事务连接信息
                    //var connec = ((IDbConnection)((TransactionEventData)value.Value).Transaction.Connection);
                    ////获取连接的库的信息
                    //var database = connec.Database;
                    ////获取当前连接对应的ef配置连接信息
                    //var info = options.Value.Where(a => a.WriteReadConnectionName.ToLower().Contains($"database={database}") || a.ReadOnlyConnectionName.ToLower().Contains($"database={database}")).FirstOrDefault();
                    //if (info == null)
                    //{
                    //    throw new ArgumentNullException("找不到EF配置连接信息!");
                    //}
                    //if (connec.State == ConnectionState.Open)
                    //{
                    //    connec.Close();
                    //}
                    ////更改为主库的连接字符串
                    //connec.ConnectionString = info.WriteReadConnectionName;
                    ////
                    //if (connec.State == ConnectionState.Closed)
                    //{
                    //    connec.Open();
                    //}
                    #endregion

                    isSumbitTran = true;
                }
                //验证是否追踪到的是efcore 的 savechange
                else if (value.Key == CoreEventId.SaveChangesStarting.Name && isSumbitTran == false)
                {
                    var dbContext = ((DbContextEventData)value.Value).Context;

                    if (dbContext.Database.GetDbConnection().State == ConnectionState.Open)
                    {
                        dbContext.Database.GetDbConnection().Close();
                    }

                    //获取只读的连接字符串
                    dbContext.Database.GetDbConnection().ConnectionString = options.Value.Where(a => a.DbContextType == dbContext.GetType()).FirstOrDefault().WriteReadConnectionString;

                    //更改连接的状态
                    if (dbContext.Database.GetDbConnection().State == ConnectionState.Closed)
                    {
                        dbContext.Database.GetDbConnection().Open();
                    }
                }
                //跟踪执行脚本
                else if (value.Key == RelationalEventId.CommandExecuting.Name && isSumbitTran == false)
                {
                    var command = ((CommandEventData)value.Value).Command;
                    if (command.CommandText.StartsWith("SELECT"))
                    {
                        //获取当前连接信息
                        var connec = command.Connection;
                        //获取使用的库
                        var database = connec.Database;
                        //获取当前连接对应的ef配置连接信息
                        var info = options.Value.Where(a => a.WriteReadConnectionString.ToLower().Contains($"database={database}")).FirstOrDefault();
                        if (info == null)
                        {
                            throw new ArgumentNullException("找不到EF配置连接信息!");
                        }
                        if (connec.State == ConnectionState.Open)
                        {
                            connec.Close();
                        }

                        //更改为从库的连接字符串
                        var connections = SlaveConnection(info.DbContextType);
                        //
                        if (connec.State == ConnectionState.Closed)
                        {
                            connec.Open();
                        }
                    }
                }
                //上下文释放之后 并且状态还为处于事务的 状态
                else if (value.Key == CoreEventId.ContextDisposed.Name && isSumbitTran)
                {
                    //更改状态为未开启事务的状态
                    isSumbitTran = false;
                }
            }
        }

        /// <summary>
        /// 获取从库的连接字符串( 读取规则，当所有的从库无法使用的时候读取返回主库的连接字符串)
        /// </summary>
        /// <param name="dbContextType"></param>
        /// <returns></returns>
        private string SlaveConnection(Type dbContextType)
        {
            //获取从库的信息
            var slaveInfo = SlavePools.slaveConnec.Where(a => a.Key == dbContextType).Select(a => a.Value).FirstOrDefault().Where(a => a.IsAvailable).OrderBy(a => Guid.NewGuid()).FirstOrDefault();
            if (slaveInfo == null)
            {
                return options.Value.Where(a => a.DbContextType == dbContextType).Select(a => a.WriteReadConnectionString).FirstOrDefault();
            }
            //进行心跳检查
            var isBeat = SlavePools.HeartBeatCheck(dbContextType, slaveInfo);
            if (isBeat == false)
            {
                return SlaveConnection(dbContextType);
            }
            return slaveInfo.ConnectionString;
        }

    }
}
