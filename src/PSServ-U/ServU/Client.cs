using System;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using System.Net;
using System.Net.Http;
using System.Net.Http.Handlers;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace PSServU.ServU {
    /// <summary>
    /// Client API for SOLARWINDs Serv-U file transfer server
    /// </summary>
    public class Client {
        private Uri baseAddress;
        private HttpClient httpClient;
        private int transferId = new Random().Next(100000, 1000000);


        public delegate void ProgressReportHandler(int percentage, long bytesSent, long? totalBytes);
        public event ProgressReportHandler ProgressReport;

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

            var progressHandler = new ProgressMessageHandler(handler);
            progressHandler.HttpSendProgress += HttpSendProgress;
            progressHandler.HttpReceiveProgress += HttpReceiveProgress;

            
            httpClient = new HttpClient(progressHandler) {
                BaseAddress = baseAddress,
                Timeout = Timeout.InfiniteTimeSpan
            };

            using(HttpResponseMessage response = await httpClient.PostAsync("/Common/Java/Responses/Login.xml?Command=Login", content)) {    
                response.EnsureSuccessStatusCode();
                var ser = new XmlSerializer(typeof(LoginResponse));
                var loginResponse = (LoginResponse)ser.Deserialize(await response.Content.ReadAsStreamAsync());
                httpClient.DefaultRequestHeaders.Add("X-Csrf-Token", loginResponse.CsrfToken);
                httpClient.DefaultRequestHeaders.Add("User-Agent", "FTP Voyager JV 15.1.7.162");
                httpClient.DefaultRequestHeaders.Add("X-User-Agent", "FTP%20Voyager%20JV%2015.1.7.162");
                httpClient.DefaultRequestHeaders.Add("X-Is-FVJV", "1");
                httpClient.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
                httpClient.DefaultRequestHeaders.Add("Pragma", "no-cache");
                return loginResponse.WelcomeMsg;
            }
        }

        public List<RemoteFileSystemInfo> GetFileSystemInfo(string remotePath) {
            return GetFileSystemInfoAsync(remotePath).GetAwaiter().GetResult();
        }

        public async Task<List<RemoteFileSystemInfo>> GetFileSystemInfoAsync(string remotePath) {
            if(string.IsNullOrWhiteSpace(remotePath)) {
                remotePath = "/";
            } else {
                // Ensure remotePath starts end ends with a /
                if(!remotePath.StartsWith("/")) remotePath = "/" + remotePath;
                if(!remotePath.EndsWith("/")) remotePath += "/";
            }

            var result = new List<RemoteFileSystemInfo>();

            using(HttpResponseMessage response = await httpClient.GetAsync($"/?Command=List&InternalDir=Common&InternalFile=/Java/Responses/List.csv&ListFile=/Java/Responses/List.csv&Dir={Uri.EscapeDataString(remotePath)}")) {
                response.EnsureSuccessStatusCode();
                using(var sr = new StreamReader(await response.Content.ReadAsStreamAsync())) {
                    // The response is in csv format. The first row has information about the directory iteself including
                    // the directory name. No reverse-engineering done on the meaning of these columns.
                    // The subsequent columns hold file and directory information with the following 19 columns:
                    // 0  - File Name
                    // 1  - File Size (Bytes)
                    // 2  - Last Modified date (Unix Epoch seconds)
                    // 3  - Last Access (Unix Epoch seconds)
                    // 4  - Created On (Unix Epoch seconds)
                    // 5  - IsDirectory
                    // 6  - IsReadOnly
                    // 7  - IsLink
                    // 8  - IsImage
                    // 9  - IsDrive
                    // 10 - FileDriveType
                    // 11 - FileDriveLabel
                    // 12 - FileIsNormal
                    // 13 - FileIsHidden
                    // 14 - FileIsCompressed
                    // 15 - FileIsEncrypted
                    // 16 - FileIsArchived
                    // 17 - FileIsAudio
                    // 18 - FileIsVideo
                    // 
                    // By skipping rows with fewer than 9 columns, we skip the row with directory information and prevent
                    // index out of range exceptions when parsing the result string
                    while(!sr.EndOfStream) {
                        var line = await sr.ReadLineAsync();
                        if(line != null) {
                            var cols = line.Split(',');
                            // Skip header row which has fewer than 9 columns
                            if(cols.Length >= 9) {
                                var name = Uri.UnescapeDataString(cols[0]);
                                RemoteFileSystemInfo item = new RemoteFileSystemInfo {
                                    Type = cols[5] == "1" ? RemoteFileSystemItemType.Directory : RemoteFileSystemItemType.File,
                                    Length = cols[5] == "0" ? long.Parse(cols[1]) : (long?)null,
                                    CreationTime = DateTimeOffset.FromUnixTimeSeconds(long.Parse(cols[4])),
                                    FullName = Path.Combine(remotePath, name),
                                    LastAccessTime = DateTimeOffset.FromUnixTimeSeconds(long.Parse(cols[3])),
                                    LastWriteTime = DateTimeOffset.FromUnixTimeSeconds(long.Parse(cols[2])),
                                    Name = name
                                };

                                result.Add(item);
                            }
                        }
                    }
                }
            }
            return result;
        }

        public void SendFile(string file, string remotePath) {
            SendFileAsync(file, remotePath).GetAwaiter().GetResult();
        }

        public async Task SendFileAsync(string file, string remotePath) {
            if(string.IsNullOrWhiteSpace(remotePath)) {
                remotePath = "/";
            } else {
                // Ensure remotePath starts end ends with a /
                if(!remotePath.StartsWith("/")) remotePath = "/" + remotePath;
                if(!remotePath.EndsWith("/")) remotePath += "/";
            }

            // A ServU quirc: The server is not stateless - even beyond the authentication aspect. We actually need to list
            // the directory first as that list list directory determines the folder where the file is uploaded
            await GetFileSystemInfoAsync(remotePath);

            var fileName = Path.GetFileName(file);

            // To prevent the upload session from being terminated by the server, we need to query the Transfer Stats
            // regularly. We do this using a timer that who's callback gets the transfer status ever 5 seconds
            var statusTimer = new Timer(statusTimerCallback, transferId, 5000, 5000);

            using(var fs = new FileStream(file, FileMode.Open, FileAccess.Read)) {

                HttpContent fileStreamContent = new StreamContent(fs);
                // Set your own boundary, otherwise the internal boundary between multiple parts remains with quotes which
                // RFC compliant but ServU chokes on.
                string boundaryDelimiter = $"----------{DateTime.Now.Ticks:x}";
                using(var _multiPartContent = new MultipartFormDataContent(boundaryDelimiter)) {
                    _multiPartContent.Add(fileStreamContent, "file1", fileName);
                    //remove quotes from ContentType Header
                    _multiPartContent.Headers.Remove("Content-Type");
                    _multiPartContent.Headers.TryAddWithoutValidation("Content-Type", "multipart/form-data; boundary=" + boundaryDelimiter);

                    var response = httpClient.PostAsync($"?Command=Upload&Dir={Uri.EscapeDataString(remotePath)}&InternalDir=Common&InternalFile=/Java/Responses/Result.csv&File={Uri.EscapeDataString(fileName)}&TransferID={transferId++}", _multiPartContent).Result;
                    if(!response.IsSuccessStatusCode) {
                        var body = await response.Content.ReadAsStringAsync();
                        throw new ApplicationException($"Upload failed with response [{body}]");
                    }
                }
            }
        }

        private void statusTimerCallback(object state) {
            int tid = (int)state;

            // We're not really gona use the result - we just call the remote end to prevent a long running transfer
            // from being terminated. Kindof a silly keep-alive.

            // Note: HttpClient.GetAsync is thread-safe so we can safely reuse the HttpClient instance that's also used
            // for the upload of the file. In fact, we should...

            using(HttpResponseMessage response = httpClient.GetAsync($"/Web%20Client/FileUpload.xml?Command=TransferStats&TransferID={tid}").Result) {
                response.EnsureSuccessStatusCode();
                // We don't need the response, so we just read the entire response but don't use it.
                response.Content.ReadAsStringAsync().Wait();
            }
        }

        public void DownloadFile(string remoteFile, string localPath, bool overwrite = false) {
            DownloadFileAsync(remoteFile, localPath, overwrite).GetAwaiter().GetResult();
        }

        public async Task DownloadFileAsync(string remoteFile, string localPath, bool overwrite = false) {
            if(string.IsNullOrWhiteSpace(remoteFile)) throw new ArgumentNullException("remoteFile", "remoteFile is a required parameter that cannot be null, empty or whitespace only");
            // Ensure remoteFile starts end ends with a /
            if(!remoteFile.StartsWith("/")) remoteFile = "/" + remoteFile;

            string fileName = Path.GetFileName(remoteFile);
            string localFileName = string.IsNullOrWhiteSpace(localPath) ? fileName : Path.Combine(localPath, fileName);

            using(HttpResponseMessage response = await httpClient.GetAsync($"/?Command=Download&File={Uri.EscapeDataString(remoteFile)}&InternalDir=Common&InternalFile=/Java/Responses/Result.csv")) {
                response.EnsureSuccessStatusCode();

                // ServU quirc: When the remote file doesn't exist, we still get an HTTP Success (200) response.
                // But we can detect this condition in several ways:
                // - Either of the following headers is not in the response: Content-Type, Content-Disposition, Last-Modified, SU-Last-Modified
                // - Response body only has the digit 6
                // For now, we'll just look for the SU-Last-Modified header (I was having trouble checking the Content-Type
                //and Content-Disposition header). If the SU-Last-Modfied header not there, we assume file is not found.

                if(response.Headers.Contains("SU-Last-Modified")) {
                    using(var fs = new FileStream(localFileName, overwrite ? FileMode.Create : FileMode.CreateNew, FileAccess.Write)) {
                        await response.Content.CopyToAsync(fs);
                        fs.Close();
                    }
                } else {
                    throw new FileNotFoundException($"Remote file [{remoteFile}] not found");
                }
            }
        }

        private void HttpSendProgress(object sender, HttpProgressEventArgs e) {
            if(ProgressReport != null) ProgressReport(e.ProgressPercentage, e.BytesTransferred, e.TotalBytes);
        }

        private void HttpReceiveProgress(object sender, HttpProgressEventArgs e) {
            if(ProgressReport != null) ProgressReport(e.ProgressPercentage, e.BytesTransferred, e.TotalBytes);
        }
    }
}
