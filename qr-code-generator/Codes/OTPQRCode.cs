using System.Net.Mail;
using System.Text;
using SimpleBase;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
public class OTPQRCode : BaseQRCode
{
    [Required(ErrorMessage = "AccountName is required.")]
    public string? AccountName { get; set; }

    [Required(ErrorMessage = "Issuer is required.")]
    public string? Issuer { get; set; }

    [Required(ErrorMessage = "Secret is required.")]
    public string? Secret { get; set; }

    public OTPType OTPType { get; set; } = OTPType.TOTP;
    public Algorithm Algorithm { get; set; } = Algorithm.SHA1;

    [Range(1, int.MaxValue, ErrorMessage = "Digits must be greater than 0.")]
    public int Digits { get; set; } = 6;

    [Range(1, int.MaxValue, ErrorMessage = "Period must be greater than 0.")]
    public int Period { get; set; } = 30;

    // Only required if the type is OTPType.HOTP
    [Range(0, int.MaxValue, ErrorMessage = "Counter must be non-negative for HOTP.")]
    public int Counter { get; set; } = 0;

    private const string _qrTypeString = "otpauth://";

    public OTPQRCode(string uri) : base(uri)
    {
        if (string.IsNullOrWhiteSpace(uri))
        {
            return;
        }

        var uriObj = new Uri(uri);
        var queryParams = System.Web.HttpUtility.ParseQueryString(uriObj.Query);

        foreach (var param in queryParams.AllKeys)
        {
            var property = GetType().GetProperty(param, BindingFlags.Public | BindingFlags.Instance);
            if (property != null)
            {
                try
                {
                    var value = queryParams[param];
                    if (property.PropertyType.IsEnum)
                    {
                        if (Enum.TryParse(property.PropertyType, value, true, out var enumValue))
                        {
                            property.SetValue(this, enumValue);
                        }
                        else
                        {
                            throw new ArgumentException($"Invalid value '{value}' for enum type '{property.PropertyType.Name}'");
                        }
                    }
                    else
                    {
                        var convertedValue = Convert.ChangeType(value, property.PropertyType);
                        property.SetValue(this, convertedValue);
                    }
                }
                catch (Exception ex)
                {
                    throw new ArgumentException($"Error setting property '{param}': {ex.Message}", ex);
                }
            }
        }
    }

    public override bool ValidateInput()
    {
        var errors = new List<string>();
        var properties = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            var value = property.GetValue(this);
            var requiredAttribute = property.GetCustomAttribute<RequiredAttribute>();
            var rangeAttribute = property.GetCustomAttribute<RangeAttribute>();

            if (requiredAttribute != null && value == null)
            {
                errors.Add(requiredAttribute.ErrorMessage ?? $"{property.Name} is required.");
            }

            if (rangeAttribute != null && value is int intValue)
            {
                if (intValue < (int)rangeAttribute.Minimum || intValue > (int)rangeAttribute.Maximum)
                {
                    errors.Add(rangeAttribute.ErrorMessage ?? $"{property.Name} must be between {rangeAttribute.Minimum} and {rangeAttribute.Maximum}.");
                }
            }
        }

        if (errors.Count > 0)
        {
            throw new InvalidOperationException(string.Join(" ", errors));
        }

        return true;
    }

    public override string ToString()
    {
        if (!ValidateInput())
        {
            throw new InvalidOperationException("Invalid input");
        }

        var typeString = OTPType.ToString().ToLower();
        var issuerEncoded = Uri.EscapeDataString(Issuer);
        var accountNameEncoded = AccountNameEncode();
        var secretEncoded = SecretEncode();
        var algorithmString = Algorithm.ToString().ToUpper();

        var uriBuilder = new StringBuilder();
        uriBuilder.Append(_qrTypeString);
        uriBuilder.Append($"{typeString}/{issuerEncoded}:{accountNameEncoded}");
        uriBuilder.Append($"?secret={secretEncoded}");
        uriBuilder.Append($"&issuer={issuerEncoded}");
        uriBuilder.Append($"&algorithm={algorithmString}");
        uriBuilder.Append($"&digits={Digits}");
        uriBuilder.Append($"&period={Period}");

        if (OTPType == OTPType.HOTP)
        {
            uriBuilder.Append($"&counter={Counter}");
        }

        return uriBuilder.ToString();
    }

    public override IEnumerable<InputDefinition> GetInputDefinitions()
    {
        return new List<InputDefinition>
        {
            new()
            {
                Name = nameof(AccountName),
                Type = InputType.String,
                Placeholder = "Account Name",
                Description = "The account name associated with the OTP",
                ValidationRules = new List<ValidationRule>
                {
                    new() { Rule = "required", ErrorMessage = "AccountName is required." }
                }
            },
            new() {
                Name = nameof(Issuer),
                Type = InputType.String,
                Placeholder = "Issuer",
                Description = "The issuer of the OTP",
                ValidationRules = new List<ValidationRule>
                {
                    new() { Rule = "required", ErrorMessage = "Issuer is required." }
                }
            },
            new()
            {
                Name = nameof(Secret),
                Type = InputType.String,
                Placeholder = "Secret",
                Description = "The secret key for the OTP",
                ValidationRules = new List<ValidationRule>
                {
                    new() { Rule = "required", ErrorMessage = "Secret is required." }
                }
            },
            new()
            {
                Name = nameof(OTPType),
                Type = InputType.Dropdown,
                Placeholder = "OTPType",
                Description = "The type of OTP (TOTP or HOTP)",
                DropdownOptions = ["TOTP", "HOTP"]
            },
            new()
            {
                Name = nameof(Algorithm),
                Type = InputType.Dropdown,
                Placeholder = "Algorithm",
                Description = "The algorithm used for the OTP",
                DropdownOptions = ["SHA1", "SHA256", "SHA512" ]
            },
            new()
            {
                Name = nameof(Digits),
                Type = InputType.String,
                Placeholder = "Digits",
                Description = "The number of digits in the OTP",
                ValidationRules = new List<ValidationRule>
                {
                    new() { Rule = "range", ErrorMessage = "Digits must be greater than 0." }
                }
            },
            new()
            {
                Name = nameof(Period),
                Type = InputType.Dropdown,
                Placeholder = "Period",
                Description = "The period for the OTP",
                DropdownOptions = ["15", "30", "60" ]
            },
            new()
            {
                Name = nameof(Counter),
                Type = InputType.String,
                Placeholder = "Counter",
                Description = "The counter for HOTP",
                ValidationRules = new List<ValidationRule>
                {
                    new() { Rule = "range", ErrorMessage = "Counter must be non-negative for HOTP." }
                }
            }
        };
    }

    private string AccountNameEncode()
    {
        if (string.IsNullOrWhiteSpace(AccountName))
        {
            AccountName = string.Empty;
            return AccountName;
        }

        if (IsValidEmail(AccountName))
        {
            return AccountName;
        }
        else
        {
            return Uri.EscapeDataString(AccountName);
        }
    }

    private static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        try
        {
            var mailAddress = new MailAddress(email);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private string SecretEncode()
    {
        try
        {
            // Try to decode the secret to check if it is already BASE32 encoded
            Base32.Rfc4648.Decode(Secret);
            return Secret; // If decoding succeeds, return the original secret
        }
        catch
        {
            // If decoding fails, encode the secret in BASE32
            var secretBytes = Encoding.UTF8.GetBytes(Secret);
            return Base32.Rfc4648.Encode(secretBytes);
        }
    }
}

public enum OTPType { TOTP, HOTP };

public enum Algorithm { SHA1, SHA256, SHA512 };