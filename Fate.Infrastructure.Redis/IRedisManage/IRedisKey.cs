﻿using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Fate.Infrastructure.Redis.IRedisManage
{
    /// <summary>
    /// key的操作
    /// </summary>
    public interface IRedisKey : IRedisDependency
    {
        #region 同步

        /// <summary>
        /// 移除key
        /// </summary>
        /// <param name="key"></param>
        void KeyRemove(string key, KeyOperatorEnum keyOperatorEnum = default, CommandFlags flags = CommandFlags.None);
        /// <summary>
        /// 移除key
        /// </summary>
        /// <param name="key"></param>
        void KeyRemove(List<string> key, KeyOperatorEnum keyOperatorEnum = default, CommandFlags flags = CommandFlags.None);
        /// <summary>
        /// 判断key是否存在
        /// </summary>
        /// <param name="key"></param>
        bool KeyExists(string key, KeyOperatorEnum keyOperatorEnum = default, CommandFlags flags = CommandFlags.None);

        #endregion

        #region 异步
        /// <summary>
        /// 移除key
        /// </summary>
        /// <param name="key"></param>
        Task<bool> KeyRemoveAsync(string key, KeyOperatorEnum keyOperatorEnum = default, CommandFlags flags = CommandFlags.None);
        /// <summary>
        /// 移除key
        /// </summary>
        /// <param name="key"></param>
        Task<long> KeyRemoveAsync(List<string> key, KeyOperatorEnum keyOperatorEnum = default, CommandFlags flags = CommandFlags.None);
        /// <summary>
        /// 判断key是否存在
        /// </summary>
        /// <param name="key"></param>
        Task<bool> KeyExistsAsync(string key, KeyOperatorEnum keyOperatorEnum = default, CommandFlags flags = CommandFlags.None);
        #endregion
    }
}