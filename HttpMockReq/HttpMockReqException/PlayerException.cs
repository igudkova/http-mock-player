using System;
using System.Runtime.Serialization;

namespace HttpMockReq.HttpMockReqException
{
    /// <summary>
    /// Serves as the base class for player exceptions.
    /// </summary>
    [Serializable]
    public class PlayerException : Exception
    {
        /// <summary>
        /// Gets the state the player was in, when <see cref="PlayerException"/> was thrown.
        /// </summary>
        public Player.State State { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PlayerException"/> class with a specified player state.
        /// <param name="state">The current state of the player.</param>
        /// </summary>
        public PlayerException(Player.State state)
        {
            State = state;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PlayerException"/> class with a specified player state and an error message.
        /// </summary>
        /// <param name="state">The current state of the player.</param>
        /// <param name="message">The message that describes the error.</param>
        public PlayerException(Player.State state, string message) : base(message)
        {
            State = state;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PlayerException"/> class with a specified player state, an error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="state">The current state of the player.</param>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="inner">The exception that is the cause of the current exception.</param>
        public PlayerException(Player.State state, string message, Exception inner) : base(message, inner)
        {
            State = state;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PlayerException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The object that holds the serialized object data.</param>
        /// <param name="context">The contextual information about the source or destination.</param>
        protected PlayerException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
