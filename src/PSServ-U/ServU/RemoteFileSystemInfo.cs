using System;
using System.Collections.Generic;
using System.Text;

namespace PSServU.ServU {

    public enum RemoteFileSystemItemType {
        Directory,
        File
    }

    /// <summary>
    /// Provides information about a file or directory item in the remove file system folder/directory
    /// </summary>
    public  class RemoteFileSystemInfo {
        public RemoteFileSystemItemType Type { get; set; }
        public string Name { get; set; }
        public string FullName { get; set; }
        public DateTimeOffset LastWriteTime { get; set; }
        public DateTimeOffset LastAccessTime { get; set; }
        public DateTimeOffset CreationTime { get; set; }
        public long? Length { get; set; }
    }
}
