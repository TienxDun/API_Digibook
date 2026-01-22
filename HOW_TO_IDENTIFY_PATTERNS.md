# 🔍 Cách Nhận Biết Design Patterns Trong API

## 📚 Tổng Quan

Tài liệu này giúp bạn **nhận biết nhanh** một API đang sử dụng pattern nào bằng cách quan sát code structure.

---

## 1. 📦 REPOSITORY PATTERN

### ✅ Dấu Hiệu Nhận Biết:

#### 🔍 Trong Controller:
```csharp
public class BooksController : ControllerBase
{
    private readonly IBookRepository _bookRepository;  // ← Inject repository interface
    
    public BooksController(IBookRepository bookRepository)  // ← Constructor injection
    {
        _bookRepository = bookRepository;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var books = await _bookRepository.GetAllAsync();  // ← Gọi repository methods
        return Ok(books);
    }
}
```

#### 🔍 Trong Repositories Folder:
```
Repositories/
├── IBookRepository.cs        ← Interface
└── BookRepository.cs         ← Implementation
```

#### 🔍 Key Indicators:
- ✅ Controller inject `IRepository` interface
- ✅ Không có Firestore/Database code trong Controller
- ✅ Methods như: `GetAllAsync()`, `GetByIdAsync()`, `AddAsync()`, `UpdateAsync()`, `DeleteAsync()`
- ✅ Repository class kế thừa từ `FirestoreRepository<T>`

### 📋 Checklist:

| Dấu hiệu | Có | Không |
|----------|-----|-------|
| Controller có inject `IXxxRepository`? | ✅ | ❌ |
| Controller gọi `_repository.MethodAsync()`? | ✅ | ❌ |
| Có folder `Repositories/`? | ✅ | ❌ |
| Có interface `IRepository<T>`? | ✅ | ❌ |

**Kết luận:** Nếu có 3/4 ✅ → **Repository Pattern**

---

## 2. ⚡ COMMAND PATTERN

### ✅ Dấu Hiệu Nhận Biết:

#### 🔍 Trong Controller:
```csharp
public class OrdersController : ControllerBase
{
    private readonly CommandInvoker _commandInvoker;  // ← Có CommandInvoker
    
    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] Order order)
    {
        // Tạo Command object
        var command = new CreateOrderCommand(order, _repository, _logger);  // ← Tạo command
        
        // Execute qua Invoker
        var result = await _commandInvoker.ExecuteAsync(command);  // ← Execute command
        
        if (result.Success)
            return Ok(result);
        
        return BadRequest(result);
    }
}
```

#### 🔍 Trong Commands Folder:
```
Commands/
├── ICommand.cs                    ← Command interface
├── CommandInvoker.cs              ← Invoker
└── Orders/
    ├── CreateOrderCommand.cs      ← Concrete commands
    ├── UpdateOrderCommand.cs
    └── CancelOrderCommand.cs
```

#### 🔍 Command Class Structure:
```csharp
public class CreateOrderCommand : ICommand<CommandResult>
{
    private readonly Order _order;
    private readonly IOrderRepository _repository;
    
    public CreateOrderCommand(Order order, IOrderRepository repository)
    {
        _order = order;
        _repository = repository;
    }
    
    public async Task<CommandResult> ExecuteAsync()  // ← ExecuteAsync method
    {
        // Business logic here
        return CommandResult.SuccessResult("Created");
    }
}
```

#### 🔍 Key Indicators:
- ✅ Controller inject `CommandInvoker`
- ✅ Controller tạo Command objects: `new XxxCommand(...)`
- ✅ Gọi `_commandInvoker.ExecuteAsync(command)`
- ✅ Return `CommandResult` với `Success`, `Message`, `Data`, `Error`
- ✅ Có folder `Commands/` với các concrete command classes
- ✅ Commands implement `ICommand<TResult>`

### 📋 Checklist:

| Dấu hiệu | Có | Không |
|----------|-----|-------|
| Controller có `CommandInvoker`? | ✅ | ❌ |
| Controller tạo Command objects? | ✅ | ❌ |
| Gọi `_commandInvoker.ExecuteAsync()`? | ✅ | ❌ |
| Có folder `Commands/`? | ✅ | ❌ |
| Commands implement `ICommand<T>`? | ✅ | ❌ |
| Return `CommandResult`? | ✅ | ❌ |

**Kết luận:** Nếu có 4/6 ✅ → **Command Pattern**

### 🎯 Tại Sao Dùng Command Pattern?
- Encapsulate operations thành objects
- Easy to add undo/redo
- Command history tracking
- Separate business logic từ controller

