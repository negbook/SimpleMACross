using System;
using TradingPlatform.BusinessLayer;

namespace SimpleMACross
{
    /// <summary>
    /// 訂單處理輔助類
    /// 提供統一的訂單處理方法
    /// </summary>
    public static class OrderHelper
    {
        /// <summary>
        /// 創建市價單請求參數
        /// </summary>
        public static PlaceOrderRequestParameters CreateMarketOrderRequest(
            Account account,
            Symbol symbol,
            string orderTypeId,
            double quantity,
            Side side,
            string sendingSource,
            SlTpHolder stopLoss = null,
            SlTpHolder takeProfit = null)
        {
            return new PlaceOrderRequestParameters
            {
                Account = account,
                Symbol = symbol,
                OrderTypeId = orderTypeId,
                Quantity = quantity,
                Side = side,
                SendingSource = sendingSource,
                StopLoss = stopLoss,
                TakeProfit = takeProfit
            };
        }

        /// <summary>
        /// 創建限價單請求參數
        /// </summary>
        public static PlaceOrderRequestParameters CreateLimitOrderRequest(
            Account account,
            Symbol symbol,
            string orderTypeId,
            double quantity,
            Side side,
            double limitPrice,
            string sendingSource,
            SlTpHolder stopLoss = null,
            SlTpHolder takeProfit = null)
        {
            var request = CreateMarketOrderRequest(account, symbol, orderTypeId, quantity, side, sendingSource, stopLoss, takeProfit);
            request.Price = limitPrice;
            return request;
        }

        /// <summary>
        /// 創建停損單請求參數
        /// </summary>
        public static PlaceOrderRequestParameters CreateStopOrderRequest(
            Account account,
            Symbol symbol,
            string orderTypeId,
            double quantity,
            Side side,
            double triggerPrice,
            string sendingSource,
            SlTpHolder stopLoss = null,
            SlTpHolder takeProfit = null)
        {
            var request = CreateMarketOrderRequest(account, symbol, orderTypeId, quantity, side, sendingSource, stopLoss, takeProfit);
            request.TriggerPrice = triggerPrice;
            return request;
        }

        /// <summary>
        /// 創建止損設置
        /// </summary>
        public static SlTpHolder CreateStopLoss(Symbol symbol, double entryPrice, double stopPrice)
        {
            return SlTpHolder.CreateSL(
                Math.Abs(symbol.CalculateTicks(entryPrice, stopPrice)), 
                PriceMeasurement.Offset);
        }

        /// <summary>
        /// 創建止盈設置
        /// </summary>
        public static SlTpHolder CreateTakeProfit(Symbol symbol, double entryPrice, double takeProfitPrice)
        {
            return SlTpHolder.CreateTP(
                Math.Abs(symbol.CalculateTicks(entryPrice, takeProfitPrice)), 
                PriceMeasurement.Offset);
        }

        /// <summary>
        /// 生成發送源標識
        /// </summary>
        public static string GenerateSendingSource(int strategyId, string symbolName)
        {
            return $"MA_Cross_{strategyId}_{symbolName ?? "Unknown"}";
        }

        /// <summary>
        /// 格式化價格到正確的 Tick Size
        /// </summary>
        public static double FormatPrice(Symbol symbol, double price)
        {
            if (symbol == null)
                throw new ArgumentNullException(nameof(symbol));

            double roundedPrice = symbol.RoundPriceToTickSize(price, symbol.TickSize);
            return double.TryParse(symbol.FormatPrice(roundedPrice), out double p) ? 
                   symbol.RoundPriceToTickSize(price, symbol.TickSize) : 
                   roundedPrice;
        }
    }
} 