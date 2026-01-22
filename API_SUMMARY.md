# API DigiBook - Tổng Hợp API & Design Patterns

## 📚 TỔNG QUAN PROJECT

**Project Name:** API_DigiBook  
**Framework:** ASP.NET Core 6.0  
**Database:** Firebase Firestore  
**Architecture:** Repository Pattern với Design Patterns

---

## 🎯 DANH SÁCH API CONTROLLERS

### 1. 📖 **BooksController** (`/api/books`)

**Pattern:** Repository Pattern

| Method | Endpoint | Mô tả | Pattern |
|--------|----------|-------|---------|
| GET | `/api/books` | Lấy tất cả sách | Repository |
| GET | `/api/books/isbn/{isbn}` | Lấy sách theo ISBN (DUY NHẤT) | Repository |
| GET | `/api/books/search?title=` | Tìm sách theo tên | Repository |
| GET | `/api/books/author/{authorId}` | Lấy sách theo tác giả | Repository |
| GET | `/api/books/category/{category}` | Lấy sách theo thể loại | Repository |
| GET | `/api/books/top-rated?count=10` | Lấy sách đánh giá cao | Repository |
| GET | `/api/books/test-connection` | Test kết nối Firebase | - |
| POST | `/api/books` | Tạo sách mới | Repository + **Singleton Logger** |
| PUT | `/api/books/{id}` | Cập nhật sách | Repository |
| DELETE | `/api/books/{id}` | Xóa sách | Repository + **Singleton Logger** |

**Đặc điểm:**
- ❌ KHÔNG có GET by ID (vì ID random)
- ✅ Chỉ dùng ISBN để get sách cụ thể
- 📝 Đã tích hợp LoggerService (Singleton)

---

### 2. 📂 **CategoriesController** (`/api/categories`)

**Pattern:** Repository Pattern

| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `/api/categories` | Lấy tất cả thể loại |
| GET | `/api/categories/{id}` | Lấy thể loại theo ID |
| POST | `/api/categories` | Tạo thể loại mới |
| PUT | `/api/categories/{id}` | Cập nhật thể loại |
| DELETE | `/api/categories/{id}` | Xóa thể loại |

---

### 3. 👤 **AuthorsController** (`/api/authors`)

**Pattern:** Repository Pattern

| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `/api/authors` | Lấy tất cả tác giả |
| GET | `/api/authors/{id}` | Lấy tác giả theo ID |
| POST | `/api/authors` | Tạo tác giả mới |
| PUT | `/api/authors/{id}` | Cập nhật tác giả |
| DELETE | `/api/authors/{id}` | Xóa tác giả |

---

### 4. 👥 **UsersController** (`/api/users`)

**Pattern:** Repository Pattern

| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `/api/users` | Lấy tất cả user |
| GET | `/api/users/{id}` | Lấy user theo ID |
| GET | `/api/users/email/{email}` | Lấy user theo email |
| POST | `/api/users` | Tạo user mới |
| PUT | `/api/users/{id}` | Cập nhật user |
| DELETE | `/api/users/{id}` | Xóa user |

---

### 5. 🛒 **OrdersController** (`/api/orders`)

**Pattern:** Repository + **Command Pattern** ⭐

| Method | Endpoint | Mô tả | Command |
|--------|----------|-------|---------|
| GET | `/api/orders` | Lấy tất cả đơn hàng | - |
| GET | `/api/orders/{id}` | Lấy đơn hàng theo ID | - |
| GET | `/api/orders/user/{userId}` | Lấy đơn hàng theo user | - |
| GET | `/api/orders/status/{status}` | Lấy đơn hàng theo trạng thái | - |
| GET | `/api/orders/recent?count=10` | Lấy đơn hàng gần nhất | - |
| POST | `/api/orders` | Tạo đơn hàng | **CreateOrderCommand** |
| PUT | `/api/orders/{id}` | Cập nhật đơn hàng | **UpdateOrderCommand** |
| PATCH | `/api/orders/{id}/status` | Cập nhật trạng thái | **UpdateOrderStatusCommand** |
| POST | `/api/orders/{id}/cancel` | Hủy đơn hàng | **CancelOrderCommand** |
| DELETE | `/api/orders/{id}` | Xóa đơn hàng | **DeleteOrderCommand** |

