using DDona.AzureComputerVision.Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Microsoft.Extensions.Options;

namespace DDona.AzureComputerVision.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomVisionController : ControllerBase
    {
        private readonly CustomVisionSettings _CustomVisionSettings;

        public CustomVisionController(IOptions<CustomVisionSettings> customVisitionSettingsOption)
        {
            _CustomVisionSettings = customVisitionSettingsOption.Value;
        }

        [HttpGet("send-request")]
        public async Task<ActionResult<string>> GetSendRequest()
        {
            ComputerVisionClient client = Authenticate();
            string operationId = await SendReadRequest(client);
            return Ok(operationId);
        }

        [HttpGet("get-request-result/{operationId}")]
        public async Task<ActionResult> GetRequestResult(string operationId)
        {
            ComputerVisionClient client = Authenticate();
            ReadOperationResult results = await client.GetReadResultAsync(Guid.Parse(operationId));

            switch (results.Status)
            {
                case OperationStatusCodes.Running:
                case OperationStatusCodes.NotStarted:
                    return Ok("waiting to be processed");
                case OperationStatusCodes.Failed:
                    return BadRequest("Operation Failed!");
                case OperationStatusCodes.Succeeded:
                    return Ok(GetData(results.AnalyzeResult.ReadResults));
                default:
                    return BadRequest("Invalid Operation Status Code");
            }
        }

        private ComputerVisionClient Authenticate()
        {
            return new ComputerVisionClient(new ApiKeyServiceClientCredentials(_CustomVisionSettings.Key))
            { 
                Endpoint = _CustomVisionSettings.EndpointUrl,
            };
        }

        private async Task<string> SendReadRequest(ComputerVisionClient client)
        {
            ReadHeaders headers = await client.ReadAsync(_CustomVisionSettings.ImageUrl, language: "pt");
            string operationLocation = headers.OperationLocation;
            const int numberOfCharsInOperationId = 36;
            return operationLocation.Substring(operationLocation.Length - numberOfCharsInOperationId);
        }

        private object? GetData(IList<ReadResult> readResults)
        {
            return System.Text.Json.JsonSerializer.Serialize(readResults);
        }
    }
}
