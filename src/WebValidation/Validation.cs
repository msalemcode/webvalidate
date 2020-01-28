using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Net.Http;
using System.Runtime.Serialization;

namespace WebValidation
{
    // integration test for testing any REST API or web site
    public partial class Test : IDisposable
    {
        /// <summary>
        /// Run all of the validation tests
        /// </summary>
        /// <param name="r">Request</param>
        /// <param name="resp">HttpResponseMessage</param>
        /// <param name="body">string</param>
        /// <returns>string</returns>
        public static string ValidateAll(Request r, HttpResponseMessage resp, string body)
        {
            string res = string.Empty;

            // validate the response
            if (resp != null && r?.Validation != null)
            {
                body ??= string.Empty;

                res += ValidateStatusCode(r, resp);

                // don't validate if status code is incorrect
                if (string.IsNullOrEmpty(res))
                {
                    res += ValidateContentType(r, resp);
                }

                // don't validate if content-type is incorrect
                if (string.IsNullOrEmpty(res))
                {
                    res += ValidateContentLength(r, resp);
                    res += ValidateContains(r, body);
                    res += ValidateExactMatch(r, body);
                    res += ValidateJsonArray(r, body);
                    res += ValidateJsonObject(r, body);
                }
            }

            return res;
        }

        // validate the status code
        public static string ValidateStatusCode(Request r, HttpResponseMessage resp)
        {
            if (r == null)
            {
                throw new ArgumentNullException(nameof(r));
            }

            if (resp == null)
            {
                throw new ArgumentNullException(nameof(resp));
            }

            string res = string.Empty;

            if ((int)resp.StatusCode != r.Validation.Code)
            {
                res += string.Format(CultureInfo.InvariantCulture, $"\tStatusCode: {(int)resp.StatusCode} Expected: {r.Validation.Code}\n");
            }

            return res;
        }

        // validate the content type header if specified in the test
        public static string ValidateContentType(Request r, HttpResponseMessage resp)
        {
            if (r == null)
            {
                throw new ArgumentNullException(nameof(r));
            }

            if (resp == null)
            {
                throw new ArgumentNullException(nameof(resp));
            }

            string res = string.Empty;

            if (!string.IsNullOrEmpty(r.Validation.ContentType))
            {
                if (resp.Content.Headers.ContentType != null && !resp.Content.Headers.ContentType.ToString().StartsWith(r.Validation.ContentType, StringComparison.OrdinalIgnoreCase))
                {
                    res += string.Format(CultureInfo.InvariantCulture, $"\tContentType: {resp.Content.Headers.ContentType} Expected: {r.Validation.ContentType}\n");
                }
            }

            return res;
        }

        // validate the content length range if specified in test
        public static string ValidateContentLength(Request r, HttpResponseMessage resp)
        {
            if (r == null)
            {
                throw new ArgumentNullException(nameof(r));
            }

            if (resp == null)
            {
                throw new ArgumentNullException(nameof(resp));
            }

            string res = string.Empty;

            // validate the content min length if specified in test
            if (r.Validation.MinLength > 0)
            {
                if (resp.Content.Headers.ContentLength < r.Validation.MinLength)
                {
                    res = string.Format(CultureInfo.InvariantCulture, $"\tMinContentLength: Actual: {resp.Content.Headers.ContentLength} Expected: {r.Validation.MinLength}\n");
                }
            }

            // validate the content max length if specified in test
            if (r.Validation.MaxLength > 0)
            {
                if (resp.Content.Headers.ContentLength > r.Validation.MaxLength)
                {
                    res += string.Format(CultureInfo.InvariantCulture, $"\tMaxContentLength: Actual: {resp.Content.Headers.ContentLength} Expected: {r.Validation.MaxLength}\n");
                }
            }

            return res;
        }

        // validate the exact match rule
        public static string ValidateExactMatch(Request r, string body)
        {
            if (r == null)
            {
                throw new ArgumentNullException(nameof(r));
            }

            string res = string.Empty;

            if (!string.IsNullOrEmpty(body) && r.Validation.ExactMatch != null)
            {
                // compare values
                if (!body.Equals(r.Validation.ExactMatch.Value, r.Validation.ExactMatch.IsCaseSensitive ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture))
                {
                    res += string.Format(CultureInfo.InvariantCulture, $"\tExactMatch: Actual : {body.PadRight(40).Substring(0, 40).Trim()} : Expected: {r.Validation.ExactMatch.Value.PadRight(40).Substring(0, 40).Trim()}\n");
                }
            }

            return res;
        }

