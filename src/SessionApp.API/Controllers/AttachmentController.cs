using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SessionApp.Application.Common.Models;
using SessionApp.Application.Features.Attachments.Commands.UploadAttachment;

namespace SessionApp.API.Controllers;

[Authorize]
public class AttachmentController : ApiControllerBase
{
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(BaseResponse<string>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(BaseResponse<string>))]
    public async Task<ActionResult<BaseResponse<string>>> UploadAttachment(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(BaseResponse<string>.Failure("No file was uploaded or file is empty."));
        }

        using var stream = file.OpenReadStream();
        var result = await Mediator.Send(new UploadAttachmentCommand
        {
            FileStream = stream,
            FileName = file.FileName
        });

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}