**Commands:**
1. `CreateOrderCommand` - Tạo đơn với validation
2. `UpdateOrderCommand` - Cập nhật đơn
3. `UpdateOrderStatusCommand` - Đổi trạng thái với rules
4. `CancelOrderCommand` - Hủy đơn với lý do
5. `DeleteOrderCommand` - Xóa đơn với validation

**Invoker:** `CommandInvoker` - Execute commands với logging

---

### 6. ⭐ **ReviewsController** (`/api/reviews`)

**Pattern:** Repository Pattern

| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `/api/reviews` | Lấy tất cả review |
| GET | `/api/reviews/{id}` | Lấy review theo ID |
| GET | `/api/reviews/book/{bookId}` | Lấy review theo sách |
| GET | `/api/reviews/user/{userId}` | Lấy review theo user |
| POST | `/api/reviews` | Tạo review mới |
| PUT | `/api/reviews/{id}` | Cập nhật review |
| DELETE | `/api/reviews/{id}` | Xóa review |

---

### 7. 🎫 **CouponsController** (`/api/coupons`)

**Pattern:** Repository Pattern

| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `/api/coupons` | Lấy tất cả coupon |
| GET | `/api/coupons/{id}` | Lấy coupon theo ID |
| GET | `/api/coupons/code/{code}` | Lấy coupon theo code |
| GET | `/api/coupons/active` | Lấy coupon đang active |
| POST | `/api/coupons` | Tạo coupon mới |
| PUT | `/api/coupons/{id}` | Cập nhật coupon |
| DELETE | `/api/coupons/{id}` | Xóa coupon |

---

### 8. 📝 **LogsController** (`/api/logs`)

**Pattern:** **Singleton Pattern** ⭐

| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `/api/logs` | Lấy tất cả logs |
| GET | `/api/logs/status/{status}` | Lấy logs theo status (SUCCESS/ERROR/WARNING/INFO) |
| GET | `/api/logs/user/{user}` | Lấy logs theo user |
| GET | `/api/logs/action/{action}` | Lấy logs theo action |
| GET | `/api/logs/statistics` | Thống kê logs |
| POST | `/api/logs/test` | Tạo test log |
| DELETE | `/api/logs/cleanup?days=30` | Xóa logs cũ |

**Singleton Service:** `LoggerService.Instance`

**Methods:**
- `LogSuccessAsync(action, detail, user)`
- `LogErrorAsync(action, detail, user)`
- `LogWarningAsync(action, detail, user)`
- `LogInfoAsync(action, detail, user)`

---

### 9. 🎁 **DiscountController** (`/api/discount`)

**Pattern:** **Decorator Pattern** ⭐

| Method | Endpoint | Mô tả | Decorators |
|--------|----------|-------|------------|
| POST | `/api/discount/calculate` | Tính discount với nhiều loại | Stack decorators |
| GET | `/api/discount/quick?price=&percentage=` | Tính nhanh theo % | Single decorator |
| POST | `/api/discount/black-friday` | Black Friday sale | Multiple decorators |
| POST | `/api/discount/apply-coupon` | Apply mã coupon | Coupon decorator |

**Decorators:**
1. `PercentageDiscountDecorator` - Giảm theo % (10%, 20%)
2. `FixedAmountDiscountDecorator` - Giảm cố định (-50,000 VND)
3. `MembershipDiscountDecorator` - Theo tier (BRONZE/SILVER/GOLD/PLATINUM)
4. `CouponDiscountDecorator` - Mã giảm giá
5. `SeasonalDiscountDecorator` - Sale theo mùa (có date range)
6. `BulkPurchaseDiscountDecorator` - Mua nhiều giảm nhiều

