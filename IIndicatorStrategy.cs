using TradingPlatform.BusinessLayer;

namespace SimpleMACross
{
    /// <summary>
    /// 指標策略接口
    /// 定義了所有指標策略必須實現的方法
    /// </summary>
    public interface IIndicatorStrategy
    {
        /// <summary>
        /// 初始化指標
        /// </summary>
        /// <param name="symbol">交易品種</param>
        void InitializeIndicators(Symbol symbol);

        /// <summary>
        /// 將指標添加到歷史數據管理器
        /// </summary>
        /// <param name="hdm">歷史數據管理器</param>
        void AddIndicatorsToHistory(HistoricalData hdm);

        /// <summary>
        /// 檢查是否出現做多信號
        /// </summary>
        /// <returns>true 表示出現做多信號</returns>
        bool IsLongSignal();

        /// <summary>
        /// 檢查是否出現做空信號
        /// </summary>
        /// <returns>true 表示出現做空信號</returns>
        bool IsShortSignal();

        /// <summary>
        /// 檢查是否需要平倉
        /// </summary>
        /// <returns>true 表示需要平倉</returns>
        bool ShouldClosePosition();

        /// <summary>
        /// 釋放資源
        /// </summary>
        void Dispose();
    }
} 