namespace CODE.Framework.Services.Client
{
    /// <summary>
    /// Communication Protocol
    /// </summary>
    public enum Protocol
    {
        /// <summary>
        /// Net TCP
        /// </summary>
        NetTcp,
        /// <summary>
        /// Local in process service
        /// </summary>
        InProcess,
        /// <summary>
        /// Basic HTTP
        /// </summary>
        BasicHttp,
        /// <summary>
        /// WS HTTP
        /// </summary>
        WsHttp,
        /// <summary>
        /// XML Formatted REST over HTTP
        /// </summary>
        RestHttpXml,
        /// <summary>
        /// JSON Formatted REST over HTTP
        /// </summary>
        RestHttpJson
    }

    /// <summary>
    /// Message size
    /// </summary>
    public enum MessageSize
    {
        /// <summary>
        /// Normal (default message size as defined by WCF)
        /// </summary>
        Normal,
        /// <summary>
        /// Large (up to 100MB)
        /// </summary>
        Large,
        /// <summary>
        /// Medium (up to 10MB) - this is the default
        /// </summary>
        Medium,
        /// <summary>
        /// For internal use only
        /// </summary>
        Undefined,
        /// <summary>
        /// Very large (up to 1GB)
        /// </summary>
        VeryLarge,
        /// <summary>
        /// Maximum size (equal to int.MaxValue, about 2GB)
        /// </summary>
        Max
    }

}
