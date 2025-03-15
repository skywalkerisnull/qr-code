using System.Text;

public abstract class BaseQRCode
{
    public string Name { get; set; }
    public Dictionary<string, string> Parameters { get; private set; }

    private const string _qrTypeString = "";

    public BaseQRCode(string uri)
    {
        Parameters = ParseUri(uri);
        if (Parameters.ContainsKey("name"))
        {
            Name = Parameters["name"];
        }
    }

    private Dictionary<string, string> ParseUri(string uri)
    {
        var parameters = new Dictionary<string, string>();
        var uriParts = uri.Split('?');
        if (uriParts.Length > 1)
        {
            var queryParams = uriParts[1].Split('&');
            foreach (var param in queryParams)
            {
                var keyValue = param.Split('=');
                if (keyValue.Length == 2)
                {
                    parameters[keyValue[0]] = keyValue[1];
                }
            }
        }
        return parameters;
    }

    public abstract bool ValidateInput();

    public abstract IEnumerable<InputDefinition> GetInputDefinitions();

    public override string ToString()
    {
        StringBuilder uriBuilder = new StringBuilder();
        uriBuilder.Append(_qrTypeString);
        uriBuilder.Append(string.Join(", ", Parameters));

        return uriBuilder.ToString();
    }
}

public enum QRCodeType
{
    OTP,
    WiFi,
    Email,
    Phone,
    SMS,
    URL,
    Text
}