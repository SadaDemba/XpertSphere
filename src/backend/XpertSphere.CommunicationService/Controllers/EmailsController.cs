using Microsoft.AspNetCore.Mvc;
using XpertSphere.CommunicationService.Dto;
using XpertSphere.CommunicationService.Exceptions;
using XpertSphere.CommunicationService.Services.Interfaces;

namespace XpertSphere.CommunicationService.Controllers;

[ApiController]
[Route("api/emails")]
public class EmailsController(IEmailService emailService, ILogger<EmailsController> logger) : ControllerBase
{
    [HttpPost("send")]
    [ProducesResponseType(typeof(SendEmailResponseDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(502)]
    public async Task<IActionResult> Send([FromBody] SendTemplatedEmailRequestDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var messageId = await emailService.SendTemplatedEmailAsync(dto.TemplateName, dto.To, dto.TemplateData, dto.Language, cancellationToken);
            return Ok(new SendEmailResponseDto { MessageId = messageId, Status = "Sent" });
        }
        catch (TemplateNotFoundException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (MissingTemplateVariableException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (EmailSendException ex)
        {
            logger.LogError(ex, "Failed to send templated email {TemplateName} to {To}", dto.TemplateName, dto.To);
            return StatusCode(502, new { message = "Failed to send email" });
        }
    }
}
