namespace API_DigiBook.Services.Discount
{
    /// <summary>
    /// Base interface for price calculation (Component in Decorator Pattern)
    /// </summary>
    public interface IPriceCalculator
    {
        double Calculate();
        string GetDescription();
    }
}
