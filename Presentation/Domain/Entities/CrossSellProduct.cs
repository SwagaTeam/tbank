namespace Domain.Entities
{
    public class CrossSellProduct
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public FinancialSegment TargetSegment { get; set; }
        public string DeepLink { get; set; }
    }
}
