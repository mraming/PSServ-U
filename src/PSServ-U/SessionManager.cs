using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;

namespace PSServU {

    /// <summary>
    /// Manage PS Sessions to ServU server
    /// </summary>
    internal static class SessionManager {
        public static Session NewSession(SessionState pssession) {
            int lastSessionId = 0;
            // Get copy of sessions from the global variable or new empty list
            var sessions = pssession.PSVariable.GetValue("Global:ServUSessions") as List<Session> ?? new List<Session>();
            if(sessions.Any()) lastSessionId = sessions.Max(s => s.Sessionid);

            var session = new Session(lastSessionId++);
            // Set the Global Variable for the sessions.
            pssession.PSVariable.Set((new PSVariable("Global:SshSessions", sessions, ScopedItemOptions.AllScope)));

            return session;
        }
    }
}
