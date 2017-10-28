using System.Collections.Generic;
using System.Runtime.InteropServices;
using Std.NanoMsg.Internal;
using Std.NanoMsg.Native;

namespace Std.NanoMsg
{
    public class NanoException : ExternalException
    {
        /// <summary>
        ///     This managed cache of error messages tries to avoid a repetitive re-marshaling of error strings.
        /// </summary>
        private static readonly Dictionary<int, string> _errorMessages = new Dictionary<int, string>();

        public NanoException(string customError, int errorCode)
            : base(CreateError(customError, errorCode), errorCode)
        {
        }

        public NanoException(string customError)
            : this(customError, Library.nn_errno())
        {
        }

        public NanoException()
            : this(null, Library.nn_errno())
        {
        }

        public static string ErrorCodeToMessage(int errorCode)
        {
            string errorMessage;
            lock (_errorMessages)
            {
                if (!_errorMessages.TryGetValue(errorCode, out errorMessage))
                {
                    errorMessage = _errorMessages[errorCode] = Library.GetErrorDescription(errorCode);
                }
            }
            return errorMessage;
        }

        private static string CreateError(string customError, int errorCode)
        {
            var errorMessage = ErrorCodeToMessage(errorCode);

            if (string.IsNullOrEmpty(customError))
            {
                return errorMessage;
            }

            return string.Concat(customError, ": ", errorMessage);
        }
    }
}