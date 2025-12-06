using System.Drawing;

namespace TrainingDay.Maui.Extensions
{
    public class ResizeImageService
    {
        public static byte[] ResizeImage(byte[] imageData, float width, float height, bool rotate)
        {
#if ANDROID
            Android.Graphics.Bitmap originalImage = Android.Graphics.BitmapFactory.DecodeByteArray(imageData, 0, imageData.Length);

            var newHeight = originalImage.Height * width / originalImage.Width;
            Android.Graphics.Bitmap resizedImage = Android.Graphics.Bitmap.CreateScaledBitmap(originalImage, (int)width, (int)newHeight, false);

            using (MemoryStream ms = new MemoryStream())
            {
                resizedImage.Compress(Android.Graphics.Bitmap.CompressFormat.Jpeg, 100, ms);
                return ms.ToArray();
            }
#elif IOS
            UIKit.UIImage originalImage;
            try
            {
                originalImage = new UIKit.UIImage(Foundation.NSData.FromArray(imageData));
            }
            catch (Exception e)
            {
                Console.WriteLine("Image load failed: " + e.Message);
                return null;
            }

            UIKit.UIImageOrientation orientation = originalImage.Orientation;

            var newHeight = originalImage.CGImage.Height * width / originalImage.CGImage.Width;
            //var newHeight = height;
            //create a 24bit RGB image
            using (CoreGraphics.CGBitmapContext context = new CoreGraphics.CGBitmapContext(IntPtr.Zero,
                                                 (int)width, (int)newHeight, 8,
                                                 4 * (int)width, CoreGraphics.CGColorSpace.CreateDeviceRGB(),
                                                 CoreGraphics.CGImageAlphaInfo.PremultipliedFirst))
            {

                RectangleF imageRect = new RectangleF(0, 0, width, (float)newHeight);

                // draw the image
                context.DrawImage(imageRect, originalImage.CGImage);

                UIKit.UIImage resizedImage = UIKit.UIImage.FromImage(context.ToImage(), 0, rotate ? UIKit.UIImageOrientation.Right : orientation);

                // save the image as a jpeg
                return resizedImage.AsJPEG().ToArray();
            }
#endif

            return Array.Empty<byte>();
        }
    }
}
