using System;

namespace DerasaX.Domain.Exceptions
{
    public class ValidationException(string message) : Exception(message);
}
