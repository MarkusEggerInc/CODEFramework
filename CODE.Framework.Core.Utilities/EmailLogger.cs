using CODE.Framework.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Threading;

namespace CODE.Framework.Core.Utilities
{
    /// <summary>
    /// Email log logger class. Eses the Threadpool to send emails of log entries via smtp.
    /// </summary>
    /// <example>
    /// var logger = new EPS.Utilities.EmailLogLogger("Service Logger","services@mycompany.com", new {"Tech Support"}, new {"support@mycompany.com"}, "smtp.mycompany.com");
    /// EPS.Utilities.LoggingMediator.AddLogger(logger);
    /// 
    /// EPS.Utilities.LoggingMediator.Log("Hello World!", EPS.Utilities.LogEventType.Information);
    /// EPS.Utilities.LoggingMediator.Log("A critical error occured!", EPS.Utilities.LogEventType.Critical | EPS.Utilities.LogEventType.Error);
    /// </example>    
    public class EmailLogger : Logger
    {
        /// <summary>
        /// Sends log entries as emails to the specified recipient(s)
        /// </summary>
        /// <param name="senderName">Sender Name</param>
        /// <param name="senderEmail">Sender Email Address</param>
        /// <param name="recipients">The recipients.</param>
        /// <param name="appName">The name of the application. To be used on the subject line of the email</param>
        /// <param name="mailServer">Mail server used to route the email. If null or not supplied, uses DefaultMailServer appSetting from config file</param>
        /// <param name="portNumber">If null or not supplied, uses default of 25</param>
        /// <param name="userName">Only required if SMTP server requires authentication to send</param>
        /// <param name="password">Only required if SMTP server requires authentication to send</param>
        public EmailLogger(string senderName, string senderEmail, List<EmailRecipient> recipients, string appName, string mailServer = null, int portNumber = 25, string userName = null, string password = null)
        {
            _senderName = senderName;
            _senderEmail = senderEmail;
            _recipients = recipients;
            _appName = appName;
            _mailServer = mailServer;
            _portNumber = portNumber;
            _userName = userName;
            _password = password;
        }

        private readonly string _senderName;
        private readonly string _senderEmail;
        private readonly List<EmailRecipient> _recipients;
        private readonly string _appName;
        private readonly string _mailServer;
        private readonly int _portNumber;
        private readonly string _userName;
        private readonly string _password;

        /// <summary>
        /// Logs the specified event (text).
        /// </summary>
        /// <param name="logEvent">The event (text).</param>
        /// <param name="type">The event type.</param>
        /// <remarks>
        /// Log types are mapped to the following system event types (in this order):
        /// LogEventType.Critical   = EventLogEntryType.Error
        /// LogEventType.Error      = EventLogEntryType.FailureAudit
        /// LogEventType.Exception  = EventLogEntryType.FailureAudit
        /// LogEventType.Warning    = EventLogEntryType.Warning
        /// LogEventType.Success    = EventLogEntryType.SuccessAudit
        ///    other:               = EventLogEntryType.Information
        /// </remarks>
        public override void Log(string logEvent, LogEventType type)
        {
            if (_recipients.Count == 0) throw new ArgumentOutOfRangeException("You must specify at least 1 recipient email address");
            var subject = Enum.GetName(typeof(LogEventType), type) + ": Log Entry for " + _appName;
            ThreadPool.QueueUserWorkItem(c =>
            {
                foreach (var recipient in _recipients)
                {
                    try
                    {
                        EmailHelper.SendEmail(_senderName, _senderEmail, recipient.Name, recipient.EmailAddress, subject, logEvent,
                            null, _mailServer, _portNumber, _userName, _password);
                    }
                    catch (Exception ex)
                    { 
                        System.Diagnostics.Debug.WriteLine(ExceptionHelper.GetExceptionText(ex)); 
                    }
                }
            });
        }

        /// <summary>
        /// Reads the EmailLoggerReceipients appSetting from .config file and parses the value into a list.
        /// Multiple email addresses should be separated by semi-colons. Triendly names, if present should be separated by pipes '|'.
        /// For example:
        ///     support@mycompany.com
        ///     support@mycompany.com;fred@mycompany.com
        ///     support@mycompany.com|MyCompany Support Desk;fred@mycompany.com|Fred Flintstone
        /// </summary>
        public static List<EmailRecipient> GetRecipientsFromConfigFile()
        {
            var list = new List<EmailRecipient>();
            var recipients = ConfigurationSettings.Settings["EmailLoggerRecipients"].Split(';');
            foreach (var recipient in recipients)
            {
                var split = recipient.Split('|');
                list.Add(new EmailRecipient { EmailAddress = split[0].Trim(), Name = split.Length > 0 ? split[1].Trim() : split[0].Trim() });
            }
            return list;
        }
    }

    /// <summary>
    /// Used by GetRecipientsFromConfigFile() when parsing email addresses and names from the .config file
    /// </summary>
    public class EmailRecipient
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EmailRecipient"/> class.
        /// </summary>
        public EmailRecipient()
        {
            Name = string.Empty;
            EmailAddress = string.Empty;
        }

        /// <summary>
        /// Email recipient name
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }
        /// <summary>
        /// Recipient email address
        /// </summary>
        /// <value>The email address.</value>
        public string EmailAddress { get; set; }
    }
}
