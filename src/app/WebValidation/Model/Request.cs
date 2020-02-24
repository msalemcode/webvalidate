using System;
using System.Collections.Generic;
using System.Globalization;


namespace WebValidation.Model
{
    public class Request
    {
        public string Verb { get; set; } = "GET";
        public string Path { get; set; }
        public PerfTarget PerfTarget { get; set; }
        public Validation Validation { get; set; }

        /// <summary>
        /// validate the request rules
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
            if (Validation.StatusCode < 100 || Validation.StatusCode > 599)
            {
                message = "statusCode: invalid status code: " + Validation.StatusCode.ToString(CultureInfo.InvariantCulture);
                return false;
            }

            // validate ContentType
            if (Validation.ContentType != null && Validation.ContentType.Length == 0)
            {
                message = "contentType: invalid mime type: " + Validation.ContentType.ToString(CultureInfo.InvariantCulture);
                return false;
            }

            // validate ContentType
            if (Validation.ExactMatch != null && Validation.ExactMatch.Length == 0)
            {
                message = "exactMatch: exactMatch cannot be empty string";
                return false;
            }

            //validate lengths
            if (!ValidateLengths(out message))
            {
                return false;
            }

            // validate MaxMilliSeconds
            if (Validation.MaxMilliseconds != null && Validation.MaxMilliseconds <= 0)
            {
                message = "maxMilliseconds: maxMilliseconds cannot be less than zero";
                return false;
            }

            // validate perfTarget
            if (!ValidatePerfTarget(out message))
            {
                return false;
            }

            // validate json array parameters
            if (!ValidateJsonArray(out message))
            {
                return false;
            }

            // validated
            message = string.Empty;
            return true;
        }

        ///<summary>
        /// validate PerfTarget
        ///</summary>
        /// <param name="message">error message</param>
        /// <returns>bool success (out message)</returns>
        private bool ValidatePerfTarget(out string message)
        {
            // null check
            if (Validation.PerfTarget == null)
            {
                message = string.Empty;
                return true;
            }

            // validate Category
            if (string.IsNullOrWhiteSpace(Validation.PerfTarget.Category))
            {
                message = "category: category cannot be empty";
                return false;
            }

            //validate Targets
            if (Validation.PerfTarget.Quartiles == null || Validation.PerfTarget.Quartiles.Count != 3)
            {
                message = "quartiles: quartiles must have 3 values";
                return false;
            }

            Validation.PerfTarget.Category = Validation.PerfTarget.Category.Trim();

            //validated
            message = string.Empty;
            return true;
        }

        ///<summary>
        /// validate Length, MinLength and MaxLength
        ///</summary>
        /// <param name="message">error message</param>
        /// <returns>bool success (out message)</returns>
        private bool ValidateLengths(out string message)
        {

            // validate Length
            if (Validation.Length != null && Validation.Length < 0)
            {
                message = "length: length cannot be empty";
                return false;
            }

            // validate MinLength
            if (Validation.MinLength != null && Validation.MinLength < 0)
            {
                message = "minlength: minLength cannot be empty";
                return false;
            }

            // validate MaxLength
            if (Validation.MaxLength != null)
            {
                if (Validation.MaxLength < 0)
                {
                    message = "maxLength: maxLength must be greater than zero";
                    return false;
                }

                if (Validation.MinLength != null && Validation.MaxLength <= Validation.MinLength)
                {
                    message = "maxLength: maxLength must be greater than minLength";
                    return false;
                }
            }

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
            // path is required
            if (string.IsNullOrWhiteSpace(Path))
            {
                message = "path: path is required";
                return false;
            }

            // path must begin with /
            if (!Path.StartsWith("/", StringComparison.OrdinalIgnoreCase))
            {
                message = "path: path must begin with /";
                return false;
            }

            // validated
            message = string.Empty;
            return true;
        }

        /// <summary>
        /// validate the json array rules
        /// </summary>
        /// <param name="rule">JsonArray</param>
        /// <param name="message">error message</param>
        /// <returns>bool success (out message)</returns>
        private bool ValidateJsonArray(out string message)
        {
            // null check
            if (Validation.JsonArray == null)
            {
                message = string.Empty;
                return true;
            }

            // must be >= 0
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
