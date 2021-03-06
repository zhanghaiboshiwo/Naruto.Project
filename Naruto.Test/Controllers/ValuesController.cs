﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Naruto.Application.Services;
using Naruto.Domain.Model;
using Naruto.Domain.Model.Entities;
using Naruto.Configuration.Management.DB;
using Naruto.Redis.IRedisManage;
using Naruto.Repository.UnitOfWork;
using Microsoft.AspNetCore.Mvc;

namespace Naruto.Test.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        public SettingApp settingApp { get; set; }

        public IRedisOperationHelp Redis { get; set; }
        private readonly MysqlDbContent mysqlDbContent;

        private readonly IUnitOfWork<MysqlDbContent> unitOfWork;
        public ValuesController(MysqlDbContent _mysqlDbContent, IUnitOfWork<MysqlDbContent> _unitOfWork,IUnitOfWork<ConfigurationDbContent> unitOf)
        {
            mysqlDbContent = _mysqlDbContent;
            unitOfWork = _unitOfWork;
        }
        // GET api/values
        [HttpGet]
        public ActionResult<IEnumerable<string>> Get()
        {
            //var db3 = mysqlDbContent.Clone();
            ////var re = iserverPri.GetRequiredService<IRepositoryFactory>();
            //var list22 = mysqlDbContent.test1.Where(a => 1 == 1).ToList();
            //mysqlDbContent.Dispose();

            //list22 = db3.test1.Where(a => 1 == 1).ToList();
            //db3.Dispose();
          
            // Redis.RedisString().Add(Guid.NewGuid().ToString(), "1");
            return new string[] { "value1", "value2" };
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public ActionResult<string> Get(int id)
        {
            return "value";
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody] string value)
        {

        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
