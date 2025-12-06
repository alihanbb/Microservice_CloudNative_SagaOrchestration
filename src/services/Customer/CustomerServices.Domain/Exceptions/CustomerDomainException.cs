namespace CustomerServices.Domain.Exceptions
{
    public class CustomerDomainException : Exception
    {
        public CustomerDomainException() { }

        public CustomerDomainException(string message)
            : base(message)
        { }
        public CustomerDomainException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }
}
