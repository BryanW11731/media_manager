using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using static System.Net.WebRequestMethods;

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
    internal abstract class mm_reader
    {
        internal abstract long Length { get; }
        internal abstract long Position { get; set; }
        internal abstract long RemainingLength { get; }
        internal abstract byte[] ReadBytes(uint count);
        internal abstract byte ReadUInt8();
        internal abstract UInt16 ReadUInt16BE();
        internal abstract UInt32 ReadUInt24BE();
        internal abstract UInt32 ReadUInt32BE();
        internal abstract string ReadCharsAsString(uint count);
        internal abstract mm_file_box ReadBox();
        internal abstract string ReadString();
    }
    internal class mm_file : mm_reader
    {
        BinaryReader mReader;
        internal mm_file(BinaryReader br)
        {
            mReader = br;
        }
        internal override long Length
        {
            get
            {
                return mReader.BaseStream.Length;
            }
        }
        internal override long Position
        {
            get
            {
                return mReader.BaseStream.Position;
            }
            set
            {
                mReader.BaseStream.Position = value;
            }
        }
        internal override long RemainingLength
        {
            get
            {
                return mReader.BaseStream.Length - mReader.BaseStream.Position;
            }
        }
        internal override byte[] ReadBytes(uint count)
        {
            return mReader.ReadBytes((int)count);
        }
        internal override byte ReadUInt8()
        {
            if (RemainingLength < 1)
            {
                throw new Exception("ReadUInt8: minimum length violation");
            }
            return mReader.ReadByte();
        }
        internal override UInt16 ReadUInt16BE()
        {
            if (RemainingLength < 2)
            {
                throw new Exception("ReadUInt16BE: minimum length violation");
            }
            byte[] data = mReader.ReadBytes(2);
            return (UInt16)((((UInt16)data[0]) << 8) |
                    (((UInt16)data[1]) << 0));
        }
        internal override UInt32 ReadUInt24BE()
        {
            if (RemainingLength < 3)
            {
                throw new Exception("ReadUInt24BE: minimum length violation");
            }
            byte[] data = mReader.ReadBytes(3);
            return (((UInt32)data[0]) << 16) |
                   (((UInt32)data[1]) << 8) |
                   (((UInt32)data[2]) << 0);
        }
        internal override UInt32 ReadUInt32BE()
        {
            if (RemainingLength < 4)
            {
                throw new Exception("ReadUInt32BE: minimum length violation");
            }
            byte[] data = mReader.ReadBytes(4);
            return (((UInt32)data[0]) << 24) |
                    (((UInt32)data[1]) << 16) |
                    (((UInt32)data[2]) << 8) |
                    (((UInt32)data[3]) << 0);
        }
        internal override string ReadCharsAsString(uint count)
        {
            if (RemainingLength < count)
            {
                throw new Exception("ReadCharsAsString: minimum length violation");
            }
            string result = "";
            byte[] chars = mReader.ReadBytes((int)count);
            for (int index = 0; index < chars.Length; index++)
            {
                result += (char)chars[index];
            }
            return result;
        }
        internal override mm_file_box ReadBox()
        {
            return new mm_file_box(this);
        }
        internal override string ReadString()
        {
            string result = "";
            byte ch = 0;
            while ((ch = mReader.ReadByte()) != 0)
            {
                result += (char)ch;
            }
            return result;
        }
    }
    internal class mm_file_box : mm_reader
    {
        internal const int cHeaderSize = 8;
        mm_reader mReader;
        long mBasePosition;
        uint mBoxLength;
        uint mBoxOffset;
        string mBoxName;

        internal mm_file_box(mm_reader reader)
        { 
            //assume reader is already pointing to the box start
            mReader = reader;
            
            mBasePosition = mReader.Position;
            long remainingLength = mReader.RemainingLength;

            if (remainingLength < cHeaderSize)
            {
                throw new Exception(
                    string.Format("mm_file_box: minimum size violation: mBasePosition = {0}, remainingLength = {1}",
                        mBasePosition, remainingLength));
            }

            UInt32 boxSize = mReader.ReadUInt32BE();
            if (remainingLength < boxSize)
            {
                throw new Exception(
                    string.Format("mm_file_box: box size too big: boxSize = {0}, remainingLength = {1}",
                        boxSize, remainingLength));
            }
            if (boxSize < 8)
            {
                throw new Exception(
                    string.Format("mm_file_box: box size too small: boxSize = {0}", boxSize));
            }
            mBoxOffset = 0;
            mBoxName = mReader.ReadCharsAsString(4);
            mBoxLength = boxSize - cHeaderSize;
            mReader.Position = mBasePosition + boxSize;
        }
        public string Name
        {
            get { return mBoxName; }
        }
        internal override long Length
        {
            get
            {
                return mBoxLength;
            }
        }
        internal override long Position
        { 
            get
            {
                return mBoxOffset;
            }
            set
            {
                if ((value < 0) || (value > mBoxLength))
                {
                    throw new ArgumentOutOfRangeException("mm_file_box.Position: value");
                }
                mBoxOffset = (uint)value;
            }
        }
        internal override long RemainingLength
        {
            get
            {
                return mBoxLength - mBoxOffset;
            }
        }
        internal override byte[] ReadBytes(uint count)
        {
            long remainingLength = RemainingLength;
            if (remainingLength < count)
            {
                count = (uint)remainingLength;                
            }
            long originalPosition = mReader.Position;
            mReader.Position = mBasePosition + cHeaderSize + mBoxOffset;
            byte[] result = mReader.ReadBytes(count);
            mBoxOffset += count;
            mReader.Position = originalPosition;
            return result;
        }
        internal override byte ReadUInt8()
        {
            long originalPosition = mReader.Position;
            mReader.Position = mBasePosition + cHeaderSize + mBoxOffset;
            byte result = mReader.ReadUInt8();
            mBoxOffset += sizeof(byte);
            mReader.Position = originalPosition;
            return result;
        }
        internal override UInt16 ReadUInt16BE()
        {
            long originalPosition = mReader.Position;
            mReader.Position = mBasePosition + cHeaderSize + mBoxOffset;
            UInt16 result = mReader.ReadUInt16BE();
            mBoxOffset += sizeof(UInt16);
            mReader.Position = originalPosition;
            return result;
        }
        internal override UInt32 ReadUInt24BE()
        {
            long originalPosition = mReader.Position;
            mReader.Position = mBasePosition + cHeaderSize + mBoxOffset;
            UInt32 result = mReader.ReadUInt24BE();
            mBoxOffset += 3;
            mReader.Position = originalPosition;
            return result;
        }
        internal override UInt32 ReadUInt32BE()
        {
            long originalPosition = mReader.Position;
            mReader.Position = mBasePosition + cHeaderSize + mBoxOffset;
            UInt32 result = mReader.ReadUInt32BE();
            mBoxOffset += sizeof(UInt32);
            mReader.Position = originalPosition;
            return result;
        }
        internal override string ReadCharsAsString(uint count)
        {
            long originalPosition = mReader.Position;
            mReader.Position = mBasePosition + cHeaderSize + mBoxOffset;
            string result = mReader.ReadCharsAsString(count);
            mBoxOffset += sizeof(UInt32);
            mReader.Position = originalPosition;
            return result;
        }
        internal override mm_file_box ReadBox()
        {
            long originalPosition = mReader.Position;
            mReader.Position = mBasePosition + cHeaderSize + mBoxOffset;
            mm_file_box result = mReader.ReadBox();
            mBoxOffset += (uint)result.Length + cHeaderSize;
            mReader.Position = originalPosition;
            return result;
        }
        internal override string ReadString()
        {
            long originalPosition = mReader.Position;
            long starting_position = mBasePosition + cHeaderSize + mBoxOffset;
            mReader.Position = starting_position;
            string result = mReader.ReadString();
            mBoxOffset += (uint)(mReader.Position - starting_position);
            mReader.Position = originalPosition;
            return result;
        }
    }
    internal class mm_file_region : mm_reader
    {
        mm_reader mReader;
        long mBasePosition;
        uint mOffset;
        uint mLength;
        internal mm_file_region(mm_reader reader, long basePosition, uint length)
        {
            mReader = reader;
            mBasePosition = basePosition;
            mLength = length;
            mOffset = 0;
        }
        internal override long Length
        {
            get
            {
                return mLength;
            }
        }
        internal override long Position
        {
            get
            {
                return mOffset;
            }
            set
            {
                if ((value < 0) || (value > mLength))
                {
                    throw new ArgumentOutOfRangeException(
                        string.Format("mm_file_region.Position: value = {0}, Length = {0}",
                            value, mLength));
                }
                mOffset = (uint)value;
            }
        }
        internal override long RemainingLength
        {
            get
            {
                return mLength - mOffset;
            }
        }
        internal override byte[] ReadBytes(uint count)
        {
            long remainingLength = RemainingLength;
            if (remainingLength < count)
            {
                count = (uint)remainingLength;
            }
            long originalPosition = mReader.Position;
            mReader.Position = mBasePosition + mOffset;
            byte[] result = mReader.ReadBytes(count);
            mOffset += count;
            mReader.Position = originalPosition;
            return result;
        }
        internal override byte ReadUInt8()
        {
            long originalPosition = mReader.Position;
            mReader.Position = mBasePosition + mOffset;
            byte result = mReader.ReadUInt8();
            mOffset += sizeof(byte);
            mReader.Position = originalPosition;
            return result;
        }
        internal override UInt16 ReadUInt16BE()
        {
            long originalPosition = mReader.Position;
            mReader.Position = mBasePosition + mOffset;
            UInt16 result = mReader.ReadUInt16BE();
            mOffset += sizeof(UInt16);
            mReader.Position = originalPosition;
            return result;
        }
        internal override UInt32 ReadUInt24BE()
        {
            long originalPosition = mReader.Position;
            mReader.Position = mBasePosition + mOffset;
            UInt32 result = mReader.ReadUInt24BE();
            mOffset += 3;
            mReader.Position = originalPosition;
            return result;
        }
        internal override UInt32 ReadUInt32BE()
        {
            long originalPosition = mReader.Position;
            mReader.Position = mBasePosition + mOffset;
            UInt32 result = mReader.ReadUInt32BE();
            mOffset += sizeof(UInt32);
            mReader.Position = originalPosition;
            return result;
        }
        internal override string ReadCharsAsString(uint count)
        {
            long originalPosition = mReader.Position;
            mReader.Position = mBasePosition + mOffset;
            string result = mReader.ReadCharsAsString(count);
            mOffset += count;
            mReader.Position = originalPosition;
            return result;
        }
        internal override mm_file_box ReadBox()
        {
            long originalPosition = mReader.Position;
            mReader.Position = mBasePosition + mOffset;
            mm_file_box result = mReader.ReadBox();
            mOffset += (uint)result.Length + mm_file_box.cHeaderSize;
            mReader.Position = originalPosition;
            return result;
        }
        internal override string ReadString()
        {
            long originalPosition = mReader.Position;
            long starting_position = mBasePosition + mOffset;
            mReader.Position = starting_position;
            string result = mReader.ReadString();
            mOffset += (uint)(mReader.Position - starting_position);
            mReader.Position = originalPosition;
            return result;
        }
    }
    internal class mm_item_info_entity
    {
        byte mVersion;
        UInt32 mFlags;
        UInt16 mItemID;
        UInt16 mItemProtectionIndex;
        string mItemType;
        internal mm_item_info_entity(mm_file_box infe_box)
        {
            if (infe_box.Name != "infe")
            {
                throw new Exception(
                    string.Format("item_info_entity: box type = {0}, expected infe", infe_box.Name));
            }
            infe_box.Position = 0;
            mVersion = infe_box.ReadUInt8();
            mFlags = infe_box.ReadUInt24BE();
            if (mVersion != 2)
            {
                throw new Exception(
                    string.Format("item_info_entity: version = {0}, expected 2", mVersion));
            }
            mItemID = infe_box.ReadUInt16BE();
            mItemProtectionIndex = infe_box.ReadUInt16BE();
            mItemType = infe_box.ReadCharsAsString(4);
            if (infe_box.ReadUInt8() != 0)
            {
                throw new Exception("item_info_entity: item name present, expected null terminator");
            }
        }
        internal UInt16 ItemID
        {
            get
            {
                return mItemID;
            }
        }
        internal string ItemType
        {
            get
            {
                return mItemType;
            }
        }
    }
    internal class mm_item_info
    {
        mm_item_info_entity[] mItemList;
        internal mm_item_info(mm_file_box iinf_box)
        {
            if (iinf_box.Name != "iinf")
            {
                throw new Exception(
                    string.Format("item_info: box type = {0}, expected iinf", iinf_box.Name));
            }
            iinf_box.Position = 0;
            byte version = iinf_box.ReadUInt8();
            if (version != 0)
            {
                throw new Exception(
                    string.Format("item_info: version = {0}, expected version 0", version));
            }
            UInt32 flags = iinf_box.ReadUInt24BE();
            if (flags != 0)
            {
                throw new Exception(
                    string.Format("item_info: flags = 0x{0:X6}, expected 0x000000",
                        flags));
            }
            UInt16 itemCount = iinf_box.ReadUInt16BE();
            mItemList = new mm_item_info_entity[itemCount];
            UInt16 itemIndex = 0;
            while (itemIndex < itemCount)
            {
                mm_file_box infe_box = new mm_file_box(iinf_box);
                if (infe_box.Name != "infe")
                {
                    throw new Exception(
                        string.Format("item_info: sub box type = {0}, expected infe",
                            infe_box.Name));
                }
                mItemList[itemIndex] = new mm_item_info_entity(infe_box);
                itemIndex++;
            }
        }
        internal int CountType(string type)
        {
            int result = 0;
            for (int i = 0; i < mItemList.Length; i++)
            {
                if (mItemList[i].ItemType == type)
                {
                    result++;
                }
            }
            return result;
        }
        internal UInt16 GetID(string type, int index)
        {
            Int32 result = -1;
            for (int i = 0; i < mItemList.Length; i++)
            {
                if (mItemList[i].ItemType == type)
                {
                    if (index == 0)
                    {
                        result = mItemList[i].ItemID;
                        break;
                    }
                    index--;
                }
            }
            if (result < 0)
            {
                //not found
                throw new Exception("ID not found");
            }
            return (UInt16)result;
        }
    }
    internal class mm_item_location_element
    {
        UInt16 mItemID;
        UInt32 mOffset;
        UInt32 mLength;
        internal mm_item_location_element(UInt16 itemID, UInt32 offset, UInt32 length)
        {
            mItemID = itemID;
            mOffset = offset;
            mLength = length;
        }
        internal UInt16 ItemID
        {
            get
            {
                return mItemID;
            }
        }
        internal UInt32 Offset
        {
            get
            {
                return mOffset;
            }
        }
        internal UInt32 Length
        {
            get
            {
                return mLength;
            }
        }
    }
    internal class mm_item_location
    {
        mm_item_location_element[] mItemList;
        internal mm_item_location(mm_file_box iloc_box)
        {
            if (iloc_box.Name != "iloc")
            {
                throw new Exception(
                    string.Format("item_location: box type = {0}, expected iloc", iloc_box.Name));
            }
            iloc_box.Position = 0;
            byte version = iloc_box.ReadUInt8();
            if (version != 1)
            {
                throw new Exception(
                    string.Format("item_location: version = {0}, expected version 1", version));
            }
            UInt32 flags = iloc_box.ReadUInt24BE();
            if (flags != 0)
            {
                throw new Exception(
                    string.Format("item_location: flags = 0x{0:X6}, expected 0x000000",
                        flags));
            }
            UInt16 sizes = iloc_box.ReadUInt16BE();
            if (sizes != 0x4400)
            {
                //if this is different then the items need to adjust their reading size
                throw new Exception(
                    string.Format("item_location: sizes = 0x{0:X4}, expected 0x4400", sizes));
            }
            UInt16 item_count = iloc_box.ReadUInt16BE();
            mItemList = new mm_item_location_element[item_count];
            for (int i = 0; i < item_count; i++)
            {
                UInt16 item_ID = iloc_box.ReadUInt16BE();
                UInt32 base_offset = iloc_box.ReadUInt32BE();
                UInt16 extent_count = iloc_box.ReadUInt16BE();
                if (extent_count != 1)
                {
                    throw new Exception(
                        string.Format("item_location: extent_count = {0}, expected 1", extent_count));
                }
                UInt32 extent_offset = base_offset + iloc_box.ReadUInt32BE();
                UInt32 extent_length = iloc_box.ReadUInt32BE();
                mItemList[i] = new mm_item_location_element(item_ID, extent_offset, extent_length);
            }
            if (iloc_box.RemainingLength != 0)
            {
                throw new Exception(
                    string.Format("item_location: RemainingLength = {0}, expected 0",
                        iloc_box.RemainingLength));
            }
        }
        internal mm_item_location_element? GetElementByID(UInt16 id)
        {
            mm_item_location_element? result = null;
            if (mItemList != null)
            {
                for (int i = 0; i < mItemList.Length; i++)
                {
                    if (mItemList[i].ItemID == id)
                    {
                        result = mItemList[i];
                        break;
                    }
                }
            }
            return result;
        }
    }
    internal class mm_exif
    {
        mm_reader mReader;
        mm_tiff_header mTiff;
        mm_file_region mTiffRegion;
        internal mm_exif(mm_reader reader)
        {
            mReader = reader;
            mReader.Position = 0;
            uint offset = mReader.ReadUInt32BE();
            if (offset != 6)
            {
                throw new Exception(
                    string.Format("Exif: offset = {0}, expected 6", offset));
            }
            string identifier = mReader.ReadCharsAsString(4);
            UInt16 zeros = mReader.ReadUInt16BE();
            if ((identifier != "Exif") || (zeros != 0))
            {
                throw new Exception(
                    string.Format("Exif: identifer = {0}, zeros = {1}, expected Exif, 0",
                        identifier, zeros));
            }
            mTiffRegion = new mm_file_region(mReader, mReader.Position,
                (uint)mReader.RemainingLength);
            mTiff = new mm_tiff_header(mTiffRegion);        }
        internal mm_tiff_header Tiff
        {
            get
            {
                return mTiff;
            }
        }
        internal mm_file_region TiffRegion
        {
            get
            {
                return mTiffRegion;
            }
        }
    }
    internal class mm_tiff_header
    {
        UInt32 mIfdOffset;
        internal mm_tiff_header(mm_reader reader)
        {
            string byte_order = reader.ReadCharsAsString(2);
            if (byte_order != "MM")
            {
                throw new Exception(
                    string.Format("TIFF: byte_order = {0}, expected MM",
                        byte_order));
            }
            UInt16 magic_number = reader.ReadUInt16BE();
            if (magic_number != 0x002A)
            {
                throw new Exception(
                    string.Format("TIFF: magic_number = 0x{0:X4}, expected 0x002A",
                    magic_number));
            }
            mIfdOffset = reader.ReadUInt32BE();
            if ((mIfdOffset != 0) && (mIfdOffset < 8))
            {
                throw new Exception(
                    string.Format("TIFF: ifd_offset = {0}, expected > 8 or 0",
                        mIfdOffset));
            }
        }
        internal UInt32 IfdOffset
        {
            get => mIfdOffset;
        }
    }
    internal class mm_tiff_ifd
    {
        mm_tiff_directory_entry[] mDirectoryEntryList;
        UInt32 mNextIfdOffset;
        internal mm_tiff_ifd(mm_reader reader)
        {
            //assume reader position is at the start of IFD
            UInt16 count = reader.ReadUInt16BE();
            if (count == 0)
            {
                throw new Exception("tiff_ifd: count == 0");
            }
            mDirectoryEntryList = new mm_tiff_directory_entry[count];
            for (UInt16 i = 0; i < count; i++)
            {
                mDirectoryEntryList[i] = new mm_tiff_directory_entry(reader);
            }
            mNextIfdOffset = reader.ReadUInt32BE();
        }
        internal int Length
        {
            get
            {
                return mDirectoryEntryList.Length;
            }
        }
        internal mm_tiff_directory_entry this[int index]
        {
            get => mDirectoryEntryList[index];
        }
        internal UInt32 NextIfdOffset
        {
            get
            {
                return mNextIfdOffset;
            }
        }
    }
    internal class mm_tiff_directory_entry
    {
        UInt16 mTag;
        UInt16 mType;
        UInt32 mCount;
        UInt32 mValueOffset;
        internal mm_tiff_directory_entry(mm_reader reader)
        {
            //assuming reader position is at the start of directory entry
            mTag = reader.ReadUInt16BE();
            mType = reader.ReadUInt16BE();
            mCount = reader.ReadUInt32BE();
            mValueOffset = reader.ReadUInt32BE();
        }
        internal UInt16 Tag
        {
            get { return mTag; }
        }
        internal UInt16 Type
        {
            get { return mType; }
        }
        internal UInt32 Count
        {
            get { return mCount; }
        }
        internal UInt32 ValueOffset
        {
            get { return mValueOffset; }
        }
    }
}
