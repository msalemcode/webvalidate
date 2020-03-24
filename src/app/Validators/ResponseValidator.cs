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
                return result;
            }

            if (response == null)
            {
                result.ValidationErrors.Add("validate: null http response message");
                return result;
            }

            // validate status code - fail on error
            result.Add(ValidateStatusCode((int)response.StatusCode, r.Validation.StatusCode));

            // redirects don't have body or headers
            if ((int)response.StatusCode >= 300 && (int)response.StatusCode <= 399)
            {
                return result;
            }

            // handle framework 404s
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound && response.Content.Headers.ContentType == null)
            {
                return result;
            }

            // validate ContentType - fail on error
            result.Add(ValidateContentType(response.Content.Headers.ContentType.ToString(), r.Validation.ContentType));

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
                result.Add(ValidateContains(v.Contains, body));
                result.Add(ValidateExactMatch(v.ExactMatch, body));
                result.Add(Validate(v.JsonObject, body));

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
                            if (property.Validation != null)
                            {
                                var res = Validate(property.Validation, JsonConvert.SerializeObject(dict[property.Field]));
                            }

                            // null values check for the existance of the field in the payload
                            // used when values are not known
                            if (property.Value != null && !dict[property.Field].Equals(property.Value))
                            {
                                // whole numbers map to int
                                if (!((property.Value.GetType() == typeof(double) ||
                                    property.Value.GetType() == typeof(float) ||
                                    property.Value.GetType() == typeof(decimal)) &&
                                    double.TryParse(dict[property.Field].ToString(), out double d) &&
                                    (double)property.Value == d))
                                {
                                    result.ValidationErrors.Add(string.Format(CultureInfo.InvariantCulture, $"\tjson: {property.Field}: {dict[property.Field]} : Expected: {property.Value}"));
                                }
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

                result.Add(ValidateByIndex(jArray.ByIndex, resList));
            }
            catch (SerializationException se)
            {
                result.ValidationErrors.Add(string.Format(CultureInfo.InvariantCulture, $"\tException: {se.Message}"));
            }

            catch (Exception ex)
            {
                result.ValidationErrors.Add(string.Format(CultureInfo.InvariantCulture, $"\tException: {ex.Message}"));
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
                        result.Add(Validate(fe, JsonConvert.SerializeObject(doc)));
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
                double d = 0;
                int ndx = -1;

                foreach (var property in byIndexList)
                {
                    ndx++;

                    // check index in bounds
                    if (property.Index < 0 || property.Index >= documentList.Count)
                    {
                        result.ValidationErrors.Add(string.Format(CultureInfo.InvariantCulture, $"\byIndex: Index out of bounds: {property.Index}"));
                        break;
                    }

                    // validate recursively
                    if (property.Validation != null)
                    {
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
                    else if (!string.IsNullOrEmpty(property.Field) && property.Value != null)
                    {
                        // null values check for the existance of the field in the payload
                        // used when values are not known
                        if (documentList[(int)property.Index][property.Field] != property.Value)
                        {
                            // whole numbers map to int
                            if (!((property.Value.GetType() == typeof(double) ||
                                property.Value.GetType() == typeof(float) ||
                                property.Value.GetType() == typeof(decimal)) &&
                                double.TryParse(documentList[(int)property.Index][property.Field].ToString(), out d) &&
                                (double)property.Value == d))
                            {
                                result.ValidationErrors.Add(string.Format(CultureInfo.InvariantCulture, $"\tjson: {property.Field}: {documentList[(int)property.Index][property.Field]} : Expected: {property.Value}"));
                            }
                        }
                    }
                    else if (property.Value != null)
                    {
                        // used for checking array of simple type
                        if (!property.Value.Equals(documentList[(int)property.Index]))
                        {
                            result.ValidationErrors.Add(string.Format(CultureInfo.InvariantCulture, $"\tjson: {property.Field}: {documentList[(int)property.Index]} : Expected: {property.Value}"));
                        }
                    }
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
