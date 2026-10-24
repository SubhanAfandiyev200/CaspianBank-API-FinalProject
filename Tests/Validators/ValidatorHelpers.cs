using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace CaspianBank.Tests.Validators
{
    // Bütün validator testləri üçün ortaq köməkçilər
    public static class V
    {
        public static List<string> Errors<T>(IValidator<T> validator, T model)
        {
            return validator.Validate(model).Errors.Select(e => e.ErrorMessage).ToList();
        }

        public static bool IsValid<T>(IValidator<T> validator, T model)
        {
            return validator.Validate(model).IsValid;
        }

        public static IFormFile File(string name = "file.png", int length = 10)
        {
            return new FormFile(new MemoryStream(new byte[length]), 0, length, "file", name);
        }

        public static IFormFile EmptyFile()
        {
            return new FormFile(new MemoryStream(), 0, 0, "file", "empty.png");
        }

        public static string Text(int length)
        {
            return new string('a', length);
        }

        // Kənar boşluqları atandan sonra Limit simvoldan çox olan mətn
        public static string Padded(int length)
        {
            return "  " + new string('b', length) + "  ";
        }
    }
}
