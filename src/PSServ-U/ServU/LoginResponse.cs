using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using Microsoft.PowerShell.Commands;

namespace PSServU.ServU {
    
    /// <summary>
    /// Class in which the ServU XML response from a (successful) login can deserialized
    /// </summary>
    [XmlRoot("Response")]
    public class LoginResponse {

        /// <summary>
        /// Result status of login request
        /// </summary>
        /// <remarks>
        /// 0 indicates successful login
        /// </remarks>
        public int Result { get; set; }
        public string EncSessionID { get; set; }
        public string SessionID { get; set; }
        public string CsrfToken { get; set; }
        public bool CanChangePassword { get; set; }
        public string HeaderURL { get; set; }
        public bool IsLocalAdmin { get; set; }
        public string UserEmailAddress { get; set; }
        public bool UserCanSetEmailAddress { get; set; }
        public string RequireEmailAddress { get; set; }
        public string WelcomeMsg { get; set; }
    }
}
