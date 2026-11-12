using InvoiceApp.Domain;
using Xunit;

namespace InvoiceApp.Tests;

public class DiscountCalculatorTests
{
    private readonly DiscountCalculator _calculator = new();

    [Theory]
    [InlineData(9, 0)]
    [InlineData(10, 0.05)]
    [InlineData(49, 0.05)]
    [InlineData(50, 0.10)]
    [InlineData(99, 0.10)]
    [InlineData(100, 0.15)]
    [InlineData(101, 0.15)]
    public void QuantityDiscount_UsesVolumeTiers(int quantity, decimal expectedDiscount)
    {
        Assert.Equal(expectedDiscount, _calculator.QuantityDiscount(quantity));
    }

    [Theory]
    [InlineData(5, 0.05)]
    [InlineData(10, 0.10)]
    [InlineData(20, 0.10)]
    public void LoyaltyDiscount_IncreasesThenCapsAtTenPercent(
        int yearsAsCustomer,
        decimal expectedDiscount)
    {
        Assert.Equal(expectedDiscount, _calculator.LoyaltyDiscount(yearsAsCustomer));
    }

    [Theory]
    [InlineData("Nonprofit", 0.15)]
    [InlineData("Commercial", 0.10)]
    public void CombinedDiscount_AddsQuantityLoyaltyAndNonprofitDiscounts(
        string customerType,
        decimal expectedDiscount)
    {
        Assert.Equal(expectedDiscount, _calculator.CombinedDiscount(10, 5, customerType));
    }

    [Theory]
    [InlineData(100, 0.15, 85)]
    [InlineData(10, 0.333, 6.67)]
    public void ApplyDiscount_AppliesRateAndRoundsToCents(
        decimal amount,
        decimal discountRate,
        decimal expectedAmount)
    {
        Assert.Equal(expectedAmount, _calculator.ApplyDiscount(amount, discountRate));
    }
}
