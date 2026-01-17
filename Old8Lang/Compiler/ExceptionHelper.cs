namespace Old8Lang.Compiler;

public static class ExceptionHelper
{
    public static bool IsMatch(Exception exception, string? exceptionType)
    {
        if (string.IsNullOrEmpty(exceptionType) || exceptionType == "Exception" || exceptionType == "Old8Exception")
        {
            return true;
        }

        // Handle ExceptionWrapper
        // ExceptionWrapper does not inherit from Exception, so we can't cast/pattern match Exception to it directly
        // in a way that satisfies the compiler if the static type is Exception.
        // However, in our runtime/compiler logic, we treat System.Exception as the base.
        // If we ever wrap it in ExceptionWrapper, it's usually for the variable in the catch block, not the exception itself caught by CLR.
        // So we can remove the ExceptionWrapper check here because the input 'exception' is the raw exception caught by CLR.
        
        var currentType = exception.GetType();
        while (currentType is not null)
        {
            // Exact match
            if (currentType.Name == exceptionType)
            {
                return true;
            }

            // Full name match
            if (currentType.FullName == exceptionType)
            {
                return true;
            }

            // Namespace match (e.g. "Error.RuntimeError")
            if (currentType.FullName?.EndsWith($".{exceptionType}") == true || 
                currentType.FullName?.Contains($".{exceptionType}.") == true)
            {
                return true;
            }

            // Base type
            currentType = currentType.BaseType;
        }

        return false;
    }
}
