using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HelloWorldController : ControllerBase
    {
        /// <summary>
        /// Gets the default greeting message.
        /// </summary>
        /// <returns>The greeting message.</returns>
        [HttpGet]
        [Route("Index")]
        [ProducesResponseType(typeof(string), 200)]
        public IActionResult Get()
        {
            return Ok("Hello, World!");
        }

        /// <summary>
        /// Gets information about the endpoint.
        /// </summary>
        /// <returns>The information message.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(string), 200)]
        public IActionResult GetInfo()
        {
            return Ok("This is the info endpoint.");
        }
    }
}
