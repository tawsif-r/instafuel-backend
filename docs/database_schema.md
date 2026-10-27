# InstaFuel Database Schema

The current API uses one PostgreSQL database managed by Entity Framework Core.
This diagram reflects the existing EF Core entities and relationships. It does
not imply that endpoints or workflows for every entity are implemented.

```mermaid
erDiagram
    USER {
        uuid id PK
        string full_name
        string email UK
        string phone UK
        string password_hash
        boolean is_active
        datetime created_at
        datetime updated_at
    }
    ROLE {
        int id PK
        string name UK
    }
    USER_ROLE {
        uuid user_id PK, FK
        int role_id PK, FK
        datetime granted_at
        uuid granted_by
    }
    REFRESH_TOKEN {
        uuid id PK
        uuid user_id FK
        string token_hash UK
        datetime created_at
        datetime expires_at
        datetime revoked_at
    }
    SUPPLIER {
        uuid id PK
        string legal_name
        string display_name
        string status
        datetime created_at
    }
    SUPPLIER_MEMBER {
        uuid supplier_id PK, FK
        uuid user_id PK, FK
        string member_role
        datetime created_at
    }
    SUPPLIER_LOCATION {
        uuid id PK
        uuid supplier_id FK
        string name
        string address_line
        string city
        decimal latitude
        decimal longitude
        boolean is_active
    }
    CATEGORY {
        uuid id PK
        uuid parent_id FK
        string name
        string slug UK
        boolean is_active
    }
    PRODUCT {
        uuid id PK
        uuid category_id FK
        string name
        string product_type
        string unit_code
        string fuel_grade
        boolean is_active
    }
    SUPPLIER_OFFER {
        uuid id PK
        uuid supplier_id FK
        uuid product_id FK
        string seller_sku
        decimal unit_price
        string currency
        string status
    }
    INVENTORY_BALANCE {
        uuid id PK
        uuid supplier_location_id FK
        uuid supplier_offer_id FK
        decimal quantity_on_hand
        decimal quantity_reserved
        datetime updated_at
    }
    INVENTORY_MOVEMENT {
        uuid id PK
        uuid inventory_balance_id FK
        string movement_type
        decimal quantity_delta
        string reference_type
        uuid reference_id
        datetime created_at
    }
    ORDERS {
        uuid id PK
        string order_number UK
        uuid customer_id FK
        string status
        string currency
        decimal subtotal
        decimal delivery_fee
        decimal discount_amount
        decimal total_amount
        datetime created_at
    }
    SUPPLIER_ORDER {
        uuid id PK
        uuid order_id FK
        uuid supplier_id FK
        uuid supplier_location_id FK
        string status
        decimal subtotal
    }
    ORDER_ITEM {
        uuid id PK
        uuid supplier_order_id FK
        uuid supplier_offer_id FK
        string product_name
        string seller_name
        decimal quantity
        decimal unit_price
        decimal line_total
    }
    DELIVERY {
        uuid id PK
        uuid supplier_order_id FK
        uuid delivery_agent_id FK
        string status
        datetime assigned_at
        datetime picked_up_at
        datetime delivered_at
    }
    DELIVERY_STATUS_HISTORY {
        uuid id PK
        uuid delivery_id FK
        uuid recorded_by_user_id FK
        string status
        datetime created_at
    }
    PAYMENT {
        uuid id PK
        uuid order_id FK
        decimal amount
        string currency
        string method
        string status
        string idempotency_key
        datetime paid_at
    }
    REFUND {
        uuid id PK
        uuid payment_id FK
        decimal amount
        string status
        datetime created_at
    }

    USER ||--o{ USER_ROLE : assigned
    ROLE ||--o{ USER_ROLE : grants
    USER ||--o{ REFRESH_TOKEN : owns
    USER ||--o{ SUPPLIER_MEMBER : belongs
    SUPPLIER ||--o{ SUPPLIER_MEMBER : has
    SUPPLIER ||--o{ SUPPLIER_LOCATION : operates
    CATEGORY ||--o{ CATEGORY : parent_of
    CATEGORY ||--o{ PRODUCT : groups
    PRODUCT ||--o{ SUPPLIER_OFFER : offered_as
    SUPPLIER ||--o{ SUPPLIER_OFFER : publishes
    SUPPLIER_LOCATION ||--o{ INVENTORY_BALANCE : stores
    SUPPLIER_OFFER ||--o{ INVENTORY_BALANCE : tracked_as
    INVENTORY_BALANCE ||--o{ INVENTORY_MOVEMENT : records
    USER ||--o{ ORDERS : places
    ORDERS ||--o{ SUPPLIER_ORDER : split_into
    SUPPLIER ||--o{ SUPPLIER_ORDER : fulfills
    SUPPLIER_LOCATION ||--o{ SUPPLIER_ORDER : fulfills_from
    SUPPLIER_ORDER ||--o{ ORDER_ITEM : contains
    SUPPLIER_OFFER ||--o{ ORDER_ITEM : snapshots
    SUPPLIER_ORDER ||--o{ DELIVERY : delivered_by
    USER ||--o{ DELIVERY : assigned_to
    DELIVERY ||--o{ DELIVERY_STATUS_HISTORY : tracks
    USER ||--o{ DELIVERY_STATUS_HISTORY : records
    ORDERS ||--o{ PAYMENT : paid_by
    PAYMENT ||--o{ REFUND : refunded_by
```

`user_roles` and `supplier_members` use composite primary keys. Role names and
statuses are currently stored as strings. The entity configurations define
the database constraints, indexes, and delete behavior.
