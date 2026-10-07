namespace Service.Helpers.Settings
{
    public class SmtpSettings
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 587;
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;   // user-secrets-də saxlanır
        public string FromAddress { get; set; } = string.Empty;
        public string FromName { get; set; } = "Caspian Bank";

        // Sertifikatın ləğv olunub-olunmadığının (CRL/OCSP) yoxlanması. Şəbəkə/antivirus bu yoxlamanı bloklayırsa
        // lokal sınaq üçün false edilə bilər; sertifikat zənciri və host adı yenə yoxlanılır.
        public bool CheckCertificateRevocation { get; set; } = true;

        // SMTP qurulmayıbsa OTP kodu və reset linki cavabda da qaytarılsın (yalnız Development-də true edilir).
        // Real email olmadan sınaq/təqdimat üçün; production-da MÜTLƏQ false qalmalıdır.
        public bool ShowCodesWhenNotConfigured { get; set; } = false;

        public bool ExposeCodes
        {
            get
            {
                return !IsConfigured && ShowCodesWhenNotConfigured;
            }
        }

        // Host, istifadəçi adı və parol verilməyibsə email göndərilmir, mətn konsola yazılır (yalnız sınaq üçün)
        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(Host)
            && !string.IsNullOrWhiteSpace(UserName)
            && !string.IsNullOrWhiteSpace(Password);
    }
}
