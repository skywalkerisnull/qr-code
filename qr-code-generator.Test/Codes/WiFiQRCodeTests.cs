public class WifiQRCodeTests
{
    [Fact]
    public void ToString_ShouldReturnCorrectUri()
    {
        // Arrange
        var wifiQRCode = new WifiQRCode("")
        {
            SSID = """"foo;bar\\baz"""",
            Password = "abc",
            Security = WifiSecurity.WPA
        };

        // Act
        var result = wifiQRCode.ToString();

        // Assert
        var expected = """WIFI:S:\"foo\;bar\\baz\";T:WPA;P:abc;;""";
        Assert.Equal(expected, result);
    }
}