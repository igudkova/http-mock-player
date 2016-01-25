using System;
using System.Runtime.Serialization;

namespace HttpMockReq.HttpMockReqException
{
    /// <summary>
    /// The exception that is thrown when player attempts to play a request, which is not present at the current position of the record queue.
    /// </summary>
    [Serializable]
    public class RequestNotFoundException : PlayerException
    {
        /// <summary>
        /// Gets <see cref="Uri"/> of the missing request.
        /// </summary>
        public Uri RequestUri { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestNotFoundException"/> class.
        /// </summary>
        public RequestNotFoundException() : base(Player.State.Playing) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestNotFoundException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public RequestNotFoundException(string message) : base(Player.State.Playing, message) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestNotFoundException"/> class with a specified error message and Uri of the missing request.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="requestUri">Uri of the request.</param>
        public RequestNotFoundException(string message, Uri requestUri) : base(Player.State.Playing, message)
        {
            RequestUri = requestUri;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestNotFoundException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="inner">The exception that is the cause of the current exception.</param>
        public RequestNotFoundException(string message, Exception inner) : base(Player.State.Playing, message, inner) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestNotFoundException"/> class with a specified error message, Uri of the missing request and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="requestUri">Uri of the request.</param>
        /// <param name="inner">The exception that is the cause of the current exception.</param>
        public RequestNotFoundException(string message, Uri requestUri, Exception inner) : base(Player.State.Playing, message, inner)
        {
            RequestUri = requestUri;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestNotFoundException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The object that holds the serialized object data.</param>
        /// <param name="context">The contextual information about the source or destination.</param>
        protected RequestNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
