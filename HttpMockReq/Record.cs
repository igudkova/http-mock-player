using System.Collections;

namespace HttpMockReq
{
    class Record
    {
        private string name;
        public readonly Queue Queue;

        public Record(string name, Queue queue)
        {
            this.name = name;
            this.Queue = queue;
        }
    }
}