---

## 3. 🔐 SINGLETON PATTERN

### ✅ Dấu Hiệu Nhận Biết:

#### 🔍 Trong Controller:
```csharp
public class BooksController : ControllerBase
{
    private readonly LoggerService _logger;  // ← Singleton service
    
    public BooksController()
    {
        _logger = LoggerService.Instance;  // ← .Instance property (không inject!)
    }
    
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Book book)
    {
        await _logger.LogSuccessAsync("CREATE_BOOK", "...", "admin");  // ← Gọi singleton
        return Ok();
    }
}
```

#### 🔍 Trong Service Class:
```csharp
public sealed class LoggerService  // ← sealed class
{
    private static LoggerService? _instance;  // ← static instance
    private static readonly object _lock = new object();  // ← lock object
    
    // Private constructor
    private LoggerService()  // ← private constructor
    {
        // Initialize
    }
    
    // Public static property
    public static LoggerService Instance  // ← Static Instance property
    {
        get
        {
            if (_instance == null)  // ← Double-check locking
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new LoggerService();
                    }
                }
            }
            return _instance;
        }
    }
    
    public async Task LogAsync(...) { }  // ← Instance methods
}
```

#### 🔍 Cách Sử Dụng:
```csharp
// Lấy instance (chỉ có 1 instance duy nhất cho toàn app)
var logger = LoggerService.Instance;

// Sử dụng
await logger.LogSuccessAsync(...);
```

#### 🔍 Key Indicators:
- ✅ Class có `sealed` keyword
- ✅ Constructor là `private` (không thể `new` từ bên ngoài)
- ✅ Có `static` instance field: `private static LoggerService? _instance`
- ✅ Có `static` property: `public static LoggerService Instance`
- ✅ Có `lock` object cho thread-safety
- ✅ Double-check locking pattern trong getter
- ✅ Controller dùng `.Instance` (KHÔNG inject qua constructor)

### 📋 Checklist:

| Dấu hiệu | Có | Không |
|----------|-----|-------|
| Class có `sealed` keyword? | ✅ | ❌ |
| Constructor là `private`? | ✅ | ❌ |
| Có `static` instance field? | ✅ | ❌ |
| Có `public static Instance` property? | ✅ | ❌ |
| Controller dùng `Service.Instance`? | ✅ | ❌ |
| KHÔNG inject qua constructor? | ✅ | ❌ |

**Kết luận:** Nếu có 5/6 ✅ → **Singleton Pattern**

### 🎯 Tại Sao Dùng Singleton?
- Chỉ cần 1 instance duy nhất (logger, config)
- Global access point
- Memory efficient
- Thread-safe với locking

---

## 4. 🎁 DECORATOR PATTERN

### ✅ Dấu Hiệu Nhận Biết:

#### 🔍 Trong Controller:
```csharp
public class DiscountController : ControllerBase
{
    [HttpPost("calculate")]
    public async Task<IActionResult> Calculate([FromBody] DiscountRequest request)
    {
        // Start với base component
        IPriceCalculator calculator = new BasePriceCalculator(100000, "Book");  // ← Base
        
        // Wrap/decorate với các decorators (STACKING!)
        calculator = new PercentageDiscountDecorator(calculator, 20);  // ← Decorator 1
        calculator = new MembershipDiscountDecorator(calculator, "GOLD");  // ← Decorator 2
        calculator = new CouponDiscountDecorator(calculator, "SAVE10");  // ← Decorator 3
        
        // Calculate final result
        var finalPrice = calculator.Calculate();  // ← Gọi qua decorators
        
        return Ok(finalPrice);
    }
}
```

#### 🔍 Trong Services/Discount/ Folder:
```
Services/Discount/
├── IPriceCalculator.cs                  ← Component interface
├── BasePriceCalculator.cs               ← Concrete component
├── DiscountDecorator.cs                 ← Abstract decorator
├── PercentageDiscountDecorator.cs       ← Concrete decorator 1
├── FixedAmountDiscountDecorator.cs      ← Concrete decorator 2
├── MembershipDiscountDecorator.cs       ← Concrete decorator 3
└── CouponDiscountDecorator.cs           ← Concrete decorator 4
```

#### 🔍 Base Component:
```csharp
public interface IPriceCalculator  // ← Component interface
{
    double Calculate();
    string GetDescription();
}

public class BasePriceCalculator : IPriceCalculator  // ← Concrete component
{
    private readonly double _basePrice;
    
    public double Calculate() => _basePrice;
    public string GetDescription() => $"Base: {_basePrice}";
}
```

