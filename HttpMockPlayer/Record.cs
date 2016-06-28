using System.Collections;

namespace HttpMockPlayer
{
    internal class Record
    {
        internal ArrayList List { get; }
        internal IEnumerator Enumerator { get; private set; }
        internal string Name { get; }

        internal Record(string name)
        {
            List = new ArrayList();

            Enumerator = List.GetEnumerator();

            Name = name;
        }

        internal bool IsEmpty()
        {
            return List.Count == 0;
        }

        internal object Read()
        {
            if(Enumerator.MoveNext())
            {
                return Enumerator.Current;
            }
            else
            {
                return null;
            }
        }

        internal void Write(object request)
        {
            List.Add(request);

            Enumerator = List.GetEnumerator();
        }

        internal void WriteRange(IList requests)
        {
            List.AddRange(requests);

            Enumerator = List.GetEnumerator();
        }

        internal void Rewind()
        {
            Enumerator.Reset();
        }
    }
}
