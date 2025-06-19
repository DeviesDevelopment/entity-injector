namespace EntityInjector.Route.Exceptions;

public class InternalServerErrorException(string message) : Exception(message);

public class NotFoundException(string message) : Exception(message);
