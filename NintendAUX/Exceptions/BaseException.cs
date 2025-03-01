using System;

namespace NintendAUX.Exceptions;

public class BaseException : Exception
{
    public Exception Exception { get; protected set; }
    public string ExceptionName => "BaseException";
    
    protected BaseException(string message)
    {
        Exception = new Exception(message);
    }
}