using System;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using PSServU;
using PSServU.ServU;

namespace PSServ_U {
    [Cmdlet(VerbsCommon.New, "ServUSession")]
    [OutputType(typeof(Session))]
    public class NewSessionCmdlet : PSCmdlet {
        // Hosts to conect to
        [ValidateNotNullOrEmpty]
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, Position = 0, HelpMessage = "URl to ServU server to connect to")]
        public string[] Url { get; set; }

        // Credentials for Connection
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, Position = 1, HelpMessage = "Credentials used to connect to ServU Server")]
        [ValidateNotNullOrEmpty, Credential]
        public PSCredential Credential { get; set; }

        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true, HelpMessage = "Optional FQDN or IP Address to proxy server to use for connection")]
        public String ProxyServer { get; set; }

        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true, HelpMessage = "Optional TCP port of proxy server to use - 8080 when omitted but proxy server is specified")]
        public Int32 ProxyPort { get; set; }

        [ValidateNotNullOrEmpty, Credential()]
        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true, HelpMessage = "Optional credentials for proxy server")]
        public PSCredential ProxyCredential { get; set; }

        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true, HelpMessage = "Optional interval in seconds between NOOP keepalive requests to keep server authentication session alive; no keepalive when not specified. FTP Voyager is approx. 30 secs")]
        public int? KeepAliveInterval { get; set; } = null;

        protected override void ProcessRecord() {
            foreach(var url in Url) {
                var suClient = new Client();
                var session = SessionManager.NewSession(SessionState);

                try {
                    session.Connect(url, Credential.UserName, Credential.GetNetworkCredential().Password);
                    WriteObject(session, true);
                } catch(Exception e) {
                    WriteError(new ErrorRecord(e, null, ErrorCategory.NotSpecified, session));
                }
            }
        }
    }
}
