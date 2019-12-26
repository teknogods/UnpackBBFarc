using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnpackFarc
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Border Break FARC lame unpacker by Reaver/TeknoGods");

            if (!ValidateArgs(args))
                return;

            var fileData = File.ReadAllBytes(args[0]);

            if (fileData[0] != 'F' ||
                fileData[1] != 'A' ||
                fileData[2] != 'R' ||
                fileData[3] != 'C')
            {
                // TODO: Add more sanity checks!
                Console.WriteLine("Not a valid FARC file");
                return;
            }

            ProcessFile(fileData, args[1]);
        }

        private static bool ValidateArgs(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: UnpackFarc.exe <location of farc> <location of dump dir>");
                return false;
            }

            if (!File.Exists(args[0]))
            {
                Console.WriteLine($"Cannot find file: {args[1]}, aboring...");
                return false;
            }

            if (!Directory.Exists(args[1]))
            {
                Console.WriteLine($"Cannot directory: {args[1]}, aboring...");
                return false;
            }

            return true;
        }

        private static void ProcessFile(byte[] fileData, string dumpDir)
        {
            uint endOffset = GetDWORD(fileData, 4);
            endOffset *= 0x10;
            endOffset += 0x10; // We use absolute file offsets

            // Fetch end of files offset!
            for(uint i = 0x10; i < endOffset; i += 0x10)
            {
                string fileName = ReadFileName(fileData, i);
                uint fileOffset = GetDWORD(fileData, (uint)i + 4);
                uint compressedFileSize = GetDWORD(fileData, (uint)i + 8);
                uint uncompressedFileSize = GetDWORD(fileData, (uint)i + 12);

                if(compressedFileSize != uncompressedFileSize)
                {
                    Console.WriteLine("WARNING COMPRESSED SIZE DIFFERS FROM UNCOMPRESSED SIZE! Aborting...");
                    return;
                }

                Console.WriteLine($"[{i.ToString("X4")}] File Entry: {fileName} {fileOffset.ToString("X4")}/{compressedFileSize.ToString("X4")}/{uncompressedFileSize.ToString("X4")}");
                DumpFile(fileData, fileOffset, uncompressedFileSize, fileName, dumpDir);
            }
        }

        private static void DumpFile(byte[] fileData, uint fileOffset, uint uncompressedFileSize, string fileName, string dumpDir)
        {
            byte[] fileBytes = new byte[uncompressedFileSize];
            for(int i = 0;i < uncompressedFileSize; i++)
            {
                fileBytes[i] = fileData[fileOffset + i];
            }

            File.WriteAllBytes(Path.Combine(dumpDir, fileName), fileBytes);
        }

        // Name just for fun, autists stay away.
        private static uint GetDWORD(byte[] fileData, uint offset)
        {
            byte Val1 = fileData[offset + 3];
            byte Val2 = fileData[offset + 2];
            byte Val3 = fileData[offset + 1];
            byte Val4 = fileData[offset];

            return ((uint)(Val1 * 0x1000000) + ((uint)Val2 * 0x10000) + ((uint)Val3 * 0x100) + (uint)Val4);
        }

        private static string ReadFileName(byte[] fileData, uint offset)
        {
            byte Val1 = fileData[offset + 3];
            byte Val2 = fileData[offset + 2];
            byte Val3 = fileData[offset + 1];
            byte Val4 = fileData[offset];

            uint fileOffset = ((uint)(Val1 * 0x1000000) + ((uint)Val2 * 0x10000) + ((uint)Val3 * 0x100) + (uint)Val4);

            return ReadFileNameData(fileData, fileOffset);
        }

        private static string ReadFileNameData(byte[] fileData, uint offset)
        {
            List<byte> myByte = new List<byte>();
            myByte.Clear();

            // TODO: Add better check for filename size...
            for(int i = 0; i < 99; i++)
            {
                if (fileData[offset + i] == 0x00)
                    return Encoding.GetEncoding("ISO-8859-1").GetString(myByte.ToArray());
                myByte.Add(fileData[offset + i]);
            }
            return Encoding.GetEncoding("ISO-8859-1").GetString(myByte.ToArray());
        }
    }
}
