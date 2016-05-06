using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using HttpMockReq.HttpMockReqException;
using System.Collections;

namespace HttpMockReq
{
    /// <summary>
    /// 
    /// </summary>
    public class Cassette
    {
        private string path;
        private List<Record> records;

        private void ReadFromFile()
        {
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);

                foreach (var jrecord in JArray.Parse(json))
                {
                    var name = jrecord["name"].ToString();
                    var requests = jrecord["requests"].ToObject<IList>();

                    var record = new Record(name);

                    record.WriteRange(requests);

                    records.Add(record);
                }
            }
        }

        private void WriteToFile()
        {
            JArray jrecords = new JArray();

            foreach (var record in records)
            {
                JArray jrequests = new JArray();

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

            var directory = Path.GetDirectoryName(path);
            Directory.CreateDirectory(directory);

            using (StreamWriter fileWriter = new StreamWriter(path))
            {
                fileWriter.Write(jrecords);
            }
        }

        internal Record Find(string name)
        {
            return records.Find(record => record.Name == name);
        }

        internal void Save(Record record)
        {
            records.Add(record);

            try
            {
                WriteToFile();
            }
            catch (Exception ex)
            {
                throw new CassetteException($"Record {record.Name} cannot be saved.", path, ex);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        public Cassette(string path)
        {
            this.path = path;
            this.records = new List<Record>();

            try
            {
                ReadFromFile();
            }
            catch (Exception ex)
            {
                throw new CassetteException("Cassette cannot be parsed.", path, ex);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool Contains(string name)
        {
            return records.Exists(record => record.Name == name);
        }
    }
}
