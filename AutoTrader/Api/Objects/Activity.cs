public class Activity
{
    public string id { get; set; }
    public string orderType { get; set; }
    public string side { get; set; }
    public double sellQty { get; set; }
    public double buyQty { get; set; }
    public double execSellQty { get; set; }
    public double execBuyQty { get; set; }
    public string sellCurrency { get; set; }
    public string buyCurrency { get; set; }
    public string currency { get; set; }
    public double price { get; set; }
    public double triggerPrice { get; set; }
    public int pricePrecision { get; set; }
    public string market { get; set; }
    public string status { get; set; }
    public double amount { get; set; }
    public string feeCurrency { get; set; }
    public long time { get; set; }
    public string type { get; set; }
    public double feeAmount { get; set; }
    public string activityCurrency { get; set; }
}

public enum ActivityType
{
    Completed,
    Open,
    All
}