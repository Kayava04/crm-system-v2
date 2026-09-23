namespace Identity.Contracts;

// Thrown by IProfileLinker implementations so registration can answer with a proper status instead of a 500
public sealed class ProfileNotFoundException(string message) : InvalidOperationException(message);

public sealed class ProfileAlreadyLinkedException(string message) : InvalidOperationException(message);
