using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;

namespace CODE.Framework.Core.Utilities
{
    /// <summary>
    /// Abstract logger class.
    /// </summary>
    /// 
    /// <example>
    /// public class MyLogger : Logger
    /// {
    /// public override void Log(string logEvent, LogEventType type)
    /// {
    /// MessageBox.Show(logEvent);
    /// }
    /// }
    /// </example>
    /// <remarks>This class provides the basic implementation of a logger class.
    /// The only part that must be overriden is the Log() method with the string overload.</remarks>
    public abstract class Logger : ILogger, IExceptionLogger
    {
        /// <summary>
        /// Logs the specified event (text).
        /// </summary>
        /// <param name="logEvent">The event (text).</param>
        /// <param name="type">The event type.</param>
        public abstract void Log(string logEvent, LogEventType type);

        /// <summary>
        /// Logs the specified event (object).
        /// </summary>
        /// <param name="logEvent">The event (object).</param>
        /// <param name="type">The event type.</param>
        public virtual void Log(object logEvent, LogEventType type)
        {
            Log(logEvent.ToString(), type);
        }

        /// <summary>
        /// For internal use only
        /// </summary>
        private LogEventType _typeFilter = LogEventType.Undefined;

        /// <summary>
        /// Gets or sets the type filter.
        /// </summary>
        /// <value>The type filter.</value>
        /// <remarks>
        /// Only events that match the type filter will be considered by this logger.
        /// </remarks>
        public virtual LogEventType TypeFilter
        {
            get { return _typeFilter; }
            set { _typeFilter = value; }
        }

        /// <summary>
        /// Logs the specified event (text).
        /// </summary>
        /// <param name="exception">The exception that is to be logged.</param>
        /// <param name="type">The event type.</param>
        public virtual void Log(Exception exception, LogEventType type)
        {
            var text = GetSerializedExceptionText(exception, type);
            Log(text, type);
        }

        /// <summary>
        /// Logs the specified event (text).
        /// </summary>
        /// <param name="leadingText">The leading text.</param>
        /// <param name="exception">The exception that is to be logged.</param>
        /// <param name="type">The event type.</param>
        public virtual void Log(string leadingText, Exception exception, LogEventType type)
        {
            var text = leadingText + GetSerializedExceptionText(exception, type);
            Log(text, type);
        }

        /// <summary>
        /// Serializes the exception and returns the serialzied text
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <param name="type">The log info type.</param>
        /// <returns>Serialized exception information</returns>
        /// <remarks>This method is designed to be overridden in subclasses</remarks>
        protected virtual string GetSerializedExceptionText(Exception exception, LogEventType type)
        {
            return ExceptionHelper.GetExceptionText(exception);
        }
    }

    /// <summary>
    /// Console logger class
    /// </summary>
    /// <remarks>
    /// Performs the equivalent of a Console.WriteLine()
    /// </remarks>
    /// <example>
    /// EPS.Utilities.LoggingMediator.AddLogger(new EPS.Utilities.ConsoleLogger());
    /// 
    /// EPS.Utilities.LoggingMediator.Log("Hello World!", EPS.Utilities.LogEventType.Information);
    /// EPS.Utilities.LoggingMediator.Log("A critical error occured!", EPS.Utilities.LogEventType.Critical | EPS.Utilities.LogEventType.Error);
    /// </example>
    public class ConsoleLogger : Logger
    {
        /// <summary>
        /// Logs the specified event (text).
        /// </summary>
        /// <param name="logEvent">The event (text).</param>
        /// <param name="type">The event type.</param>
        public override void Log(string logEvent, LogEventType type)
        {
            Console.WriteLine(type + ": " + logEvent);
        }
    }

    /// <summary>
    /// Output Window logger class
    /// </summary>
    /// <remarks>
    /// Performs the equivalent of a System.Diagnostics.Debug.WriteLine()
    /// </remarks>
    /// <example>
    /// EPS.Utilities.LoggingMediator.AddLogger(new EPS.Utilities.OutputWindowLogger());
    /// 
    /// EPS.Utilities.LoggingMediator.Log("Hello World!", EPS.Utilities.LogEventType.Information);
    /// EPS.Utilities.LoggingMediator.Log("A critical error occured!", EPS.Utilities.LogEventType.Critical | EPS.Utilities.LogEventType.Error);
    /// </example>
    public class OutputWindowLogger : Logger
    {
        /// <summary>
        /// Logs the specified event (text).
        /// </summary>
        /// <param name="logEvent">The event (text).</param>
        /// <param name="type">The event type.</param>
        public override void Log(string logEvent, LogEventType type)
        {
            System.Diagnostics.Debug.WriteLine(type + ": " + logEvent);
        }
    }


