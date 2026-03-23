using FluentValidation;
using UniCliqueBackend.Application.DTOs.Auth;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using UniCliqueBackend.Application.Options;

namespace UniCliqueBackend.Application.Validators.Auth
{
    public class RegisterRequestValidator : AbstractValidator<RegisterRequestDto>
    {
        private readonly EmailPolicyOptions _emailPolicy;
        private readonly List<string> _domains;
        private readonly List<string> _suffixes;

        public RegisterRequestValidator(IOptions<EmailPolicyOptions> emailPolicy)
        {
            _emailPolicy = emailPolicy.Value ?? new EmailPolicyOptions();
            _domains = (_emailPolicy.AllowedDomains ?? new List<string>()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            _suffixes = (_emailPolicy.AllowedSuffixes ?? new List<string>()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (_domains.Count == 0 && _suffixes.Count == 0 && _emailPolicy.RestrictToAllowedList)
            {
                _domains = new List<string> { "gmail.com", "outlook.com", "hotmail.com" };
                _suffixes = new List<string> { ".edu.tr", ".edu.com.tr" };
            }

            RuleFor(x => x.FullName)
                .NotEmpty().MaximumLength(100);

            RuleFor(x => x.Email)
                .NotEmpty()
                .Must(IsValidEmail).WithMessage("Geçerli bir e-posta adresi girin.")
                .Must(IsAllowedDomain).WithMessage(GetAllowedDomainsMessage());

            RuleFor(x => x.Username)
                .NotEmpty().MinimumLength(3).MaximumLength(50);

            RuleFor(x => x.Password)
                .NotEmpty()
                .MinimumLength(8)
                .Must(HasUpper).WithMessage("Parola en az bir büyük harf içermeli.")
                .Must(HasLower).WithMessage("Parola en az bir küçük harf içermeli.")
                .Must(HasPunctuation).WithMessage("Parola en az bir noktalama işareti içermeli.");

            RuleFor(x => x.PhoneNumber)
                .NotEmpty()
                .Must(IsValidPhone).WithMessage("Telefon numarası 0 ile başlamalı ve toplam 11 haneli olmalı (05XXXXXXXXX).");

            RuleFor(x => x.BirthDate)
                .Must(IsAdult).WithMessage("Yaş 18’den küçük olamaz.");

            RuleFor(x => x.AcceptKvkk)
                .Equal(true).WithMessage("KVKK must be accepted.");

            RuleFor(x => x.AcceptTerms)
                .Equal(true).WithMessage("Terms must be accepted.");

            RuleFor(x => x.AcceptPrivacy)
                .Equal(true).WithMessage("Privacy policy must be accepted.");
        }

        private static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        }

        private bool IsAllowedDomain(string email)
        {
            if (!_emailPolicy.RestrictToAllowedList) return true;
            var atIndex = email.LastIndexOf('@');
            if (atIndex < 0 || atIndex == email.Length - 1) return false;
            var domain = email[(atIndex + 1)..].ToLowerInvariant();

            if (_domains.Any(d => string.Equals(d, domain, StringComparison.OrdinalIgnoreCase)))
                return true;

            if (_suffixes.Any(suffix => domain.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)))
                return true;

            return false;
        }

        private string GetAllowedDomainsMessage()
        {
            if (!_emailPolicy.RestrictToAllowedList) return "Geçerli bir e-posta adresi girin.";
            var domains = string.Join(", ", _domains);
            var suffixes = string.Join(", ", _suffixes);
            if (!string.IsNullOrEmpty(suffixes))
                return $"E-posta alanı şu listede olmalı: {domains} veya şu son eklerle bitmeli: {suffixes}.";
            return $"E-posta alanı şu listede olmalı: {domains}.";
        }

        private static bool IsValidPhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return false;
            phone = phone.Trim();
            // Zorunlu kural: 0 ile başlamalı ve toplam 11 hane olmalı; GSM: 05XXXXXXXXX
            return Regex.IsMatch(phone, @"^0\d{10}$");
        }

        private static bool IsAdult(DateTime birthDate)
        {
            var minDate = DateTime.UtcNow.AddYears(-18);
            return birthDate <= minDate;
        }

        private static bool HasUpper(string password)
        {
            if (string.IsNullOrEmpty(password)) return false;
            foreach (var ch in password)
            {
                if (char.IsUpper(ch)) return true;
            }
            return false;
        }

        private static bool HasLower(string password)
        {
            if (string.IsNullOrEmpty(password)) return false;
            foreach (var ch in password)
            {
                if (char.IsLower(ch)) return true;
            }
            return false;
        }

        private static bool HasPunctuation(string password)
        {
            if (string.IsNullOrEmpty(password)) return false;
            foreach (var ch in password)
            {
                if (char.IsPunctuation(ch)) return true;
            }
            return false;
        }
    }
}
