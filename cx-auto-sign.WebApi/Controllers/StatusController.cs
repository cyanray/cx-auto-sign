using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace cx_auto_sign.WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class StatusController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            var status = new Status()
            {
                Username = "631805010409",
                CxAutoSignEnabled = false
            };
            return new JsonResult(status);
        }
    }
}