#### 🔍 Abstract Decorator:
```csharp
public abstract class DiscountDecorator : IPriceCalculator  // ← Abstract decorator
{
    protected readonly IPriceCalculator _priceCalculator;  // ← Wraps component
    
    protected DiscountDecorator(IPriceCalculator priceCalculator)
    {
        _priceCalculator = priceCalculator;  // ← Store reference
    }
    
    public virtual double Calculate()
    {
        return _priceCalculator.Calculate();  // ← Delegate to wrapped component
    }
}
```

#### 🔍 Concrete Decorator:
```csharp
public class PercentageDiscountDecorator : DiscountDecorator  // ← Concrete decorator
{
    private readonly double _percentage;
    
    public PercentageDiscountDecorator(
        IPriceCalculator calculator,  // ← Receives component to wrap
        double percentage) 
        : base(calculator)  // ← Pass to base
    {
        _percentage = percentage;
    }
    
    public override double Calculate()
    {
        var basePrice = _priceCalculator.Calculate();  // ← Get base value
        return basePrice - (basePrice * _percentage / 100);  // ← Add behavior
    }
}
```

#### 🔍 Key Indicators:
- ✅ Controller tạo base component: `new BaseXxx(...)`
- ✅ Controller wrap component nhiều lần: `calc = new Decorator1(calc); calc = new Decorator2(calc);`
- ✅ **STACKING**: Decorators được stack lên nhau
- ✅ Decorators nhận interface làm constructor parameter
- ✅ Decorators implement cùng interface với component
- ✅ Decorators delegate calls tới wrapped component
- ✅ Có abstract decorator class
- ✅ Có nhiều concrete decorators

### 📋 Checklist:

| Dấu hiệu | Có | Không |
|----------|-----|-------|
| Controller tạo base component? | ✅ | ❌ |
| Controller wrap nhiều lần (stacking)? | ✅ | ❌ |
| Decorators nhận interface parameter? | ✅ | ❌ |
| Decorators implement cùng interface? | ✅ | ❌ |
| Decorators delegate tới wrapped object? | ✅ | ❌ |
| Có abstract decorator class? | ✅ | ❌ |
| Có nhiều concrete decorators? | ✅ | ❌ |

**Kết luận:** Nếu có 5/7 ✅ → **Decorator Pattern**

### 🎯 Tại Sao Dùng Decorator?
- Add behavior động mà không modify code gốc
- Stack nhiều behaviors (discounts)
- Open/Closed principle
- Flexible composition

---

## 🎯 BẢNG SO SÁNH NHANH

| Pattern | Key Indicator | Code Pattern | Ở Đâu? |
|---------|---------------|--------------|---------|
| **Repository** | `private readonly IRepository` | `_repository.GetAllAsync()` | Controller inject repository |
| **Command** | `private readonly CommandInvoker` | `new XxxCommand(...)` → `_invoker.ExecuteAsync()` | Controller tạo commands |
| **Singleton** | `.Instance` | `Service.Instance.Method()` | Controller dùng .Instance |
| **Decorator** | Stacking/Wrapping | `calc = new Decorator(calc)` | Controller wrap nhiều lần |

---

## 📊 QUICK IDENTIFICATION FLOWCHART

```
Nhìn vào Controller Constructor:
│
├─ Inject IXxxRepository? 
│  └─ YES → ✅ REPOSITORY PATTERN
│
├─ Inject CommandInvoker?
│  └─ YES → ✅ COMMAND PATTERN
│
├─ Dùng Service.Instance (không inject)?
│  └─ YES → ✅ SINGLETON PATTERN
│
└─ Tạo object rồi wrap nhiều lần?
   └─ YES → ✅ DECORATOR PATTERN
```

---

## 💡 EXAMPLES - NHẬN BIẾT NHANH

### Example 1: BooksController
```csharp
public class BooksController : ControllerBase
{
    private readonly IBookRepository _bookRepository;  // ← REPOSITORY
    private readonly LoggerService _logger;
    
    public BooksController(IBookRepository bookRepository)  // ← Inject repository
    {
        _bookRepository = bookRepository;
        _logger = LoggerService.Instance;  // ← SINGLETON
    }
    
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var books = await _bookRepository.GetAllAsync();  // ← Repository method
        return Ok(books);
    }
}
```
**Patterns:** ✅ Repository + ✅ Singleton

---

