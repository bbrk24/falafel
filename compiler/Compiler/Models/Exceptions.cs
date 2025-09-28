namespace Compiler.Models;

public class TypeCheckException : Exception
{
    public int? LineNumber { get; init; }

    public TypeCheckException(string message, Exception? innerException = null)
        : base(message, innerException) { }

    public TypeCheckException(string message, int? lineNumber)
        : base(message)
    {
        LineNumber = lineNumber;
    }

    public override string Message
    {
        get
        {
            var message = base.Message;

            if (LineNumber is not null)
            {
                return $"line {LineNumber}: {message}";
            }

            return message;
        }
    }
}
