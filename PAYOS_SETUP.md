# PayOS Payment Integration - Setup Guide

## 📋 Tổng quan

Tích hợp thanh toán PayOS với Factory Method Design Pattern, hỗ trợ 2 phương thức:
- **COD** (Cash on Delivery)
- **PayOS** (Chuyển khoản ngân hàng, QR Code, Ví điện tử)

## 🔧 Cấu hình Backend (.NET)

### 1. Cập nhật appsettings.json

Thêm thông tin PayOS credentials:

```json
{
  "PayOS": {
    "ClientId": "YOUR_PAYOS_CLIENT_ID",
    "ApiKey": "YOUR_PAYOS_API_KEY",
    "ChecksumKey": "YOUR_PAYOS_CHECKSUM_KEY"
  }
}
```

### 2. Lấy PayOS Credentials

1. Đăng ký tài khoản tại: https://payos.vn
2. Vào Dashboard → Settings → API Keys
3. Copy **Client ID**, **API Key**, và **Checksum Key**
4. Paste vào `appsettings.json`

### 3. Firestore Collections

Tự động tạo collection mới:
- `PaymentTransactions`: Lưu lịch sử giao dịch

## 🎨 Cấu hình Frontend (React)

### 1. Environment Variables

Tạo file `.env` trong thư mục `DigiBook/`:

```env
VITE_API_URL=http://localhost:5197/api
```

### 2. Package đã cài

- `@payos/payos-checkout`: SDK PayOS cho React

## 🚀 Cách sử dụng

### Backend API Endpoints

#### Tạo payment link
```http
POST /api/payment/create
Content-Type: application/json

{
  "orderId": "ORDER_ID",
  "orderCode": "12345678",
  "amount": 100000,
  "description": "Thanh toán đơn hàng #12345678",
  "returnUrl": "https://yourdomain.com/payment-callback",
  "cancelUrl": "https://yourdomain.com/payment-cancel",
  "customer": {
    "name": "Nguyễn Văn A",
    "email": "test@example.com",
    "phone": "0123456789"
  },
  "items": [
    {
      "name": "Sách ABC",
      "quantity": 1,
      "price": 100000
    }
  ]
}
```

#### Verify payment
```http
GET /api/payment/verify/{orderId}
```

#### Webhook callback
```http
POST /api/payment/webhook
Content-Type: application/json

{
  "orderCode": "12345678",
  "status": "PAID",
  ...
}
```

### Frontend Usage

Payment flow được xử lý tự động trong `CheckoutPage`:

1. User chọn payment method (COD hoặc PayOS)
2. Click "Đặt hàng"
3. Nếu chọn PayOS:
   - Tạo order trong database
   - Gọi API create payment link
   - Mở PayOS popup
   - User thanh toán
   - Callback về `/payment-callback`
   - Verify và redirect về `/payment-success/:orderId`

## 🏗️ Architecture

### Factory Method Pattern

```
PaymentProviderFactory
    ├── PayOSProvider (implements IPaymentProvider)
    └── CODProvider (không cần, xử lý trực tiếp)

PaymentServiceFactory (Backend)
    ├── PayOSService (implements IPaymentService)
    └── CODService (không cần)
```

### File Structure

#### Backend
```
API_Digibook/
├── Controllers/
│   └── PaymentController.cs
├── Factories/
│   └── PaymentServiceFactory.cs
├── Interfaces/Payment/
│   └── IPaymentService.cs
├── Models/
│   ├── PaymentRequest.cs
│   ├── PaymentResponse.cs
│   ├── PaymentVerification.cs
│   └── PaymentTransaction.cs
└── Services/Payment/
    └── PayOSService.cs
```

#### Frontend
```
DigiBook/src/
├── pages/
│   ├── PaymentCallbackPage.tsx
│   ├── PaymentCancelPage.tsx
│   └── PaymentSuccessPage.tsx
└── services/payment/
    ├── IPaymentProvider.ts
    ├── PaymentProviderFactory.ts
    ├── types.ts
    └── providers/
        └── PayOSProvider.ts
```

## 🔐 Security

### Backend
- HMAC SHA256 signature verification cho webhooks
- Validate orderCode trước khi xử lý
- Rate limiting (recommended)

### Frontend
- CSP headers cho phép PayOS iframe
- HTTPS required cho production
- Validate payment events

## 🧪 Testing

### Test Mode

PayOS cung cấp sandbox environment để test:
- Sử dụng test credentials
- Không tính phí thực tế
- QR code giả lập

### Production

- Thay test credentials bằng production keys
- Configure webhook URL
- Test toàn bộ flow: success, cancel, timeout

## 📊 Payment Status Flow

```
PENDING → User chưa thanh toán
   ↓
PAID → Thanh toán thành công
   ↓
Order.Status = "Đã thanh toán"
```

```
PENDING → User hủy thanh toán
   ↓
CANCELLED → Order.Status = "Đã hủy"
```

## ⚠️ Lưu ý

1. **OrderCode**: Phải là số nguyên dương, unique
2. **Amount**: Đơn vị là VND, số nguyên
3. **ReturnUrl**: Phải trùng với domain đang chạy payment
4. **Webhook**: Cần public URL để PayOS gọi callback (dùng ngrok cho local dev)
5. **Timeout**: Payment link có thời gian hết hạn (default: 15 phút)

## 🐛 Troubleshooting

### Lỗi "Invalid signature"
- Kiểm tra ChecksumKey đúng chưa
- Verify signature algorithm (HMAC SHA256)

### Popup không mở
- Check console log errors
- Verify PayOS script đã load (`payos-initialize.js`)
- Check CSP headers

### Webhook không nhận được
- Dùng ngrok để expose localhost
- Configure webhook URL trong PayOS dashboard
- Check firewall settings

## 📞 Support

- PayOS Docs: https://payos.vn/docs
- Support Email: support@payos.vn
- Hotline: 1900 xxxx

---

**Hoàn thành!** 🎉 Payment integration đã sẵn sàng sử dụng.
