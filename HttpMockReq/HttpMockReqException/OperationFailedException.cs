using System;
using System.Runtime.Serialization;

namespace HttpMockReq.HttpMockReqException
{
    /// <summary>
    /// The exception that is thrown when player fails to perfom an operation.
    /// </summary>
    [Serializable]
    public class OperationFailedException : PlayerException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OperationFailedException"/> class with a specified player state.
        /// </summary>
        public OperationFailedException(Player.State state) : base(state) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="OperationFailedException"/> class with a specified player state and an error message.
        /// </summary>
        /// <param name="state">The current state of the player.</param>
        /// <param name="message">The message that describes the error.</param>
        public OperationFailedException(Player.State state, string message) : base(state, message) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="OperationFailedException"/> class with a specified player state, an error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="state">The current state of the player.</param>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="inner">The exception that is the cause of the current exception.</param>
        public OperationFailedException(Player.State state, string message, Exception inner) : base(state, message, inner) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="OperationFailedException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The object that holds the serialized object data.</param>
        /// <param name="context">The contextual information about the source or destination.</param>
        protected OperationFailedException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
