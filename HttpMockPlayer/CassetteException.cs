using System;
using System.Runtime.Serialization;

namespace HttpMockPlayer
{
    /// <summary>
    /// Represents the generic cassette exception.
    /// </summary>
    [Serializable]
    public class CassetteException : Exception
    {
        /// <summary>
        /// Gets the cassette file path.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CassetteException"/> class.
        /// </summary>
        public CassetteException() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CassetteException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public CassetteException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CassetteException"/> class with a specified error message and cassette file path.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="path">The path of the cassette file.</param>
        public CassetteException(string message, string path) : base(message)
        {
            Path = path;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CassetteException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="inner">The exception that is the cause of the current exception.</param>
        public CassetteException(string message, Exception inner) : base(message, inner) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CassetteException"/> class with a specified error message, cassette file path and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="path">The path of the cassette file.</param>
        /// <param name="inner">The exception that is the cause of the current exception.</param>
        public CassetteException(string message, string path, Exception inner) : base(message, inner)
        {
            Path = path;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CassetteException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The object that holds the serialized object data.</param>
        /// <param name="context">The contextual information about the source or destination.</param>
        protected CassetteException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }

            Path = info.GetString("Path");
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }

            info.AddValue("Path", Path);

            base.GetObjectData(info, context);
        }
    }
}
