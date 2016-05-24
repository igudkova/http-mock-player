using System;
using System.Runtime.Serialization;

namespace HttpMockPlayer
{
    /// <summary>
    /// Exception thrown when a <see cref="Player"/> object is not in a valid state to start an operation.
    /// </summary>
    [Serializable]
    class PlayerStateException : Exception
    {
        /// <summary>
        /// Gets the state the player was in, when <see cref="PlayerStateException"/> was thrown.
        /// </summary>
        public Player.State State { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PlayerStateException"/> class with a specified state
        /// of the <see cref="Player"/> object that triggered this exception.
        /// <param name="state">The current state of the <see cref="Player"/> object.</param>
        /// </summary>
        public PlayerStateException(Player.State state)
        {
            State = state;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PlayerStateException"/> class with a specified state 
        /// of the <see cref="Player"/> object that triggered this exception and an error message.
        /// </summary>
        /// <param name="state">The current state of the <see cref="Player"/> object.</param>
        /// <param name="message">The message that describes the error.</param>
        public PlayerStateException(Player.State state, string message) : base(message)
        {
            State = state;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PlayerStateException"/> class with a specified state
        /// of the <see cref="Player"/> object that triggered this exception, an error message 
        /// and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="state">The current state of the <see cref="Player"/> object.</param>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="inner">The exception that is the cause of the current exception.</param>
        public PlayerStateException(Player.State state, string message, Exception inner) : base(message, inner)
        {
            State = state;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PlayerStateException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The object that holds the serialized object data.</param>
        /// <param name="context">The contextual information about the source or destination.</param>
        protected PlayerStateException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }

            State = (Player.State)Enum.Parse(typeof(Player.State), info.GetString("State"));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }

            info.AddValue("State", State);

            base.GetObjectData(info, context);
        }
    }
}
