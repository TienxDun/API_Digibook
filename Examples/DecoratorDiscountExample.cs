using API_DigiBook.Services.Discount;

namespace API_DigiBook.Examples
{
    /// <summary>
    /// Examples demonstrating Decorator Pattern for Discount System
    /// </summary>
    public class DecoratorDiscountExample
    {
        /// <summary>
        /// Example 1: Simple percentage discount
        /// </summary>
        public static void Example1_SimpleDiscount()
        {
            Console.WriteLine("=== Example 1: Simple Percentage Discount ===\n");

            IPriceCalculator calculator = new BasePriceCalculator(100000, "Programming Book");
            calculator = new PercentageDiscountDecorator(calculator, 15, "Weekend Sale");

            Console.WriteLine(calculator.GetDescription());
            Console.WriteLine($"\nFinal Price: {calculator.Calculate():N0} VND");
            Console.WriteLine($"You Save: {100000 - calculator.Calculate():N0} VND\n");
        }

        /// <summary>
        /// Example 2: Stacking multiple discounts
        /// </summary>
        public static void Example2_StackingDiscounts()
        {
            Console.WriteLine("=== Example 2: Stacking Multiple Discounts ===\n");

            IPriceCalculator calculator = new BasePriceCalculator(200000, "Laptop Mouse");

            // Add member discount
            calculator = new MembershipDiscountDecorator(calculator, "GOLD");

            // Add seasonal sale
            calculator = new PercentageDiscountDecorator(calculator, 20, "Black Friday");

            // Add coupon
            calculator = new CouponDiscountDecorator(calculator, "SAVE10", 10, true);

            Console.WriteLine(calculator.GetDescription());
            Console.WriteLine($"\nFinal Price: {calculator.Calculate():N0} VND");
            Console.WriteLine($"Total Savings: {200000 - calculator.Calculate():N0} VND");
            Console.WriteLine($"Discount: {((200000 - calculator.Calculate()) / 200000 * 100):N2}%\n");
        }

        /// <summary>
        /// Example 3: Bulk purchase discount
        /// </summary>
        public static void Example3_BulkPurchase()
        {
            Console.WriteLine("=== Example 3: Bulk Purchase Discount ===\n");

            var quantities = new[] { 1, 3, 5, 10, 20 };

            foreach (var qty in quantities)
            {
                IPriceCalculator calculator = new BasePriceCalculator(50000, "Book");
                calculator = new BulkPurchaseDiscountDecorator(calculator, qty);

                var finalPrice = calculator.Calculate();
                var discount = 50000 - finalPrice;

                Console.WriteLine($"Quantity: {qty,2} | " +
                                $"Base: {50000:N0} VND | " +
                                $"Final: {finalPrice:N0} VND | " +
                                $"Saved: {discount:N0} VND");
            }
            Console.WriteLine();
        }

        /// <summary>
        /// Example 4: Seasonal discount with date range
        /// </summary>
        public static void Example4_SeasonalDiscount()
        {
            Console.WriteLine("=== Example 4: Seasonal Discount ===\n");

            var startDate = DateTime.Now;
            var endDate = DateTime.Now.AddDays(7);

            IPriceCalculator calculator = new BasePriceCalculator(300000, "Smart Watch");
            calculator = new SeasonalDiscountDecorator(
                calculator,
                "New Year Sale",
                25,
                startDate,
                endDate
            );

            Console.WriteLine(calculator.GetDescription());
            Console.WriteLine($"\nSale Period: {startDate:dd/MM/yyyy} - {endDate:dd/MM/yyyy}");
            Console.WriteLine($"Final Price: {calculator.Calculate():N0} VND\n");
        }

