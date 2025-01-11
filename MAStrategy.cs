using TradingPlatform.BusinessLayer;

namespace SimpleMACross
{
    /// <summary>
    /// MA交叉策略實現
    /// </summary>
    public class MAStrategy : IIndicatorStrategy
    {
        private readonly int fastPeriod;
        private readonly int slowPeriod;
        private Indicator fastMA;
        private Indicator slowMA;

        /// <summary>
        /// 構造函數
        /// </summary>
        /// <param name="fastPeriod">快線週期</param>
        /// <param name="slowPeriod">慢線週期</param>
        public MAStrategy(int fastPeriod, int slowPeriod)
        {
            this.fastPeriod = fastPeriod;
            this.slowPeriod = slowPeriod;
        }

        public void InitializeIndicators(Symbol symbol)
        {
            this.fastMA = Core.Instance.Indicators.BuiltIn.SMA(this.fastPeriod, PriceType.Close);
            this.slowMA = Core.Instance.Indicators.BuiltIn.SMA(this.slowPeriod, PriceType.Close);
        }

        public void AddIndicatorsToHistory(HistoricalData hdm)
        {
            hdm.AddIndicator(this.fastMA);
            hdm.AddIndicator(this.slowMA);
        }

        public bool IsLongSignal()
        {
            return this.fastMA.GetValue(2) < this.slowMA.GetValue(2) && 
                   this.fastMA.GetValue(1) > this.slowMA.GetValue(1);
        }

        public bool IsShortSignal()
        {
            return this.fastMA.GetValue(2) > this.slowMA.GetValue(2) && 
                   this.fastMA.GetValue(1) < this.slowMA.GetValue(1);
        }

        public bool ShouldClosePosition()
        {
            return this.fastMA.GetValue(1) < this.slowMA.GetValue(1) || 
                   this.fastMA.GetValue(1) > this.slowMA.GetValue(1);
        }

        public void Dispose()
        {
            this.fastMA?.Dispose();
            this.slowMA?.Dispose();
        }
    }
} 