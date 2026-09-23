namespace Shared.Files;

// The uploaded file as a whole cannot be used; the message is meant for the person who uploaded it
public class ImportFileException(string message) : Exception(message);
