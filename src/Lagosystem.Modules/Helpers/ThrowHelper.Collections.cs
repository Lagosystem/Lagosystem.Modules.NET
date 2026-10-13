
using Lagosystem.Modules.Collections;
using Lagosystem.Modules.LanguagesResource;

namespace Lagosystem.Modules.Helpers
{
    using Modules.Collections;
    using System.Runtime.CompilerServices;
    using Modules.LanguagesResource;
    using Modules.Extensions;

    using msca = System.Diagnostics.CodeAnalysis;
    using jba = JetBrains.Annotations;

    internal static partial class ThrowHelper
    {

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowIfDestinationArrayTooSmall(int required, int available, 
            [jba.InvokerParameterName, jba.LocalizationRequired(false)] string? paramName = null)
        {
            if (required <= available) return;
            // Formatando a string do Resource com os valores atuais
            string message = string.Format(ExceptionsResource.StringList_CopyTo_ArrayDestinationTooSmall, 
                required, available);
            throw paramName.IsBlank() ? new ArgumentException(message) : new ArgumentException(message, paramName);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowIfArrayRankNotOne(Array array,
            [jba.InvokerParameterName, jba.LocalizationRequired(false)]
            string? paramName = null)
        {
            if (array is null) return;
            if (array.Rank == 1) return;
            string message = string.Format(ExceptionsResource.ArrayRankMustBeOne, array.Rank);            
            throw new ArgumentException(message, nameof(array)); 
        }

    }
}