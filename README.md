# Mini E-Commerce & Order Management Engine

A backend-focused mini e-commerce API built with **ASP.NET Core 8 Web API** and **EF Core**, designed to demonstrate relational data modeling, transactional stock control, and extensible business logic rather than UI polish.

## Why this project

Most portfolio to-do-list clones don't say much about backend ability. This project focuses on the things that actually get discussed in backend interviews:

- A **normalized relational schema** with proper foreign keys and constraints (unique indexes, check constraints, cascade rules).
- **Atomic, race-condition-safe stock control** at checkout, implemented with a real DB transaction and a conditional SQL update — not a naive read-then-write.
- A **loosely-coupled discount/coupon engine** built with the Strategy pattern, open for extension without modifying existing code.
- Clean **RESTful endpoints** with filtering, pagination and role-based JWT authorization.

## Tech Stack

| Layer | Choice |
|---|---|
| Framework | ASP.NET Core 8 Web API (Controllers) |
| ORM | Entity Framework Core 8 |
| Database | SQLite (file-based, zero setup — swap to SQL Server/PostgreSQL by changing one line, see below) |
| Auth | JWT Bearer, role-based (`Customer`, `Admin`) |
| Password hashing | BCrypt.Net |
| API docs | Swagger / OpenAPI |

## Architecture

```
Controllers  -> thin, HTTP-concern only (validation via DataAnnotations, status codes)
Services     -> all business logic (stock, coupons, ordering, auth)
Data (EF)    -> AppDbContext, fluent configuration, migrations
Models       -> entities
DTOs         -> request/response contracts, never expose entities directly
Middleware   -> global exception handling, maps domain exceptions to HTTP codes
```

Domain exceptions (`NotFoundException`, `BadRequestException`, `InsufficientStockException`, `CouponInvalidException`) are thrown from services and translated centrally by `ExceptionHandlingMiddleware` into consistent JSON error responses (`404`, `400`, `409`, etc.) — controllers stay free of try/catch noise.

## Entity-Relationship Diagram

```mermaid
erDiagram
    USER ||--o| CART : has
    USER ||--o{ ORDER : places
    CATEGORY ||--o{ CATEGORY : "parent of"
    CATEGORY ||--o{ PRODUCT : contains
    CART ||--o{ CART_ITEM : contains
    PRODUCT ||--o{ CART_ITEM : "referenced by"
    PRODUCT ||--o{ ORDER_ITEM : "referenced by"
    ORDER ||--o{ ORDER_ITEM : contains
    COUPON ||--o{ ORDER : "applied to"

    USER {
        int Id PK
        string FullName
        string Email UK
        string PasswordHash
        int Role
    }
    CATEGORY {
        int Id PK
        string Name
        int ParentCategoryId FK "nullable, self-reference"
    }
    PRODUCT {
        int Id PK
        string Name
        decimal Price
        int StockQuantity
        int CategoryId FK
        bool IsActive
        bytes RowVersion "optimistic concurrency"
    }
    CART {
        int Id PK
        int UserId FK UK "one active cart per user"
    }
    CART_ITEM {
        int Id PK
        int CartId FK
        int ProductId FK
        int Quantity
    }
    ORDER {
        int Id PK
        int UserId FK
        int CouponId FK "nullable"
        int Status
        decimal SubTotal
        decimal DiscountAmount
        decimal TotalAmount
    }
    ORDER_ITEM {
        int Id PK
        int OrderId FK
        int ProductId FK
        string ProductName "price snapshot"
        decimal UnitPrice "price snapshot"
        int Quantity
    }
    COUPON {
        int Id PK
        string Code UK
        int DiscountType
        decimal Value
        decimal MinCartAmount "nullable"
        datetime ExpiryDate "nullable"
        int UsageLimit "nullable"
        int TimesUsed
    }
```

