using System.Security.Cryptography;

namespace Service.Helpers
{
    // 16 rəqəmli kart nömrəsi: 4532 + 11 təsadüfi rəqəm + Luhn yoxlama rəqəmi
    public static class CardNumberGenerator
    {
        private const string Prefix = "4532";

        public static string Generate()
        {
            var digits = new char[15];
            Prefix.CopyTo(0, digits, 0, Prefix.Length);
            for (var i = Prefix.Length; i < 15; i++)
            {
                digits[i] = (char)('0' + RandomNumberGenerator.GetInt32(10));
            }

            var body = new string(digits);
            return body + LuhnCheckDigit(body);
        }

        // Kart nömrəsinin Luhn yoxlaması (top-up-da daxil edilən xarici kart üçün)
        public static bool IsValid(string number)
        {
            if (number.Length < 13 || number.Length > 19 || !number.All(char.IsAsciiDigit))
            {
                return false;
            }

            var sum = 0;
            for (var i = 0; i < number.Length; i++)
            {
                var digit = number[number.Length - 1 - i] - (char)48;
                if (i % 2 == 1)
                {
                    digit *= 2;
                    if (digit > 9)
                    {
                        digit -= 9;
                    }
                }
                sum += digit;
            }
            return sum % 10 == 0;
        }

        private static char LuhnCheckDigit(string body)
        {
            var sum = 0;
            for (var i = 0; i < body.Length; i++)
            {
                var digit = body[body.Length - 1 - i] - '0';
                if (i % 2 == 0)
                {
                    digit *= 2;
                    if (digit > 9)
                    {
                        digit -= 9;
                    }
                }
                sum += digit;
            }
            return (char)('0' + (10 - sum % 10) % 10);
        }
    }
}
