using System;
using System.Collections.Generic;
using System.Globalization;
using WebValidation.Model;


namespace WebValidation.Parameters
{
    public static class Validator
    {
        /// <summary>
        /// validate the request rules
        /// </summary>
        /// <param name="r">Request</param>
        /// <param name="message">out string error message</param>
        /// <returns></returns>
        public static ValidationResult Validate(Request r)
        {
            ValidationResult result = new ValidationResult();

            if (r == null)
            {
                result.Failed = true;
                result.ValidationErrors.Add("request is null");
                return result;
            }

            // validate the request path
            if (!result.Add(ValidatePath(r.Path)))
            {
                return result;
            }

            // validate the verb
            if (!result.Add(ValidateVerb(r.Verb)))
            {
                return result;
            }

            // validate the rules
            result.Add(Validate(r.Validation));

            return result;
        }

        public static ValidationResult Validate(Validation v)
        {
            ValidationResult res = new ValidationResult();

            // nothing to validate
            if (v == null)
            {
                return res;
            }

            // validate http status code
            if (v.StatusCode < 100 || v.StatusCode > 599)
            {
                res.ValidationErrors.Add("statusCode: invalid status code: " + v.StatusCode.ToString(CultureInfo.InvariantCulture));
            }

            // validate ContentType
            if (v.ContentType != null && v.ContentType.Length == 0)
            {
                res.ValidationErrors.Add("contentType: ContentType cannot be empty");
            }

            // validate ContentType
            if (v.ExactMatch != null && v.ExactMatch.Length == 0)
            {
                res.ValidationErrors.Add("exactMatch: exactMatch cannot be empty string");
            }

            //validate lengths
            res.Add(ValidateLength(v));

            // validate MaxMilliSeconds
            if (v.MaxMilliseconds != null && v.MaxMilliseconds <= 0)
            {
                res.ValidationErrors.Add("maxMilliseconds: maxMilliseconds cannot be less than zero");
            }

            // validate Contains
            res.Add(ValidateContains(v.Contains));

            // validate perfTarget
            res.Add(Validate(v.PerfTarget));

            // validate json array parameters
            res.Add(Validate(v.JsonArray));

            return res;
        }

        ///<summary>
        /// validate PerfTarget
        ///</summary>
        /// <param name="message">error message</param>
        /// <returns>bool success (out message)</returns>
        public static ValidationResult Validate(PerfTarget p)
        {
            ValidationResult res = new ValidationResult();

            // null check
            if (p == null)
            {
                return res;
            }

            // validate Category
            if (string.IsNullOrWhiteSpace(p.Category))
            {
                res.ValidationErrors.Add("category: category cannot be empty");
            }

            //validate Targets
            if (p.Quartiles == null || p.Quartiles.Count != 3)
            {
                res.ValidationErrors.Add("quartiles: quartiles must have 3 values");
            }

            p.Category = p.Category.Trim();

            return res;
        }

        /// <summary>
        /// validate the json array rules
        /// </summary>
        /// <param name="rule">JsonArray</param>
        /// <param name="message">error message</param>
        /// <returns>bool success (out message)</returns>
        public static ValidationResult Validate(JsonArray a)
        {
            ValidationResult res = new ValidationResult();

            // null check
            if (a == null)
            {
                return res;
            }

            // must be >= 0
            if (a.Count < 0 || a.MinCount < 0 || a.MaxCount < 0)
            {
                res.ValidationErrors.Add("jsonArray: count parameters must be >= 0");
            }

            return res;
        }

        ///<summary>
        /// validate Length, MinLength and MaxLength
        ///</summary>
        /// <param name="message">error message</param>
        /// <returns>bool success (out message)</returns>
        private static ValidationResult ValidateLength(Validation v)
        {
            ValidationResult res = new ValidationResult();

            // nothing to validate
            if (v == null)
            {
                return res;
            }

            // validate Length
            if (v.Length != null && v.Length < 0)
            {
                res.ValidationErrors.Add("length: length cannot be empty");
            }

            // validate MinLength
            if (v.MinLength != null && v.MinLength < 0)
            {
                res.ValidationErrors.Add("minlength: minLength cannot be empty");
            }

            // validate MaxLength
            if (v.MaxLength != null)
            {
                if (v.MaxLength < 0)
                {
                    res.ValidationErrors.Add("maxLength: maxLength must be greater than zero");
                }

                if (v.MinLength != null && v.MaxLength <= v.MinLength)
                {
                    res.ValidationErrors.Add("maxLength: maxLength must be greater than minLength");
                }
            }

            return res;
        }

        /// <summary>
        /// validate the request HTTP verb
        /// </summary>
        /// <param name="verb">string</param>
        /// <param name="message">out string error message</param>
        /// <returns></returns>
        private static ValidationResult ValidateVerb(string verb)
        {
            ValidationResult res = new ValidationResult();

            if (!string.IsNullOrEmpty(verb))
            {
                verb = verb.Trim().ToUpperInvariant();
            }

            // verb must be in this list
            if (!(new List<string> { "GET", "HEAD", "POST", "PUT", "DELETE", "TRACE", "OPTIONS", "CONNECT", "PATCH" }).Contains(verb))
            {
                res.Failed = true;
                res.ValidationErrors.Add("verb: invalid verb: " + verb);
            }

            return res;
        }

        /// <summary>
        /// validate Contains
        /// </summary>
        /// <param name="message">out string error message</param>
        /// <returns></returns>
        private static ValidationResult ValidateContains(List<string> contains)
        {
            ValidationResult res = new ValidationResult();

            // null check
            if (contains == null || contains.Count == 0)
            {
                return res;
            }

            // validate each value
            foreach (string c in contains)
            {
                if (string.IsNullOrEmpty(c))
                {
                    res.ValidationErrors.Add("contains: values cannot be empty");
                }
            }

            return res;
        }

        /// <summary>
        /// validate request path
        /// </summary>
        /// <param name="path">string</param>
        /// <param name="message">out string error message</param>
        /// <returns></returns>
        private static ValidationResult ValidatePath(string path)
        {
            ValidationResult res = new ValidationResult();

            // path is required
            if (string.IsNullOrWhiteSpace(path))
            {
                res.Failed = true;
                res.ValidationErrors.Add("path: path is required");
            }
            // path must begin with /
            else if (!path.StartsWith("/", StringComparison.OrdinalIgnoreCase))
            {
                res.Failed = true;
                res.ValidationErrors.Add("path: path must begin with /");
            }

            return res;
        }
    }
}
