using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HttpMockReq
{
    /// <summary>
    /// 
    /// </summary>
    public class Cassette
    {
        public List<Record> Records;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        public Cassette(string path)
        {
            if(path == null)
            {
                throw new ArgumentNullException("path");
            }

            Records = new List<Record>(5);

            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                if(json == string.Empty)
                {
                    return;
                }

                try
                {
                    foreach (var record in JArray.Parse(json))
                    {
                        var name = record["name"].ToString();

                        var queue = new Queue();
                        foreach (var request in record["requests"])
                        {
                            queue.Enqueue(request);
                        }

                        Records.Add(new Record(name, queue));
                    }
                }
                catch (JsonReaderException ex)
                {
                    throw new InvalidOperationException("Cassette cannot be parsed.", ex);
                }
            }
            else
            {
                var directory = Path.GetDirectoryName(path);

                Directory.CreateDirectory(directory);
                File.Create(path);
            }
        }

        internal void Save(Record record)
        {

        }
    }
}