    /// <summary>
    /// Trace logger class
    /// </summary>
    /// <remarks>
    /// Performs the equivalent of a System.Diagnostics.Trace.WriteLine()
    /// </remarks>
    /// <example>
    /// EPS.Utilities.LoggingMediator.AddLogger(new EPS.Utilities.TraceLogger());
    /// 
    /// EPS.Utilities.LoggingMediator.Log("Hello World!", EPS.Utilities.LogEventType.Information);
    /// EPS.Utilities.LoggingMediator.Log("A critical error occured!", EPS.Utilities.LogEventType.Critical | EPS.Utilities.LogEventType.Error);
    /// </example>
    public class TraceLogger : Logger
    {
        /// <summary>
        /// Logs the specified event (text).
        /// </summary>
        /// <param name="logEvent">The event (text).</param>
        /// <param name="type">The event type.</param>
        public override void Log(string logEvent, LogEventType type)
        {
            try
            {
                System.Diagnostics.Trace.WriteLine(type + ": " + logEvent);
            }
            catch
            {
            }
        }
    }

    /// <summary>
    /// Multiple file logger class
    /// </summary>
    /// <remarks>
    /// Logs the provided information into multiple files.
    /// By default, the location of the files is the application data path.
    /// By default, the name of each file is a Guid. The extension is ".log".
    /// </remarks>
    /// <example>
    /// EPS.Utilities.MultiFileLogger logger = new EPS.Utilities.MultiFileLogger(@"c:\Logs\");
    /// logger.Extension = "event";  // Creates *.event files
    /// EPS.Utilities.LoggingMediator.AddLogger(logger);
    /// 
    /// EPS.Utilities.LoggingMediator.Log("Hello World!", EPS.Utilities.LogEventType.Information);
    /// EPS.Utilities.LoggingMediator.Log("A critical error occured!", EPS.Utilities.LogEventType.Critical | EPS.Utilities.LogEventType.Error);
    /// </example>
    public class MultiFileLogger : Logger
    {
        /// <summary>
        /// For internal use only
        /// </summary>
        private string _extension = "log";

        /// <summary>
        /// Log file extension
        /// </summary>
        public string Extension
        {
            get { return _extension; }
            set
            {
                if (value.StartsWith(".")) value = value.Substring(1);
                _extension = value;
            }
        }