        /// <summary>
        /// Example 5: Complex e-commerce scenario
        /// </summary>
        public static void Example5_ComplexScenario()
        {
            Console.WriteLine("=== Example 5: Complex E-commerce Scenario ===\n");
            Console.WriteLine("Customer: PLATINUM member buying 8 books during Black Friday with coupon\n");

            var basePrice = 150000;
            var quantity = 8;

            IPriceCalculator calculator = new BasePriceCalculator(basePrice, "Premium Programming Book");

            // 1. Black Friday Sale: 30% off
            calculator = new PercentageDiscountDecorator(calculator, 30, "Black Friday Mega Sale");

            // 2. PLATINUM membership: 20% off
            calculator = new MembershipDiscountDecorator(calculator, "PLATINUM");

            // 3. Bulk purchase (8 items): 10% off
            calculator = new BulkPurchaseDiscountDecorator(calculator, quantity);

            // 4. Apply coupon: 15% off
            calculator = new CouponDiscountDecorator(calculator, "WELCOME15", 15, true);

            // 5. Fixed store credit: -10,000 VND
            calculator = new FixedAmountDiscountDecorator(calculator, 10000, "Store Credit");

            Console.WriteLine(calculator.GetDescription());
            Console.WriteLine($"\n{'=',-60}");
            Console.WriteLine($"Original Price: {basePrice:N0} VND");
            Console.WriteLine($"Final Price:    {calculator.Calculate():N0} VND");
            Console.WriteLine($"Total Saved:    {basePrice - calculator.Calculate():N0} VND");
            Console.WriteLine($"Discount Rate:  {((basePrice - calculator.Calculate()) / basePrice * 100):N2}%");
            Console.WriteLine($"{'=',-60}\n");
        }

        /// <summary>
        /// Example 6: Membership tier comparison
        /// </summary>
        public static void Example6_MembershipComparison()
        {
            Console.WriteLine("=== Example 6: Membership Tier Comparison ===\n");

            var basePrice = 100000;
            var tiers = new[] { "BRONZE", "SILVER", "GOLD", "PLATINUM" };

            Console.WriteLine($"Base Price: {basePrice:N0} VND\n");
            Console.WriteLine($"{"Tier",-10} | {"Discount",-10} | {"Final Price",-15} | {"Savings",-10}");
            Console.WriteLine(new string('-', 55));

            foreach (var tier in tiers)
            {
                IPriceCalculator calculator = new BasePriceCalculator(basePrice, "Product");
                calculator = new MembershipDiscountDecorator(calculator, tier);

                var finalPrice = calculator.Calculate();
                var savings = basePrice - finalPrice;
                var discountPercent = (savings / basePrice * 100);

                Console.WriteLine($"{tier,-10} | {discountPercent,8:N0}% | " +
                                $"{finalPrice,13:N0} VND | {savings,8:N0} VND");
            }
            Console.WriteLine();
        }

        /// <summary>
        /// Example 7: Order matters - demonstrate different orders
        /// </summary>
        public static void Example7_OrderMatters()
        {
            Console.WriteLine("=== Example 7: Order of Decorators Matters ===\n");

            var basePrice = 100000;

            // Order 1: Percentage then Fixed
            Console.WriteLine("Order 1: Base → 20% Off → -10,000 VND");
            IPriceCalculator calc1 = new BasePriceCalculator(basePrice, "Item");
            calc1 = new PercentageDiscountDecorator(calc1, 20, "20% Off");
            calc1 = new FixedAmountDiscountDecorator(calc1, 10000, "Fixed 10K Off");
            Console.WriteLine(calc1.GetDescription());
            Console.WriteLine($"Final: {calc1.Calculate():N0} VND\n");

            // Order 2: Fixed then Percentage
            Console.WriteLine("Order 2: Base → -10,000 VND → 20% Off");
            IPriceCalculator calc2 = new BasePriceCalculator(basePrice, "Item");
            calc2 = new FixedAmountDiscountDecorator(calc2, 10000, "Fixed 10K Off");
            calc2 = new PercentageDiscountDecorator(calc2, 20, "20% Off");
            Console.WriteLine(calc2.GetDescription());
            Console.WriteLine($"Final: {calc2.Calculate():N0} VND\n");

            Console.WriteLine($"Difference: {Math.Abs(calc1.Calculate() - calc2.Calculate()):N0} VND\n");
        }

        /// <summary>
        /// Run all examples
        /// </summary>
        public static void RunAllExamples()
        {
            Example1_SimpleDiscount();
            Example2_StackingDiscounts();
            Example3_BulkPurchase();
            Example4_SeasonalDiscount();
            Example5_ComplexScenario();
            Example6_MembershipComparison();
            Example7_OrderMatters();

            Console.WriteLine("\n🎉 All examples completed!");
        }
    }
}
