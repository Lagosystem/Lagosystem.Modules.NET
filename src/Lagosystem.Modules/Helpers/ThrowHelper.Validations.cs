namespace Lagosystem.Modules.Helpers;
using Lagosystem.Modules.LanguagesResource;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

internal static partial class ThrowHelper
{
    [DoesNotReturn]
    internal static void ThrowErrorStateRegistration([InvokerParameterName, LocalizationRequired(false)] string paramName) => 
        throw new ArgumentOutOfRangeException(paramName, ExceptionsResource.StateRegistrationInvalidState);
}