> GitHub renders this Mermaid block automatically. For the CV/portfolio, export a PNG (e.g. via the [Mermaid Live Editor](https://mermaid.live)) and drop it in `/docs/erd.png`, then embed it here as an image for reviewers who don't have Mermaid rendering.

### Key constraints enforced at the database level
- `User.Email` — unique index.
- `Cart.UserId` — unique index (one active cart per user).
- `CartItem (CartId, ProductId)` — unique index (no duplicate line items).
- `Coupon.Code` — unique index.
- `CHECK (Price >= 0)`, `CHECK (StockQuantity >= 0)`, `CHECK (Quantity > 0)` on relevant tables.
- Products cannot be hard-deleted once referenced by an order (`Restrict`) — they are soft-deleted (`IsActive = false`) instead, preserving order history.

## The interesting part: preventing stock race conditions

A naive implementation reads the stock, checks it in application code, then writes it back:

```
read stock (10)        read stock (10)         <- both requests read before either writes
check 10 >= 1 OK        check 10 >= 1 OK
write stock = 9         write stock = 9         <- lost update: two units sold, stock only dropped by 1
```

This project avoids that entirely by using a **single atomic conditional UPDATE** per line item, wrapped in a DB transaction, instead of a read-check-write cycle:

```sql
UPDATE Products
SET StockQuantity = StockQuantity - @qty
WHERE Id = @productId AND StockQuantity >= @qty
```

The database guarantees this statement executes atomically. If the affected row count is `0`, another transaction already consumed the stock (or there simply isn't enough), and the whole order transaction is rolled back with an `InsufficientStockException` (`409 Conflict`). If every line item succeeds, the order and its items are persisted and the transaction is committed — all or nothing (see `OrderService.CreateOrderAsync`).

Cancelling an order restocks items the same way, inside its own transaction.

## Coupon / Discount Engine

Implemented with the **Strategy pattern** so new discount types can be added without touching existing logic (Open/Closed Principle):

- `IDiscountStrategy` — contract for calculating a discount.
- `FixedAmountDiscountStrategy`, `PercentageDiscountStrategy` — current implementations.
- `IDiscountStrategyFactory` — resolves the correct strategy at runtime from `Coupon.DiscountType`, injected via DI as `IEnumerable<IDiscountStrategy>`.
- `CouponService` centrally validates business rules (active, not expired, usage limit, minimum cart amount) before delegating the actual math to the resolved strategy.

Adding a "Buy One Get One" or "Free Shipping" coupon later means: add an enum value, add one class implementing `IDiscountStrategy`, register it in `Program.cs`. Nothing else changes.

## API Endpoints

Full interactive documentation is available via Swagger UI at `/swagger` when running in Development mode.

### Auth
| Method | Endpoint | Auth | Description |
|---|---|---|---|
| POST | `/api/auth/register` | - | Register a new customer, returns JWT |
| POST | `/api/auth/login` | - | Login, returns JWT |

### Categories
| Method | Endpoint | Auth | Description |
|---|---|---|---|
| GET | `/api/categories` | - | Category tree (parent/subcategories) |
| GET | `/api/categories/{id}` | - | Single category |
| POST | `/api/categories` | Admin | Create category |
| PUT | `/api/categories/{id}` | Admin | Update category |
| DELETE | `/api/categories/{id}` | Admin | Delete (blocked if it has products/children) |

### Products
| Method | Endpoint | Auth | Description |
|---|---|---|---|
| GET | `/api/products?categoryId=&minPrice=&maxPrice=&search=&inStockOnly=&page=&pageSize=&sortBy=` | - | Filtered, paginated product list |
| GET | `/api/products/{id}` | - | Product detail |
| POST | `/api/products` | Admin | Create product |
| PUT | `/api/products/{id}` | Admin | Update product |
| DELETE | `/api/products/{id}` | Admin | Delete (soft-delete if referenced by orders) |

### Cart
| Method | Endpoint | Auth | Description |
|---|---|---|---|
| GET | `/api/cart` | Any user | Get current user's cart |
| POST | `/api/cart/items` | Any user | Add item (blocked if stock insufficient) |
| PUT | `/api/cart/items/{productId}` | Any user | Update quantity |
| DELETE | `/api/cart/items/{productId}` | Any user | Remove item |
| DELETE | `/api/cart` | Any user | Clear cart |

### Orders
| Method | Endpoint | Auth | Description |
|---|---|---|---|
| POST | `/api/orders` | Any user | Checkout current cart (transactional stock decrement + optional coupon) |
| GET | `/api/orders` | Any user | Own order history |
| GET | `/api/orders/{id}` | Any user / Admin | Order detail |
| PUT | `/api/orders/{id}/cancel` | Any user / Admin | Cancel + restock atomically |

### Coupons
| Method | Endpoint | Auth | Description |
|---|---|---|---|
| GET | `/api/coupons` | Admin | List all coupons |
| POST | `/api/coupons` | Admin | Create a coupon |
| GET | `/api/coupons/{code}/preview?cartTotal=` | Any user | Preview discount without applying it |

## Running Locally

Prerequisites: [.NET 8 SDK](https://dotnet.microsoft.com/download).

```bash
git clone <your-repo-url>
cd MiniECommerce/src/MiniECommerce.Api

dotnet restore
dotnet run
```

The app seeds a SQLite database (`miniecommerce.db`) on first run with:
- An admin account: `admin@miniecommerce.dev` / `Admin123!`
- Sample categories, products (including one out-of-stock and one single-unit item, useful for testing stock limits) and coupons (`WELCOME10`, `FLAT50`, `EXPIRED5`).

Open `http://localhost:5080/swagger` to explore and test every endpoint. Authorize with a Bearer token obtained from `/api/auth/login` or `/api/auth/register` to hit protected endpoints.

### Switching to SQL Server / PostgreSQL
SQLite was chosen so reviewers can clone and run with zero external setup. To use a "real" server database for production:
1. Swap the NuGet package (`Microsoft.EntityFrameworkCore.SqlServer` or `Npgsql.EntityFrameworkCore.PostgreSQL`).
2. Change `options.UseSqlite(...)` to `options.UseSqlServer(...)` / `UseNpgsql(...)` in `Program.cs`.
3. Update the connection string in `appsettings.json`.
4. Generate migrations: `dotnet ef migrations add Init` and `dotnet ef database update`.

## Testing the Race Condition (manually)

1. Note that `MacBook Air M3` seeds with `StockQuantity = 1`.
2. Add it to two different users' carts (register two accounts).
3. Fire both `POST /api/orders` checkout requests at nearly the same time (e.g. two terminal tabs with `curl`, or a quick script).
4. One request succeeds (`200 OK`); the other receives `409 Conflict` with an `InsufficientStockException` message — stock never goes negative.

## Suggested Commit History

Instead of one giant commit, this project is structured to be built (and should be pushed) in logical increments:

```
feat: scaffold ASP.NET Core Web API project and EF Core DbContext
feat: add domain entities and fluent configuration (constraints, indexes)
feat: implement JWT authentication and role-based authorization
feat: implement category and product endpoints with filtering/pagination
feat: implement cart CRUD endpoints with stock validation
feat: add order transaction logic with atomic stock decrement
feat: implement coupon/discount engine (strategy pattern)
feat: add global exception handling middleware
chore: add database seeding and Swagger configuration
docs: add README with ERD, API docs and setup instructions
```

## Possible Extensions
- Unit tests for `OrderService` (mocking `AppDbContext` via EF Core InMemory or SQLite in-memory) covering the insufficient-stock and coupon-rejection paths.
- Refresh tokens / token revocation.
- Idempotency keys on `POST /api/orders` to make retried checkout requests safe.
- Outbox pattern + background worker for order-confirmation emails.
