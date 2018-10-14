using System;
using System.IO;
using System.Collections.Generic;
using System.Collections;
using Newtonsoft.Json.Linq;

namespace HttpMockPlayer
{
    /// <summary>
    /// Represents a collection of mock records stored in a JSON file.
    /// </summary>
    public class Cassette
    {
        internal string Path { get; }
        internal List<Record> Records { get; }

        private void ReadFromFile()
        {
            if (File.Exists(Path))
            {
                var json = File.ReadAllText(Path);

                foreach (var jrecord in JArray.Parse(json))
                {
                    var name = jrecord["name"].ToString();
                    var requests = jrecord["requests"].ToObject<IList>();

                    var record = new Record(name);
                    record.WriteRange(requests);

                    Records.Add(record);
                }
            }
        }

        private void WriteToFile()
        {
            var jrecords = new JArray();

            foreach (var record in Records)
            {
                var jrequests = new JArray();

                for (var request = record.Read(); request != null; request = record.Read())
                {
                    jrequests.Add((JObject)request);
                }

                record.Rewind();

                var jrecord = JObject.FromObject(new
                {
                    name = record.Name,
                    requests = jrequests
                });

                jrecords.Add(jrecord);
            }

            var directory = System.IO.Path.GetDirectoryName(Path);
            Directory.CreateDirectory(directory);

            using (StreamWriter fileWriter = new StreamWriter(Path))
            {
                fileWriter.Write(jrecords);
            }
        }

        internal Record Find(string name)
        {
            return Records.Find(record => record.Name == name);
        }

        internal void Save(Record record)
        {
            Records.Add(record);

            try
            {
                WriteToFile();
            }
            catch (Exception ex)
            {
                throw new CassetteException($"Record {record.Name} cannot be saved.", Path, ex);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Cassette"/> class with a specified file path. 
        /// </summary>
        /// <param name="path">Cassette file path. If a file with this path exists, its contents is parsed into a list of records.</param>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="CassetteException"/>
        public Cassette(string path)
        {
            this.Path = path ?? throw new ArgumentNullException("path");
            this.Records = new List<Record>();

            try
            {
                ReadFromFile();
            }
            catch (Exception ex)
            {
                throw new CassetteException("Cassette cannot be read.", path, ex);
            }
        }

        /// <summary>
        /// Determines whether this <see cref="Cassette"/> object contains a record with a specified name.
        /// </summary>
        /// <param name="name">Name of the record.</param>
        /// <returns>true, if the <see cref="Cassette"/> contains a record with the specified name; otherwise, false.</returns>
        public bool Contains(string name)
        {
            return Records.Exists(record => record.Name == name);
        }
    }
}
