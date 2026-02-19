using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Utilities
{
    public static class FeeCalculator
    {
        private const decimal GST_PERCENTAGE = 18m; // 18% GST (fixed)

        public static (decimal convenienceFee, decimal gstAmount, decimal finalAmount)
            CalculateFees(decimal baseAmount, decimal convenienceFeePercentage = 6m)
        {
            // Calculate convenience fee using dynamic percentage (default 6%)
            decimal convenienceFee = Math.Round(baseAmount * (convenienceFeePercentage / 100m), 2);

            // Calculate GST on convenience fee only (18%)
            decimal gstAmount = Math.Round(convenienceFee * (GST_PERCENTAGE / 100m), 2);

            // Calculate final amount
            decimal finalAmount = Math.Round(baseAmount + convenienceFee + gstAmount, 2);

            return (convenienceFee, gstAmount, finalAmount);
        }
    }
}
