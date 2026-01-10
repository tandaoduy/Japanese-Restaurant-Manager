using System;
using System.Configuration;
using System.Net;
using System.Net.Mail;
using System.Web;
using Project_65133141.Models;

namespace Project_65133141.Services
{
    public class EmailService
    {
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _smtpUsername;
        private readonly string _smtpPassword;
        private readonly string _fromEmail;
        private readonly string _fromName;

        public EmailService()
        {
            // Read from Web.config or use defaults
            _smtpServer = ConfigurationManager.AppSettings["SmtpServer"] ?? "smtp.gmail.com";
            _smtpPort = int.Parse(ConfigurationManager.AppSettings["SmtpPort"] ?? "587");
            _smtpUsername = ConfigurationManager.AppSettings["SmtpUsername"] ?? "tan.dd.65cntt@ntu.edu.vn";
            _smtpPassword = ConfigurationManager.AppSettings["SmtpPassword"] ?? "";
            _fromEmail = ConfigurationManager.AppSettings["FromEmail"] ?? "tan.dd.65cntt@ntu.edu.vn";
            _fromName = ConfigurationManager.AppSettings["FromName"] ?? "SAKURA KAZOKU Restaurant";
        }

        /// <summary>
        /// Gửi email xác nhận đặt bàn
        /// </summary>
        public bool SendBookingConfirmationEmail(DatBan datBan, string recipientEmail, string recipientName, bool isConfirmed = false)
        {
            try
            {
                // Validate email
                if (string.IsNullOrEmpty(recipientEmail) || !recipientEmail.Contains("@"))
                {
                    System.Diagnostics.Debug.WriteLine("[EmailService] Invalid recipient email: " + (recipientEmail ?? "null"));
                    return false;
                }

                // Validate SMTP password
                if (string.IsNullOrEmpty(_smtpPassword))
                {
                    System.Diagnostics.Debug.WriteLine("[EmailService] WARNING: SMTP password not configured. Please set SmtpPassword in Web.config appSettings.");
                    System.Diagnostics.Debug.WriteLine("[EmailService] For Gmail, you need to create an App Password at: https://myaccount.google.com/apppasswords");
                    System.Diagnostics.Debug.WriteLine("[EmailService] Email will NOT be sent until SmtpPassword is configured.");
                    return false;
                }

                System.Diagnostics.Debug.WriteLine($"[EmailService] Preparing to send email to: {recipientEmail}");
                System.Diagnostics.Debug.WriteLine($"[EmailService] SMTP Server: {_smtpServer}:{_smtpPort}");
                System.Diagnostics.Debug.WriteLine($"[EmailService] From: {_fromEmail}");

                // Load related data
                var db = new QuanLyNhaHangNhat_65133141Entities6();
                if (datBan.BanID.HasValue && datBan.BanAn == null)
                {
                    datBan.BanAn = db.BanAns.Find(datBan.BanID.Value);
                }

                // Generate email body from template
                var emailBody = GenerateBookingTicketEmail(datBan, isConfirmed);

                // Create mail message
                using (var message = new MailMessage())
                {
                    message.From = new MailAddress(_fromEmail, _fromName);
                    message.To.Add(new MailAddress(recipientEmail, recipientName));
                    message.Subject = isConfirmed 
                        ? $"Xác nhận đặt bàn thành công - Mã đặt bàn: {datBan.DatBanID}" 
                        : $"Đặt bàn thành công - Mã đặt bàn: {datBan.DatBanID}";
                    message.Body = emailBody;
                    message.IsBodyHtml = true;
                    message.Priority = MailPriority.High;

                    // Configure SMTP client
                    using (var smtpClient = new SmtpClient(_smtpServer, _smtpPort))
                    {
                        smtpClient.EnableSsl = true;
                        smtpClient.Credentials = new NetworkCredential(_smtpUsername, _smtpPassword);
                        smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
                        smtpClient.Timeout = 30000; // 30 seconds

                        // Send email
                        System.Diagnostics.Debug.WriteLine("[EmailService] Sending email...");
                        smtpClient.Send(message);
                        System.Diagnostics.Debug.WriteLine("[EmailService] Email sent successfully!");
                    }
                }

                return true;
            }
            catch (SmtpException smtpEx)
            {
                System.Diagnostics.Debug.WriteLine($"[EmailService] SMTP Error: {smtpEx.Message}");
                System.Diagnostics.Debug.WriteLine($"[EmailService] SMTP Status Code: {smtpEx.StatusCode}");
                if (smtpEx.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[EmailService] Inner Exception: {smtpEx.InnerException.Message}");
                }
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EmailService] Error sending email: {ex.Message}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[EmailService] Inner Exception: {ex.InnerException.Message}");
                }
                return false;
            }
        }

        /// <summary>
        /// Gửi email chung với subject và body tùy chỉnh
        /// </summary>
        public bool SendEmail(string toEmail, string subject, string body)
        {
            try
            {
                // Validate email
                if (string.IsNullOrEmpty(toEmail) || !toEmail.Contains("@"))
                {
                    System.Diagnostics.Debug.WriteLine("[EmailService] Invalid recipient email: " + (toEmail ?? "null"));
                    return false;
                }

                // Validate SMTP password
                if (string.IsNullOrEmpty(_smtpPassword))
                {
                    System.Diagnostics.Debug.WriteLine("[EmailService] WARNING: SMTP password not configured.");
                    return false;
                }

                System.Diagnostics.Debug.WriteLine($"[EmailService] Sending email to: {toEmail}");

                using (var message = new MailMessage())
                {
                    message.From = new MailAddress(_fromEmail, _fromName);
                    message.To.Add(new MailAddress(toEmail));
                    message.Subject = subject;
                    message.Body = body;
                    message.IsBodyHtml = true;
                    message.Priority = MailPriority.High;

                    using (var smtpClient = new SmtpClient(_smtpServer, _smtpPort))
                    {
                        smtpClient.EnableSsl = true;
                        smtpClient.Credentials = new NetworkCredential(_smtpUsername, _smtpPassword);
                        smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
                        smtpClient.Timeout = 30000;

                        smtpClient.Send(message);
                        System.Diagnostics.Debug.WriteLine("[EmailService] Email sent successfully!");
                    }
                }

                return true;
            }
            catch (SmtpException smtpEx)
            {
                System.Diagnostics.Debug.WriteLine($"[EmailService] SMTP Error: {smtpEx.Message}");
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EmailService] Error sending email: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Generate HTML email template giống vé xem phim
        /// </summary>
        private string GenerateBookingTicketEmail(DatBan datBan, bool isConfirmed)
        {
            var tableName = datBan.BanAn != null ? datBan.BanAn.TenBan : "Chưa được sắp xếp";
            var tableCapacity = datBan.BanAn != null ? datBan.BanAn.SucChua.ToString() : "-";
            var statusText = isConfirmed ? "ĐÃ XÁC NHẬN" : "CHỜ XÁC NHẬN";
            var statusColor = isConfirmed ? "#10b981" : "#f59e0b";
            var dateTime = datBan.ThoiGianDen.ToString("dd/MM/yyyy HH:mm");
            var bookingId = datBan.DatBanID.ToString().PadLeft(6, '0');

            return $@"
<!DOCTYPE html>
<html lang='vi'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Vé Đặt Bàn - SAKURA KAZOKU</title>
    <style>
        * {{
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }}
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            padding: 20px;
            min-height: 100vh;
        }}
        .ticket-container {{
            max-width: 600px;
            margin: 0 auto;
            background: white;
            border-radius: 20px;
            overflow: hidden;
            box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3);
        }}
        .ticket-header {{
            background: linear-gradient(135deg, #dc2626 0%, #b91c1c 100%);
            color: white;
            padding: 30px;
            text-align: center;
            position: relative;
        }}
        .ticket-header::after {{
            content: '';
            position: absolute;
            bottom: -15px;
            left: 50%;
            transform: translateX(-50%);
            width: 30px;
            height: 30px;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            border-radius: 50%;
            border: 5px solid white;
        }}
        .restaurant-logo {{
            width: 80px;
            height: 80px;
            margin: 0 auto 15px;
            background: white;
            border-radius: 50%;
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 32px;
            font-weight: bold;
            color: #dc2626;
            box-shadow: 0 4px 15px rgba(0, 0, 0, 0.2);
        }}
        .restaurant-name {{
            font-size: 28px;
            font-weight: bold;
            margin-bottom: 5px;
            letter-spacing: 2px;
        }}
        .restaurant-subtitle {{
            font-size: 14px;
            opacity: 0.9;
            letter-spacing: 1px;
        }}
        .ticket-body {{
            padding: 40px 30px;
            background: white;
        }}
        .status-badge {{
            display: inline-block;
            padding: 8px 20px;
            background: {statusColor};
            color: white;
            border-radius: 20px;
            font-weight: bold;
            font-size: 14px;
            letter-spacing: 1px;
            margin-bottom: 25px;
        }}
        .ticket-info {{
            background: linear-gradient(135deg, #f8f9fa 0%, #e9ecef 100%);
            border-radius: 15px;
            padding: 25px;
            margin-bottom: 25px;
            border-left: 5px solid #dc2626;
        }}
        .info-row {{
            display: flex;
            justify-content: space-between;
            align-items: center;
            padding: 12px 0;
            border-bottom: 1px solid #e0e0e0;
        }}
        .info-row:last-child {{
            border-bottom: none;
        }}
        .info-label {{
            font-weight: 600;
            color: #666;
            font-size: 14px;
        }}
        .info-value {{
            font-weight: bold;
            color: #1a1a1a;
            font-size: 16px;
            text-align: right;
        }}
        .booking-id {{
            text-align: center;
            margin: 30px 0;
            padding: 20px;
            background: linear-gradient(135deg, #fef3c7 0%, #fde68a 100%);
            border-radius: 15px;
            border: 2px dashed #f59e0b;
        }}
        .booking-id-label {{
            font-size: 12px;
            color: #92400e;
            text-transform: uppercase;
            letter-spacing: 2px;
            margin-bottom: 8px;
        }}
        .booking-id-value {{
            font-size: 32px;
            font-weight: bold;
            color: #92400e;
            letter-spacing: 4px;
            font-family: 'Courier New', monospace;
        }}
        .qr-section {{
            text-align: center;
            margin: 30px 0;
            padding: 20px;
            background: #f8f9fa;
            border-radius: 15px;
        }}
        .qr-placeholder {{
            width: 150px;
            height: 150px;
            margin: 0 auto;
            background: white;
            border: 3px solid #dc2626;
            border-radius: 10px;
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 12px;
            color: #666;
            text-align: center;
            padding: 10px;
        }}
        .ticket-footer {{
            background: #f8f9fa;
            padding: 25px 30px;
            text-align: center;
            border-top: 2px dashed #e0e0e0;
        }}
        .footer-text {{
            font-size: 12px;
            color: #666;
            line-height: 1.6;
        }}
        .footer-highlight {{
            color: #dc2626;
            font-weight: bold;
        }}
        .perforated-line {{
            height: 1px;
            background: repeating-linear-gradient(
                to right,
                #e0e0e0 0px,
                #e0e0e0 10px,
                transparent 10px,
                transparent 20px
            );
            margin: 20px 0;
        }}
        .important-note {{
            background: #fff3cd;
            border-left: 4px solid #ffc107;
            padding: 15px;
            border-radius: 8px;
            margin: 20px 0;
        }}
        .important-note-title {{
            font-weight: bold;
            color: #856404;
            margin-bottom: 8px;
            font-size: 14px;
        }}
        .important-note-text {{
            font-size: 13px;
            color: #856404;
            line-height: 1.5;
        }}
        @media only screen and (max-width: 600px) {{
            .ticket-container {{
                margin: 10px;
                border-radius: 15px;
            }}
            .ticket-header {{
                padding: 20px;
            }}
            .ticket-body {{
                padding: 25px 20px;
            }}
            .info-row {{
                flex-direction: column;
                align-items: flex-start;
            }}
            .info-value {{
                text-align: left;
                margin-top: 5px;
            }}
        }}
    </style>
</head>
<body>
    <div class='ticket-container'>
        <!-- Header -->
        <div class='ticket-header'>
            <div class='restaurant-logo'>SK</div>
            <div class='restaurant-name'>SAKURA KAZOKU</div>
            <div class='restaurant-subtitle'>Nhà hàng Nhật Bản</div>
        </div>

        <!-- Body -->
        <div class='ticket-body'>
            <div style='text-align: center; margin-bottom: 20px;'>
                <span class='status-badge'>{statusText}</span>
            </div>

            <div class='booking-id'>
                <div class='booking-id-label'>Mã đặt bàn</div>
                <div class='booking-id-value'>{bookingId}</div>
            </div>

            <div class='ticket-info'>
                <div class='info-row'>
                    <span class='info-label'>Khách hàng:</span>
                    <span class='info-value'>{HttpUtility.HtmlEncode(datBan.HoTenKhach)}</span>
                </div>
                <div class='info-row'>
                    <span class='info-label'>Số điện thoại:</span>
                    <span class='info-value'>{HttpUtility.HtmlEncode(datBan.SDTKhach)}</span>
                </div>
                <div class='info-row'>
                    <span class='info-label'>Số người:</span>
                    <span class='info-value'>{datBan.SoNguoi ?? 0} người</span>
                </div>
                <div class='info-row'>
                    <span class='info-label'>Thời gian:</span>
                    <span class='info-value'>{dateTime}</span>
                </div>
                <div class='info-row'>
                    <span class='info-label'>Bàn:</span>
                    <span class='info-value'>{tableName}</span>
                </div>
                {(datBan.BanAn != null ? $@"
                <div class='info-row'>
                    <span class='info-label'>Sức chứa:</span>
                    <span class='info-value'>{tableCapacity} người</span>
                </div>" : "")}
                {(datBan.BanAn != null && !string.IsNullOrEmpty(datBan.BanAn.ViTri) ? $@"
                <div class='info-row'>
                    <span class='info-label'>Vị trí:</span>
                    <span class='info-value'>{HttpUtility.HtmlEncode(datBan.BanAn.ViTri)}</span>
                </div>" : "")}
            </div>

            {(isConfirmed ? $@"
            <div class='important-note'>
                <div class='important-note-title'>✓ Đặt bàn đã được xác nhận!</div>
                <div class='important-note-text'>
                    Đặt bàn của bạn đã được xác nhận thành công. Vui lòng đến đúng giờ đã đặt. 
                    Nếu có thay đổi, vui lòng liên hệ nhà hàng trước ít nhất 2 giờ.
                </div>
            </div>" : $@"
            <div class='important-note'>
                <div class='important-note-title'>⏳ Đang chờ xác nhận</div>
                <div class='important-note-text'>
                    Đặt bàn của bạn đã được ghi nhận. Chúng tôi sẽ liên hệ với bạn sớm nhất để xác nhận và sắp xếp bàn phù hợp.
                </div>
            </div>")}

            {(string.IsNullOrEmpty(datBan.GhiChu) ? "" : $@"
            <div class='ticket-info' style='margin-top: 20px;'>
                <div class='info-label' style='margin-bottom: 10px;'>Ghi chú:</div>
                <div style='color: #333; font-size: 14px; line-height: 1.6;'>{HttpUtility.HtmlEncode(datBan.GhiChu)}</div>
            </div>")}

            <div style='text-align: center; margin-top: 20px; padding: 15px; background: #f0fdf4; border-radius: 10px; border: 1px solid #bbf7d0;'>
                <div style='font-weight: bold; color: #166534; margin-bottom: 8px;'>Mã đặt bàn: {bookingId}</div>
                <div style='font-size: 13px; color: #166534;'>Vui lòng cung cấp mã này khi đến nhà hàng</div>
            </div>
        </div>

        <!-- Footer -->
        <div class='ticket-footer'>
            <div class='footer-text'>
                <div style='margin-bottom: 10px;'>
                    <span class='footer-highlight'>Địa chỉ:</span> 70 Phù Đổng, phường Nha Trang, tỉnh Khánh Hòa
                </div>
                <div style='margin-bottom: 10px;'>
                    <span class='footer-highlight'>Hotline:</span> +012 345 67890
                </div>
                <div style='margin-bottom: 10px;'>
                    <span class='footer-highlight'>Email:</span> tan.dd.65cntt@ntu.edu.vn
                </div>
                <div style='margin-top: 15px; padding-top: 15px; border-top: 1px solid #e0e0e0;'>
                    Cảm ơn bạn đã lựa chọn <span class='footer-highlight'>SAKURA KAZOKU</span>!<br/>
                    Chúng tôi rất hân hạnh được phục vụ bạn.
                </div>
            </div>
        </div>
    </div>
</body>
</html>";
        }
    }
}

