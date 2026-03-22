using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace media_manager
{
    internal class mm_directory
    {
        static mm_directory? OpenDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                DirectoryInfo info = new DirectoryInfo(path);
                return new mm_directory(info);
            }
            return null;
        }
        DirectoryInfo mDirectoryInfo;
        private mm_directory(DirectoryInfo dirInfo)
        {
            mDirectoryInfo = dirInfo;
            Debug.Assert(mDirectoryInfo.Exists);
        }
        int NumberOfFiles
        {
            get
            {
                return 0;// mDirectoryInfo.GetF
            }
        }
    }
}
