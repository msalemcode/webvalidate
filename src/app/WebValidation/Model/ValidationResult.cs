using System.Collections.Generic;

namespace WebValidation.Model
{
    public class ValidationResult
    {
        public bool Failed => ValidationErrors.Count > 0;

        public List<string> ValidationErrors { get; } = new List<string>();

        public void Add(ValidationResult result)
        {
            if (result != null)
            {
                if (result.ValidationErrors != null && result.ValidationErrors.Count > 0)
                {
                    ValidationErrors.AddRange(result.ValidationErrors);
                }
            }
        }
    }
}
