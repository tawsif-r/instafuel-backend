# InstaFuel Database Schema

## Overview

InstaFuel uses PostgreSQL as its relational database and Entity Framework Core (EF Core) as its Object-Relational Mapper (ORM).

The database supports the fuel marketplace, including customers, suppliers, products, inventory, orders, payments, refunds, and deliveries.

## Entity Relationship Diagram

```mermaid
erDiagram

    USERS {
        uuid Id PK
        string FullName
        string Email
        string Phone
        string PasswordHash
        boolean IsActive
        datetime CreatedAt
        datetime UpdatedAt
    }

    SUPPLIERS {
        uuid Id PK
        string LegalName
        string DisplayName
        string Status
        datetime CreatedAt
    }

    SUPPLIER_MEMBERS {
        uuid SupplierId PK, FK
        uuid UserId PK, FK
        string MemberRole
        datetime CreatedAt
    }

    SUPPLIER_LOCATIONS {
        uuid Id PK
        uuid SupplierId FK
        string Name
        string AddressLine
        string City
        decimal Latitude
        decimal Longitude
        boolean IsActive
        datetime CreatedAt
    }

    CATEGORIES {
        uuid Id PK
        uuid ParentId FK
        string Name
        string Slug
        boolean IsActive
    }

    PRODUCTS {
        uuid Id PK
        uuid CategoryId FK
        string Name
        string Description
        string ProductType
        string UnitCode
        string FuelGrade
        boolean IsActive
        datetime CreatedAt
    }

    SUPPLIER_OFFERS {
        uuid Id PK
        uuid SupplierId FK
        uuid ProductId FK
        string SellerSku
        decimal UnitPrice
        string Currency
        string Status
        datetime CreatedAt
        datetime UpdatedAt
    }

    INVENTORY_BALANCES {
        uuid Id PK
        uuid SupplierLocationId FK
        uuid SupplierOfferId FK
        decimal QuantityOnHand
        decimal QuantityReserved
        datetime UpdatedAt
    }

    INVENTORY_MOVEMENTS {
        uuid Id PK
        uuid InventoryBalanceId FK
        string MovementType
        decimal QuantityDelta
        string ReferenceType
        uuid ReferenceId
        string Notes
        datetime CreatedAt
    }

    ORDERS {
        uuid Id PK
        string OrderNumber
        uuid CustomerId FK
        string Status
        string Currency
        decimal Subtotal
        decimal DeliveryFee
        decimal DiscountAmount
        decimal TotalAmount
        string RecipientName
        string RecipientPhone
        string DeliveryAddress
        string DeliveryCity
        decimal DeliveryLatitude
        decimal DeliveryLongitude
        string CustomerNotes
        datetime CreatedAt
        datetime UpdatedAt
    }

    SUPPLIER_ORDERS {
        uuid Id PK
        uuid OrderId FK
        uuid SupplierId FK
        uuid SupplierLocationId FK
        string Status
        decimal Subtotal
        datetime CreatedAt
        datetime UpdatedAt
    }

    ORDER_ITEMS {
        uuid Id PK
        uuid SupplierOrderId FK
        uuid SupplierOfferId FK
        string ProductName
        string SellerName
        decimal Quantity
        string UnitCode
        decimal UnitPrice
        decimal LineTotal
        datetime CreatedAt
    }

    PAYMENTS {
        uuid Id PK
        uuid OrderId FK
        decimal Amount
        string Currency
        string Method
        string Status
        string ProviderReference
        string IdempotencyKey
        datetime PaidAt
        datetime CreatedAt
    }

    REFUNDS {
        uuid Id PK
        uuid PaymentId FK
        decimal Amount
        string Status
        string ProviderReference
        datetime CreatedAt
    }

    DELIVERIES {
        uuid Id PK
        uuid SupplierOrderId FK
        uuid DeliveryAgentId FK
        string Status
        datetime AssignedAt
        datetime PickedUpAt
        datetime DeliveredAt
        datetime CreatedAt
    }

    DELIVERY_STATUS_HISTORY {
        uuid Id PK
        uuid DeliveryId FK
        string Status
        string Notes
        uuid RecordedByUserId FK
        datetime CreatedAt
    }

    USERS ||--o{ SUPPLIER_MEMBERS : has
    SUPPLIERS ||--o{ SUPPLIER_MEMBERS : has

    SUPPLIERS ||--o{ SUPPLIER_LOCATIONS : operates
    CATEGORIES o|--o{ CATEGORIES : parent_of
    CATEGORIES ||--o{ PRODUCTS : classifies

    PRODUCTS ||--o{ SUPPLIER_OFFERS : offered_as
    SUPPLIERS ||--o{ SUPPLIER_OFFERS : sells

    SUPPLIER_LOCATIONS ||--o{ INVENTORY_BALANCES : stores
    SUPPLIER_OFFERS ||--o{ INVENTORY_BALANCES : stocked_as
    INVENTORY_BALANCES ||--o{ INVENTORY_MOVEMENTS : records

    USERS ||--o{ ORDERS : places
    ORDERS ||--o{ SUPPLIER_ORDERS : split_into
    SUPPLIERS ||--o{ SUPPLIER_ORDERS : fulfills
    SUPPLIER_LOCATIONS ||--o{ SUPPLIER_ORDERS : fulfills_from

    SUPPLIER_ORDERS ||--o{ ORDER_ITEMS : contains
    SUPPLIER_OFFERS ||--o{ ORDER_ITEMS : purchased_as

    ORDERS ||--o{ PAYMENTS : receives
    PAYMENTS ||--o{ REFUNDS : may_have

    SUPPLIER_ORDERS ||--o{ DELIVERIES : fulfilled_by
    USERS o|--o{ DELIVERIES : assigned_to
    DELIVERIES ||--o{ DELIVERY_STATUS_HISTORY : tracks
    USERS o|--o{ DELIVERY_STATUS_HISTORY : records
```

