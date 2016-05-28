using System;
using System.IO;
using NUnit.Framework;
using Newtonsoft.Json.Linq;

namespace HttpMockPlayer.Tests
{
    [TestFixture]
    class CassetteTests
    {
        [Test]
        public void Initialize_NullPath_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new Cassette(null));
        }

        [Test]
        public void Initialize_FileNotExists_CreatesEmptyRecords()
        {
            var cassette = new Cassette(Context.CassetteNew);

            Assert.IsNotNull(cassette.Records);
            Assert.IsEmpty(cassette.Records);
        }

        [Test]
        public void Initialize_ValidFile_LoadsRecords()
        {
            var cassette = new Cassette(Context.Cassette1);

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
            Assert.Throws<CassetteException>(() => new Cassette(Context.Cassette3));
        }

        [Test]
        public void Save_FileNotExists_CreatesValidFile()
        {
            var cassette = new Cassette(Context.CassetteNew);
            var record = new Record("record");
            var request = JObject.Parse("{\"request\":{},\"response\":{}}");

            record.Write(request);
            cassette.Save(record);

            Assert.IsTrue(File.Exists(cassette.Path));

            var anotherCassette = new Cassette(cassette.Path);

            Assert.AreEqual(anotherCassette.Records.Count, 1);

            record = anotherCassette.Records[0];

            Assert.AreEqual(record.Name, "record");
            Assert.AreEqual(record.List.Count, 1);
        }

        [Test]
        public void Save_FileExists_UpdatesToValidFile()
        {
            var cassette = new Cassette(Context.Cassette1);
            var record = new Record("record");
            var request = JObject.Parse("{\"request\":{},\"response\":{}}");

            record.Write(request);
            cassette.Save(record);

            var anotherCassette = new Cassette(cassette.Path);

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
            var cassette = new Cassette(Context.Cassette1);

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
            var cassette = new Cassette(Context.Cassette1);
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
            var cassette = new Cassette(Context.Cassette1);

            Assert.IsTrue(cassette.Contains("record1"));
        }

        [Test]
        public void Contain_RecordNotExists_ReturnsFalse()
        {
            var cassette = new Cassette(Context.Cassette1);

            Assert.IsFalse(cassette.Contains("wrong"));
        }

        [Test]
        public void Find_RecordExists_ReturnsRecord()
        {
            var cassette = new Cassette(Context.Cassette1);
            var record = cassette.Find("record1");

            Assert.IsNotNull(record);
            Assert.AreEqual(record.Name, "record1");
            Assert.AreEqual(record.List.Count, 2);
        }

        [Test]
        public void Find_RecordNotExists_ReturnsNull()
        {
            var cassette = new Cassette(Context.Cassette1);
            var record = cassette.Find("wrong");

            Assert.IsNull(record);
        }
    }
}
