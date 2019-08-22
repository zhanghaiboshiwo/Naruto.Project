﻿
using Fate.Common.Redis.RedisConfig;
using Fate.Common.Repository.Base;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fate.Common.OcelotStore.EFCore
{
    public class CacheOptions
    {
        /// <summary>
        /// 存储的缓存中的key （默认为 ocelotef）
        /// </summary>
        internal string CacheKey { get; set; } = "ocelotef";

        /// <summary>
        /// ef的参数配置
        /// </summary>
        public Action<EFOptions> EFOptions { get; set; } = null;

        /// <summary>
        /// redis的参数配置
        /// </summary>
        public Action<RedisOptions> RedisOptions { get; set; } = null;
    }
}