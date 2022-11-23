using TrueClient;

var authenticatorClient = new OmsAuthenticatorClient(
    new Uri("http://omsauthenticator.westeurope.cloudapp.azure.com:5000"),
    "j4g5w0"); // True API token provider configuration

// Demo:       https://markirovka.sandbox.crptech.ru
// Production: https://markirovka.crpt.ru
var trueApiClient = new TrueApiClient(new Uri("https://markirovka.sandbox.crptech.ru/"));

var tokenResult = await authenticatorClient.GetTrueApiToken();

var caseResult = await tokenResult.ConvertAsync(async token =>
    await trueApiClient.GetCodesInformation(token,
        "04016001412434VF50xQX",
        "040160014124347TiKS29",
        "04016001412434cyriYhH",
        "04016001412434D1qUbyY",
        "010463633245553721iq4x535",
        "010463633245553721kM1RrYs",
        "010463633245553721f0xzPx3",
        "010463633245553721yhD9rlz",
        "010463633245553721oGxqDBh",
        "010463633245553721YFDGNdx",
        "010463633245553721XHucJgn",
        "010463633245553721CyC6Rck",
        "011401600141245521200NK4DLFMV6240RU-TOBACCO-001"));

caseResult.Match(
    statuses =>
    {
        foreach (var status in statuses) Console.WriteLine(status);
    },
    errors =>
    {
        foreach (var error in errors) Console.WriteLine(error);
    });
