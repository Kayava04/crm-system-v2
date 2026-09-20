namespace Identity.Application.Abstractions;

// The email is already taken; thrown when a second request created the same account a moment earlier
public sealed class UserAlreadyExistsException(string message) : InvalidOperationException(message);
