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
            return new JsonResult(IntervalData.Status);
        }

        [HttpGet("Enable")]
        public IActionResult Enable()
        {
            IntervalData.Status.CxAutoSignEnabled = true;
            return Ok(new { code = 0, msg = "success" });
        }

        [HttpGet("Disable")]
        public IActionResult Disable()
        {
            IntervalData.Status.CxAutoSignEnabled = false;
            return Ok(new { code = 0, msg = "success" });
        }

    }
}
