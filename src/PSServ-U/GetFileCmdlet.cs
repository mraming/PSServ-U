using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using PSServU;
using PSServU.ServU;
using static PSServU.ServU.Client;

namespace PSServ_U {
    [Cmdlet(VerbsCommon.Get, "ServUFile", DefaultParameterSetName = "SessionId")]
    public class GetFileCmdlet : PSCmdlet {
        /// <summary>
        /// Index of the ServU Session.
        /// </summary>
        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true, Position = 0, ParameterSetName = "SessionId")]
        public int[] SessionId { get; set; }

        /// <summary>
        /// Session paramter that takes a session object 
        /// </summary>
        [ValidateNotNullOrEmpty]
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, Position = 0, ParameterSetName = "Session")]
        public Session[] Session { get; set; }

        /// <summary>
        /// Remote path and file name to get
        /// </summary>
        [ValidateNotNullOrEmpty]
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, Position = 1)]
        public string[] RemoteFile { get; set; }

        /// <summary>
        /// Local path where downloaded files will be stored
        /// </summary>
        /// <remarks>Current folder when omitted</remarks>
        [ValidateNotNullOrEmpty]
        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true, Position = 2)]
        [Alias("PSPath")]
        public string LocalPath { get; set; }

        /// <summary>
        /// Overwrite file on local filesystem if it already exists
        /// </summary>
        [Parameter(Position = 3)]
        public SwitchParameter Overwrite { get; set; }

        // Supress progress bar.
        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true, ParameterSetName = "SessionId")]
        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true, ParameterSetName = "Session")]
        public SwitchParameter NoProgress { get; set; }

        private IReadOnlyList<Session> SessionsToProcess { get; set; }

        protected override void BeginProcessing() {
            // Collect the sessions
            base.BeginProcessing();
            switch(ParameterSetName) {
                case "Session":
                    SessionsToProcess = Session;
                    break;
                case "SessionId":
                    SessionsToProcess = SessionManager.GetSessionsById(SessionState, SessionId);
                    break;
                default:
                    throw new NotImplementedException($"Support for ParameterSetName {ParameterSetName} has not been implemented");
            }
        }

        protected override void ProcessRecord() {

            bool reportProgress = !NoProgress.ToBool();
            int activityId = 0;
            string fqfn = null;
            int lastPercentReported = -1;
            ProgressReportHandler progressHandler = (int percent, long bytesSent, long? totalBytes) => {
                if(lastPercentReported != percent) {
                    var progressRecord = new ProgressRecord(activityId, $"Uploading {fqfn}", $"{bytesSent:#,##0} Bytes Uploaded of {totalBytes:#,##0}") {
                        PercentComplete = percent
                    };

                    Host.UI.WriteProgress(1, progressRecord);
                    lastPercentReported = percent;
                }
            };

            foreach(var session in SessionsToProcess) {
                activityId++;
                lastPercentReported = -1;
                try {
                    if(reportProgress) session.Client.ProgressReport += progressHandler;

                    foreach(var rf in RemoteFile) {
                        fqfn = rf;
                        lastPercentReported = -1;
                        try {
                            session.Client.DownloadFile(rf, LocalPath, Overwrite.ToBool());
                        } catch(Exception e) {
                            WriteError(new ErrorRecord(e, null, ErrorCategory.NotSpecified, session));
                        }
                    }

                } finally {
                    if(reportProgress) session.Client.ProgressReport -= progressHandler;
                }
            }
        }
    }
}
