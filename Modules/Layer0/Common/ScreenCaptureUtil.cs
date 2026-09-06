using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace HeaplitLauncher
{
    public static class ScreenCaptureUtil
    {
        public static byte[]? CapturePrimaryScreen(bool saveToDisk = false)
        {
            try
            {
                // Capture the entire Virtual Screen (all monitors) to ensure Heaplit sees everything
                var bounds = SystemInformation.VirtualScreen;
                using (Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height))
                {
                    using (Graphics g = Graphics.FromImage(bitmap))
                    {
                        g.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size);
                    }

                    if (saveToDisk)
                    {
                        try {
                            string dir = Path.Combine(PathHandler.GetDataDirectory(), "Screenshots");
                            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                            string filename = $"AI_Vision_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
                            bitmap.Save(Path.Combine(dir, filename), ImageFormat.Jpeg);
                        } catch { }
                    }

                    using (MemoryStream ms = new MemoryStream())
                    {
                        // Save as JPEG to reduce payload size for API
                        var encoderParams = new EncoderParameters(1);
                        encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, 75L);
                        var jpegCodec = ImageCodecInfo.GetImageEncoders().First(c => c.FormatID == ImageFormat.Jpeg.Guid);

                        bitmap.Save(ms, jpegCodec, encoderParams);
                        return ms.ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error capturing screen: {ex.Message}");
                return null;
            }
        }

        public static string? CapturePrimaryScreenToBase64(bool saveToDisk = false)
        {
            var bytes = CapturePrimaryScreen(saveToDisk);
            return bytes != null ? Convert.ToBase64String(bytes) : null;
        }
    }
}
