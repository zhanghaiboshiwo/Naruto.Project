﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

namespace Fate.Commom.Consul.Object
{
    /// <summary>
    /// 服务的 实体
    /// </summary>
    public class Service
    {
        /// <summary>
        /// 服务id
        /// </summary>
        public string Id { get; }
        /// <summary>
        /// 服务名称
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// 版本
        /// </summary>
        public string Version { get; }
        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<string> Tags { get; }
        /// <summary>
        /// 主机地址
        /// </summary>
        public IPEndPoint HostAndPort { get; }
    }
}
