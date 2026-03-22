namespace media_manager
{
    internal class Program
    {
        static void DisplayUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("media_manager -import <source_folder> <destination_folder>");
        }
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
            if (args.Length > 0)
            {
                for (int index = 0; index < args.Length; index++)
                {
                    Console.WriteLine("[{0}] = {1}",index, args[index]);
                }
                if (args[0] == "-import")
                {
                    //scan through a source folder
                    //find all supported media files
                    //for each supported media file
                    //  if that file is already present ignore it.
                    //  else copy it to the appropriate location in history.
                    //       add it to shaindex
                }
                if (args[0] == "file_exists")
                {
                    if (args.Length >= 2)
                    {
                        bool result = File.Exists(args[1]);
                        Console.WriteLine("File.Exists(\"{0}\") == {1}", args[1], result);
                    }
                }
                if (args[0] == "dir_exists")
                {
                    if (args.Length >= 2)
                    {
                        bool result = Directory.Exists(args[1]);
                        Console.WriteLine("Directory.Exists(\"{0}\") == {1}", args[1], result);
                    }
                }
                if (args[0] == "creation_time")
                {
                    if (args.Length >= 2)
                    {
                        DateTime result = File.GetCreationTime(args[1]);
                        Console.WriteLine("File.GetCreationTime(\"{0}\") == {1}", args[1], result);
                    }
                }
                if (args[0] == "last_access_time")
                {
                    if (args.Length >= 2)
                    {
                        DateTime result = File.GetLastAccessTime(args[1]);
                        Console.WriteLine("File.GetLastAccessTime(\"{0}\") == {1}", args[1], result);
                    }
                }
                if (args[0] == "last_write_time")
                {
                    if (args.Length >= 2)
                    {
                        DateTime result = File.GetLastWriteTime(args[1]);
                        Console.WriteLine("File.GetLastWriteTime(\"{0}\") == {1}", args[1], result);
                        Console.WriteLine("    y{0:D4}.m{1:D2}.d{2:D2}_h{3:D2}:m{4:D2}:s{5:D2}:ms{6:D3}:us{7:D3}",
                            result.Year, result.Month, result.Day,
                            result.Hour, result.Minute, result.Second, result.Millisecond, result.Microsecond);
                    }
                }
                if (args[0] == "copy")
                {
                    if (args.Length >= 3)
                    {
                        File.Copy(args[1], args[2]);
                    }
                }
                if (args[0] == "list")
                {
                    DirectoryInfo info = new DirectoryInfo(args[1]);
                    FileInfo[] files = info.GetFiles();
                    if (files != null)
                    {
                        Console.WriteLine("{0} files", files.Length);
                        for (int i = 0; i < files.Length; i++)
                        {
                            Console.WriteLine("[{0}] = \"{1}\"", i, files[i].Name);
                        }
                    }
                }
                if (args[0] == "dump")
                {
                    FileStream stream = File.OpenRead(args[1]);
                    BinaryReader br = new BinaryReader(stream);
                    br.BaseStream.Position = 0;
                    long length = br.BaseStream.Length;
                    long offset = 0;
                    if (length > 256)
                        length = 256;
                    while (length > 0) {
                        long lineLength = 16;
                        if (length < lineLength)
                            lineLength = length;
                        byte[] data = br.ReadBytes((int)lineLength);
                        Console.Write("{0:X4} ", offset);
                        int byteIndex = 0;
                        while (byteIndex < data.Length)
                        {
                            Console.Write("{0:X2} ", data[byteIndex]);
                            byteIndex++;
                        }
                        while (byteIndex < 16)
                        {
                            Console.Write("   ");
                            byteIndex++;
                        }
                        Console.Write(" ");
                        byteIndex = 0;
                        while (byteIndex < data.Length)
                        {
                            byte ch = data[byteIndex];
                            if ((ch >= 32) && (ch <= 126))
                            {
                                Console.Write("{0}", (char)ch);
                            }
                            else
                            {
                                Console.Write(".");
                            }
                            byteIndex++;
                        }
                        Console.WriteLine();
                        length -= lineLength;
                        offset += lineLength;
                    }
                }
                if (args[0] == "read")
                {
                    FileStream stream = File.OpenRead(args[1]);
                    BinaryReader br = new BinaryReader(stream);
                    br.BaseStream.Position = 0;
                    long filelength = br.BaseStream.Length;
                    byte[] length_bytes = br.ReadBytes(4);
                    uint length = (((uint)length_bytes[0]) << 24) |
                                  (((uint)length_bytes[1]) << 16) |
                                  (((uint)length_bytes[2]) << 8) |
                                  (((uint)length_bytes[3]) << 0);
                    char[] label = br.ReadChars(4);
                    string label_str = String.Format("{0}{1}{2}{3}", label[0], label[1], label[2], label[3]);
                    Console.WriteLine("Root Box");
                    Console.WriteLine("Length = {0}", length);
                    Console.WriteLine("label = {0}", label_str);

                }
            }
            else
            {
                Console.WriteLine("no args");
            }
        }
    }
}
