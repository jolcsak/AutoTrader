namespace AutoTrader.Config
{
    public class TestConfig : IConfig
    {
        public string OrgId => "80a54153-c6a5-422d-84cc-007453c62700";

        public string ApiKey => "35b973da-2210-4a21-82e2-43dcc555cf12";

        public string ApiSecret => "69863b23-44a9-4a7f-b0d1-9caaedc5fa00a6e61f33-08a1-4616-8a0d-6baa753b827a";

        public bool IsProd => false;

        public static IConfig Instance => new TestConfig();
    }
}