        /// <summary>
        /// Gets or sets the folder the files are to be put into.
        /// </summary>
        /// <value>The folder.</value>
        public string Folder { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiFileLogger"/> class.
        /// </summary>
        /// <param name="folder">The folder the files are to be put into.</param>
        public MultiFileLogger(string folder)
        {
            Folder = folder;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiFileLogger"/> class.
        /// </summary>
        /// <param name="folder">The folder the files are to be put into.</param>
        public MultiFileLogger(Environment.SpecialFolder folder)
        {
            Folder = Environment.GetFolderPath(folder);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiFileLogger"/> class.
        /// </summary>
        /// <remarks>By default, the application data files folder is used for the log files.</remarks>
        public MultiFileLogger()
        {
            Folder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        }

        /// <summary>
        /// Logs the specified event (text).
        /// </summary>
        /// <param name="logEvent">The event (text).</param>
        /// <param name="type">The event type.</param>
        public override void Log(string logEvent, LogEventType type)
        {
            var fileName = StringHelper.AddBS(Folder) + GetNextFileName();
            StringHelper.ToFile(logEvent, fileName);
        }

        /// <summary>
        /// Gets the name of the next file.
        /// </summary>
        /// <returns>Next file name.</returns>
        /// <remarks>Override this method to create a different file name schema.</remarks>
        protected virtual string GetNextFileName()
        {
            return Guid.NewGuid() + "." + Extension;
        }
    }

    /// <summary>
    /// Logs an event to an XML file.
    /// </summary>
    /// <remarks>
    /// Each event is stored into a separate XML file.
    /// 
    /// Typically, the text passed to the logger is not in XML format. In that case, the
    /// logger automatically adds an XML declaration as well as a root tag and a tag for the event
    /// content. The name of the root tag is "log" by default, and the event tag is "event" by default.
    /// In addition, the event element has a "type" attribute that indicates the type of the logged event.
    /// There also is a "timeStamp" attribute that indicates the date and time of the event (GMT).
    /// However, all tag/attribute names can be modified by means of the XmlRootNode and XmlEventNode properties.
    /// 
    /// If the provided text is well formed XML, no other tags are added and the value is used as is,
    /// except that the XML declaration may be added or modified. Also the format of the XML may be changed
    /// since this logger always creates line feeds and proper tag indentation. No type attribute will be added.
    /// </remarks>
    /// <example>
    /// EPS.Utilities.MultiXmlFileLogger logger = new EPS.Utilities.MultiXmlFileLogger(@"c:\Logs\");
    /// EPS.Utilities.LoggingMediator.AddLogger(logger); // Creates *.xml files
    /// 
    /// EPS.Utilities.LoggingMediator.Log("Hello World!", EPS.Utilities.LogEventType.Information);
    /// EPS.Utilities.LoggingMediator.Log("A critical error occured!", EPS.Utilities.LogEventType.Critical | EPS.Utilities.LogEventType.Error);
    /// </example>
    public class MultiXmlFileLogger : MultiFileLogger
    {
        /// <summary>
        /// Gets or sets the XML root node name.
        /// </summary>
        /// <value>The XML root node name.</value>
        public string XmlRootNode { get; set; }

        /// <summary>
        /// Gets or sets the XML event node name.
        /// </summary>
        /// <value>The XML event node name.</value>
        public string XmlEventNode { get; set; }

        /// <summary>
        /// Gets or sets the XML event type attribute name.
        /// </summary>
        /// <value>The XML event type attribute.</value>
        public string XmlEventTypeAttribute { get; set; }

        /// <summary>
        /// Gets or sets the XML event time stamp attribute name.
        /// </summary>
        /// <value>The XML event type attribute.</value>
        public string XmlEventTimeStampAttribute { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiFileLogger"/> class.
        /// </summary>
        /// <param name="folder">The folder the files are to be put into.</param>
        public MultiXmlFileLogger(string folder) : base(folder)
        {
            XmlEventTimeStampAttribute = "timeStamp";
            XmlEventTypeAttribute = "type";
            XmlEventNode = "event";
            XmlRootNode = "log";
            Extension = "xml";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiFileLogger"/> class.
        /// </summary>
        /// <param name="folder">The folder the files are to be put into.</param>
        public MultiXmlFileLogger(Environment.SpecialFolder folder) : base(folder)
        {
            XmlEventTimeStampAttribute = "timeStamp";
            XmlEventTypeAttribute = "type";
            XmlEventNode = "event";
            XmlRootNode = "log";
            Extension = "xml";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiFileLogger"/> class.
        /// </summary>
        /// <remarks>By default, the application data files folder is used for the log files.</remarks>
        public MultiXmlFileLogger()
        {
            XmlEventTimeStampAttribute = "timeStamp";
            XmlEventTypeAttribute = "type";
            XmlEventNode = "event";
            XmlRootNode = "log";
            Extension = "xml";
        }

        /// <summary>
        /// Logs the specified event (text) to an XML file in XML format.
        /// </summary>
        /// <param name="logEvent">The event (text).</param>
        /// <param name="type">The event type.</param>
        public override void Log(string logEvent, LogEventType type)
        {
            var fileName = StringHelper.AddBS(Folder) + GetNextFileName();

            // To see if the provided value is well formed XML, we try to load it into an XML document
            var document = new XmlDocument();
            try
            {
                document.LoadXml(logEvent);
            }
            catch (XmlException)
            {
                // The XML is not well formed. Most likely, this isn't XML at all
                document = new XmlDocument();
                var root = document.AppendChild(document.CreateNode(XmlNodeType.Element, XmlRootNode, string.Empty));
                var eventNode = root.AppendChild(document.CreateNode(XmlNodeType.Element, XmlEventNode, string.Empty));
                eventNode.InnerText = logEvent;
                if (eventNode.Attributes != null)
                {
                    var eventTypeNode = eventNode.Attributes.SetNamedItem(document.CreateNode(XmlNodeType.Attribute, XmlEventTypeAttribute, string.Empty));
                    eventTypeNode.Value = type.ToString();
                }
                if (eventNode.Attributes != null)
                {
                    var eventTimeStampNode = eventNode.Attributes.SetNamedItem(document.CreateNode(XmlNodeType.Attribute, XmlEventTimeStampAttribute, string.Empty));
                    eventTimeStampNode.Value = DateTime.Now.ToUniversalTime().ToString(CultureInfo.InvariantCulture);
                }
            }

            // Ready to save the file away
            if (File.Exists(fileName)) File.Delete(fileName);
            using (var stream = new FileStream(fileName, FileMode.CreateNew, FileAccess.ReadWrite))
            {
                var writer = new XmlTextWriter(stream, Encoding.UTF8) {Formatting = Formatting.Indented, Indentation = 2};
                document.Save(writer);
                writer.Close();
                stream.Close();
            }
        }
    }

    /// <summary>
    /// Single file logger class
    /// </summary>
    /// <remarks>
    /// Logs the provided information into a single file.
    /// By default, the name of the log file is "Log.log". It is probably smart to change the name of that file.
    /// </remarks>
    /// <example>
    /// EPS.Utilities.SingleFileLogger logger = new EPS.Utilities.SingleFileLogger(@"c:\Logs\", "MyLogFile.log");
    /// EPS.Utilities.LoggingMediator.AddLogger(logger);
    /// 
    /// EPS.Utilities.LoggingMediator.Log("Hello World!", EPS.Utilities.LogEventType.Information);
    /// EPS.Utilities.LoggingMediator.Log("A critical error occured!", EPS.Utilities.LogEventType.Critical | EPS.Utilities.LogEventType.Error);
    /// </example>
    public class SingleFileLogger : Logger
    {
        /// <summary>
        /// Log file name
        /// </summary>
        public string FileName { get; set; }


        /// <summary>
        /// Gets or sets the folder the files are to be put into.
        /// </summary>
        /// <value>The folder.</value>
        public string Folder { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SingleFileLogger"/> class.
        /// </summary>
        /// <param name="folder">The folder the files are to be put into.</param>
        public SingleFileLogger(string folder)
        {
            FileName = "Log.log";
            Folder = folder;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SingleFileLogger"/> class.
        /// </summary>
        /// <param name="folder">The folder the files are to be put into.</param>
        /// <param name="fileName">Name of the log file.</param>
        public SingleFileLogger(string folder, string fileName)
        {
            Folder = folder;
            FileName = fileName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SingleFileLogger"/> class.
        /// </summary>
        /// <param name="folder">The folder the files are to be put into.</param>
        public SingleFileLogger(Environment.SpecialFolder folder)
        {
            FileName = "Log.log";
            Folder = Environment.GetFolderPath(folder);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SingleFileLogger"/> class.
        /// </summary>
        /// <param name="folder">The folder the files are to be put into.</param>
        /// <param name="fileName">Name of the log file.</param>
        public SingleFileLogger(Environment.SpecialFolder folder, string fileName)
        {
            Folder = Environment.GetFolderPath(folder);
            FileName = fileName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SingleFileLogger"/> class.
        /// </summary>
        /// <remarks>By default, the application data files folder is used for the log files.</remarks>
        public SingleFileLogger()
        {
            FileName = "Log.log";
            Folder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        }

        /// <summary>
        /// Logs the specified event (text).
        /// </summary>
        /// <param name="logEvent">The event (text).</param>
        /// <param name="type">The event type.</param>
        public override void Log(string logEvent, LogEventType type)
        {
            var fileName = StringHelper.AddBS(Folder) + FileName;

            //Create the file if it does not exist and open it
            using (var stream = new FileStream(fileName, FileMode.Append, FileAccess.Write))
            using (var writer = new StreamWriter(stream, Encoding.Default))
            {
                writer.Write(logEvent);
                writer.Flush();
                writer.Close();
                stream.Close();
            }
        }
    }

    /// <summary>
    /// Single XML file logger class
    /// </summary>
    /// <remarks>
    /// All events are stored into a common XML file.
    /// 
    /// Text passed to the logger is not XML formatted. Instead, the text gets logged into an XML file,
    /// using the XML file as a database. The text logged is made the content of each "record". If actual XML
    /// is passed to the logger, then that XML is simple treated as content that is stored in the log.
    /// 
    /// The XML log file follows a specific format using a "log" tag as the root element, with "event" tags for
    /// each actual event that gets logged. That tag also has "type" and "timeStamp" attributes.
    /// All tag and attribute names can be modified through the properties on this object.
    /// 
    /// The log can be limited to a maximum number of entries. If that number is exceeded, the oldest
    /// events get truncated from the log.
    /// 
    /// The XML file structure has to be compatible with the one defined by this object's properties. If not,
    /// the existing XML file is overwritten with a brand new one. (Compatible in this sense means that the root
    /// element name has to match. Additional elements that aren't defined by this log object are allowed to
    /// exist. They are simply ignored by this logger).
    /// </remarks>
    /// <example>
    /// EPS.Utilities.SingleXmlFileLogger logger = new EPS.Utilities.SingleXmlFileLogger(@"c:\Logs\", "MyLog.xml");
    /// logger.MaximumEntries = 100;
    /// EPS.Utilities.LoggingMediator.AddLogger(logger); // Creates the MyLog.xml file
    /// 
    /// EPS.Utilities.LoggingMediator.Log("Hello World!", EPS.Utilities.LogEventType.Information);
    /// EPS.Utilities.LoggingMediator.Log("A critical error occured!", EPS.Utilities.LogEventType.Critical | EPS.Utilities.LogEventType.Error);
    /// </example>
    public class SingleXmlFileLogger : SingleFileLogger
    {
        /// <summary>
        /// Gets or sets the XML root node name.
        /// </summary>
        /// <value>The XML root node name.</value>
        public string XmlRootNode { get; set; }

        /// <summary>
        /// Gets or sets the XML event node name.
        /// </summary>
        /// <value>The XML event node name.</value>
        public string XmlEventNode { get; set; }

        /// <summary>
        /// Gets or sets the XML event type attribute name.
        /// </summary>
        /// <value>The XML event type attribute.</value>
        public string XmlEventTypeAttribute { get; set; }

        /// <summary>
        /// Gets or sets the XML event time stamp attribute name.
        /// </summary>
        /// <value>The XML event type attribute.</value>
        public string XmlEventTimeStampAttribute { get; set; }

        /// <summary>
        /// Defines the maximum number of entries in the log file.
        /// -1 = unlimited.
        /// </summary>
        /// <value>Maximum number of entries.</value>
        public int MaximumEntries { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SingleXmlFileLogger"/> class.
        /// </summary>
        /// <param name="folder">The folder the files are to be put into.</param>
        public SingleXmlFileLogger(string folder) : base(folder)
        {
            MaximumEntries = -1;
            XmlEventTimeStampAttribute = "timeStamp";
            XmlEventTypeAttribute = "type";
            XmlEventNode = "event";
            XmlRootNode = "log";
            FileName = "Log.xml";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SingleXmlFileLogger"/> class.
        /// </summary>
        /// <param name="folder">The folder the files are to be put into.</param>
        /// <param name="fileName">Name of the log file.</param>
        public SingleXmlFileLogger(string folder, string fileName) : base(folder, fileName)
        {
            MaximumEntries = -1;
            XmlEventTimeStampAttribute = "timeStamp";
            XmlEventTypeAttribute = "type";
            XmlEventNode = "event";
            XmlRootNode = "log";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SingleXmlFileLogger"/> class.
        /// </summary>
        /// <param name="folder">The folder the files are to be put into.</param>
        public SingleXmlFileLogger(Environment.SpecialFolder folder) : base(folder)
        {
            MaximumEntries = -1;
            XmlEventTimeStampAttribute = "timeStamp";
            XmlEventTypeAttribute = "type";
            XmlEventNode = "event";
            XmlRootNode = "log";
            FileName = "Log.xml";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SingleXmlFileLogger"/> class.
        /// </summary>
        /// <param name="folder">The folder the files are to be put into.</param>
        /// <param name="fileName">Name of the log file.</param>
        public SingleXmlFileLogger(Environment.SpecialFolder folder, string fileName) : base(folder, fileName)
        {
            MaximumEntries = -1;
            XmlEventTimeStampAttribute = "timeStamp";
            XmlEventTypeAttribute = "type";
            XmlEventNode = "event";
            XmlRootNode = "log";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SingleXmlFileLogger"/> class.
        /// </summary>
        /// <remarks>By default, the application data files folder is used for the log files.</remarks>
        public SingleXmlFileLogger()
        {
            MaximumEntries = -1;
            XmlEventTimeStampAttribute = "timeStamp";
            XmlEventTypeAttribute = "type";
            XmlEventNode = "event";
            XmlRootNode = "log";
            FileName = "Log.xml";
        }

        /// <summary>
        /// Logs the specified event (text).
        /// </summary>
        /// <param name="logEvent">The event (text).</param>
        /// <param name="type">The event type.</param>
        public override void Log(string logEvent, LogEventType type)
        {
            var fileName = StringHelper.AddBS(Folder) + FileName;

            // If the file exists, we try to load it
            var xml = GetXmlDocument(fileName);
            var root = xml.SelectSingleNode(XmlRootNode);

            // We add another event
            if (root != null)
            {
                var eventNode = root.AppendChild(xml.CreateNode(XmlNodeType.Element, XmlEventNode, string.Empty));
                eventNode.InnerText = logEvent;
                if (eventNode.Attributes != null)
                {
                    var eventTypeNode = eventNode.Attributes.SetNamedItem(xml.CreateNode(XmlNodeType.Attribute, XmlEventTypeAttribute, string.Empty));
                    eventTypeNode.Value = type.ToString();
                    var eventTimeStampNode = eventNode.Attributes.SetNamedItem(xml.CreateNode(XmlNodeType.Attribute, XmlEventTimeStampAttribute, string.Empty));
                    eventTimeStampNode.Value = DateTime.Now.ToUniversalTime().ToString(CultureInfo.InvariantCulture);
                }
            }

            // If log growth is restricted, we truncate it
            TruncateLog(xml);

            // We are ready to save the file
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }
            using (var stream = new FileStream(fileName, FileMode.CreateNew, FileAccess.ReadWrite))
            {
                var writer = new XmlTextWriter(stream, Encoding.UTF8) {Formatting = Formatting.Indented, Indentation = 2};
                xml.Save(writer);
                writer.Close();
                stream.Close();
            }
        }

        /// <summary>
        /// Loads or creates the specified XML document
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <returns>Event log XML document</returns>
        protected virtual XmlDocument GetXmlDocument(string fileName)
        {
            var xml = new XmlDocument();
            if (File.Exists(fileName))
            {
                try
                {
                    xml.Load(fileName);
                    if (xml.SelectSingleNode(XmlRootNode) == null) // Must exist to be valid
                        xml = CreateXmlDocument();
                }
                catch
                {
                    xml = CreateXmlDocument();
                }
            }
            else
                xml = CreateXmlDocument();
            return xml;
        }

        /// <summary>
        /// Creates the XML document from scratch.
        /// </summary>
        protected virtual XmlDocument CreateXmlDocument()
        {
            var xml = new XmlDocument();
            xml.AppendChild(xml.CreateNode(XmlNodeType.Element, XmlRootNode, string.Empty));
            return xml;
        }

        /// <summary>
        /// Truncates the log if need be.
        /// </summary>
        /// <param name="log">The log XML document.</param>
        protected virtual void TruncateLog(XmlDocument log)
        {
            if (MaximumEntries == -1) return; // Unlimited growth

            var nodePath = XmlRootNode + "/" + XmlEventNode;
            var nodes = log.SelectNodes(nodePath);
            var root = log.SelectSingleNode(XmlRootNode);
            while (nodes != null && nodes.Count > MaximumEntries)
            {
                var childNode = log.SelectSingleNode(nodePath);
                if (childNode == null) // Should never happen
                    return; // But just in case...
                if (root != null) root.RemoveChild(childNode);
                nodes = log.SelectNodes(nodePath);
            }
        }
    }

    /// <summary>
    /// Event log logger class
    /// </summary>
    /// <remarks>
    /// This class logs events to the windows event log.
    /// 
    /// If a log of the specified name does not exist on the specified machine (typically
    /// the local machine), then it is automatically created.
    /// </remarks>
    /// <example>
    /// EPS.Utilities.EventLogLogger logger = new EPS.Utilities.EventLogLogger("My Application Log");
    /// logger.Source = "My Application";
    /// EPS.Utilities.LoggingMediator.AddLogger(logger);
    /// 
    /// EPS.Utilities.LoggingMediator.Log("Hello World!", EPS.Utilities.LogEventType.Information);
    /// EPS.Utilities.LoggingMediator.Log("A critical error occured!", EPS.Utilities.LogEventType.Critical | EPS.Utilities.LogEventType.Error);
    /// </example>
    public class EventLogLogger : Logger
    {
        /// <summary>
        /// For internal use only
        /// </summary>
        private readonly EventLog _eventLog = new EventLog();

        /// <summary>
        /// Internal reference to the actual event log object
        /// </summary>
        /// <value>The event log.</value>
        protected EventLog InternalEventLog
        {
            get { return _eventLog; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventLogLogger"/> class.
        /// </summary>
        public EventLogLogger()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventLogLogger"/> class.
        /// </summary>
        /// <param name="logName">Name of the log as it appears in the windows system log.</param>
        public EventLogLogger(string logName)
        {
            InternalEventLog.Log = logName;
            InternalEventLog.Source = "Undefined";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventLogLogger"/> class.
        /// </summary>
        /// <param name="logName">Name of the log as it appears in the windows system log.</param>
        /// <param name="machineName">Name of the machine the log resides on. (Current/local machine = ".")</param>
        public EventLogLogger(string logName, string machineName)
        {
            InternalEventLog.Log = logName;
            if (string.IsNullOrEmpty(machineName)) machineName = ".";
            InternalEventLog.MachineName = machineName;
            InternalEventLog.Source = "Undefined";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventLogLogger"/> class.
        /// </summary>
        /// <param name="logName">Name of the log as it appears in the windows system log.</param>
        /// <param name="machineName">Name of the machine the log resides on. (Current/local machine = ".")</param>
        /// <param name="sourceName">Name of the source (typically the name of the current application).</param>
        public EventLogLogger(string logName, string machineName, string sourceName)
        {
            InternalEventLog.Log = logName;
            if (string.IsNullOrEmpty(machineName)) machineName = ".";
            InternalEventLog.MachineName = machineName;
            InternalEventLog.Source = sourceName;
        }

        /// <summary>
        /// Logs the specified event (text).
        /// </summary>
        /// <param name="logEvent">The event (text).</param>
        /// <param name="type">The event type.</param>
        /// <remarks>
        /// Milos log types are mapped to the following system event types (in this order):
        /// LogEventType.Critical   = EventLogEntryType.Error
        /// LogEventType.Error      = EventLogEntryType.FailureAudit
        /// LogEventType.Exception  = EventLogEntryType.FailureAudit
        /// LogEventType.Warning    = EventLogEntryType.Warning
        /// LogEventType.Success    = EventLogEntryType.SuccessAudit
        ///    other:               = EventLogEntryType.Information
        /// </remarks>
        public override void Log(string logEvent, LogEventType type)
        {
            // We make sure the source exists
            if (!EventLog.SourceExists(InternalEventLog.Source, InternalEventLog.MachineName))
                EventLog.CreateEventSource(new EventSourceCreationData(InternalEventLog.Source, InternalEventLog.Log) {MachineName = InternalEventLog.MachineName});

            // We are ready to log the event
            if (type == LogEventType.Undefined)
                InternalEventLog.WriteEntry(logEvent);
            else
            {
                EventLogEntryType systemType;
                if ((type & LogEventType.Critical) == LogEventType.Critical) systemType = EventLogEntryType.Error;
                else if (((type & LogEventType.Error) == LogEventType.Error) || ((type & LogEventType.Exception) == LogEventType.Exception)) systemType = EventLogEntryType.FailureAudit;
                else if ((type & LogEventType.Warning) == LogEventType.Warning) systemType = EventLogEntryType.Warning;
                else if ((type & LogEventType.Success) == LogEventType.Success) systemType = EventLogEntryType.SuccessAudit;
                else systemType = EventLogEntryType.Information;
                InternalEventLog.WriteEntry(logEvent, systemType);
            }
        }
    }
}