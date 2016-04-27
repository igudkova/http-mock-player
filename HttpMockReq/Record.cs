using System.Collections;

namespace HttpMockReq
{
    internal class Record
    {
        private ArrayList list;
        private IEnumerator enumerator;

        internal string Name { get; }

        internal Record(string name)
        {
            list = new ArrayList();

            enumerator = list.GetEnumerator();

            Name = name;
        }

        internal object Read()
        {
            if(enumerator.MoveNext())
            {
                return enumerator.Current;
            }
            else
            {
                return null;
            }
        }

        internal void Write(object request)
        {
            list.Add(request);

            enumerator = list.GetEnumerator();
        }

        internal void WriteRange(object[] requests)
        {
            list.AddRange(requests);

            enumerator = list.GetEnumerator();
        }

        internal void Rewind()
        {
            enumerator.Reset();
        }
    }
}