**Base:** `BasePriceCalculator` → Stack decorators → Final price

---

## 🏗️ DESIGN PATTERNS ĐÃ SỬ DỤNG

### 1. 📦 **Repository Pattern** (Structural)

**Mục đích:** Tách biệt data access logic khỏi business logic

**Implemented:**
- `IRepository<T>` - Generic interface
- `FirestoreRepository<T>` - Base implementation
- Specific repositories: `BookRepository`, `CategoryRepository`, etc.

**Sử dụng ở:**
- ✅ Books
- ✅ Categories
- ✅ Authors
- ✅ Users
- ✅ Orders
- ✅ Reviews
- ✅ Coupons

**Lợi ích:**
- Clean separation of concerns
- Easy to test (mock repositories)
- Consistent data access pattern
- Reusable code

---

### 2. ⚡ **Command Pattern** (Behavioral)

**Mục đích:** Đóng gói requests thành objects, hỗ trợ undo/redo, logging

**Implemented:**
- `ICommand<TResult>` - Command interface
- `CommandResult` - Result wrapper
- `CommandInvoker` - Executor với logging
- 5 concrete commands cho Orders

**Sử dụng ở:**
- ✅ **OrdersController** - Tất cả write operations

**Commands:**
1. `CreateOrderCommand` - Tạo đơn hàng
2. `UpdateOrderCommand` - Cập nhật đơn hàng
3. `UpdateOrderStatusCommand` - Đổi trạng thái (có rules)
4. `CancelOrderCommand` - Hủy đơn (có validation)
5. `DeleteOrderCommand` - Xóa đơn (có rules)

**Lợi ích:**
- Business logic tách biệt
- Easy to test
- Support command history
- Automatic logging
- Validation trong commands

---

### 3. 🔐 **Singleton Pattern** (Creational)

**Mục đích:** Đảm bảo chỉ có 1 instance duy nhất trong app

**Implemented:**
- `LoggerService` - Thread-safe singleton
- Double-check locking
- Private constructor
- Static Instance property

**Sử dụng ở:**
- ✅ **LogsController** - Quản lý logs
- ✅ **BooksController** - Log operations
- ✅ Có thể dùng ở mọi controller

**Features:**
- Log to Firebase Firestore
- 4 log levels: SUCCESS, ERROR, WARNING, INFO
- Query logs by status, user, action
- Auto cleanup old logs
- Statistics

**Lợi ích:**
- Global access
- Thread-safe
- Memory efficient
- Consistent logging

---

### 4. 🎁 **Decorator Pattern** (Structural)

**Mục đích:** Thêm behavior động vào objects mà không modify code gốc

**Implemented:**
- `IPriceCalculator` - Component interface
- `BasePriceCalculator` - Concrete component
- `DiscountDecorator` - Abstract decorator
- 6 concrete decorators

**Sử dụng ở:**
- ✅ **DiscountController** - Discount system

**Decorators:**
1. `PercentageDiscountDecorator`
2. `FixedAmountDiscountDecorator`
3. `MembershipDiscountDecorator`
4. `CouponDiscountDecorator`
5. `SeasonalDiscountDecorator`
6. `BulkPurchaseDiscountDecorator`

**Lợi ích:**
- Stack nhiều discounts
- Flexible combination
- Open/Closed principle
- Easy to add new discounts
- Runtime composition

---

## 📊 THỐNG KÊ PROJECT

### Controllers & Endpoints

