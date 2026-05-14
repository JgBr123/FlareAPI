namespace FlareAPI
{
    internal class InvalidResponseBodyException : Exception
    {
        internal InvalidResponseBodyException() : base("Only one content type is allowed per response.") { }
    }
    internal class UnmodifiableResponseException : Exception
    {
        internal UnmodifiableResponseException() : base("A response that has a body cannot be modified.") { }
    }
    internal class InvalidContentRangeException : Exception
    {
        internal InvalidContentRangeException() : base("Content range was not in the correct format.") { }
    }
    internal class FileNotFoundException : Exception
    {
        internal FileNotFoundException(string filePath) : base($"The file \"{filePath}\" could not be found.") { }
    }
}
