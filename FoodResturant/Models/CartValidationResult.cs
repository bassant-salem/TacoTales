namespace FoodResturant.Models
{
   
    public class CartValidationResult
    {
        public bool IsValid { get; private set; }
        public string ErrorMessage { get; private set; } = string.Empty;

        private CartValidationResult(bool isValid, string errorMessage = "")
        {
            IsValid = isValid;
            ErrorMessage = errorMessage;
        }

      
        public static CartValidationResult Ok() => new(true);
        public static CartValidationResult Fail(string errorMessage) => new(false, errorMessage);
    }
}