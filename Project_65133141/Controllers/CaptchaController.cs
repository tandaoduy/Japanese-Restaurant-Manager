using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Web;
using System.Web.Mvc;

namespace Project_65133141.Controllers
{
    public class CaptchaController : Controller
    {
        /// <summary>
        /// Generate CAPTCHA image and store code in session
        /// </summary>
        public ActionResult Generate()
        {
            // Generate random 5-digit code
            Random random = new Random();
            string captchaCode = random.Next(10000, 99999).ToString();
            
            // Store in session
            Session["CaptchaCode"] = captchaCode;
            
            // Create image
            int width = 150;
            int height = 50;
            using (Bitmap bitmap = new Bitmap(width, height))
            {
                using (Graphics graphics = Graphics.FromImage(bitmap))
                {
                    // Fill background
                    graphics.Clear(Color.White);
                    
                    // Add noise lines
                    Pen pen = new Pen(Color.LightGray, 1);
                    for (int i = 0; i < 10; i++)
                    {
                        graphics.DrawLine(pen, 
                            random.Next(0, width), 
                            random.Next(0, height),
                            random.Next(0, width), 
                            random.Next(0, height));
                    }
                    
                    // Draw text
                    Font font = new Font("Arial", 24, FontStyle.Bold);
                    Brush brush = new SolidBrush(Color.DarkBlue);
                    
                    // Add slight rotation and position variation
                    for (int i = 0; i < captchaCode.Length; i++)
                    {
                        float x = 20 + (i * 25);
                        float y = 10 + random.Next(-5, 5);
                        float angle = random.Next(-10, 10);
                        
                        graphics.TranslateTransform(x, y);
                        graphics.RotateTransform(angle);
                        graphics.DrawString(captchaCode[i].ToString(), font, brush, 0, 0);
                        graphics.ResetTransform();
                    }
                }
                
                // Convert to byte array
                using (MemoryStream stream = new MemoryStream())
                {
                    bitmap.Save(stream, ImageFormat.Png);
                    return File(stream.ToArray(), "image/png");
                }
            }
        }
        
        /// <summary>
        /// Verify CAPTCHA code
        /// </summary>
        public static bool VerifyCaptcha(System.Web.HttpSessionStateBase session, string userInput)
        {
            if (session == null || string.IsNullOrEmpty(userInput))
                return false;
                
            string captchaCode = session["CaptchaCode"] as string;
            if (string.IsNullOrEmpty(captchaCode))
                return false;
                
            // Case-insensitive comparison
            bool isValid = captchaCode.Equals(userInput.Trim(), StringComparison.OrdinalIgnoreCase);
            
            // Clear CAPTCHA after verification (one-time use)
            if (isValid)
            {
                session["CaptchaCode"] = null;
            }
            
            return isValid;
        }
    }
}

