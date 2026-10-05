namespace Identity.Application.Abstractions;

public sealed class UserAlreadyExistsException(string message) : InvalidOperationException(message);
