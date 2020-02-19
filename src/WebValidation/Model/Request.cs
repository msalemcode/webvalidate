using System;
using System.Collections.Generic;
using System.Globalization;

namespace WebValidation
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "can't be read-only - json serialization")]
    public class Request
    {
        public string Verb { get; set; } = "GET";
        public string Url { get; set; }
        public PerfTarget PerfTarget { get; set; }
        public Validation Validation { get; set; }

        /// <summary>
        /// Validate the request rules
        /// </summary>
        /// <param name="r">Request</param>
        /// <param name="message">out string error message</param>
        /// <returns></returns>
        public bool Validate(out string message)
        {
            // validate the request path
            if (!ValidatePath(out message))
            {
                return false;
            }

            // validate the verb
            if (!ValidateVerb(out message))
            {
                return false;
            }

            // nothing to check
            if (Validation == null)
            {
                message = string.Empty;
                return true;
            }

            // validate http status code
            if (Validation.Code < 100 || Validation.Code > 599)
            {
                message = "statusCode: invalid status code: " + Validation.Code.ToString(CultureInfo.InvariantCulture);
                return false;
            }

            // validate ContentType

            // validate Length

            // validate MinLength

            // validate MaxLength

            // validate MaxMilliSeconds

            // validate json array parameters
            if (Validation.JsonArray != null && !ValidateJsonArray(out message))
            {
                return false;
            }

            // TODO - validate other params

            // validated
            message = string.Empty;
            return true;
        }

        /// <summary>
        /// validate the request HTTP verb
        /// </summary>
        /// <param name="verb">string</param>
        /// <param name="message">out string error message</param>
        /// <returns></returns>
        private bool ValidateVerb(out string message)
        {
            if (!string.IsNullOrEmpty(Verb))
            {
                Verb = Verb.Trim().ToUpperInvariant();
            }

            // verb must be in this list
            if (!(new List<string> { "GET", "HEAD", "POST", "PUT", "DELETE", "TRACE", "OPTIONS", "CONNECT", "PATCH" }).Contains(Verb))
            {
                message = "verb: invalid verb: " + Verb;
                return false;
            }

            // validated
            message = string.Empty;
            return true;
        }

        /// <summary>
        /// validate request path
        /// </summary>
        /// <param name="path">string</param>
        /// <param name="message">out string error message</param>
        /// <returns></returns>
        private bool ValidatePath(out string message)
        {
            // url is required
            if (string.IsNullOrWhiteSpace(Url))
            {
                message = "url: url is required";
                return false;
            }

            // url must begin with /
            if (!Url.StartsWith("/", StringComparison.OrdinalIgnoreCase))
            {
                message = "url: url must begin with /";
                return false;
            }

            // validated
            message = string.Empty;
            return true;
        }

        /// <summary>
        /// Validate the json array rules
        /// </summary>
        /// <param name="rule">JsonArray</param>
        /// <param name="message">error message</param>
        /// <returns>bool success (out message)</returns>
        private bool ValidateJsonArray(out string message)
        {
            // null check
            if (Validation.JsonArray == null)
            {
                message = "jsonArray: cannot validate null request";
                return false;
            }

            // must be >= 0 (0 is default
            if (Validation.JsonArray.Count < 0 || Validation.JsonArray.MinCount < 0 || Validation.JsonArray.MaxCount < 0)
            {
                message = "jsonArray: count parameters must be >= 0";
                return false;
            }

            // validated
            message = string.Empty;
            return true;
        }
    }
}
