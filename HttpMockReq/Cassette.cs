using System.IO;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace HttpMockReq
{
    /// <summary>
    /// 
    /// </summary>
    class Cassette
    {
        private List<Record> records;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        public Cassette(string path)
        {
            records = new List<Record>(5);

            if (File.Exists(path))
            {
                foreach (var record in JArray.Parse(File.ReadAllText(path)))
                {
                    var name = record["name"].ToString();

                    var queue = new Queue();
                    foreach (var request in record["requests"])
                    {
                        queue.Enqueue(request);
                    }

                    records.Add(new Record(name, queue));
                }
            }
        }

        internal void Save(Record record)
        {

        }
    }
}
