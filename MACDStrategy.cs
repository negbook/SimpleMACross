using TradingPlatform.BusinessLayer;

namespace SimpleMACross
{
    /// <summary>
    /// MACD策略實現
    /// 使用MACD柱狀圖和信號線交叉產生交易信號
    /// </summary>
    public class MACDStrategy : IIndicatorStrategy
    {
        private readonly int fastPeriod;
        private readonly int slowPeriod;
        private readonly int signalPeriod;
        private Indicator macd;

        /// <summary>
        /// 構造函數
        /// </summary>
        /// <param name="fastPeriod">快線週期</param>
        /// <param name="slowPeriod">慢線週期</param>
        /// <param name="signalPeriod">信號線週期</param>
        public MACDStrategy(int fastPeriod = 12, int slowPeriod = 26, int signalPeriod = 9)
        {
            this.fastPeriod = fastPeriod;
            this.slowPeriod = slowPeriod;
            this.signalPeriod = signalPeriod;
        }

        public void InitializeIndicators(Symbol symbol)
        {
            this.macd = Core.Instance.Indicators.BuiltIn.MACD(
                this.fastPeriod,
                this.slowPeriod,
                this.signalPeriod,
                IndicatorCalculationType.AllAvailableData);
        }

        public void AddIndicatorsToHistory(HistoricalData hdm)
        {
            hdm.AddIndicator(this.macd);
        }

        public bool IsLongSignal()
        {
            // MACD柱狀圖由負轉正
            return this.macd.GetValue(2, 2) < 0 && // 前一個柱狀圖為負
                   this.macd.GetValue(1, 2) > 0;   // 當前柱狀圖為正
        }

        public bool IsShortSignal()
        {
            // MACD柱狀圖由正轉負
            return this.macd.GetValue(2, 2) > 0 && // 前一個柱狀圖為正
                   this.macd.GetValue(1, 2) < 0;   // 當前柱狀圖為負
        }

        public bool ShouldClosePosition()
        {
            // 當MACD柱狀圖穿越零軸時平倉
            double currentHistogram = this.macd.GetValue(1, 2);
            double previousHistogram = this.macd.GetValue(2, 2);
            return (currentHistogram > 0 && previousHistogram < 0) || 
                   (currentHistogram < 0 && previousHistogram > 0);
        }

        public void Dispose()
        {
            this.macd?.Dispose();
        }
    }
} 