using TradingPlatform.BusinessLayer;

namespace SimpleMACross
{
    /// <summary>
    /// RSI策略實現
    /// 當RSI超買超賣時產生交易信號
    /// </summary>
    public class RSIStrategy : IIndicatorStrategy
    {
        private readonly int period;
        private readonly double overboughtLevel;
        private readonly double oversoldLevel;
        private Indicator rsi;

        /// <summary>
        /// 構造函數
        /// </summary>
        /// <param name="period">RSI週期</param>
        /// <param name="overboughtLevel">超買水平</param>
        /// <param name="oversoldLevel">超賣水平</param>
        public RSIStrategy(int period = 14, double overboughtLevel = 70, double oversoldLevel = 30)
        {
            this.period = period;
            this.overboughtLevel = overboughtLevel;
            this.oversoldLevel = oversoldLevel;
        }

        public void InitializeIndicators(Symbol symbol)
        {
            this.rsi = Core.Instance.Indicators.BuiltIn.RSI(
                period: this.period,                                   // RSI週期
                priceType: PriceType.Close,                           // 使用收盤價
                rsiMode: RSIMode.Simple,                              // 簡單RSI模式
                maMode: MaMode.SMA,                                   // 使用簡單移動平均
                maperiod: 1,                                          // MA週期設為1（標準RSI）
                calculationType: IndicatorCalculationType.AllAvailableData
            );
        }

        public void AddIndicatorsToHistory(HistoricalData hdm)
        {
            hdm.AddIndicator(this.rsi);
        }

        public bool IsLongSignal()
        {
            // RSI從超賣區域向上突破
            return this.rsi.GetValue(2) < this.oversoldLevel && 
                   this.rsi.GetValue(1) > this.oversoldLevel;
        }

        public bool IsShortSignal()
        {
            // RSI從超買區域向下突破
            return this.rsi.GetValue(2) > this.overboughtLevel && 
                   this.rsi.GetValue(1) < this.overboughtLevel;
        }

        public bool ShouldClosePosition()
        {
            // 當RSI回到中性區域時平倉
            double currentRSI = this.rsi.GetValue(1);
            return currentRSI > 45 && currentRSI < 55;
        }

        public void Dispose()
        {
            this.rsi?.Dispose();
        }
    }
} 