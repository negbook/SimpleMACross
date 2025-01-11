// Copyright QUANTOWER LLC. © 2017-2022. All rights reserved.
// https://github.com/Quantower/Examples/blob/master/Strategies/SimpleMACross.cs

/*
指標切換指南：
1. 在構造函數中替換指標策略：
   - MA策略：this.indicatorStrategy = new MAStrategy(this.FastMA, this.SlowMA);
   - RSI策略：this.indicatorStrategy = new RSIStrategy(14, 70, 30);
   
2. 可用的指標策略：
   - MAStrategy: 移動平均線交叉策略
   - RSIStrategy: RSI超買超賣策略
   
3. 添加新策略：
   - 創建新的策略類
   - 實現 IIndicatorStrategy 接口
   - 在構造函數中使用新策略
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics.Metrics;
using TradingPlatform.BusinessLayer;
using System.Runtime.CompilerServices;

namespace SimpleMACross
{
    /// <summary>
    /// 通用指標交易策略框架
    /// 支持多種指標策略的切換和組合
    /// </summary>
    public sealed class SimpleMACross : Strategy, ICurrentAccount, ICurrentSymbol
    {
        #region Input Parameters
        /// <summary>
        /// 策略輸入參數定義區域
        /// </summary>
        [InputParameter("Symbol", 0)]
        public Symbol CurrentSymbol { get; set; }

        [InputParameter("Account", 1)]
        public Account CurrentAccount { get; set; }

        [InputParameter("Fast MA", 2, minimum: 1, maximum: 100, increment: 1, decimalPlaces: 0)]
        public int FastMA { get; set; }

        [InputParameter("Slow MA", 3, minimum: 1, maximum: 100, increment: 1, decimalPlaces: 0)]
        public int SlowMA { get; set; }

        [InputParameter("Quantity", 4, 0.1, 99999, 0.1, 2)]
        public double Quantity { get; set; }

        [InputParameter("Period", 5)]
        public Period Period { get; set; }

        [InputParameter("Start point", 6)]
        public DateTime StartPoint { get; set; }

        [InputParameter("Strategy ID", 7, minimum: 1, maximum: 99999, increment: 1, decimalPlaces: 0)]
        public int StrategyId { get; set; }
        #endregion

        #region Core Components
        /// <summary>
        /// 核心組件定義區域
        /// </summary>
        private readonly PositionManager positionManager;
        private readonly OrderExecutor orderExecutor;
        private readonly MetricsManager metricsManager;
        private readonly EventManager eventManager;
        private readonly TradingStateManager tradingStateManager;
        private readonly TradeResultManager tradeResultManager;
        private readonly IIndicatorStrategy indicatorStrategy;
        #endregion

        #region State Variables
        /// <summary>
        /// 狀態變量定義區域
        /// </summary>
        private HistoricalData hdm;
        private int longPositionsCount;
        private int shortPositionsCount;
        private string orderTypeId;
        private bool waitOpenPosition;
        private bool waitClosePositions;
        private double totalNetPl;
        private double totalGrossPl;
        private double totalFee;
        #endregion

        #region Monitoring and Connection
        /// <summary>
        /// 監控和連接相關定義
        /// </summary>
        public override string[] MonitoringConnectionsIds => new string[] 
        { 
            this.CurrentSymbol?.ConnectionId, 
            this.CurrentAccount?.ConnectionId 
        };
        #endregion

        #region Constructor
        /// <summary>
        /// 策略構造函數，初始化基本參數
        /// </summary>
        public SimpleMACross()
            : base()
        {
            this.Name = "Simple MA Cross strategy";
            this.Description = "Raw strategy without any additional functional";

            this.FastMA = 5;
            this.SlowMA = 10;
            this.Period = Period.MIN5;
            this.StartPoint = Core.TimeUtils.DateTimeUtcNow.AddDays(-100);

            // 初始化所有模組
            this.positionManager = new PositionManager(this);
            this.orderExecutor = new OrderExecutor(this);
            this.metricsManager = new MetricsManager(this);
            this.eventManager = new EventManager(this);
            this.tradingStateManager = new TradingStateManager(this);
            this.tradeResultManager = new TradeResultManager(this);
            // 使用MACD策略（參數分別是：快線週期, 慢線週期, 信號線週期）
            //this.indicatorStrategy = new MACDStrategy(12, 26, 9);
            //this.indicatorStrategy = new RSIStrategy(14, 90, 10);  // 週期, 超買水平, 超賣水平
            this.indicatorStrategy = new MAStrategy(this.FastMA, this.SlowMA);  // 新增：初始化指標策略
            
            this.StrategyId = 1;
        }
        #endregion

        #region Strategy Lifecycle Methods
        /// <summary>
        /// 策略啟動時執行的方法
        /// 負責初始化指標、訂閱事件等
        /// </summary>
        protected override void OnRun()
        {
            if (!ValidateAndInitializeBasicComponents())
                return;

            InitializeIndicators();
            InitializeHistoryData();
            SubscribeToEvents();
        }

        /// <summary>
        /// 驗證並初始化基礎組件
        /// </summary>
        private bool ValidateAndInitializeBasicComponents()
        {
            this.totalNetPl = 0D;

            // 驗證並恢復交易品種
            if (!ValidateSymbol())
                return false;

            // 驗證並恢復交易帳戶
            if (!ValidateAccount())
                return false;

            // 驗證連接ID
            if (this.CurrentSymbol.ConnectionId != this.CurrentAccount.ConnectionId)
            {
                this.Log("輸入參數錯誤... 交易品種和帳戶來自不同的連接", StrategyLoggingLevel.Error);
                return false;
            }

            // 驗證市價單支持
            if (!ValidateMarketOrderSupport())
                return false;

            return true;
        }

        /// <summary>
        /// 驗證交易品種
        /// </summary>
        private bool ValidateSymbol()
        {
            if (this.CurrentSymbol != null && this.CurrentSymbol.State == BusinessObjectState.Fake)
                this.CurrentSymbol = Core.Instance.GetSymbol(this.CurrentSymbol.CreateInfo());

            if (this.CurrentSymbol == null)
            {
                this.Log("輸入參數錯誤... 未指定交易品種", StrategyLoggingLevel.Error);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 驗證交易帳戶
        /// </summary>
        private bool ValidateAccount()
        {
            if (this.CurrentAccount != null && this.CurrentAccount.State == BusinessObjectState.Fake)
                this.CurrentAccount = Core.Instance.GetAccount(this.CurrentAccount.CreateInfo());

            if (this.CurrentAccount == null)
            {
                this.Log("輸入參數錯誤... 未指定交易帳戶", StrategyLoggingLevel.Error);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 驗證市價單支持
        /// </summary>
        private bool ValidateMarketOrderSupport()
        {
            this.orderTypeId = Core.OrderTypes.FirstOrDefault(x => 
                x.ConnectionId == this.CurrentSymbol.ConnectionId && 
                x.Behavior == OrderTypeBehavior.Market)?.Id;

            if (string.IsNullOrEmpty(this.orderTypeId))
            {
                this.Log("所選交易品種不支持市價單", StrategyLoggingLevel.Error);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 初始化技術指標
        /// </summary>
        private void InitializeIndicators()
        {
            this.indicatorStrategy.InitializeIndicators(this.CurrentSymbol);
        }

        /// <summary>
        /// 初始化歷史數據
        /// </summary>
        private void InitializeHistoryData()
        {
            this.hdm = this.CurrentSymbol.GetHistory(this.Period, this.CurrentSymbol.HistoryType, this.StartPoint);
            this.indicatorStrategy.AddIndicatorsToHistory(this.hdm);
        }

        /// <summary>
        /// 訂閱策略所需事件
        /// </summary>
        private void SubscribeToEvents()
        {
            eventManager.SubscribeAll();
        }

        /// <summary>
        /// 策略停止時執行的方法
        /// 負責清理資源、取消事件訂閱等
        /// </summary>
        protected override void OnStop()
        {
            eventManager.UnsubscribeAll();
            indicatorStrategy.Dispose();  // 新增：釋放指標資源
            base.OnStop();
        }

        /// <summary>
        /// 初始化策略監控指標
        /// </summary>
        protected override void OnInitializeMetrics(Meter meter)
        {
            base.OnInitializeMetrics(meter);
            metricsManager.InitializeAllMetrics(meter);
        }
        #endregion

        #region Event Handlers
        /// <summary>
        /// 倉位添加事件處理
        /// 更新倉位計數並處理開倉等待狀態
        /// </summary>
        private void Core_PositionAdded(Position obj)
        {
            var positions = positionManager.GetCurrentPositions();
            positionManager.UpdatePositionCounts(positions);
            
            double currentPositionsQty = positionManager.CalculateCurrentPositionQuantity(positions);

            if (Math.Abs(currentPositionsQty) == this.Quantity)
            {
                tradingStateManager.SetOpeningState(false);
            }
        }

        /// <summary>
        /// 倉位移除事件處理
        /// 更新倉位計數並處理平倉等待狀態
        /// </summary>
        private void Core_PositionRemoved(Position obj)
        {
            var positions = positionManager.GetCurrentPositions();
            positionManager.UpdatePositionCounts(positions);

            if (!positions.Any())
            {
                tradingStateManager.SetClosingState(false);
            }
        }

        /// <summary>
        /// 訂單歷史添加事件處理
        /// 處理訂單拒絕情況
        /// </summary>
        private void Core_OrdersHistoryAdded(OrderHistory obj)
        {
            if (obj.Symbol != this.CurrentSymbol || obj.Account != this.CurrentAccount)
                return;

            if (obj.Status == OrderStatus.Refused)
                this.ProcessTradingRefuse();
        }

        /// <summary>
        /// 成交事件處理
        /// 更新盈虧和手續費統計
        /// </summary>
        private void Core_TradeAdded(Trade obj)
        {
            tradeResultManager.ProcessTradeResult(obj);
        }

        /// <summary>
        /// 歷史數據更新事件處理
        /// 觸發策略更新
        /// </summary>
        private void Hdm_HistoryItemUpdated(object sender, HistoryEventArgs e) => this.OnUpdate();
        #endregion

        #region Trading Logic
        /// <summary>
        /// 策略核心邏輯
        /// 根據指標策略進行交易決策
        /// </summary>
        private void OnUpdate()
        {
            var positions = positionManager.GetCurrentPositions();

            if (!tradingStateManager.CanTrade())
                return;

            if (positions.Any())
            {
                if (indicatorStrategy.ShouldClosePosition())
                {
                    orderExecutor.ExecuteClose(positions);
                }
            }
            else
            {
                if (indicatorStrategy.IsLongSignal())
                {
                    orderExecutor.ExecuteOpen(Side.Buy);
                }
                else if (indicatorStrategy.IsShortSignal())
                {
                    orderExecutor.ExecuteOpen(Side.Sell);
                }
            }
        }

        /// <summary>
        /// 交易拒絕處理
        /// 當交易被拒絕時停止策略運行
        /// </summary>
        private void ProcessTradingRefuse()
        {
            this.Log("策略收到交易拒絕信號，將停止運行", StrategyLoggingLevel.Error);
            this.Stop();
        }
        #endregion

        #region Helper Classes
        /// <summary>
        /// 倉位管理模塊
        /// </summary>
        private class PositionManager
        {
            private readonly SimpleMACross strategy;

            public PositionManager(SimpleMACross strategy)
            {
                this.strategy = strategy;
            }

            /// <summary>
            /// 獲取當前倉位
            /// </summary>
            public Position[] GetCurrentPositions()
            {
                return Core.Instance.Positions
                    .Where(x => x.Symbol == strategy.CurrentSymbol && 
                               x.Account == strategy.CurrentAccount)
                    .ToArray();
            }

            /// <summary>
            /// 更新倉位計數
            /// </summary>
            public void UpdatePositionCounts(Position[] positions)
            {
                strategy.longPositionsCount = positions.Count(x => x.Side == Side.Buy);
                strategy.shortPositionsCount = positions.Count(x => x.Side == Side.Sell);
            }

            /// <summary>
            /// 計算當前倉位總量
            /// </summary>
            public double CalculateCurrentPositionQuantity(Position[] positions)
            {
                return positions.Sum(x => x.Side == Side.Buy ? x.Quantity : -x.Quantity);
            }
        }

        /// <summary>
        /// 訂單執行模塊
        /// </summary>
        private class OrderExecutor
        {
            private readonly SimpleMACross strategy;

            public OrderExecutor(SimpleMACross strategy)
            {
                this.strategy = strategy;
            }

            /// <summary>
            /// 生成發送源標識
            /// </summary>
            private string GenerateSource(string caller)
            {
                return $"MA_Cross_{strategy.StrategyId}_{strategy.CurrentSymbol?.Name ?? "Unknown"}_{caller ?? "Unknown"}";
            }

            /// <summary>
            /// 執行開倉
            /// </summary>
            public void ExecuteOpen(Side side, [CallerMemberName] string caller = null)
            {
                string source = GenerateSource(caller);
                strategy.tradingStateManager.SetOpeningState(true);
                strategy.Log($"[{source}] 開始開立{(side == Side.Buy ? "多" : "空")}頭倉位");

                var result = Core.Instance.PlaceOrder(new PlaceOrderRequestParameters
                {
                    Account = strategy.CurrentAccount,
                    Symbol = strategy.CurrentSymbol,
                    OrderTypeId = strategy.orderTypeId,
                    Quantity = strategy.Quantity,
                    Side = side,
                    SendingSource = source
                });

                HandleOrderResult(result, side, source);
            }

            /// <summary>
            /// 執行平倉
            /// </summary>
            public void ExecuteClose(Position[] positions, [CallerMemberName] string caller = null)
            {
                string source = GenerateSource(caller);
                strategy.tradingStateManager.SetClosingState(true);
                strategy.Log($"[{source}] 開始平倉 (倉位數量: {positions.Length})");
                
                Core.Instance.AdvancedTradingOperations.Flatten(
                    strategy.CurrentSymbol, 
                    strategy.CurrentAccount, 
                    source);
            }

            private void HandleOrderResult(TradingOperationResult result, Side side, string source)
            {
                if (result.Status == TradingOperationResultStatus.Failure)
                {
                    strategy.Log($"[{source}] 開立{(side == Side.Buy ? "多" : "空")}頭倉位被拒絕: {(string.IsNullOrEmpty(result.Message) ? result.Status : result.Message)}", 
                        StrategyLoggingLevel.Trading);
                    strategy.ProcessTradingRefuse();
                }
                else
                    strategy.Log($"[{source}] 倉位已開立: {result.Status}", StrategyLoggingLevel.Trading);
            }
        }

        /// <summary>
        /// 指標監控模塊
        /// </summary>
        private class MetricsManager
        {
            private readonly SimpleMACross strategy;

            public MetricsManager(SimpleMACross strategy)
            {
                this.strategy = strategy;
            }

            /// <summary>
            /// 初始化所有監控指標
            /// </summary>
            public void InitializeAllMetrics(Meter meter)
            {
                InitializePositionMetrics(meter);
                InitializeProfitMetrics(meter);
                InitializeCostMetrics(meter);
            }

            private void InitializePositionMetrics(Meter meter)
            {
                meter.CreateObservableCounter("total-long-positions", 
                    () => strategy.longPositionsCount, 
                    description: "多頭倉位總數",
                    unit: "{positions}");

                meter.CreateObservableCounter("total-short-positions", 
                    () => strategy.shortPositionsCount, 
                    description: "空頭倉位總數",
                    unit: "{positions}");
            }

            private void InitializeProfitMetrics(Meter meter)
            {
                meter.CreateObservableCounter("total-pl-net", 
                    () => strategy.totalNetPl, 
                    description: "淨盈虧（扣除手續費後的盈虧）",
                    unit: "currency");

                meter.CreateObservableCounter("total-pl-gross", 
                    () => strategy.totalGrossPl, 
                    description: "毛盈虧（未扣除手續費的盈虧）",
                    unit: "currency");
            }

            private void InitializeCostMetrics(Meter meter)
            {
                meter.CreateObservableCounter("total-fee", 
                    () => strategy.totalFee, 
                    description: "累計交易手續費",
                    unit: "currency");
            }
        }

        /// <summary>
        /// 事件管理模塊
        /// </summary>
        private class EventManager
        {
            private readonly SimpleMACross strategy;

            public EventManager(SimpleMACross strategy)
            {
                this.strategy = strategy;
            }

            public void SubscribeAll()
            {
                Core.PositionAdded += strategy.Core_PositionAdded;
                Core.PositionRemoved += strategy.Core_PositionRemoved;
                Core.OrdersHistoryAdded += strategy.Core_OrdersHistoryAdded;
                Core.TradeAdded += strategy.Core_TradeAdded;
                strategy.hdm.HistoryItemUpdated += strategy.Hdm_HistoryItemUpdated;
            }

            public void UnsubscribeAll()
            {
                Core.PositionAdded -= strategy.Core_PositionAdded;
                Core.PositionRemoved -= strategy.Core_PositionRemoved;
                Core.OrdersHistoryAdded -= strategy.Core_OrdersHistoryAdded;
                Core.TradeAdded -= strategy.Core_TradeAdded;
                
                if (strategy.hdm != null)
                {
                    strategy.hdm.HistoryItemUpdated -= strategy.Hdm_HistoryItemUpdated;
                    strategy.hdm.Dispose();
                }
            }
        }

        /// <summary>
        /// 交易狀態管理模塊
        /// </summary>
        private class TradingStateManager
        {
            private readonly SimpleMACross strategy;

            public TradingStateManager(SimpleMACross strategy)
            {
                this.strategy = strategy;
            }

            public void SetOpeningState(bool isWaiting)
            {
                strategy.waitOpenPosition = isWaiting;
                if (!isWaiting)
                    strategy.Log("開倉確認完成，解除等待標記", StrategyLoggingLevel.Trading);
            }

            public void SetClosingState(bool isWaiting)
            {
                strategy.waitClosePositions = isWaiting;
                if (!isWaiting)
                    strategy.Log("所有倉位平倉完成，解除等待標記", StrategyLoggingLevel.Trading);
            }

            public bool CanTrade()
            {
                if (strategy.waitOpenPosition)
                {
                    strategy.Log("正在等待開倉確認，跳過本次訊號", StrategyLoggingLevel.Trading);
                    return false;
                }

                if (strategy.waitClosePositions)
                {
                    strategy.Log("正在等待平倉完成，跳過本次訊號", StrategyLoggingLevel.Trading);
                    return false;
                }

                return true;
            }
        }

        /// <summary>
        /// 交易結果處理模塊
        /// </summary>
        private class TradeResultManager
        {
            private readonly SimpleMACross strategy;

            public TradeResultManager(SimpleMACross strategy)
            {
                this.strategy = strategy;
            }

            public void ProcessTradeResult(Trade trade)
            {
                if (trade.NetPnl != null)
                    strategy.totalNetPl += trade.NetPnl.Value;

                if (trade.GrossPnl != null)
                    strategy.totalGrossPl += trade.GrossPnl.Value;

                if (trade.Fee != null)
                    strategy.totalFee += trade.Fee.Value;
            }
        }
        #endregion
    }
}
