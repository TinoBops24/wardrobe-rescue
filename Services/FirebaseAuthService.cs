using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace INF4027W_BPTTIN002_MiniPrj_2026.Services
{
    /// <summary>
    /// Thin wrapper around the Firebase Auth REST API.
    /// Handles sign-in and sign-up only — no tokens are stored server-side.
    /// The Firebase UID returned is used once to look up / create the UserProfile,
    /// then discarded. The session is written with the stable custom User ID instead.
    /// </summary>
    public class FirebaseAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ILogger<FirebaseAuthService> _logger;

        private const string SignInUrl = "https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={0}";
        private const string SignUpUrl = "https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={0}";

        public FirebaseAuthService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<FirebaseAuthService> logger)
        {
            _httpClient = httpClient;
            _apiKey = configuration["Firebase:ApiKey"]
                          ?? throw new InvalidOperationException("Firebase:ApiKey is missing from configuration.");
            _logger = logger;
        }

        // Public API 

        /// <summary>
        /// Validates email and password against Firebase Auth.
        /// Returns the Firebase UID on success, an error message on failure.
        /// </summary>
        public async Task<FirebaseAuthResult> SignInAsync(string email, string password)
        {
            var payload = new { email, password, returnSecureToken = true };
            return await PostAsync(string.Format(SignInUrl, _apiKey), payload);
        }

        /// <summary>
        /// Creates a new Firebase Auth account.
        /// Returns the new Firebase UID on success, an error message on failure.
        /// </summary>
        public async Task<FirebaseAuthResult> SignUpAsync(string email, string password)
        {
            var payload = new { email, password, returnSecureToken = true };
            return await PostAsync(string.Format(SignUpUrl, _apiKey), payload);
        }

        //  Private helper

        private async Task<FirebaseAuthResult> PostAsync(string url, object payload)
        {
            try
            {
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, content);
                var body = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var data = JsonSerializer.Deserialize<FirebaseSuccessResponse>(body);
                    return FirebaseAuthResult.Success(data!.LocalId);
                }

                // Map Firebase error codes to user-friendly messages
                var error = JsonSerializer.Deserialize<FirebaseErrorWrapper>(body);
                var message = MapErrorMessage(error?.Error?.Message);

                return FirebaseAuthResult.Failure(message);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error contacting Firebase Auth");
                return FirebaseAuthResult.Failure("A connection error occurred. Please check your internet connection and try again.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during Firebase Auth call");
                return FirebaseAuthResult.Failure("An unexpected error occurred. Please try again.");
            }
        }

        private static string MapErrorMessage(string? firebaseCode) => firebaseCode switch
        {
            "EMAIL_NOT_FOUND" => "No account found with that email address.",
            "INVALID_PASSWORD" => "Incorrect password. Please try again.",
            "INVALID_LOGIN_CREDENTIALS" => "Incorrect email or password.",
            "USER_DISABLED" => "This account has been disabled. Please contact support.",
            "EMAIL_EXISTS" => "An account with this email address already exists.",
            "WEAK_PASSWORD : Password should be at least 6 characters" => "Password must be at least 6 characters.",
            "TOO_MANY_ATTEMPTS_TRY_LATER" => "Too many failed attempts. Please try again later.",
            "OPERATION_NOT_ALLOWED" => "Email/password sign-in is not enabled. Please contact support.",
            _ => "Authentication failed. Please try again."
        };
    }

    // Result type 

    public class FirebaseAuthResult
    {
        public bool Succeeded { get; private set; }
        public string? FirebaseUid { get; private set; }
        public string? ErrorMessage { get; private set; }

        public static FirebaseAuthResult Success(string uid) => new() { Succeeded = true, FirebaseUid = uid };
        public static FirebaseAuthResult Failure(string message) => new() { Succeeded = false, ErrorMessage = message };
    }

    // Internal deserialization types 

    internal class FirebaseSuccessResponse
    {
        [JsonPropertyName("localId")]
        public string LocalId { get; set; } = string.Empty;
    }

    internal class FirebaseErrorWrapper
    {
        [JsonPropertyName("error")]
        public FirebaseErrorDetail? Error { get; set; }
    }

    internal class FirebaseErrorDetail
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }
}