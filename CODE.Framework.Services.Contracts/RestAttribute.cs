using System;

namespace CODE.Framework.Services.Contracts
{
    /// <summary>
    /// Attribute used to specify REST behavior
    /// </summary>
    /// <example>
    /// // The following method is exposed as http://....Service/Search/...
    /// [OperationContract]
    /// [Rest(Name = "Search")]
    /// Response MethodName(Request request);
    /// 
    /// // The following method is exposed as service root (without an explicit method name... which can be done once per HTTP method) http://....Service/...
    /// [OperationContract]
    /// [Rest(Name = "")]
    /// Response MethodName(Request request);
    ///
    /// // The following method is exposed as http://....Service/Customer/Smith... and can be called with the HTTP-GET method
    /// [OperationContract]
    /// [Rest(Name = "Customer", Method = RestMethods.Get)]
    /// Response GetCustomer(Request request);
    /// 
    /// // The following method is also exposed as http://....Service/Customer/Smith... but can be called with the HTTP-PUT method
    /// [OperationContract]
    /// [Rest(Name = "Customer", Method = RestMethods.Put)]
    /// Response SaveCustomer(Request request);
    /// 
    /// // The following method is exposed as http://....Service/Smith... and can be called with the HTTP-GET method
    /// [OperationContract]
    /// [Rest(Name = "", Method = RestMethods.Get)]
    /// Response GetCustomer(Request request);
    /// 
    /// // The following method is also exposed as http://....Service/Smith... but can be called with the HTTP-PUT method
    /// [OperationContract]
    /// [Rest(Name = "", Method = RestMethods.Put)]
    /// Response SaveCustomer(Request request);
    /// </example>
    [AttributeUsage(AttributeTargets.Method)]
    public class RestAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RestAttribute"/> class.
        /// </summary>
        public RestAttribute()
        {
            Method = RestMethods.Post;
            Name = null;
        }

        /// <summary>
        /// The HTTP method/verb used for the rest operation
        /// </summary>
        /// <value>The method.</value>
        public RestMethods Method { get; set; }

        /// <summary>
        /// Exposed name of the method/operation
        /// </summary>
        /// <value>The name.</value>
        /// <remarks>If the method is an empty string, it is considered to be the root (method name does not have to be specified)</remarks>
        public string Name { get; set; }
    }

    /// <summary>
    /// This attribute can be used to define that the property maps to an unnamed URL parameter
    /// </summary>
    /// <example>
    /// [DataMember, RestUrlParameter]
    /// public string Name get; set;
    /// // This property can now be used in a URL like this: http://..../operation/Smith 
    /// // In this case, "Smith" will be mapped to the Name property
    ///
    /// [DataMember, RestUrlParameter(Sequence = 1)]
    /// public string Name get; set;
    /// [DataMember, RestUrlParameter(Sequence = 0)]
    /// public string Company get; set;
    /// // This property can now be used in a URL like this: http://..../operation/EPS/Smith
    /// // In this case, "EPS" will be mapped to the Company property and  "Smith" will be mapped to the Name property
    /// </example>
    [AttributeUsage(AttributeTargets.Property)]
    public class RestUrlParameterAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RestUrlParameterAttribute"/> class.
        /// </summary>
        public RestUrlParameterAttribute()
        {
            Mode = UrlParameterMode.Named;
        }

        /// <summary>
        /// Sets the sequence of the parameter within the URL
        /// </summary>
        /// <value>The sequence.</value>
        /// <remarks>
        /// This is technically 0-based. However, it is just use for ordering, so the actual numbers don't really matter.
        /// This setting is only valid for Mode=Inline parameters
        /// </remarks>
        public int Sequence { get; set; }

        /// <summary>
        /// The usage mode of the parameter
        /// </summary>
        /// <value>The mode.</value>
        public UrlParameterMode Mode { get; set; }
    }

    /// <summary>
    /// Different modes of passing parameters in a URL
    /// </summary>
    public enum UrlParameterMode
    {
        /// <summary>
        /// Named parameter (such as ?value=1)
        /// </summary>
        Named,
        /// <summary>
        /// Inline parameter (such as .../1/...)
        /// </summary>
        Inline
    }

    /// <summary>
    /// HTTP Method/Verb used for REST calls
    /// </summary>
    public enum RestMethods
    {
        /// <summary>HTTP POST</summary>
        Post,
        /// <summary>HTTP GET</summary>
        Get,
        /// <summary>HTTP PUT</summary>
        Put,
        /// <summary>HTTP DELETE</summary>
        Delete,
        /// <summary>HTTP HEAD</summary>
        Head,
        /// <summary>HTTP TRACE</summary>
        Trace,
        /// <summary>HTTP SEARCH</summary>
        Search,
        /// <summary>HTTP CONNECT</summary>
        Connect,
        /// <summary>HTTP PROPFIND</summary>
        PropFind,
        /// <summary>HTTP PROPPATCH</summary>
        PropPatch,
        /// <summary>HTTP PATCH</summary>
        Patch,
        /// <summary>HTTP MKCOL</summary>
        Mkcol,
        /// <summary>HTTP COPY</summary>
        Copy,
        /// <summary>HTTP MOVE</summary>
        Move,
        /// <summary>HTTP LOCK</summary>
        Lock,
        /// <summary>HTTP UNLOCK</summary>
        Unlock,
        /// <summary>HTTP OPTIONS</summary>
        Options,
        /// <summary>HTTP POST or PUT (both are valid for this setting)</summary>
        PostOrPut
    }
}
