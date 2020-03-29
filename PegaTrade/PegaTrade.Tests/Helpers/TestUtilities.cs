using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PegaTrade.Tests.Helpers
{
    public static class TestUtilities
    {
        // Returns something like E:\Development\PegaTrade.Tests\
        public static string BasePath => Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName.Replace("bin", string.Empty);

        public static string GeneratePath(string filePath) => BasePath + filePath;

        public static string File_ReadAllLinesSingle(string filePath)
        {
            return string.Join(string.Empty, File_ReadAllLines(filePath));
        }

        public static List<string> File_ReadAllLines(string filePath)
        {
            return File.ReadAllLines(filePath).ToList();
        }
    }
}