### Example 2: OrdersController
```csharp
public class OrdersController : ControllerBase
{
    private readonly IOrderRepository _orderRepository;  // ← REPOSITORY
    private readonly CommandInvoker _commandInvoker;  // ← COMMAND
    
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Order order)
    {
        var command = new CreateOrderCommand(order, _repository);  // ← Tạo command
        var result = await _commandInvoker.ExecuteAsync(command);  // ← Execute
        return Ok(result);
    }
}
```
**Patterns:** ✅ Repository + ✅ Command

---

### Example 3: DiscountController
```csharp
public class DiscountController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Calculate()
    {
        IPriceCalculator calc = new BasePriceCalculator(100000);  // ← Base
        calc = new PercentageDiscountDecorator(calc, 20);  // ← Wrap 1
        calc = new MembershipDiscountDecorator(calc, "GOLD");  // ← Wrap 2
        calc = new CouponDiscountDecorator(calc, "SAVE10");  // ← Wrap 3
        
        return Ok(calc.Calculate());
    }
}
```
**Patterns:** ✅ Decorator (stacking!)

---

### Example 4: LogsController
```csharp
public class LogsController : ControllerBase
{
    private readonly LoggerService _logger;  // ← SINGLETON
    
    public LogsController()
    {
        _logger = LoggerService.Instance;  // ← .Instance (không inject!)
    }
    
    [HttpGet]
    public async Task<IActionResult> GetLogs()
    {
        var logs = await _logger.GetAllLogsAsync();  // ← Singleton method
        return Ok(logs);
    }
}
```
**Patterns:** ✅ Singleton

---

## 📝 CHEAT SHEET - GHI NHỚ NHANH

### Repository Pattern
```
Constructor: inject IRepository
Methods: _repository.GetAllAsync()
```

### Command Pattern
```
Constructor: inject CommandInvoker
Methods: new XxxCommand(...) → _invoker.ExecuteAsync()
```

### Singleton Pattern
```
Constructor: KHÔNG inject (hoặc assign .Instance)
Methods: Service.Instance.Method()
```

### Decorator Pattern
```
Method body: 
  var obj = new Base();
  obj = new Decorator1(obj);
  obj = new Decorator2(obj);
  return obj.Method();
```

---

## 🎓 PRACTICE - TỰ KIỂM TRA

### Bài 1: Nhận biết pattern
```csharp
public class ProductsController : ControllerBase
{
    private readonly IProductRepository _repository;
    
    public ProductsController(IProductRepository repository)
    {
        _repository = repository;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var products = await _repository.GetAllAsync();
        return Ok(products);
    }
}
```
**Đáp án:** Repository Pattern
**Lý do:** Inject IProductRepository, gọi _repository.GetAllAsync()

---

### Bài 2: Nhận biết pattern
```csharp
public class PaymentController : ControllerBase
{
    private readonly CommandInvoker _invoker;
    
    [HttpPost]
    public async Task<IActionResult> ProcessPayment()
    {
        var command = new ProcessPaymentCommand(...);
        var result = await _invoker.ExecuteAsync(command);
        return Ok(result);
    }
}
```
**Đáp án:** Command Pattern
**Lý do:** Inject CommandInvoker, tạo Command, gọi ExecuteAsync()

---

### Bài 3: Nhận biết pattern
```csharp
public class AuditController : ControllerBase
{
    private readonly AuditService _audit;
    
    public AuditController()
    {
        _audit = AuditService.Instance;
    }
}
```
**Đáp án:** Singleton Pattern
**Lý do:** Dùng .Instance, không inject qua constructor

---

### Bài 4: Nhận biết pattern
```csharp
[HttpPost]
public IActionResult CalculateShipping()
{
    IShippingCalculator calc = new BaseShippingCalculator(100);
    calc = new WeightDecorator(calc, 5.0);
    calc = new DistanceDecorator(calc, 100);
    calc = new InsuranceDecorator(calc);
    
    return Ok(calc.Calculate());
}
```
**Đáp án:** Decorator Pattern
**Lý do:** Stacking decorators, wrap nhiều lần

---

## ✅ TÓM TẮT - NHẬN BIẾT NHANH

| Nhìn vào | Repository | Command | Singleton | Decorator |
|----------|------------|---------|-----------|-----------|
| Constructor | Inject IRepo | Inject Invoker | .Instance | N/A |
| Method | _repo.Async() | new Command() | .Instance | new Wrap() |
| Pattern | Inject → Use | Create → Execute | .Instance | Stack |
| Keyword | `IRepository` | `CommandInvoker` | `.Instance` | Wrapping |

---

**🎯 Kết luận:** Nhìn vào **Constructor** và **cách tạo/gọi objects** là biết ngay pattern! 🚀

---

**Created:** 2026-01-22  
**Author:** API_DigiBook Team
