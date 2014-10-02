using System.Net.Mail;
using System.Text.RegularExpressions;

namespace CODE.Framework.Core.Utilities
{
    /// <summary>
    /// This class provides basic functionaloty for email features.
    /// </summary>
    public static class EmailHelper
    {
        /// <summary>
        /// Sends an email to the specified recipient
        /// </summary>
        /// <param name="senderName">Sender Name</param>
        /// <param name="senderEmail">Sender Email Address</param>
        /// <param name="recipientName">Recipient Name</param>
        /// <param name="recipientEmail">Recipient Email Address</param>
        /// <param name="subject">Subject</param>
        /// <param name="textBody">Email Body (text-only version)</param>
        /// <param name="htmlBody">Email Body (HTML version)</param>
        /// <param name="mailServer">Mail server used to route the email</param>
        /// <returns>True if sent successfully</returns>
        public static bool SendEmail(string senderName, string senderEmail, string recipientName, string recipientEmail, string subject, string textBody, string htmlBody, string mailServer)
        {
            var smtp = new SmtpClient(mailServer);
            var message = new MailMessage(new MailAddress(senderEmail, senderName), new MailAddress(recipientEmail, recipientName));
            message.Subject = subject;
            message.IsBodyHtml = !string.IsNullOrEmpty(htmlBody);
            message.Body = message.IsBodyHtml ? htmlBody : textBody;
            smtp.Send(message);
            message.Dispose();
            return true;
        }

        /// <summary>
        /// Sends an email to the specified recipient
        /// </summary>
        /// <param name="senderName">Sender Name</param>
        /// <param name="senderEmail">Sender Email Address</param>
        /// <param name="recipientName">Recipient Name</param>
        /// <param name="recipientEmail">Recipient Email Address</param>
        /// <param name="subject">Subject</param>
        /// <param name="textBody">Email Body (text-only version)</param>
        /// <param name="htmlBody">Email Body (HTML version)</param>
        /// <returns>True if sent successfully</returns>
        /// <remarks>
        /// Uses the DefaultMailServer setting to figure out which server to use to send the email.
        /// </remarks>
        public static bool SendEmail(string senderName, string senderEmail, string recipientName, string recipientEmail, string subject, string textBody, string htmlBody)
        {
            if (Configuration.ConfigurationSettings.Settings.IsSettingSupported("DefaultMailServer"))
            {
                string mailServer = Configuration.ConfigurationSettings.Settings["DefaultMailServer"];
                return SendEmail(senderName, senderEmail, recipientName, recipientEmail, subject, textBody, htmlBody, mailServer);
            }
            throw new Exceptions.MissingConfigurationSettingException("DefaultMailServer");
        }

        /// <summary>
        /// This method returns true if the email address is well formed and thus COULD be valid.
        /// </summary>
        /// <param name="email">Email address, such as billg@microsoft.com</param>
        /// <returns>True if the address appears to be valid.</returns>
        /// <remarks>
        /// This method does NOT check whether the address in fact does exist as a valid address on a mail server.
        /// </remarks>
        public static bool IsEmailAddressWellFormed(string email)
        {
            var reg = new Regex("\\w+([-+.]\\w+)*@\\w+([-.]\\w+)*\\.\\w+([-.]\\w+)*");
            return reg.IsMatch(email.Trim());
        }
    }
}
