using System.Collections.Generic;
using System.Net;
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
        /// <param name="mailServer">Mail server used to route the email. If null or not supplied, uses DefaultMailServer appSetting from config file</param>
        /// <param name="portNumber">If null or not supplied, uses default of 25</param>
        /// <param name="userName">Only required if SMTP server requires authentication to send</param>
        /// <param name="password">Only required if SMTP server requires authentication to send</param>
        /// <param name="attachments">(Optional) Attachments to send with the email</param>
        /// <returns>True if sent successfully</returns>
        public static bool SendEmail(string senderName, string senderEmail, string recipientName, string recipientEmail, string subject, string textBody, string htmlBody,
            string mailServer = null, int portNumber = 25, string userName = null, string password = null, List<Attachment> attachments = null)
        {
            if (mailServer == null)
            {
                if (Configuration.ConfigurationSettings.Settings.IsSettingSupported("DefaultMailServer")) mailServer = Configuration.ConfigurationSettings.Settings["DefaultMailServer"];
                else throw new Exceptions.MissingConfigurationSettingException("DefaultMailServer");
            }

            var smtp = new SmtpClient(mailServer, portNumber);
            if (!string.IsNullOrWhiteSpace(userName) && !string.IsNullOrWhiteSpace(password)) smtp.Credentials = new NetworkCredential(userName, password);

            var message = new MailMessage(new MailAddress(senderEmail, senderName), new MailAddress(recipientEmail, recipientName));
            message.Subject = subject;
            message.IsBodyHtml = !string.IsNullOrEmpty(htmlBody);
            message.Body = message.IsBodyHtml ? htmlBody : textBody;
            if (attachments != null) foreach (var attachment in attachments) message.Attachments.Add(attachment);

            smtp.Send(message);
            message.Dispose();
            return true;
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