        // validate the contains rules
        public static string ValidateContains(Request r, string body)
        {
            if (r == null)
            {
                throw new ArgumentNullException(nameof(r));
            }

            string res = string.Empty;

            if (!string.IsNullOrEmpty(body) && r.Validation.Contains != null && r.Validation.Contains.Count > 0)
            {
                // validate each rule
                foreach (ValueCheck c in r.Validation.Contains)
                {
                    // compare values
                    if (!body.Contains(c.Value, c.IsCaseSensitive ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture))
                    {
                        res += string.Format(CultureInfo.InvariantCulture, $"\tContains: {c.Value.PadRight(40).Substring(0, 40).Trim()}\n");
                    }
                }
            }

            return res;
        }

        // run json array validation rules
        public static string ValidateJsonArray(Request r, string body)
        {
            if (r == null)
            {
                throw new ArgumentNullException(nameof(r));
            }

            string res = string.Empty;

            if (r.Validation.JsonArray != null)
            {
                try
                {
                    // deserialize the json
                    List<dynamic> resList = JsonConvert.DeserializeObject<List<dynamic>>(body) as List<dynamic>;

                    // validate count
                    if (r.Validation.JsonArray.Count > 0 && r.Validation.JsonArray.Count != resList.Count)
                    {
                        res += string.Format(CultureInfo.InvariantCulture, $"\tJsonCount: {r.Validation.JsonArray.Count}  Actual: {resList.Count}\n");
                    }

                    // validate count is zero
                    if (r.Validation.JsonArray.CountIsZero && 0 != resList.Count)
                    {
                        res += string.Format(CultureInfo.InvariantCulture, $"\tJsonCountIsZero: Actual: {resList.Count}\n");
                    }

                    // validate min count
                    if (r.Validation.JsonArray.MinCount > 0 && r.Validation.JsonArray.MinCount > resList.Count)
                    {
                        res += string.Format(CultureInfo.InvariantCulture, $"\tMinJsonCount: {r.Validation.JsonArray.MinCount}  Actual: {resList.Count}\n");
                    }

                    // validate max count
                    if (r.Validation.JsonArray.MaxCount > 0 && r.Validation.JsonArray.MaxCount < resList.Count)
                    {
                        res += string.Format(CultureInfo.InvariantCulture, $"\tMaxJsonCount: {r.Validation.JsonArray.MaxCount}  Actual: {resList.Count}\n");
                    }
                }
                catch (SerializationException se)
                {
                    res += string.Format(CultureInfo.InvariantCulture, $"\tException: {se.Message}\n");
                }

                catch (Exception ex)
                {
                    res += string.Format(CultureInfo.InvariantCulture, $"\tException: {ex.Message}\n");
                }
            }

            return res;
        }

        // run json object validation rules
        public static string ValidateJsonObject(Request r, string body)
        {
            if (r == null)
            {
                throw new ArgumentNullException(nameof(r));
            }

            string res = string.Empty;

            if (r.Validation.JsonObject != null && r.Validation.JsonObject.Count > 0)
            {
                try
                {
                    // deserialize the json into an IDictionary
                    IDictionary<string, object> dict = JsonConvert.DeserializeObject<ExpandoObject>(body);

                    foreach (JsonProperty f in r.Validation.JsonObject)
                    {
                        if (!string.IsNullOrEmpty(f.Field) && dict.ContainsKey(f.Field))
                        {
                            // null values check for the existance of the field in the payload
                            // used when values are not known
                            if (f.Value != null && !dict[f.Field].Equals(f.Value))
                            {
                                res += string.Format(CultureInfo.InvariantCulture, $"\tjson: {f.Field}: {dict[f.Field]} : Expected: {f.Value}\n");
                            }
                        }
                        else
                        {
                            res += string.Format(CultureInfo.InvariantCulture, $"\tjson: Field Not Found: {f.Field}\n");
                        }
                    }


                }
                catch (SerializationException se)
                {
                    res += string.Format(CultureInfo.InvariantCulture, $"\tException: {se.Message}\n");
                }

                catch (Exception ex)
                {
                    res += string.Format(CultureInfo.InvariantCulture, $"\tException: {ex.Message}\n");
                }
            }

            return res;
        }
    }
}
