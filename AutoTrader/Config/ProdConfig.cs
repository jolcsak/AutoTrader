namespace AutoTrader.Config
{
    public class ProdConfig : IConfig
    {
        public string OrgId => "01ca07e3-4093-462d-96a7-a3c38835b0de";

        public string ApiKey => "79821d0e-7b67-4c67-8462-326ee3115f11";

        public string ApiSecret => "751ed34e-ba7f-4be9-9cf5-7eed239f41725822bfff-ecff-4b47-b882-3d417a653da8";

        public bool IsProd => true;

        public static IConfig Instance => new ProdConfig();
    }
}
