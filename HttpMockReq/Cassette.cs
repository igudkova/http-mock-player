using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using HttpMockReq.HttpMockReqException;

namespace HttpMockReq
{
    /// <summary>
    /// 
    /// </summary>
    public class Cassette
    {
        internal List<Record> Records;

        /// <summary>
        /// Gets the cassette file path.
        /// </summary>
        public string Path { get; }

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

            Path = path;
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
                    throw new CassetteException("Cassette cannot be parsed.", path, ex);
                }
            }
            else
            {
                var directory = System.IO.Path.GetDirectoryName(path);

                Directory.CreateDirectory(directory);
                File.Create(path);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool Contains(string name)
        {
            return Records.Exists(r => r.Name == name);
        }

        internal void Save(Record record)
        {

        }
    }
}
