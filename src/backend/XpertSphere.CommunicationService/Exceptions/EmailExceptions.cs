namespace XpertSphere.CommunicationService.Exceptions;

public class TemplateNotFoundException(string templateName, string language)
    : Exception($"Template '{templateName}' not found for language '{language}'.");

public class MissingTemplateVariableException(string templateName, IEnumerable<string> missingVariables)
    : Exception($"Template '{templateName}' is missing required variable(s): {string.Join(", ", missingVariables)}.");

public class EmailSendException(string message, Exception innerException)
    : Exception(message, innerException);
