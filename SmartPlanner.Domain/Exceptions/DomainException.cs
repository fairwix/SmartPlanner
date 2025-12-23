namespace SmartPlanner.Domain.Exceptions;

    public class DomainException : Exception
    {
        public DomainException(string message) : base(message)
        {
        }

        public DomainException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    public class DomainValidationException : DomainException
    {
        public DomainValidationException(string message) : base(message)
        {
        }
    }
