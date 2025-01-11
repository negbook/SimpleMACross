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
            string sendingSource)
        {
            return new PlaceOrderRequestParameters
            {
                Account = account,
                Symbol = symbol,
                OrderTypeId = orderTypeId,
                Quantity = quantity,
                Side = side,
                SendingSource = sendingSource
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
            string sendingSource)
        {
            var request = CreateMarketOrderRequest(account, symbol, orderTypeId, quantity, side, sendingSource);
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
            double stopPrice,
            string sendingSource)
        {
            var request = CreateMarketOrderRequest(account, symbol, orderTypeId, quantity, side, sendingSource);
            request.TriggerPrice = stopPrice;
            return request;
        }

        /// <summary>
        /// 生成發送源標識
        /// </summary>
        public static string GenerateSendingSource(int strategyId, string symbolName)
        {
            return $"MA_Cross_{strategyId}_{symbolName ?? "Unknown"}";
        }
    }
} 