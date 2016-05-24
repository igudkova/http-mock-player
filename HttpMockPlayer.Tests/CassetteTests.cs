using System;
using System.IO;
using NUnit.Framework;
using Newtonsoft.Json.Linq;

namespace HttpMockPlayer.Tests
{
    [TestFixture]
    class CassetteTests
    {
        string pathValid = $"{Context.AssemblyDirectoryName}/valid.json";
        string pathInvalid = $"{Context.AssemblyDirectoryName}/invalid.json";
        string pathNew = $"{Context.AssemblyDirectoryName}/new.json";

        [SetUp]
        public void SetUp()
        {
            File.Copy($"{Context.AssemblyDirectoryName}/../../Cassettes/valid.json", pathValid);
            File.Copy($"{Context.AssemblyDirectoryName}/../../Cassettes/invalid.json", pathInvalid);
        }

        [TearDown]
        public void TearDown()
        {
            File.Delete(pathValid);
            File.Delete(pathInvalid);
            File.Delete(pathNew);
        }

        [Test]
        public void Initialize_NullPath_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new Cassette(null));
        }

        [Test]
        public void Initialize_FileNotExists_CreatesEmptyRecords()
        {
            var cassette = new Cassette("anyfile");

            Assert.IsNotNull(cassette.Records);
            Assert.IsEmpty(cassette.Records);
        }

        [Test]
        public void Initialize_ValidFile_LoadsRecords()
        {
            var cassette = new Cassette(pathValid);

            Assert.AreEqual(cassette.Records.Count, 2);

            var record1 = cassette.Records[0];

            Assert.AreEqual(record1.Name, "record1");
            Assert.AreEqual(record1.List.Count, 2);

            var record2 = cassette.Records[1];

            Assert.AreEqual(record2.Name, "record2");
            Assert.AreEqual(record2.List.Count, 1);
        }

        [Test]
        public void Initialize_InvalidFile_Throws()
        {
            Assert.Throws<CassetteException>(() => new Cassette(pathInvalid));
        }

        [Test]
        public void Save_FileNotExists_CreatesValidFile()
        {
            var cassette = new Cassette(pathNew);
            var record = new Record("record");
            var request = JObject.Parse("{\"request\":{},\"response\":{}}");

            record.Write(request);
            cassette.Save(record);

            Assert.IsTrue(File.Exists(pathNew));

            var newCassette = new Cassette(pathNew);

            Assert.AreEqual(newCassette.Records.Count, 1);

            record = newCassette.Records[0];

            Assert.AreEqual(record.Name, "record");
            Assert.AreEqual(record.List.Count, 1);
        }

        [Test]
        public void Save_FileExists_UpdatesToValidFile()
        {
            var cassette = new Cassette(pathValid);
            var record = new Record("record");
            var request = JObject.Parse("{\"request\":{},\"response\":{}}");

            record.Write(request);
            cassette.Save(record);

            var anotherCassette = new Cassette(pathValid);

            Assert.AreEqual(anotherCassette.Records.Count, 3);

            var record1 = anotherCassette.Records[0];

            Assert.AreEqual(record1.Name, "record1");
            Assert.AreEqual(record1.List.Count, 2);

            var record2 = anotherCassette.Records[1];

            Assert.AreEqual(record2.Name, "record2");
            Assert.AreEqual(record2.List.Count, 1);

            var record3 = anotherCassette.Records[2];

            Assert.AreEqual(record3.Name, "record");
            Assert.AreEqual(record.List.Count, 1);
        }

        [Test]
        public void Save_AppendsRecord()
        {
            var cassette = new Cassette(pathValid);

            Assert.AreEqual(cassette.Records.Count, 2);

            var record = new Record("record");
            var request = JObject.Parse("{\"request\":{},\"response\":{}}");

            record.Write(request);
            cassette.Save(record);

            Assert.AreEqual(cassette.Records.Count, 3);

            var lastRecord = cassette.Records[2];

            Assert.AreEqual(lastRecord.Name, "record");
            Assert.AreEqual(lastRecord.List.Count, 1);
        }

        [Test]
        public void Save_RewindsRecord()
        {
            var cassette = new Cassette(pathValid);
            var record = new Record("record");

            cassette.Save(record);

            var record1 = cassette.Records[0];
            var req1 = record1.Read();
            var req2 = record1.Read();

            Assert.IsNotNull(req1);
            Assert.IsNotNull(req2);
        }

        [Test]
        public void Contain_RecordExists_ReturnsTrue()
        {
            var cassette = new Cassette(pathValid);

            Assert.IsTrue(cassette.Contains("record1"));
        }

        [Test]
        public void Contain_RecordNotExists_ReturnsFalse()
        {
            var cassette = new Cassette(pathValid);

            Assert.IsFalse(cassette.Contains("wrong"));
        }

        [Test]
        public void Find_RecordExists_ReturnsRecord()
        {
            var cassette = new Cassette(pathValid);
            var record = cassette.Find("record1");

            Assert.IsNotNull(record);
            Assert.AreEqual(record.Name, "record1");
            Assert.AreEqual(record.List.Count, 2);
        }

        [Test]
        public void Find_RecordNotExists_ReturnsNull()
        {
            var cassette = new Cassette(pathValid);
            var record = cassette.Find("wrong");

            Assert.IsNull(record);
        }
    }
}
