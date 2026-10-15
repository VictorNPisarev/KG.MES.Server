namespace KG.MES.Main.Models
{
    public class ValidateException : Exception
    {
        public ValidateException() { }

        public ValidateException(string message) 
            : base(message) { }

        public ValidateException(string message, Exception inner) 
            : base(message, inner) { }
    }
}