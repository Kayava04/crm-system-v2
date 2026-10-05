namespace Identity.Contracts;

public sealed class ProfileNotFoundException(string message) : InvalidOperationException(message);

public sealed class ProfileAlreadyLinkedException(string message) : InvalidOperationException(message);
