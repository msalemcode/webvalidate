﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Net.Http;
using System.Runtime.Serialization;
using WebValidation.Model;

namespace WebValidation.Response
{
    public static class Validator
    {
        // validate Request
        public static ValidationResult Validate(Request r, HttpResponseMessage response, string body)
        {
            ValidationResult result = new ValidationResult();

            if (r == null || r.Validation == null || body == null)
            {
                result.Failed = false;
                return result;
            }

            if (response == null)
            {
                result.Failed = true;
                result.ValidationErrors.Add("validate: null http response message");
                return result;
            }

            // validate status code - fail on error
            if (result.Add(ValidateStatusCode((int)response.StatusCode, r.Validation.StatusCode)))
            {
                return result;
            }

            // not found doesn't have body or headers
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return result;
            }

            // validate ContentType - fail on error
            if (result.Add(ValidateContentType(response.Content.Headers.ContentType.ToString(), r.Validation.ContentType)))
            {
                return result;
            }

            // run validation rules
            result.Add(ValidateLength((long)response.Content.Headers.ContentLength, r.Validation));
            result.Add(Validate(r.Validation, body));

            return result;
        }

        // validate Validation
        public static ValidationResult Validate(Validation v, string body)
        {
            ValidationResult result = new ValidationResult();

            if (v != null)
            {
                ValidateContains(v.Contains, body);
                ValidateExactMatch(v.ExactMatch, body);
                Validate(v.JsonObject, body);

                result.Add(Validate(v.JsonArray, body));
            }

            return result;
        }

        // validate JsonObject
        public static ValidationResult Validate(List<JsonProperty> properties, string body)
        {
            ValidationResult result = new ValidationResult();

            // nothing to check
            if (properties == null || properties.Count == 0)
            {
                return result;
            }

            if (properties != null && properties.Count > 0)
            {
                try
                {
                    // deserialize the json into an IDictionary
                    IDictionary<string, object> dict = JsonConvert.DeserializeObject<ExpandoObject>(body);

                    foreach (JsonProperty property in properties)
                    {
                        if (!string.IsNullOrEmpty(property.Field) && dict.ContainsKey(property.Field))
                        {
                            // null values check for the existance of the field in the payload
                            // used when values are not known
                            if (property.Value != null && !dict[property.Field].Equals(property.Value))
                            {
                                result.ValidationErrors.Add(string.Format(CultureInfo.InvariantCulture, $"\tjson: {property.Field}: {dict[property.Field]} : Expected: {property.Value}"));
                            }
                        }
                        else
                        {
                            result.ValidationErrors.Add(string.Format(CultureInfo.InvariantCulture, $"\tjson: Field Not Found: {property.Field}"));
                        }
                    }


                }
                catch (SerializationException se)
                {
                    result.ValidationErrors.Add(string.Format(CultureInfo.InvariantCulture, $"\tException: {se.Message}"));
                }

                catch (Exception ex)
                {
                    result.ValidationErrors.Add(string.Format(CultureInfo.InvariantCulture, $"\tException: {ex.Message}"));
                }
            }

            return result;
        }

        // validate JsonArray
        public static ValidationResult Validate(JsonArray jArray, string body)
        {
            ValidationResult result = new ValidationResult();

            if (jArray == null || string.IsNullOrWhiteSpace(body))
            {
                return result;
            }

            try
            {
                // deserialize the json
                List<dynamic> resList = JsonConvert.DeserializeObject<List<dynamic>>(body);

                result.Add(ValidateJsonArrayLength(jArray, resList));
                result.Add(ValidateForEach(jArray.ForEach, resList));

                ValidateByIndex(jArray.ByIndex, resList);

            }
            catch (SerializationException se)
            {
                result.ValidationErrors.Add(string.Format(CultureInfo.InvariantCulture, $"\tException: {se.Message}"));
                result.Failed = true;
            }

            catch (Exception ex)
            {
                result.ValidationErrors.Add(string.Format(CultureInfo.InvariantCulture, $"\tException: {ex.Message}"));
                result.Failed = true;
            }

            return result;
        }

        // validate ForEach
        private static ValidationResult ValidateForEach(List<Validation> validationList, List<dynamic> documentList)
        {
            ValidationResult result = new ValidationResult();

            // validate foreach items recursively
            if (validationList != null && validationList.Count > 0)
            {
                foreach (dynamic doc in documentList)
                {
                    // run each validation on each doc
                    foreach (Validation fe in validationList)
                    {
                        // call validate recursively
                        if (Validate(fe, JsonConvert.SerializeObject(doc)))
                        {
                            //TODo res += msg;
                        }
                    }
                }
            }

            return result;
        }

