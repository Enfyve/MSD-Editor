using Microsoft.VisualStudio.TestTools.UnitTesting;
using MSDEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MSDEditor.Tests
{
    [TestClass()]
    public class MSDParserTests
    {
        static string BasePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        string TestFile = BasePath + "\\test.msd";
        string BadMagicFile = BasePath + "\\test_bad_magic.msd";
        string ShiftJISFile = BasePath + "\\test_shift_jis.msd";
        string UTF8File = BasePath + "\\test_utf8.msd";
        string UnicodeFile = BasePath + "\\test_unicode.msd";
        string TestWriteFile = BasePath + "\\testWrite.msd";

        [TestMethod()]
        public void TryLoadFile_Should_Fail_On_Mismatched_Magic()
        {
            MSDParser parser = new MSDParser();

            bool result = parser.TryLoadFile(BadMagicFile);

            Assert.IsFalse(result);
        }

        [TestMethod()]
        public void TryLoadFile_Should_Succeed_With_Magic()
        {
            MSDParser parser = new MSDParser();

            bool result = parser.TryLoadFile(TestFile);

            Assert.IsTrue(result);
        }

        [TestMethod()]
        public void DecodeEntries_Should_Parse_Properly()
        {
            List<MSDEntry> expected = new List<MSDEntry>()
            {
                new MSDEntry(12345, "The quíck brown föx.  \0\0" ),
                new MSDEntry(54321, "  Jumps over the lazy dog¿\0\0")
            };

            var actual = new List<MSDEntry>();

            MSDParser parser = new MSDParser();

            parser.TryLoadFile(TestFile);

            parser.DecodeEntries(Encoding.GetEncoding("Windows-1252"), ref actual);

            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod()]
        public void DecodeEntries_Should_Handle_Different_Encodings()
        {
            List<MSDEntry> expectedShiftJIS = new List<MSDEntry>()
            {
                new MSDEntry(22222, "こんにちは と さようなら。\0\0" ),
                new MSDEntry(33333, "「窓から突き落とす？」\0\0")
            };

            List<MSDEntry> expectedUTF8 = new List<MSDEntry>()
            {
                new MSDEntry(44444, "\"부탁합니다\"와 \"감사합니다\"\0\0" ),
                new MSDEntry(55555, "AI는 게으름이다.\0\0")
            };

            List<MSDEntry> expectedUnicode = new List<MSDEntry>()
            {
                new MSDEntry(66666, "wow 😲, 정말 많은 언어를 사용하시네요.\0\0" ),
                new MSDEntry(77777, "Unicodeは素晴らしい。✅\0\0")
            };

            var actual = new List<MSDEntry>();

            MSDParser parser = new MSDParser();

            parser.TryLoadFile(ShiftJISFile);
            parser.DecodeEntries(Encoding.GetEncoding("SHIFT_JIS"), ref actual);
            CollectionAssert.AreEqual(expectedShiftJIS, actual);

            actual.Clear();
            parser.TryLoadFile(UTF8File);
            parser.DecodeEntries(Encoding.UTF8, ref actual);
            CollectionAssert.AreEqual(expectedUTF8, actual);

            actual.Clear();
            parser.TryLoadFile(UnicodeFile);
            parser.DecodeEntries(Encoding.Unicode, ref actual);
            CollectionAssert.AreEqual(expectedUnicode, actual);
        }


        [TestMethod()]
        public void EncodeEntries_Should_Not_Corrupt_With_Same_Encoding()
        {
            var encoding = Encoding.GetEncoding("Windows-1252");
            List<MSDEntry> expected = new List<MSDEntry>()
            {
                new MSDEntry(12345, "The quíck brown föx.  \0\0" ),
                new MSDEntry(54321, "  Jumps over the lazy dog¿\0\0")
            };

            var actual = new List<MSDEntry>();

            MSDParser parser = new MSDParser();

            parser.TryLoadFile(TestFile);

            parser.EncodeEntries(in expected, encoding, TargetGame.FF3);

            parser.DecodeEntries(encoding, ref actual);

            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod()]
        public void TryWriteFile_Should_Fail_With_Bad_File()
        {
            MSDParser parser = new MSDParser();

            var result = parser.TryWriteFile("");

            Assert.IsFalse(result);
        }

        [TestMethod()]
        public void TryWriteFile_Should_Write_File_Accurately()
        {
            var encoding = Encoding.GetEncoding("Windows-1252");
            var entries = new List<MSDEntry>();
            MSDParser parser = new MSDParser();

            parser.TryLoadFile(TestFile);
            parser.DecodeEntries(encoding, ref entries);
            parser.EncodeEntries(in entries, encoding, TargetGame.FF3);

            parser.TryWriteFile(TestWriteFile);

            var expectedFile = new FileInfo(TestFile);
            var actualFile = new FileInfo(TestWriteFile);

            Assert.IsTrue(actualFile.Exists);
            Assert.AreEqual(expectedFile.Length, actualFile.Length);
            
            using (var expectedStream = expectedFile.OpenRead())
            {
                using (var actualStream = actualFile.OpenRead())
                {
                    while (actualStream.Position < actualStream.Length)
                    {
                        if (actualStream.ReadByte() != expectedStream.ReadByte())
                        {
                            Assert.Fail();
                            return;
                        }
                    }
                }
            }

        }
    }
}