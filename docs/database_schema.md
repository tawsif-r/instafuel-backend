Since your **Fuel On-Demand** project uses FastAPI and PostgreSQL with a microservice architecture, here is a sample database schema in **Mermaid ER diagram format**.

You can paste this code into the [Mermaid Live Editor](https://mermaid.live/) to visualize the database relationships.

### Sample Database Schema — Fuel On-Demand

```mermaid
erDiagram

    USER {
        int user_id PK
        string full_name
        string email UK
        string phone UK
        string password_hash
        string role
        string account_status
        datetime created_at
    }

    ADDRESS {
        int address_id PK
        int user_id FK
        string address_line
        string city
        float latitude
        float longitude
    }

    CUSTOMER_VEHICLE {
        int customer_vehicle_id PK
        int user_id FK
        string nickname
        string vehicle_type
        string registration_number UK
        string fuel_type
        string verification_status
        string rejection_reason
        datetime verified_at
        boolean is_active
    }

    CUSTOMER_VEHICLE_DOCUMENT {
        int document_id PK
        int customer_vehicle_id FK
        string kind
        string file_name
        datetime uploaded_at
    }

    SUPPLIER {
        int supplier_id PK
        int user_id FK
        string business_name
        string station_address
        float latitude
        float longitude
        boolean is_active
    }

    FUEL_TYPE {
        int fuel_type_id PK
        string fuel_name
        string unit
        boolean is_active
    }

    INVENTORY {
        int inventory_id PK
        int supplier_id FK
        int fuel_type_id FK
        float available_quantity
        decimal price_per_unit
        boolean is_available
    }

    ORDERS {
        int order_id PK
        int customer_id FK
        int address_id FK
        int supplier_id FK
        datetime order_date
        decimal subtotal
        decimal delivery_charge
        decimal total_amount
        string payment_method
        string order_status
    }

    ORDER_ITEM {
        int order_item_id PK
        int order_id FK
        int fuel_type_id FK
        float quantity
        decimal unit_price
        decimal total_price
    }

    PAYMENT {
        int payment_id PK
        int order_id FK
        decimal amount
        string payment_method
        string payment_status
        string transaction_reference
        datetime paid_at
    }

    DRIVER {
        int driver_id PK
        int user_id FK
        string license_number UK
        date license_expiry
        boolean dangerous_goods_certified
        string verification_status
        string employment_status
        boolean is_available
    }

    VEHICLE {
        int vehicle_id PK
        string registration_number UK
        string vehicle_type
        float capacity_litres
        int compartment_count
        date fitness_certificate_expiry
        date last_inspection_date
        string vehicle_status
        boolean is_available
    }

    DELIVERY {
        int delivery_id PK
        int order_id FK
        int driver_id FK
        int vehicle_id FK
        datetime assigned_at
        datetime dispatched_at
        datetime delivered_at
        float delivered_quantity
        string delivery_status
        string failure_reason
    }

    REVIEW {
        int review_id PK
        int customer_id FK
        int order_id FK
        int rating
        string comment
        datetime created_at
    }

    USER ||--o{ ADDRESS : has
    USER ||--o{ CUSTOMER_VEHICLE : owns
    CUSTOMER_VEHICLE ||--o{ CUSTOMER_VEHICLE_DOCUMENT : has
    USER ||--o| SUPPLIER : operates
    USER ||--o| DRIVER : works_as

    USER ||--o{ ORDERS : places
    ADDRESS ||--o{ ORDERS : receives
    SUPPLIER ||--o{ ORDERS : fulfills

    ORDERS ||--|{ ORDER_ITEM : contains
    FUEL_TYPE ||--o{ ORDER_ITEM : identifies

    SUPPLIER ||--o{ INVENTORY : manages
    FUEL_TYPE ||--o{ INVENTORY : stocked_as

    ORDERS ||--o{ PAYMENT : has
    ORDERS ||--o{ DELIVERY : requires
    DRIVER ||--o{ DELIVERY : drives
    VEHICLE ||--o{ DELIVERY : carries

    USER ||--o{ REVIEW : writes
    ORDERS ||--o| REVIEW : receives
```

### How to understand this diagram

**1. USER — The central entity**

The `USER` table stores common account information. A user can have a customer, driver, or supplier role. Sector Supervisors, Admins, and Super Admins can also have accounts.

**2. FUEL_TYPE and INVENTORY — Fuel management**

- `FUEL_TYPE` stores Petrol, Diesel, Octane, and Gas Cylinder.
- `INVENTORY` tracks how much fuel each supplier has available and its price.
- One supplier can stock multiple fuel types.

**3. ORDERS and ORDER_ITEM — Order management**

These tables represent different concepts:

- `ORDERS` stores the overall order, including the customer, supplier, delivery address, and total amount.
- `ORDER_ITEM` stores the individual fuel products and quantities included in that order.

For example, one order might contain 10 litres of Petrol and 5 litres of Diesel. The order has one record in `ORDERS` and two records in `ORDER_ITEM`.

**4. PAYMENT — Payment processing**

The `PAYMENT` table stores payment details for online payments and Cash on Delivery (COD). The payment status can be `PENDING`, `PAID`, `FAILED`, or `REFUNDED`.

**5. DRIVER, VEHICLE and DELIVERY — Fleet & delivery management**

- `DRIVER` stores driver-specific information for a `USER` with the driver role: licence, licence expiry, dangerous-goods certification, verification and employment status, and availability.
- `VEHICLE` stores the company's own delivery fleet (fuel trucks / tankers / bowsers). It is not owned by a user; it carries registration, capacity, compartment count, fitness/inspection dates, and operational status (e.g. `ACTIVE`, `UNDER_MAINTENANCE`, `RETIRED`).
- `DELIVERY` connects an order with the driver **and** the vehicle assigned to it, and records the actual `delivered_quantity`. Status follows the plan: `PENDING`, `ASSIGNED`, `DISPATCHED`, `IN_TRANSIT`, `DELIVERED`, `FAILED`, `CANCELLED`. `failure_reason` is filled when a delivery fails.

Drivers and vehicles are separate entities because they are assigned independently — any authorized driver can drive any available vehicle, and the pairing is captured per delivery.

`CUSTOMER_VEHICLE` (formerly `VEHICLE`) is the customer's own car/vehicle (or generator). It is unrelated to the delivery fleet and was renamed to avoid confusion. Orders may only include `VERIFIED` vehicles, and the driver matches `registration_number` against the plate before fuelling. `CUSTOMER_VEHICLE_DOCUMENT` holds one document per kind (registration certificate, fitness certificate, number plate photo); an admin reviews them and sets `verification_status` (`PENDING`, `VERIFIED`, `REJECTED` + `rejection_reason`). A registration number can belong to only one active vehicle.

Separating `DELIVERY` from `ORDERS` makes it possible to track delivery progress independently of the order's overall status.

### Important considerations for your actual project

This is a **sample relational schema**, not the final production database design.

For Fuel On-Demand, I recommend considering these improvements when developing the actual system:

| Requirement | Recommended approach |
|---|---|
| Multiple user roles | Use a role/permission system instead of relying only on a role string. |
| Sector Supervisors | Add `SECTOR` and `SECTOR_SUPERVISOR` relationships. |
| Driver preferred areas | Create `DRIVER_PREFERRED_AREA` and `DELIVERY_AREA` tables. |
| Delivery route planning | Create `DELIVERY_ROUTE` / `ROUTE_STOP` tables to group deliveries per vehicle trip. |
| Vehicle maintenance | Create a `VEHICLE_MAINTENANCE_LOG` table for servicing and downtime history. |
| Delivery status history | Create a `DELIVERY_STATUS_HISTORY` table. |
| Supplier inventory | Add inventory transaction records to track stock changes. |
| Refunds | Create a separate `REFUND` table. |
| Order status history | Create an `ORDER_STATUS_HISTORY` table. |
| Notifications | Create a `NOTIFICATION` table. |
| Online payments | Store gateway references and payment event records securely. |
| Microservice architecture | Define clear ownership of tables and data between services. |

**One important architecture note:** Since you're planning to use microservices with FastAPI and PostgreSQL, this diagram represents the logical relationships across the system. In production, the tables may belong to different service-owned databases rather than one shared database. For example, the Order Service would own order records, while the Delivery Service would own delivery records.

For your first implementation, you can begin with a single PostgreSQL database and separate FastAPI modules, then split the databases into independently owned services as the project grows.