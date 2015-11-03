using System;

namespace Dnx.Identity.MongoDB.Models
{
    public abstract class MongoUserContactRecord : IEquatable<MongoUserEmail>
    {
        protected MongoUserContactRecord(string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            Value = value;
        }

        public string Value { get; private set; }
        public ConfirmationOccurrence ConfirmationRecord { get; private set; }

        public bool IsConfirmed()
        {
            return ConfirmationRecord != null;
        }

        internal void SetConfirmed()
        {
            SetConfirmed(new ConfirmationOccurrence());
        }

        internal void SetConfirmed(ConfirmationOccurrence confirmationRecord)
        {
            if (ConfirmationRecord == null)
            {
                ConfirmationRecord = confirmationRecord;
            }
        }

        internal void SetUnconfirmed()
        {
            ConfirmationRecord = null;
        }

        public bool Equals(MongoUserEmail other)
        {
            return other.Value.Equals(Value, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
