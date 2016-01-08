using System.Collections;

namespace HttpMockReq
{
    public class Record
    {
        public string Name;
        public readonly Queue Queue;

        public Record(string name, Queue queue)
        {
            this.Name = name;
            this.Queue = queue;
        }
    }
}
