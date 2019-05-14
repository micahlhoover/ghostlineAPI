using System;
using System.Collections.Generic;
using System.Text;

namespace GhostLineAPI
{
    public class ValidationResponse
    {
        public bool Success { get; set; }
        public List<ValidationMessage> Messages { get; set; }

        public ValidationResponse() {
            Success = false;
        }
    }

    public enum ValidationMessageType
    {
        Unset,
        Info,
        Warning,
        Failure
    }

    /// <summary>
    /// These will be included in the HTTP response
    /// </summary>
    public class ValidationMessage
    {
        public String Message { get; set; }
        public String Code { get; set; }
        public ValidationMessageType ValidationType { get; set; }

        public ValidationMessage()
        {
            Message = String.Empty;
            Code = String.Empty;
            ValidationType = ValidationMessageType.Unset;
        }
    }
}