| Controller | Endpoints | Pattern(s) | Complexity |
|------------|-----------|------------|------------|
| Books | 10 | Repository + Singleton | ⭐⭐⭐ |
| Categories | 5 | Repository | ⭐ |
| Authors | 5 | Repository | ⭐ |
| Users | 6 | Repository | ⭐⭐ |
| **Orders** | 10 | Repository + **Command** | ⭐⭐⭐⭐⭐ |
| Reviews | 7 | Repository | ⭐⭐ |
| Coupons | 7 | Repository | ⭐⭐ |
| **Logs** | 7 | **Singleton** | ⭐⭐⭐⭐ |
| **Discount** | 4 | **Decorator** | ⭐⭐⭐⭐ |
| **TOTAL** | **61** | **4 Patterns** | - |

### Design Patterns Summary

| Pattern | Type | Controllers | LOC | Complexity |
|---------|------|-------------|-----|------------|
| Repository | Structural | 7 | ~500 | ⭐⭐⭐ |
| Command | Behavioral | 1 | ~400 | ⭐⭐⭐⭐ |
| Singleton | Creational | All | ~300 | ⭐⭐⭐ |
| Decorator | Structural | 1 | ~600 | ⭐⭐⭐⭐⭐ |

### Models (Firebase Collections)

1. `Book` - Sách
2. `Category` - Thể loại
3. `Author` - Tác giả
4. `User` - Người dùng
5. `Order` - Đơn hàng
6. `Review` - Đánh giá
7. `Coupon` - Mã giảm giá
8. `AIModel` - AI models
9. `SystemLog` - System logs
10. `SystemConfig` - Cấu hình

**Total:** 10 models

---

## 🔥 ĐIỂM NỔI BẬT

### 1. Clean Architecture
- ✅ Separation of concerns
- ✅ Repository pattern cho data access
- ✅ Services layer
- ✅ Controllers chỉ handle HTTP

### 2. Design Patterns
- ✅ 4 design patterns được áp dụng đúng cách
- ✅ Pattern cho từng use case phù hợp
- ✅ Code maintainable và scalable

### 3. Best Practices
- ✅ RESTful API design
- ✅ Async/await everywhere
- ✅ Error handling đầy đủ
- ✅ Logging tự động
- ✅ Validation trong commands

### 4. Documentation
- ✅ XML comments cho tất cả endpoints
- ✅ Swagger/OpenAPI integration
- ✅ 4 markdown files hướng dẫn pattern
- ✅ Examples folder với code mẫu

### 5. Security & Performance
- ✅ Environment variables (.env)
- ✅ Firebase credentials không commit
- ✅ CORS configured
- ✅ Async operations
- ✅ Efficient Firestore queries

---

## 📁 CẤU TRÚC PROJECT

