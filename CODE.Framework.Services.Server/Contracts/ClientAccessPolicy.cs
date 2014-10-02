using System;
using System.Collections.ObjectModel;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Xml.Linq;

namespace CODE.Framework.Services.Server.Contracts
{
    /// <summary>
    /// Interface used for self-hosted client access policy definitions
    /// </summary>
    [ServiceContract]
    public interface IClientAccessPolicy
    {
        /// <summary>
        /// Gets the client access policy.
        /// </summary>
        /// <returns></returns>
        [OperationContract]
        [WebGet(UriTemplate = "/clientaccesspolicy.xml")]
        XElement GetClientAccessPolicy();
    }

    /// <summary>
    /// Standard implementation of the client access policy for self-hosted services
    /// </summary>
    public class ClientAccessPolicy : IClientAccessPolicy
    {
        /// <summary>
        /// Collection of allowed callers
        /// </summary>
        public static Collection<Uri> AllowedCallers = new Collection<Uri>();

        /// <summary>
        /// Gets the client access policy.
        /// </summary>
        /// <returns></returns>
        public XElement GetClientAccessPolicy()
        {
            var allowFrom = new XElement("allow-from", new XAttribute("http-request-headers", "SOAPAction"));
            if (AllowedCallers.Count == 0)
                allowFrom.Add(new XElement("domain", new XAttribute("uri", "*")));
            else
                foreach (Uri allowedCaller in AllowedCallers)
                    allowFrom.Add(new XElement("domain", new XAttribute("uri", allowedCaller.AbsoluteUri)));
            return new XElement("access-policy",
                                new XElement("cross-domain-access",
                                             new XElement("policy", allowFrom,
                                                          new XElement("grant-to",
                                                                       new XElement("resource", new XAttribute("path", "/"), new XAttribute("include-subpaths", "true"))))));
        }
    }
}
