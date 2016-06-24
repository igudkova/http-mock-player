using NUnit.Framework;

namespace HttpMockPlayer.Tests
{
    [TestFixture]
    class RecordTests
    {
        [Test]
        public void Initialize_SetsName()
        {
            var record = new Record("record");

            Assert.AreEqual(record.Name, "record");
        }

        [Test]
        public void Initialize_CreatesEmptyList()
        {
            var record = new Record("record");

            Assert.IsNotNull(record.List);
            Assert.IsEmpty(record.List);
        }

        [Test]
        public void Write_AddsToList()
        {
            var record = new Record("record");
            var obj = new
            {
                value1 = 1,
                value2 = "object"
            };

            record.Write(obj);

            Assert.AreEqual(1, record.List.Count);
            Assert.AreEqual(obj, record.List[0]);
        }

        [Test]
        public void WriteRange_AddsToList()
        {
            var record = new Record("record");
            var array = new dynamic[3]
            {
                new
                {
                    value1 = 1,
                    value2 = "object"
                },
                new object(),
                "string"
            };

            record.WriteRange(array);

            Assert.AreEqual(3, record.List.Count);
            Assert.AreEqual(array, record.List);
        }

        [Test]
        public void Read_ReturnsCurrentOrNull()
        {
            var record = new Record("record");
            var obj1 = new
            {
                value1 = 1,
                value2 = "object"
            };
            var obj2 = new object();
            var obj3 = "string";

            record.Write(obj1);
            record.Write(obj2);
            record.Write(obj3);

            Assert.AreEqual(obj1, record.Read());
            Assert.AreEqual(obj2, record.Read());
            Assert.AreEqual(obj3, record.Read());
            Assert.IsNull(record.Read());
        }

        [Test]
        public void Rewind_ResetsCurrent()
        {
            var record = new Record("record");

            var obj1 = new
            {
                value1 = 1,
                value2 = "object"
            };
            var obj2 = new object();
            var obj3 = "string";

            record.Write(obj1);
            record.Write(obj2);
            record.Write(obj3);

            record.Read();
            record.Read();
            record.Read();

            record.Rewind();

            Assert.AreEqual(obj1, record.Read());
            Assert.AreEqual(obj2, record.Read());
            Assert.AreEqual(obj3, record.Read());
        }
    }
}
