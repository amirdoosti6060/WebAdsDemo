using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.Metrics;
using System.Net;
using System.Net.WebSockets;
using TwinCAT.Ads;
using TwinCAT.TypeSystem;
using WebAdsDemo.Services;

namespace WebAdsDemo.Controllers
{
    [Route("api/swg")]
    [ApiController]
    public class SwgController : ControllerBase
    {
        private readonly IAdsService _adsService;
        //private readonly WebSocket _ws;
        private readonly ILogger<SwgController> _logger;

        public SwgController(IAdsService adsService, ILogger<SwgController> logger)
        {
            _logger = logger;
            _adsService = adsService;
        }

        [HttpGet("readcounter")]
        public IActionResult ReadCounter()
        {
            try
            {
                uint value = _adsService.ReadValue<uint>("MAIN.nCounter");
                _logger.LogInformation("MAIN.nCounter read successfully.");

                return Ok(value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode((int) HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [HttpPost("writecounter")]
        public ActionResult WriteCounter([FromBody] uint counter)
        {
            try
            {
                _adsService.WriteValue<uint>("MAIN.nCounter", counter);
                _logger.LogInformation($"{counter} is written!");
                return Ok();

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [HttpPost("startstop")]
        public ActionResult StartStop([FromBody] bool start)
        {
            try
            {
                _adsService.WriteValue<bool>("MAIN.bStartStop", start);
                _logger.LogInformation($"{start} is written!");

                return Ok();

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
            }
        }
    }
}
