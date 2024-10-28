using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Threading.Tasks;

public static class BasicRoutes
{
    public static void MapBasicRoutes(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/", () => "Hello World!");
        
        endpoints.MapPost("/post", async context =>
        {
            if (!context.Request.HasFormContentType)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("The request does not contain form data.");
                return;
            }

            var form = await context.Request.ReadFormAsync();
            var file = form.Files.GetFile("image");

            if (file == null)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("No image file uploaded.");
                return;
            }

            if (!file.ContentType.StartsWith("image/"))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("Uploaded file is not an image.");
                return;
            }

            try
            {
                // Process the image file here
                // For example, save the image to a directory
                var filePath = Path.Combine("data", "uploads", file.FileName);
                Directory.CreateDirectory(Path.GetDirectoryName(filePath)); // Ensure the directory exists

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                context.Response.StatusCode = StatusCodes.Status200OK;
                await context.Response.WriteAsync("Image uploaded successfully.");
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsync($"Internal server error: {ex.Message}");
            }
        });
    }
}