        // validate ByIndex
        private static ValidationResult ValidateByIndex(List<JsonPropertyByIndex> byIndexList, List<dynamic> documentList)
        {
            ValidationResult result = new ValidationResult();

            // validate array items by index
            if (byIndexList != null && byIndexList.Count > 0)
            {
                string fieldBody;

                foreach (var property in byIndexList)
                {
                    // nothing to validate
                    if (property.Validation == null)
                    {
                        break;
                    }

                    // check index in bounds
                    if (property.Index < 0 || property.Index >= documentList.Count)
                    {
                        result.ValidationErrors.Add(string.Format(CultureInfo.InvariantCulture, $"\byIndex: Index out of bounds: {property.Index}"));
                        break;
                    }

                    // set the body to entire doc or field
                    if (property.Field == null)
                    {
                        fieldBody = JsonConvert.SerializeObject(documentList[(int)property.Index]);
                    }
                    else
                    {
                        fieldBody = JsonConvert.SerializeObject(documentList[(int)property.Index][property.Field]);
                    }

                    // validate recursively
                    result.Add(Validate(property.Validation, fieldBody));
                }
            }

            return result;
        }

        // validate JsonArray Length, MinLength and MaxLength
        private static ValidationResult ValidateJsonArrayLength(JsonArray jArray, List<dynamic> documentList)
        {
            ValidationResult result = new ValidationResult();

            // validate count
            if (jArray.Count != null && jArray.Count != documentList.Count)
            {
                result.ValidationErrors.Add(string.Format(CultureInfo.InvariantCulture, $"\tJsonArrayCount: {documentList.Count} Expected: {jArray.Count}"));
            }

            // validate min count
            if (jArray.MinCount != null && jArray.MinCount > documentList.Count)
            {
                result.ValidationErrors.Add(string.Format(CultureInfo.InvariantCulture, $"\tMinJsonCount: {jArray.MinCount}  Actual: {documentList.Count}"));
            }

            // validate max count
            if (jArray.MaxCount != null && jArray.MaxCount < documentList.Count)
            {
                result.ValidationErrors.Add(string.Format(CultureInfo.InvariantCulture, $"\tMaxJsonCount: {jArray.MaxCount}  Actual: {documentList.Count}"));
            }

            return result;
        }

        // validate StatusCode
        public static ValidationResult ValidateStatusCode(int actual, int expected)
        {
            ValidationResult result = new ValidationResult();

            if (actual != expected)
            {
                result.ValidationErrors.Add(string.Format(CultureInfo.InvariantCulture, $"\tStatusCode: {actual} Expected: {expected}"));
            }

            return result;
        }

        // validate ContentType
        public static ValidationResult ValidateContentType(string actual, string expected)
        {
            ValidationResult result = new ValidationResult();

            if (!string.IsNullOrEmpty(expected))
            {
                if (actual != null && !actual.StartsWith(expected, StringComparison.OrdinalIgnoreCase))
                {
                    result.Failed = true;
                    result.ValidationErrors.Add(string.Format(CultureInfo.InvariantCulture, $"\tContentType: {actual} Expected: {expected}"));
                }
            }

            return result;
        }

        // validate Length, MinLength and MaxLength
        public static ValidationResult ValidateLength(long actual, Validation v)
        {
            ValidationResult result = new ValidationResult();

            // nothing to validate
            if (v == null || (v.Length == null && v.MinLength == null && v.MaxLength == null))
            {
                return result;
            }

            // validate length
            if (v.Length != null)
            {
                if (actual != v.Length)
                {
                    result.ValidationErrors.Add(string.Format(CultureInfo.InvariantCulture, $"\tLength: Actual: {actual} Expected: {v.Length}"));
                }
            }

            // validate minLength
            if (v.MinLength != null)
            {
                if (actual < v.MinLength)
                {
                    result.ValidationErrors.Add(string.Format(CultureInfo.InvariantCulture, $"\tMinContentLength: Actual: {actual} Expected: {v.MinLength}"));
                }
            }

            // validate maxLength
            if (v.MaxLength != null)
            {
                if (actual > v.MaxLength)
                {
                    result.ValidationErrors.Add(string.Format(CultureInfo.InvariantCulture, $"\tMaxContentLength: Actual: {actual} Expected: {v.MaxLength}"));
                }
            }

            return result;
        }

        // validate ExactMatch
        public static ValidationResult ValidateExactMatch(string exactMatch, string body)
        {
            ValidationResult result = new ValidationResult();

            if (exactMatch == null)
            {
                return result;
            }

            if (!string.IsNullOrEmpty(body) && exactMatch != null)
            {
                // compare values
                if (exactMatch != null && body != exactMatch)
                {
                    result.ValidationErrors.Add(string.Format(CultureInfo.InvariantCulture, $"\tExactMatch: Actual : {body.PadRight(40).Substring(0, 40).Trim()} : Expected: {exactMatch.PadRight(40).Substring(0, 40).Trim()}"));
                }
            }

            return result;
        }

        // validate Contains
        public static ValidationResult ValidateContains(List<string> containsList, string body)
        {
            ValidationResult result = new ValidationResult();

            if (containsList == null || containsList.Count == 0)
            {
                return result;
            }

            if (!string.IsNullOrEmpty(body) && containsList != null && containsList.Count > 0)
            {
                // validate each rule
                foreach (string c in containsList)
                {
                    // compare values
                    if (!body.Contains(c, StringComparison.InvariantCulture))
                    {
                        result.ValidationErrors.Add(string.Format(CultureInfo.InvariantCulture, $"\tContains: {c.PadRight(40).Substring(0, 40).Trim()}"));
                    }
                }
            }

            return result;
        }
    }
}