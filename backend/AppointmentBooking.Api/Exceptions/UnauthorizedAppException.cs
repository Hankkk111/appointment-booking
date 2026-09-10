namespace AppointmentBooking.Api.Exceptions;

public class UnauthorizedAppException : Exception
{
    public UnauthorizedAppException(string message) : base(message) { }
}
