using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net;
using System.Net.Mail;

namespace SHOP_IPHONE.Helpers
{
    public class MailHelper
    {
        public static void SendEmail(string toEmail, string subject, string body)
        {
            var fromEmail = "fatnguyen2511@gmail.com";
            var fromPassword = "amalqrunvsjxsbql"; // App password Gmail

            var message = new MailMessage(fromEmail, toEmail, subject, body)
            {
                IsBodyHtml = true
            };

            var smtp = new SmtpClient("smtp.gmail.com", 587)
            {
                Credentials = new NetworkCredential(fromEmail, fromPassword),
                EnableSsl = true
            };

            smtp.Send(message);
        }
    }
}