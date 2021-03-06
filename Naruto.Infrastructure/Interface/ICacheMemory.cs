﻿
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Naruto.Infrastructure.Interface
{
    /// <summary>
    /// 进行.net 缓存的处理 实现依赖注入
    /// </summary>
    public interface ICacheMemory : ICommonDependency
    {
        /// <summary>
        /// 新增一个缓存
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        Task AddAsync<T>(string key, T value, DistributedCacheEntryOptions options);
        /// <summary>
        /// 获取指定key的缓存
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        Task<T> GetAsync<T>(string key) where T:class;
        /// <summary>
        /// 删除指定key
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        Task RemoveAsync(string key);
    }
}
