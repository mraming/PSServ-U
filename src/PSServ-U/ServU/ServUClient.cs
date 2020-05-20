using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace PSServU.ServU {
    /// <summary>
    /// Client API for SOLARWINDs Serv-U file transfer server
    /// </summary>
    public class ServUClient {
        private Uri baseAddress;
        private HttpClient httpClient;

        public string Connect(string url, string userName, string password) {
            return ConnectAsync(url, userName, password).GetAwaiter().GetResult();
        }

        public async Task<string> ConnectAsync(string url, string userName, string password) {
            baseAddress = new Uri(url);
            var postData = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("user", userName),
                new KeyValuePair<string, string>("pword", password)
            };

            HttpContent content = new FormUrlEncodedContent(postData);
            var cookieJar = new CookieContainer();
            var handler = new HttpClientHandler {
                CookieContainer = cookieJar,
                UseCookies = true,
                UseDefaultCredentials = false
            };

            httpClient = new HttpClient(handler) {
                BaseAddress = baseAddress
            };

            using(HttpResponseMessage response = await httpClient.PostAsync("/Common/Java/Responses/Login.xml?Command=Login", content)) {    
                response.EnsureSuccessStatusCode();
                var ser = new XmlSerializer(typeof(LoginResponse));
                var loginResponse = (LoginResponse)ser.Deserialize(await response.Content.ReadAsStreamAsync());
                httpClient.DefaultRequestHeaders.Add("X-Csrf-Token", loginResponse.CsrfToken);
                return loginResponse.WelcomeMsg;
            }
        }

        public override string ToString() {
            return baseAddress.ToString();
        }
    }
}
