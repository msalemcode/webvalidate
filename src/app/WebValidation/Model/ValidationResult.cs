using System.Collections.Generic;

namespace WebValidation.Model
{
    public class ValidationResult
    {
        public bool Failed { get; set; } = false;

        public bool Validated => ValidationErrors.Count == 0;

        public List<string> ValidationErrors { get; } = new List<string>();

        public bool Add(ValidationResult result)
        {
            if (result != null)
            {
                if (result.Failed)
                {
                    Failed = true;
                }

                if (result.ValidationErrors != null && !result.Validated)
                {
                    ValidationErrors.AddRange(result.ValidationErrors);
                }
            }

            return Failed;
        }
    }
}
