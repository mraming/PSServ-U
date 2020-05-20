using System;
using System.Collections.Generic;
using System.Text;
using PSServU.ServU;

namespace PSServU {
    internal class Session {

        internal Session(int sessionId) {
            Sessionid = sessionId;
        }

        public int Sessionid { get;}
        public ServUClient Client { get; private set; }

        /// <summary>
        /// Disconnect the ServU session
        /// </summary>
        /// <remarks>
        /// ServU is HTTP based and 'stateless', but we have an optional Keep Alive timer making NOOP (No Operation) requests
        /// to keep the login session alive. This will make an explicit logout call to the server to terminate the server
        /// mainteained session.
        /// </remarks>
        public void Disconnect() {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Establish a 'connection' to the ServU server and login
        /// </summary>
        /// <returns>Server welcome message from successful login</returns>
        public string Connect(string url, string userName, string password) {
            Client = new ServUClient();
            return Client.Connect(url, userName, password);
        }
    }
}
