namespace AutoTrader.Config
{
    public interface IConfig
    {
        string OrgId { get; }
        string ApiKey { get; }
        string ApiSecret { get; } 
        bool IsProd { get; }

        string BTC { get; }
    }
}
