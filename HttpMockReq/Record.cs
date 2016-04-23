using System.Collections;

namespace HttpMockReq
{
    internal class Record
    {
        private Queue queue;

        /// <summary>
        /// Gets name of the record.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="Record"/> class with a specified name.
        /// </summary>
        /// <param name="name">Name of the record.</param>
        public Record(string name)
        {
            queue = new Queue(5);

            Name = name;
        }

        public void Write(object request)
        {
            queue.Enqueue(request);
        }

        public object Read()
        {
            return queue.Dequeue();
        }
    }
}
