using Microsoft.AspNetCore.Mvc;
using PdfToImageApi.Models;

namespace PdfToImageApi.Controllers;

[Route("[controller]")]
[ApiController]
public class ImagesController : ControllerBase
{
    [HttpPost("info")]
    public async Task<IActionResult> GetImageInfo(IFormFile file, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream();
        await file.CopyToAsync(stream, cancellationToken);
        var img = SkiaSharp.SKImage.FromEncodedData(stream.ToArray());
        return new OkObjectResult(new ImageInfo
        {
            Width = img.Width,
            Height = img.Height
        });
    }

    [HttpPost("resize")]
    public async Task<IActionResult> ResizeImage([FromQuery] int percent, [FromQuery] int? quality, IFormFile file, CancellationToken cancellationToken)
    {
        if (percent < 15)
        {
            return BadRequest(new ErrorModel { ErrorMessage = "percent cannot be less than 15" });
        }

        if (percent > 90)
        {
            return BadRequest(new ErrorModel { ErrorMessage = "percent cannot be more than 90" });
        }

        if (quality is null)
        {
            quality = 100;
        }

        if (quality < 1)
        {
            return BadRequest(new ErrorModel { ErrorMessage = "quality cannot be less than 1" });
        }

        if (quality > 100)
        {
            return BadRequest(new ErrorModel { ErrorMessage = "quality cannot be more than 100" });
        }

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream, cancellationToken);
        var img = SkiaSharp.SKImage.FromEncodedData(stream.ToArray());

        var originalBitmap = SkiaSharp.SKBitmap.FromImage(img);
        int newWidth = (originalBitmap.Width * percent) / 100;
        int newHeight = (originalBitmap.Height * percent) / 100;

        var resizedBitmap = new SkiaSharp.SKBitmap(newWidth, newHeight);
        using (var canvas = new SkiaSharp.SKCanvas(resizedBitmap))
        {
            canvas.DrawBitmap(originalBitmap, new SkiaSharp.SKRect(0, 0, newWidth, newHeight));
        }

        using var resizedImage = SkiaSharp.SKImage.FromBitmap(resizedBitmap);
        using var resizedData = resizedImage.Encode(SkiaSharp.SKEncodedImageFormat.Jpeg, quality.Value);

        return File(resizedData.ToArray(), "image/jpeg");
    }
}
