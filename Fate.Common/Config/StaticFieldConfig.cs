﻿using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Fate.Common.Infrastructure;

namespace Fate.Common.Config
{
    /// <summary>
    /// 此地存放静态字段
    /// </summary>
    public class StaticFieldConfig
    {
        /// <summary>
        /// 文件的上传的根地址
        /// </summary>
        public static string UploadFilePath
        {
            get
            {
                //获取文件上传保存的地址
                var uploadFilePath = ConfigurationManage.GetAppSetting("UploadFilePath");
                //如果没有填写就默认 当前项目的根目录
                if (string.IsNullOrWhiteSpace(uploadFilePath))
                    uploadFilePath = Directory.GetCurrentDirectory();
                //合并地址
                var path = Path.Combine(uploadFilePath, LowerFolderPath);
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                return path;
            }
        }
        /// <summary>
        /// 文件上传保存的文件夹的名字
        /// </summary>
        public static string LowerFolderPath
        {
            get
            {
                //获取文件需要存放的文件夹名称
                var lowerFolderPath = ConfigurationManage.GetAppSetting("LowerFolderPath");
                if (string.IsNullOrWhiteSpace(lowerFolderPath))
                    lowerFolderPath = "UploadFile";
                return lowerFolderPath;
            }
        }
        /// <summary>
        /// 接口访问文件请求的路径
        /// </summary>
        public static string FileRequestPathName = "staticfile";

        /// <summary>
        /// 按天单号的缓存的集合
        /// </summary>
        public const string OrderNOByDayCacheList = "ordernodaylist";

        /// <summary>
        /// 按月单号的缓存的集合
        /// </summary>
        public const string OrderNOByMonthCacheList = "ordernomonthlist";

        /// <summary>
        /// 当单号达到多少数量的时候 往单号的集合添加数据
        /// </summary>
        public const int OrderNOMinLength = 1000;
        /// <summary>
        /// 单号追加时的最大的数量
        /// </summary>
        public const int OrderNOMaxLength = 10000;
    }
}