```
API_DigiBook/
├── Controllers/              (9 controllers, 61 endpoints)
│   ├── BooksController.cs
│   ├── CategoriesController.cs
│   ├── AuthorsController.cs
│   ├── UsersController.cs
│   ├── OrdersController.cs    ← Command Pattern
│   ├── ReviewsController.cs
│   ├── CouponsController.cs
│   ├── LogsController.cs      ← Singleton Pattern
│   └── DiscountController.cs  ← Decorator Pattern
│
├── Models/                   (10 models)
│   ├── Book.cs
│   ├── Category.cs
│   ├── Author.cs
│   ├── User.cs
│   ├── Order.cs
│   ├── Review.cs
│   ├── Coupon.cs
│   ├── AIModel.cs
│   ├── SystemLog.cs
│   ├── SystemConfig.cs
│   └── DiscountRequest.cs
│
├── Repositories/             (Repository Pattern)
│   ├── IRepository.cs
│   ├── FirestoreRepository.cs
│   ├── IBookRepository.cs & BookRepository.cs
│   ├── ICategoryRepository.cs & CategoryRepository.cs
│   ├── IAuthorRepository.cs & AuthorRepository.cs
│   ├── IUserRepository.cs & UserRepository.cs
│   ├── IOrderRepository.cs & OrderRepository.cs
│   ├── IReviewRepository.cs & ReviewRepository.cs
│   └── ICouponRepository.cs & CouponRepository.cs
│
├── Commands/                 (Command Pattern)
│   ├── ICommand.cs
│   ├── CommandInvoker.cs
│   └── Orders/
│       ├── CreateOrderCommand.cs
│       ├── UpdateOrderCommand.cs
│       ├── UpdateOrderStatusCommand.cs
│       ├── CancelOrderCommand.cs
│       └── DeleteOrderCommand.cs
│
├── Services/
│   ├── FirebaseService.cs
│   ├── LoggerService.cs      ← Singleton Pattern
│   └── Discount/             ← Decorator Pattern
│       ├── IPriceCalculator.cs
│       ├── BasePriceCalculator.cs
│       ├── DiscountDecorator.cs
│       ├── PercentageDiscountDecorator.cs
│       ├── FixedAmountDiscountDecorator.cs
│       ├── MembershipDiscountDecorator.cs
│       ├── CouponDiscountDecorator.cs
│       ├── SeasonalDiscountDecorator.cs
│       └── BulkPurchaseDiscountDecorator.cs
│
├── Examples/                 (Code examples)
│   ├── LoggerUsageExample.cs
│   └── DecoratorDiscountExample.cs
│
├── Documentation/
│   ├── COMMAND_PATTERN.md
│   ├── SINGLETON_LOGGER_PATTERN.md
│   ├── DECORATOR_DISCOUNT_PATTERN.md
│   ├── API_SUMMARY.md (this file)
│   └── README.md
│
├── .env                      (Environment variables)
├── .gitignore
├── Program.cs
└── API_DigiBook.csproj
```

---

## 🚀 CÁCH SỬ DỤNG

### 1. Setup
```bash
# Clone project
git clone https://github.com/monsiuerjin/API_Digibook.git

# Tạo .env file
FIREBASE_PROJECT_ID=your-project-id
FIREBASE_CREDENTIAL_PATH=./firebase-credentials.json

# Download Firebase credentials
# Đặt vào project root: firebase-credentials.json

# Restore packages
dotnet restore

# Run
dotnet run
```

### 2. Swagger UI
```
http://localhost:9905/swagger
```

### 3. Test Endpoints

**Get book by ISBN:**
```
GET http://localhost:9905/api/books/isbn/978-3-16-148410-0
```

**Create order with Command Pattern:**
```
POST http://localhost:9905/api/orders
Body: { order data }
```

**Calculate discount with Decorator:**
```
POST http://localhost:9905/api/discount/calculate
Body: {
  "basePrice": 200000,
  "discounts": [
    { "type": "PERCENTAGE", "value": 20 },
    { "type": "MEMBERSHIP", "membershipTier": "GOLD" }
  ]
}
```

**View logs (Singleton):**
```
GET http://localhost:9905/api/logs/statistics
```

---

## 🎯 NEXT STEPS (Suggestions)

### Có thể mở rộng:

1. **Authentication & Authorization**
   - JWT tokens
   - Role-based access control

2. **More Patterns**
   - Factory Pattern cho order types
   - Strategy Pattern cho payment methods
   - Observer Pattern cho notifications

3. **Advanced Features**
   - Rate limiting
   - Caching (Redis)
   - Message queue
   - Real-time updates (SignalR)

4. **Testing**
   - Unit tests
   - Integration tests
   - E2E tests

5. **DevOps**
   - Docker containerization
   - CI/CD pipeline
   - Azure/AWS deployment

---

## 📝 CREDITS

**Project:** API_DigiBook  
**Developer:** monsiuerjin  
**Repository:** https://github.com/monsiuerjin/API_Digibook  
**Patterns Implemented:** Repository, Command, Singleton, Decorator  
**Total APIs:** 61 endpoints across 9 controllers  
**Last Updated:** 2026-01-22

---

**🎉 Project successfully implements 4 major design patterns with clean architecture! 🎉**
