using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace CODE.Framework.Core.Utilities
{
    /// <summary>
    /// This class contains various useful features for detecting network state
    /// </summary>
    public static class NetworkHelper
    {
        /// <summary>
        /// Gets a value indicating whether a valid network conneciton is available
        /// </summary>
        /// <value><c>true</c> if [network available]; otherwise, <c>false</c>.</value>
        /// <example>
        /// if (NetworkHelper.NetworkAvailable)
        /// {
        ///     // ... do something
        /// }
        /// </example>
        public static bool NetworkAvailable
        {
            get { return NetworkInterface.GetIsNetworkAvailable(); }
        }

        /// <summary>
        /// Gets a value indicating whether a connection to the Internet is available
        /// </summary>
        /// <value>
        /// 	<c>true</c> if an Internet connection is available..
        /// </value>
        /// <example>
        /// if (NetworkHelper.InternetConnectionAvailable)
        /// {
        ///     // ... do something
        /// }
        /// </example>
        public static bool InternetConnectionAvailable
        {
            get
            {
                if (NetworkAvailable)
                    return CanConnect("www.msn.com");
                return false;
            }
        }

        /// <summary>
        /// Gets the speed of the fastest available connection in bits per second.
        /// </summary>
        /// <value>Connection speed.</value>
        /// <remarks>
        /// Tunnel and Loopback adapters are ignored.
        /// </remarks>
        /// <example>
        /// if (NetworkHelper.FastestConnectionSpeed >= 54000000)
        /// {
        ///     // ... download something large
        /// }
        /// else
        /// {
        ///     MessageBox.Show("You need at least an 802.11a connection (or better) for this feature.");
        /// }
        /// </example>
        public static long FastestConnectionSpeed
        {
            get
            {
                if (NetworkAvailable)
                {
                    long maxSpeed = 0;
                    var networks = NetworkInterface.GetAllNetworkInterfaces();
                    foreach (NetworkInterface network in networks)
                        if (network.OperationalStatus == OperationalStatus.Up)
                            if (network.NetworkInterfaceType != NetworkInterfaceType.Loopback && network.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                                if (network.Speed > maxSpeed)
                                    maxSpeed = network.Speed;
                    return maxSpeed;
                }
                return -1;
            }
        }

        /// <summary>
        /// Gets all the IP addresses (IPv4) utilized by the current system.
        /// </summary>
        /// <value>All IP addresses.</value>
        /// <remarks>
        /// Only IPv4 addresses are included.
        /// Tunnel and Loopback adapters are ignored.
        /// Only adapters that are up are considered.
        /// </remarks>
        /// <example>
        /// StringBuilder sb = new StringBuilder();
        /// sb.AppendLine("IP Addresses used by this system:");
        /// foreach (IPAddressInformation address in NetworkHelper.AllIPAddresses)
        /// {
        ///     sb.AppendLine(address.Address.ToString());
        /// }
        /// MessageBox.Show(sb.ToString());
        /// </example>
        public static Collection<IPAddressInformation> AllIPAddresses
        {
            get
            {
                var addresses = new Collection<IPAddressInformation>();
                var networks = NetworkInterface.GetAllNetworkInterfaces();
                foreach (var network in networks)
                    if (network.OperationalStatus == OperationalStatus.Up)
                        if (network.NetworkInterfaceType != NetworkInterfaceType.Loopback && network.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                        {
                            var ipProperties = network.GetIPProperties();
                            foreach (var address in ipProperties.UnicastAddresses)
                                if (address.Address.AddressFamily == AddressFamily.InterNetwork)
                                    addresses.Add(address);
                        }
                return addresses;
            }
        }

        /// <summary>
        /// Gets all the IP addresses (IPv4) utilized by the current system in clear text representation.
        /// </summary>
        /// <value>All IP addresses.</value>
        /// <remarks>
        /// Only IPv4 addresses are included.
        /// Tunnel and Loopback adapters are ignored.
        /// Only adapters that are up are considered.
        /// </remarks>
        /// <example>
        /// StringBuilder sb = new StringBuilder();
        /// sb.AppendLine("IP Addresses used by this system:");
        /// foreach (string address in NetworkHelper.AllIPAddressesClearText)
        /// {
        ///     sb.AppendLine(address);
        /// }
        /// MessageBox.Show(sb.ToString());
        /// </example>
        public static Collection<string> AllIPAddressesClearText
        {
            get
            {
                var addresses = AllIPAddresses;
                var addressesClearText = new Collection<string>();
                foreach (var address in addresses)
                    addressesClearText.Add(address.Address.ToString());
                return addressesClearText;
            }
        }

        /// <summary>
        /// Gets the system's current IP address (IPv4) in clear text.
        /// </summary>
        /// <value>The current ip address.</value>
        /// <remarks>
        /// If the system currently uses multiple IP addresses, the first unicast address
        /// on the fastest adapter will be returned.
        /// </remarks>
        /// <example>
        /// MessageBox.Show("Current IP Address: " + NetworkHelper.CurrentIPAddressClearText);
        /// </example>
        public static string CurrentIPAddressClearText
        {
            get
            {
                var address = CurrentIPAddress;
                if (address != null)
                    return address.Address.ToString();
                return string.Empty;
            }
        }

        /// <summary>
        /// Gets the system's current IP address (IPv4).
        /// </summary>
        /// <value>The current ip address.</value>
        /// <remarks>
        /// If the system currently uses multiple IP addresses, the first unicast address
        /// on the fastest adapter will be returned.
        /// </remarks>
        /// <example>
        /// MessageBox.Show("Current IP Address: " + NetworkHelper.CurrentIPAddress.Address.ToString());
        /// </example>
        public static IPAddressInformation CurrentIPAddress
        {
            get
            {
                var networks = NetworkInterface.GetAllNetworkInterfaces();
                NetworkInterface fastestInterface = null;
                foreach (var network in networks)
                    if (network.OperationalStatus == OperationalStatus.Up)
                        if (network.NetworkInterfaceType != NetworkInterfaceType.Loopback && network.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                            if (fastestInterface == null)
                                fastestInterface = network;
                            else if (network.Speed > fastestInterface.Speed)
                                fastestInterface = network;
                if (fastestInterface != null)
                {
                    var ipProperties = fastestInterface.GetIPProperties();
                    foreach (UnicastIPAddressInformation address in ipProperties.UnicastAddresses)
                        if (address.Address.AddressFamily == AddressFamily.InterNetwork)
                            return address;
                    return null;
                }
                return null;
            }
        }

        /// <summary>
        /// Gets a collection of all current domains we are connected to.
        /// </summary>
        /// <value>All current domains.</value>
        /// <example>
        /// StringBuilder sb = new StringBuilder();
        /// sb.AppendLine("Current domains:");
        /// foreach (string domain in NetworkHelper.AllCurrentDomains)
        /// {
        ///     sb.AppendLine(domain);
        /// }
        /// MessageBox.Show(sb.ToString());
        /// </example>
        public static Collection<string> AllCurrentDomains
        {
            get
            {
                var domains = new Collection<string>();
                var networks = NetworkInterface.GetAllNetworkInterfaces();
                foreach (var network in networks)
                    if (network.OperationalStatus == OperationalStatus.Up)
                        if (network.NetworkInterfaceType != NetworkInterfaceType.Loopback && network.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                        {
                            string domain = network.GetIPProperties().DnsSuffix;
                            if (!string.IsNullOrEmpty(domain))
                                domains.Add(domain);
                        }
                return domains;
            }
        }

        /// <summary>
        /// Returns true, if the system is currently connected to the specified domain.
        /// </summary>
        /// <param name="domain">The domain (case insensitive).</param>
        /// <returns>True if connected to the domain.</returns>
        /// <example>
        /// if (NetworkHelper.CurrentlyConnectedToDomain("mydomain"))
        /// {
        ///     // ... do something with it
        /// }
        /// </example>
        public static bool CurrentlyConnectedToDomain(string domain)
        {
            var domains = AllCurrentDomains;
            foreach (string foundDomain in domains)
                if (StringHelper.Compare(domain, foundDomain, true))
                    return true;
            return false;
        }

        /// <summary>
        /// Determines whether it is possible to connect to the specified host.
        /// </summary>
        /// <param name="host">The host.</param>
        /// <returns>
        /// 	<c>true</c> if this instance can connect the specified host; otherwise, <c>false</c>.
        /// </returns>
        /// <example>
        /// if (NetworkHelper.CanConnect("www.Microsoft.com"))
        /// {
        /// // ... do something with the connection
        /// }
        /// </example>
        /// <remarks>Port 80 is used to make the connection attempt.</remarks>
        public static bool CanConnect(string host)
        {
            return CanConnect(host, 80);
        }

        /// <summary>
        /// Determines whether it is possible to connect to the specified host.
        /// </summary>
        /// <param name="host">The host.</param>
        /// <param name="port">The port.</param>
        /// <returns>
        /// 	<c>true</c> if this instance can connect the specified host; otherwise, <c>false</c>.
        /// </returns>
        /// <example>
        /// if (NetworkHelper.CanConnect("www.Microsoft.com", 80))
        /// {
        ///     // ... do something with the connection
        /// }
        /// </example>
        public static bool CanConnect(string host, int port)
        {
            try
            {
                var localHostname = Dns.GetHostName();
                var localHostEntry = Dns.GetHostEntry(localHostname);
                var remoteHostEntry = Dns.GetHostEntry(host);

                var networks = NetworkInterface.GetAllNetworkInterfaces();
                foreach (var network in networks)
                    if (network.OperationalStatus == OperationalStatus.Up)
                        if (network.NetworkInterfaceType != NetworkInterfaceType.Loopback && network.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                        {
                            var interfaceProperties = network.GetIPProperties();
                            var adapterAddressList = GetAllAddressesForAdapter(interfaceProperties);
                            foreach (var localHostAddress in localHostEntry.AddressList)
                                if (CanConnectFromAddress(localHostAddress, adapterAddressList, remoteHostEntry, port))
                                    return true;
                        }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Gets all addresses for an adapter.
        /// </summary>
        /// <param name="interfaceProperties">The interface properties.</param>
        /// <returns>List of IP Addresses</returns>
        /// <remarks>For internal use only</remarks>
        private static List<IPAddressInformation> GetAllAddressesForAdapter(IPInterfaceProperties interfaceProperties)
        {
            var adapterAddressList = interfaceProperties.AnycastAddresses.ToList();
            adapterAddressList.AddRange(interfaceProperties.MulticastAddresses);
            adapterAddressList.AddRange(interfaceProperties.UnicastAddresses);
            return adapterAddressList;
        }

        /// <summary>
        /// Determines whether it is possible to connect to the specified local host address.
        /// </summary>
        /// <param name="localHostAddress">The local host address.</param>
        /// <param name="adapterAddressList">The adapter address list.</param>
        /// <param name="remoteHostEntry">The remote host entry.</param>
        /// <param name="port">The port.</param>
        /// <returns>true if this instance can connect from the specified local host addr; otherwise false.</returns>
        /// <remarks>For internal use only</remarks>
        private static bool CanConnectFromAddress(IPAddress localHostAddress, IEnumerable<IPAddressInformation> adapterAddressList, IPHostEntry remoteHostEntry, int port)
        {
            foreach (var adapterAddress in adapterAddressList)
                if (localHostAddress.Equals(adapterAddress.Address))
                {
                    var localEndpoint = new IPEndPoint(localHostAddress, 8081);
                    if (CanConnectFromEndPoint(localEndpoint, remoteHostEntry, port))
                        return true;
                }
            return false;
        }

        /// <summary>
        /// Determines whether it is possible to connect to the remote host from the specified endpoint.
        /// </summary>
        /// <param name="localEndPoint">The local end point.</param>
        /// <param name="remoteHostEntry">The remote host entry.</param>
        /// <param name="port">The port.</param>
        /// <returns>
        /// 	<c>true</c> if this instance [can connect from end point] the specified local EP; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>For internal use only</remarks>
        private static bool CanConnectFromEndPoint(EndPoint localEndPoint, IPHostEntry remoteHostEntry, int port)
        {
            foreach (var remoteAddress in remoteHostEntry.AddressList)
            {
                try
                {
                    var remoteEndpoint = new IPEndPoint(remoteAddress, port);

                    var sock = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    var ipLocal = localEndPoint as IPEndPoint;
                    sock.Bind(ipLocal != null ? new IPEndPoint(ipLocal.Address, ipLocal.Port) : localEndPoint);

                    bool connected;
                    try
                    {
                        sock.Connect(remoteEndpoint);
                    }
                    catch (SocketException ex)
                    {
#if DEBUG
                        Console.WriteLine(ex.Message);
#endif
                    }
                    finally
                    {
                        connected = sock.Connected;
                        if (connected)
                            sock.Disconnect(false);
                        sock.Close();
                    }
                    if (connected)
                        return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
            return false;
        }

        /// <summary>
        /// Occurs when the IP Address of a network interface changes
        /// </summary>
        /// <remarks>
        /// This event is provided for consistency only. 
        /// It simply passes through to NetworkChange.NetworkAddressChanged
        /// </remarks>
        public static event NetworkAddressChangedEventHandler NetworkAddressChanged
        {
            add
            {
                NetworkChange.NetworkAddressChanged += value;
            }
            remove
            {
                NetworkChange.NetworkAddressChanged -= value;
            }
        }

        /// <summary>
        /// Occurs when the availability of a network changes
        /// </summary>
        /// <remarks>
        /// This event is provided for consistency only. 
        /// It simply passes through to NetworkChange.NetworkAvailabilityChanged
        /// </remarks>
        public static event NetworkAvailabilityChangedEventHandler NetworkAvailabilityChanged
        {
            add
            {
                NetworkChange.NetworkAvailabilityChanged += value;
            }
            remove
            {
                NetworkChange.NetworkAvailabilityChanged -= value;
            }
        }
    }
}
