using Microsoft.AspNetCore.Mvc;
using PDFtoImage;

namespace PdfToImageApi.Controllers;

[ApiController]
[Route("[controller]")]
public class DocsController(ILogger<DocsController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> ConvertToImage([FromQuery] string? format, [FromQuery] int? quality, IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded.");
        }

        SkiaSharp.SKEncodedImageFormat convertToFormat = SkiaSharp.SKEncodedImageFormat.Jpeg;
        int convertToQuality = 100;

        if (!string.IsNullOrEmpty(format))
        {
            if (Enum.TryParse(typeof(SkiaSharp.SKEncodedImageFormat), format, out var parsedFormat))
            {
                convertToFormat = (SkiaSharp.SKEncodedImageFormat)parsedFormat;
                logger.LogInformation("using format: {format}", convertToFormat);
            }
            else
            {
                return BadRequest("Invalid format provided.");
            }
        }

        if (quality is not null)
        {
            if (quality > 1)
            {
                convertToQuality = quality.Value;
                logger.LogInformation("using quality: {convertToQuality}", convertToQuality);
            }
            else
            {
                return BadRequest("Invalid quality provided.");
            }
        }

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream, cancellationToken);

        var images = new List<string>();
        await foreach (var image in Conversion.ToImagesAsync(stream, cancellationToken: cancellationToken))
        {
            using var imageStream = new MemoryStream();
            var encoded = image.Encode(convertToFormat, convertToQuality);
            encoded.SaveTo(imageStream);
            var base64Image = Convert.ToBase64String(imageStream.ToArray());
            images.Add(base64Image);
        }
        return Ok(images);
    }

    [HttpGet("image-types")]
    public IActionResult GetImageTypes()
    {
        var names = Enum.GetNames(typeof(SkiaSharp.SKEncodedImageFormat));
        return Ok(names);
    }
}