## Entity Responsibilities

| Entity | Responsibility |
|---|---|
| Users | Stores customer, supplier-member, and delivery-agent user accounts. |
| Suppliers | Stores supplier business information. |
| SupplierMembers | Associates users with suppliers and their membership roles. |
| SupplierLocations | Stores supplier branches or fuel station locations. |
| Categories | Organizes products into a hierarchical category structure. |
| Products | Defines the products available in the marketplace. |
| SupplierOffers | Defines which products a supplier sells and at what price. |
| InventoryBalances | Tracks stock and reserved quantities for offers at locations. |
| InventoryMovements | Records inventory quantity changes. |
| Orders | Represents the customer's overall order. |
| SupplierOrders | Represents the supplier-specific portion of an order. |
| OrderItems | Stores individual items in a supplier order. |
| Payments | Tracks payment attempts and payment status. |
| Refunds | Tracks refunds associated with payments. |
| Deliveries | Tracks delivery assignment and fulfillment. |
| DeliveryStatusHistory | Records delivery status changes. |

## Key Relationships

- A user can belong to multiple suppliers through `SupplierMembers`.
- A supplier can operate multiple locations and publish multiple offers.
- A category can contain child categories and multiple products.
- A product can have offers from different suppliers.
- Inventory is tracked for a supplier offer at a supplier location.
- A customer order can be divided into multiple supplier orders.
- Each supplier order contains order items and can have delivery records.
- An order can have multiple payment records.
- A payment can have multiple refunds.
- A delivery can have multiple status-history records.

## Implementation Notes

- Entity identifiers use `Guid`, which maps to PostgreSQL UUID values through EF Core and Npgsql.
- Nullable properties represent optional data where defined in the application models.
- Monetary amounts use decimal precision appropriate for financial values.
- Inventory quantities use decimal precision to support fractional quantities.
- `InventoryMovement.ReferenceType` and `ReferenceId` are reference metadata; they should not be interpreted as a conventional foreign-key relationship without additional configuration.
- This diagram documents the intended entity relationships from the application models. Verify EF Core configuration and applied migrations before treating it as an exact representation of the deployed database.