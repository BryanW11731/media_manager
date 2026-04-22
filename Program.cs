namespace media_manager
{
    internal class Program
    {
        static void DisplayUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("media_manager -import <source_folder> <destination_folder>");
        }
        static void Dump(byte[] data)
        {
            int line_index = 0;
            while (line_index < data.Length)
            {
                int index = line_index;
                int last_index = index + 0x10;

                Console.Write("{0:X4} ", line_index);

                while (index < last_index)
                {
                    if ((index % 4) == 0)
                    {
                        Console.Write(" ");
                    }
                    if (index < data.Length)
                    {
                        Console.Write("{0:X2} ", data[index]);
                    }
                    else
                    {
                        Console.Write("   ");
                    }
                    index++;
                }

                index = line_index;
                while (index < last_index)
                {
                    if ((index % 4) == 0)
                    {
                        Console.Write(" ");
                    }
                    if (index < data.Length)
                    {
                        char ch = (char)data[index];
                        if ((ch >= 32) && (ch <= 126))
                        {
                            Console.Write("{0}", ch);
                        }
                        else
                        {
                            Console.Write(".");
                        }
                    }
                    else
                    {
                        Console.Write(" ");
                    }
                    index++;
                }

                Console.WriteLine();

                line_index += 0x10;
            }
        }
        static bool CheckFile(string path)
        {
            try
            {
                Console.WriteLine("Checking \"{0}\"", path);
                FileStream stream = File.OpenRead(path);
                BinaryReader br = new BinaryReader(stream);
                mm_file file = new mm_file(br);
                mm_file_box box = new mm_file_box(file);
                if (box.Name != "ftyp")
                {
                    Console.WriteLine("missing ftyp box type");
                    return false;
                }
                if (box.RemainingLength < 8)
                {
                    Console.WriteLine("ftyp Length to short");
                    return false;
                }
                string str = box.ReadCharsAsString(4);
                if (str != "heic")
                {
                    Console.WriteLine("missing heic as major brand");
                    return false;
                }
                UInt32 minor_version = box.ReadUInt32BE();
                if (minor_version != 0)
                {
                    Console.WriteLine("minor_version = {0}, expected 0", minor_version);
                    return false;
                }
                if ((box.RemainingLength % 4) != 0)
                {
                    Console.WriteLine("ftyp Length not a multiple of 4");
                    return false;
                }
                bool found = false;
                while (box.RemainingLength >= 4)
                {
                    str = box.ReadCharsAsString(4);
                    if (str == "heic")
                    {
                        found = true;
                    }
                }
                if (!found)
                {
                    Console.WriteLine("ftyp missing heic in compatible_brands");
                    return false;
                }
                mm_file_box meta_box = new mm_file_box(file);
                if (meta_box.Name != "meta")
                {
                    Console.WriteLine("missing meta box type");
                    return false;
                }
                byte meta_version = meta_box.ReadBytes(1)[0];
                if (meta_version != 0)
                {
                    Console.WriteLine("meta_version = {0}, expected 0", meta_version);
                    return false;
                }
                byte[] meta_flags = meta_box.ReadBytes(3);
                if ((meta_flags[0] != 0) || (meta_flags[1] != 0) || (meta_flags[2] != 0))
                {
                    Console.WriteLine("meta_flags != 0");
                    return false;
                }
                Console.WriteLine("size of meta box is {0} (0x{0:X4})", meta_box.Length);
                mm_item_info? itemInfo = null;
                mm_item_location? itemLocation = null;
                //byte[] data = null;
                while (meta_box.RemainingLength > 0)
                {
                    box = meta_box.ReadBox();
                    Console.WriteLine("found sub box \"{0}\"", box.Name);
                    if (box.Name == "iinf")
                    {
                        //data = box.ReadBytes((int)box.Length);
                        //Dump(data);
                        //box.Position = 0;
                        if (itemInfo != null)
                        {
                            throw new Exception("duplicat iinf found");
                        }
                        itemInfo = new mm_item_info(box);
                        int count = itemInfo.CountType("hvc1");
                        Console.WriteLine("iinf box: hvc1 count = {0}",
                            count);
                        count = itemInfo.CountType("Exif");
                        Console.WriteLine("iinf box: Exif count = {0}",
                            count);
                    }
                    if (box.Name == "iloc")
                    {
                        if (itemLocation != null)
                        {
                            throw new Exception("duplicate iloc found");
                        }
                        itemLocation = new mm_item_location(box);
                    }
                }
                if (itemInfo == null)
                {
                    throw new Exception("iinf not found");
                }
                if (itemLocation == null)
                {
                    throw new Exception("iloc not found");
                }
                if (itemInfo.CountType("Exif") != 1)
                {
                    throw new Exception("expected one and only one Exif section");
                }
                UInt16 id = itemInfo.GetID("Exif", 0);
                mm_item_location_element? element = itemLocation.GetElementByID(id);
                if (element == null)
                {
                    throw new Exception("cannot find Exif element in iloc");
                }
                Console.WriteLine("Found Exif at {0:X8}, length {1}",
                    element.Offset, element.Length);
                mm_file_region exif_region = new mm_file_region(file, element.Offset, element.Length);
                mm_exif exif = new mm_exif(exif_region);
                mm_tiff_header tiff = exif.Tiff;
                mm_file_region tiff_region = exif.TiffRegion;
                UInt32 ifd_offset = tiff.IfdOffset;
                while (ifd_offset > 0)
                {
                    Console.WriteLine("Found IFD at 0x{0:X4}", ifd_offset);
                    tiff_region.Position = ifd_offset;
                    mm_tiff_ifd ifd = new mm_tiff_ifd(tiff_region);
                    for (int i = 0; i < ifd.Length; i++)
                    {
                        if (ifd[i].Tag == 0x8769)
                        {
                            Console.WriteLine("Found Tag 0x8769, Type = {0}, VO = {1}",
                                ifd[i].Type, ifd[i].ValueOffset);
                            tiff_region.Position = ifd[i].ValueOffset;
                            mm_tiff_ifd exifSubIfd = new mm_tiff_ifd(tiff_region);
                            for (int j = 0; j < exifSubIfd.Length; j++)
                            {
                                Console.WriteLine("Found inside exifSufIfd, tag = 0x{0:X4}",
                                    exifSubIfd[j].Tag);
                                if (exifSubIfd[j].Tag == 0x9003)
                                {
                                    Console.WriteLine("Found Tag 0x9003, Type = {0}, count = {1}, VO = {2}",
                                        exifSubIfd[j].Type, exifSubIfd[j].Count, exifSubIfd[j].ValueOffset);
                                    tiff_region.Position = exifSubIfd[j].ValueOffset;
                                    string DateTimeOriginal = tiff_region.ReadString();
                                    Console.WriteLine("  DateTimeOriginal = \"{0}\"",
                                        DateTimeOriginal);
                                }
                            }
                        }
                    }
                    ifd_offset = ifd.NextIfdOffset;
                }
                //data = exif_region.ReadBytes((int)element.Length);
                //Dump(data);
                //TODO create mm_exif
                //  read tiff
                //    create sub tiff items
            } catch (Exception ex) {
                Console.WriteLine("CheckFile: exception, {0}", ex.Message);
                return false;
            }
            return true;
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
                    mm_file file = new mm_file(br);
                    mm_file_box box = file.ReadBox();
                    Console.WriteLine("Root Box");
                    Console.WriteLine("Length = {0}", box.Length);
                    Console.WriteLine("label = {0}", box.Name);


                    /*
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
                    */
                }
                if (args[0] == "check")
                {
                    if (CheckFile(args[1]))
                    {
                        Console.WriteLine("{0} Passed", args[1]);
                    }
                    else
                    {
                        Console.WriteLine("{0} Failed", args[1]);
                    }
                }
            }
            else
            {
                Console.WriteLine("no args");
            }
        }
    }
}
