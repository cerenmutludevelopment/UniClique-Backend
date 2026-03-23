using UniCliqueBackend.Application.DTOs.Auth;
using UniCliqueBackend.Application.Interfaces.Repositories;
using UniCliqueBackend.Application.Interfaces.Security;
using UniCliqueBackend.Application.Interfaces.Services;
using UniCliqueBackend.Domain.Entities;
using UniCliqueBackend.Domain.Enums;
using System.Security.Cryptography;

namespace UniCliqueBackend.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ITokenService _tokenService;
        private readonly IEmailService _emailService;
        private static readonly string[] _allowedProviders = new[] { "Google", "Apple", "Facebook", "Instagram" };

        public AuthService(
            IUserRepository userRepository, 
            IPasswordHasher passwordHasher, 
            ITokenService tokenService,
            IEmailService emailService)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _tokenService = tokenService;
            _emailService = emailService;
        }

        public async Task<string> RegisterAsync(RegisterRequestDto request)
        {
            var existingByEmail = await _userRepository.GetByEmailAsync(request.Email);
            if (existingByEmail != null)
            {
                if (existingByEmail.IsBanned)
                    throw new Exception("User with this email or username already exists.");
                if (existingByEmail.IsDeleted || !existingByEmail.IsEmailVerified)
                {
                    var passwordHashExisting = _passwordHasher.HashPassword(request.Password);
                    var desiredUsername = request.Username;
                    if (!string.Equals(existingByEmail.Username, desiredUsername, StringComparison.OrdinalIgnoreCase))
                    {
                        var conflict = await _userRepository.GetByUsernameAsync(desiredUsername);
                        if (conflict != null && conflict.Id != existingByEmail.Id)
                        {
                            var baseUsername = desiredUsername;
                            var usernameCandidate = baseUsername;
                            var rnd = new Random();
                            while (true)
                            {
                                var check = await _userRepository.GetByUsernameAsync(usernameCandidate);
                                if (check == null || check.Id == existingByEmail.Id) break;
                                usernameCandidate = $"{baseUsername}{rnd.Next(1000, 9999)}";
                            }
                            desiredUsername = usernameCandidate;
                        }
                    }
                    existingByEmail.FullName = request.FullName;
                    existingByEmail.Username = desiredUsername;
                    existingByEmail.PhoneNumber = request.PhoneNumber;
                    existingByEmail.BirthDate = request.BirthDate.ToUniversalTime();
                    existingByEmail.PasswordHash = passwordHashExisting;
                    existingByEmail.IsStudent = request.IsStudent;
                    existingByEmail.StudentDocumentUrl = request.IsStudent ? request.StudentDocumentUrl : null;
                    existingByEmail.StudentVerificationStatus = request.IsStudent && !string.IsNullOrWhiteSpace(request.StudentDocumentUrl)
                        ? StudentVerificationStatus.Pending
                        : StudentVerificationStatus.None;
                    existingByEmail.StudentVerifiedAt = null;
                    existingByEmail.StudentVerificationNote = null;
                    existingByEmail.IsDeleted = false;
                    existingByEmail.DeletedAt = null;
                    existingByEmail.IsActive = true;
                    await _userRepository.UpdateAsync(existingByEmail);
                    await _userRepository.RemoveActiveVerificationCodesAsync(existingByEmail.Id, VerificationPurpose.RegisterEmailVerification);
                    await SendRegisterEmailVerificationAsync(existingByEmail);
                    return existingByEmail.Id.ToString();
                }
                throw new Exception("User with this email or username already exists.");
            }

            var existingByUsername = await _userRepository.GetByUsernameAsync(request.Username);
            if (existingByUsername != null)
            {
                throw new Exception("User with this email or username already exists.");
            }

            var (phoneE164, phoneLocal) = GetTrPhoneVariants(request.PhoneNumber);
            if (await _userRepository.PhoneExistsAsync(phoneE164, phoneLocal))
            {
                throw new Exception("Phone already exists.");
            }

            var passwordHash = _passwordHasher.HashPassword(request.Password);

            var user = new User
            {
                FullName = request.FullName,
                Email = request.Email,
                Username = request.Username,
                PhoneNumber = request.PhoneNumber,
                BirthDate = request.BirthDate.ToUniversalTime(),
                PasswordHash = passwordHash,
                Role = RoleType.User,
                IsStudent = request.IsStudent,
                StudentDocumentUrl = request.IsStudent ? request.StudentDocumentUrl : null,
                StudentVerificationStatus = request.IsStudent && !string.IsNullOrWhiteSpace(request.StudentDocumentUrl)
                    ? StudentVerificationStatus.Pending
                    : StudentVerificationStatus.None,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            if (request.AcceptKvkk)
            {
                user.UserConsents.Add(new UserConsent { ConsentType = ConsentType.KVKK, IsAccepted = true, AcceptedAt = DateTime.UtcNow });
            }
            if (request.AcceptTerms)
            {
                user.UserConsents.Add(new UserConsent { ConsentType = ConsentType.TermsOfService, IsAccepted = true, AcceptedAt = DateTime.UtcNow });
            }
            if (request.AcceptPrivacy)
            {
                user.UserConsents.Add(new UserConsent { ConsentType = ConsentType.PrivacyPolicy, IsAccepted = true, AcceptedAt = DateTime.UtcNow });
            }

            await _userRepository.AddAsync(user);
            await SendRegisterEmailVerificationAsync(user);
            return user.Id.ToString();
        }

        public async Task<TokenResponseDto> LoginAsync(LoginRequestDto request)
        {
            User? user = null;

            if (request.EmailOrUsername.Contains("@"))
            {
                user = await _userRepository.GetByEmailAsync(request.EmailOrUsername);
            }
            else
            {
                user = await _userRepository.GetByUsernameAsync(request.EmailOrUsername);
            }

            if (user == null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            {
                throw new Exception("Invalid credentials.");
            }

            if (!user.IsActive || user.IsBanned)
            {
                throw new Exception("Account is not active.");
            }

            if (!user.IsEmailVerified)
            {
                throw new Exception("Email not verified.");
            }

            user.LastLoginAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);
            return await GenerateTokensForUser(user);
        }

        public async Task<TokenResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request)
        {
            var hash = _tokenService.HashRefreshToken(request.RefreshToken);
            var token = await _userRepository.GetRefreshTokenAsync(hash);
            if (token == null)
                throw new Exception("Invalid refresh token.");
            if (token.IsRevoked)
                throw new Exception("Refresh token revoked.");
            if (token.ExpiresAt <= DateTime.UtcNow)
                throw new Exception("Refresh token expired.");

            var user = await _userRepository.GetByIdAsync(token.UserId);
            if (user == null)
                throw new Exception("User not found.");

            var result = await GenerateTokensForUser(user);
            var newHash = _tokenService.HashRefreshToken(result.RefreshToken);
            await _userRepository.RevokeRefreshTokenAsync(token, newHash);
            return result;
        }

        public async Task LogoutByRefreshAsync(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken)) return;
            var hash = _tokenService.HashRefreshToken(refreshToken);
            var token = await _userRepository.GetRefreshTokenAsync(hash);
            if (token != null && !token.IsRevoked)
            {
                await _userRepository.RevokeRefreshTokenAsync(token, null);
            }
        }
        public async Task<TokenResponseDto> ExternalLoginAsync(ExternalLoginRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Provider) || string.IsNullOrWhiteSpace(request.ProviderUserId))
                throw new Exception("Invalid external login request.");

            var provider = _allowedProviders.FirstOrDefault(p => string.Equals(p, request.Provider, StringComparison.OrdinalIgnoreCase));
            if (provider == null)
                throw new Exception("Unsupported provider.");

            var user = await _userRepository.GetByExternalLoginAsync(provider, request.ProviderUserId);
            if (user == null)
            {
                if (string.IsNullOrWhiteSpace(request.Email))
                    throw new Exception("Email is required for first-time external login.");

                user = await _userRepository.GetByEmailAsync(request.Email);
                if (user == null)
                {
                    var baseUsername = request.Email.Split('@')[0];
                    var usernameCandidate = baseUsername;
                    var rnd = new Random();
                    while (await _userRepository.GetByUsernameAsync(usernameCandidate) != null)
                    {
                        usernameCandidate = $"{baseUsername}{rnd.Next(1000, 9999)}";
                    }

                    var randomPassword = Guid.NewGuid().ToString("N");
                    var passwordHash = _passwordHasher.HashPassword(randomPassword);

                    user = new User
                    {
                        FullName = string.IsNullOrWhiteSpace(request.FullName) ? baseUsername : request.FullName,
                        Email = request.Email,
                        Username = usernameCandidate,
                        PhoneNumber = $"+000{RandomNumberGenerator.GetInt32(100000000, 999999999)}",
                        BirthDate = DateTime.UtcNow.AddYears(-18),
                        PasswordHash = passwordHash,
                        Role = RoleType.User,
                        IsActive = true,
                        IsEmailVerified = true,
                        CreatedAt = DateTime.UtcNow,
                        LastLoginAt = DateTime.UtcNow
                    };

                    await _userRepository.AddAsync(user);
                }

                var external = new UserExternalLogin
                {
                    Provider = provider,
                    ProviderUserId = request.ProviderUserId,
                    ProviderEmail = request.Email
                };
                await _userRepository.AddExternalLoginAsync(user.Id, external);
            }

            user.LastLoginAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            return await GenerateTokensForUser(user);
        }

        

        public async Task<TokenResponseDto> VerifyEmailByUserIdAsync(VerifyEmailByUserIdRequestDto request)
        {
            if (!Guid.TryParse(request.UserId, out var userId))
                throw new Exception("User not found.");
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new Exception("User not found.");
            if (user.IsEmailVerified)
                throw new Exception("Email already verified.");

            var codeEntity = await _userRepository.GetLatestVerificationCodeAsync(user.Id, VerificationPurpose.RegisterEmailVerification);
            if (codeEntity == null)
                throw new Exception("Verification code not found.");
            if (codeEntity.ExpiresAt <= DateTime.UtcNow)
                throw new Exception("Verification code expired.");

            codeEntity.AttemptCount += 1;
            codeEntity.LastAttemptAt = DateTime.UtcNow;

            var ok = _passwordHasher.Verify(request.Code, codeEntity.CodeHash);
            if (!ok)
            {
                await _userRepository.UpdateVerificationCodeAsync(codeEntity);
                throw new Exception("Invalid verification code.");
            }

            codeEntity.IsUsed = true;
            codeEntity.UsedAt = DateTime.UtcNow;
            await _userRepository.UpdateVerificationCodeAsync(codeEntity);

            user.IsEmailVerified = true;
            user.EmailVerifiedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            await _userRepository.RemoveActiveVerificationCodesAsync(user.Id, VerificationPurpose.RegisterEmailVerification);

            return await GenerateTokensForUser(user);
        }

        public async Task ResendRegisterEmailVerificationAsync(ResendEmailVerificationRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
                throw new Exception("Email is required.");

            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null)
            {
                return;
            }
            if (user.IsEmailVerified)
            {
                throw new Exception("Email already verified.");
            }

            var last = await _userRepository.GetLatestVerificationCodeAsync(user.Id, VerificationPurpose.RegisterEmailVerification);
            if (last != null && last.SentAt.AddSeconds(60) > DateTime.UtcNow)
            {
                throw new Exception("Verification code sent.");
            }

            await SendRegisterEmailVerificationAsync(user);
        }

        private async Task<TokenResponseDto> GenerateTokensForUser(User user)
        {
            var accessTokenExpires = DateTime.UtcNow.AddMinutes(15);
            var refreshTokenExpires = DateTime.UtcNow.AddDays(7);

            var accessToken = _tokenService.GenerateAccessToken(user, accessTokenExpires);
            var refreshToken = _tokenService.GenerateRefreshToken();
            var refreshTokenHash = _tokenService.HashRefreshToken(refreshToken);

            var userRefreshToken = new UserRefreshToken
            {
                TokenHash = refreshTokenHash,
                ExpiresAt = refreshTokenExpires,
                IpAddress = "::1", // Placeholder
                CreatedAt = DateTime.UtcNow,
                UserId = user.Id
            };

            await _userRepository.AddRefreshTokenAsync(user.Id, userRefreshToken);

            return new TokenResponseDto
            {
                AccessToken = accessToken,
                AccessTokenExpiresAt = accessTokenExpires,
                RefreshToken = refreshToken,
                RefreshTokenExpiresAt = refreshTokenExpires
            };
        }

        private async Task SendRegisterEmailVerificationAsync(User user)
        {
            await _userRepository.RemoveActiveVerificationCodesAsync(user.Id, VerificationPurpose.RegisterEmailVerification);

            var code = RandomNumberGenerator.GetInt32(100000, 1000000);
            var codeStr = code.ToString("D6");
            var codeHash = _passwordHasher.HashPassword(codeStr);

            var verification = new UserVerificationCode
            {
                Purpose = VerificationPurpose.RegisterEmailVerification,
                CodeHash = codeHash,
                ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                SentTo = user.Email,
                UserId = user.Id,
                SentAt = DateTime.UtcNow
            };

            await _userRepository.AddVerificationCodeAsync(user.Id, verification);
            await _emailService.SendAsync(user.Email, "E-posta Doğrulama Kodu", $"Doğrulama kodunuz: {codeStr}");
        }

        private static (string e164, string local) GetTrPhoneVariants(string phone)
        {
            phone = (phone ?? "").Trim();
            var digits = new string(phone.Where(char.IsDigit).ToArray());
            var e164Match = System.Text.RegularExpressions.Regex.Match(phone, @"^\+90(5\d{9})$");
            if (e164Match.Success)
            {
                var ten = e164Match.Groups[1].Value; // 5xxxxxxxxx
                var e164 = $"+90{ten}";
                var local = $"0{ten}";
                return (e164, local);
            }
            // 05XXXXXXXXX
            var localMatch = System.Text.RegularExpressions.Regex.Match(phone, @"^0(5\d{9})$");
            if (localMatch.Success)
            {
                var ten = localMatch.Groups[1].Value; // 5xxxxxxxxx
                var local = $"0{ten}";
                var e164 = $"+90{ten}";
                return (e164, local);
            }
            // fallback: treat input as both
            if (phone.StartsWith("+"))
            {
                return (phone, phone);
            }
            return (phone, phone);
        }

        public async Task<string> ForgotPasswordStartAsync(ForgotPasswordStartRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
                throw new Exception("Email is required.");

            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null)
                throw new Exception("User not found.");

            var last = await _userRepository.GetLatestVerificationCodeAsync(user.Id, VerificationPurpose.ForgotPassword);
            if (last != null && last.SentAt.AddSeconds(60) > DateTime.UtcNow)
            {
                throw new Exception("Verification code sent.");
            }

            await _userRepository.RemoveActiveVerificationCodesAsync(user.Id, VerificationPurpose.ForgotPassword);

            var code = RandomNumberGenerator.GetInt32(100000, 1000000);
            var codeStr = code.ToString("D6");
            var codeHash = _passwordHasher.HashPassword(codeStr);

            var verification = new UserVerificationCode
            {
                Purpose = VerificationPurpose.ForgotPassword,
                CodeHash = codeHash,
                ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                SentTo = user.Email,
                UserId = user.Id,
                SentAt = DateTime.UtcNow
            };

            await _userRepository.AddVerificationCodeAsync(user.Id, verification);
            await _emailService.SendAsync(user.Email, "Şifre Sıfırlama Kodu", $"Şifre sıfırlama kodunuz: {codeStr}");
            return user.Id.ToString();
        }

        

        public async Task VerifyPasswordResetCodeByUserIdAsync(VerifyResetByUserIdRequestDto request)
        {
            if (!Guid.TryParse(request.UserId, out var userId))
                throw new Exception("Verification code not found.");
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new Exception("Verification code not found.");

            var codeEntity = await _userRepository.GetLatestVerificationCodeAsync(user.Id, VerificationPurpose.ForgotPassword);
            if (codeEntity == null)
                throw new Exception("Verification code not found.");
            if (codeEntity.ExpiresAt <= DateTime.UtcNow)
                throw new Exception("Verification code expired.");

            codeEntity.AttemptCount += 1;
            codeEntity.LastAttemptAt = DateTime.UtcNow;

            var ok = _passwordHasher.Verify(request.Code, codeEntity.CodeHash);
            if (!ok)
            {
                await _userRepository.UpdateVerificationCodeAsync(codeEntity);
                throw new Exception("Invalid verification code.");
            }
        }

        

        public async Task ResetPasswordWithCodeByUserIdAsync(ResetPasswordByUserIdRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8)
                throw new Exception("Password must be at least 8 characters.");
            if (request.NewPassword != request.ConfirmNewPassword)
                throw new Exception("Passwords do not match.");

            if (!Guid.TryParse(request.UserId, out var userId))
                throw new Exception("User not found.");
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new Exception("User not found.");

            var codeEntity = await _userRepository.GetLatestVerificationCodeAsync(user.Id, VerificationPurpose.ForgotPassword);
            if (codeEntity == null)
                throw new Exception("Verification code not found.");
            if (codeEntity.ExpiresAt <= DateTime.UtcNow)
                throw new Exception("Verification code expired.");

            var ok = _passwordHasher.Verify(request.Code, codeEntity.CodeHash);
            if (!ok)
                throw new Exception("Invalid verification code.");

            codeEntity.IsUsed = true;
            codeEntity.UsedAt = DateTime.UtcNow;
            await _userRepository.UpdateVerificationCodeAsync(codeEntity);

            user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
            await _userRepository.UpdateAsync(user);

            await _userRepository.RemoveActiveVerificationCodesAsync(user.Id, VerificationPurpose.ForgotPassword);
        }

        public async Task ResetDatabaseAsync()
        {
            await _userRepository.ClearAllUserDataAsync();
        }

        public async Task<bool> DeleteUserByEmailAsync(string email)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null) return false;
            await _userRepository.HardDeleteUserByIdAsync(user.Id);
            return true;
        }

        public async Task<bool> IsUsernameAvailableAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username)) return false;
            var existing = await _userRepository.GetByUsernameAsync(username);
            return existing == null;
        }
    }
}
