Below is a simplified Markdown-style development plan based on the uploaded fuel supply SOP. It focuses on functional requirements, core modules, workflows, and implementation priorities, rather than reproducing the operational SOP.

# Instafuel – Development Plan

## 1. Project Overview

Build an on-demand fuel ordering and delivery platform supporting:

- Octane
- Petrol
- Diesel

**Business Model:** Fuel supplier/operator with its own delivery fleet.

The platform should support individual customers, corporate customers, multiple suppliers, inventory management, order processing, payments, and delivery operations.

## 2. Core Modules

### 2.1 User & Customer Management

- Customer registration and authentication.
- Individual and corporate customer profiles.
- Multiple delivery addresses per customer.
- Default delivery address.
- Contact and recipient information.
- Customer verification details.
- Corporate billing information.
- Customer account status.

### 2.2 Product & Pricing Management

- Manage fuel products (Octane, Petrol, Diesel).
- Product-wise pricing.
- Centralized price management.
- Price effective dates and validity.
- Delivery charges based on distance or delivery zones.
- Minimum order quantity/value.
- Discounts and additional charges.

### 2.3 Supplier Management

- Supplier registration and management.
- Supplier profiles and contact information.
- Supplier-specific product availability.
- Supplier offers and pricing.
- Supplier order management.
- Supplier performance tracking.

### 2.4 Inventory Management

- Track available stock by product and supplier/location.
- Record stock purchases and replenishment.
- Record inventory movements.
- Track stock received, dispatched, and adjusted.
- Maintain inventory history.
- Stock availability validation before order confirmation.
- Low-stock alerts and reorder thresholds.
- Inventory reconciliation.

### 2.5 Order Management

- Create fuel orders.
- Select product and quantity.
- Select delivery address.
- Select requested delivery date and time.
- Validate minimum order requirements.
- Validate stock availability.
- Calculate fuel charges and delivery fees.
- Apply applicable discounts and taxes.
- Generate order confirmation.
- Support order cancellation and rescheduling.
- Maintain order status history.

**Order lead-time requirement:** Orders must be placed at least 24 hours before the requested delivery time.

### 2.6 Payment & Billing

- Support digital payments.
- Support approved corporate credit accounts.
- Generate invoices.
- Track payment status.
- Handle refunds and credit notes.
- Maintain transaction history.
- Track corporate credit limits and outstanding balances.

### 2.7 Fleet & Delivery Management

- Manage delivery vehicles.
- Manage drivers and delivery personnel.
- Assign deliveries to vehicles and drivers.
- Plan delivery routes.
- Group deliveries geographically.
- Track delivery assignments.
- Monitor delivery status.
- Record actual delivered quantities.
- Capture delivery confirmation.
- Generate delivery receipts.
- Handle failed deliveries and rescheduling.

**Delivery statuses:**

- Pending
- Assigned
- Dispatched
- In Transit
- Delivered
- Failed
- Cancelled

### 2.8 Notifications & Customer Support

- Order confirmation notifications.
- Payment confirmation notifications.
- Dispatch notifications.
- Delivery status updates.
- Delay and cancellation notifications.
- Customer complaints and support requests.

### 2.9 Admin & Access Management

Implement role-based access control.

Required roles:

- Super Admin
- Operations Manager
- Procurement Officer
- Inventory Manager
- Dispatch Officer
- Driver / Delivery Personnel
- Finance Manager
- Customer Support
- Customer

Each role should have appropriate permissions for its responsibilities.

### 2.10 Reporting & Dashboard

Provide operational dashboards covering:

- Total orders.
- Completed, pending, and failed orders.
- Fuel sales by product.
- Revenue and collections.
- Inventory levels.
- Inventory discrepancies.
- Vehicle utilization.
- Delivery performance.
- Customer activity.
- Supplier performance.

## 3. Core Business Workflows

### Customer Order Workflow

1. Customer selects fuel product.
2. Customer specifies quantity.
3. Customer selects delivery address.
4. Customer selects delivery date and time.
5. System validates the 24-hour lead time.
6. System checks minimum order requirements and availability.
7. System calculates fuel price and delivery charges.
8. Customer confirms the order.
9. System processes payment or validates approved credit.
10. Order is confirmed.

### Procurement & Inventory Workflow

1. Monitor available inventory.
2. Identify replenishment requirements.
3. Create supplier purchase orders.
4. Record received stock.
5. Update inventory balances.
6. Maintain inventory movement history.
7. Trigger low-stock notifications.

### Delivery Workflow

1. Retrieve confirmed orders.
2. Group orders based on delivery locations.
3. Assign drivers and vehicles.
4. Generate delivery assignments.
5. Mark orders as dispatched.
6. Update delivery progress.
7. Confirm customer identity and delivery location.
8. Record actual delivered quantity.
9. Capture customer acknowledgement.
10. Complete the delivery and generate a receipt.

## 4. Important Business Rules

- Orders require at least 24 hours' advance notice.
- Orders cannot be confirmed without sufficient available stock.
- Minimum order requirements must be configurable.
- Fuel prices must be centrally managed.
- Delivery charges must be calculated using configurable distance or zone rules.
- Inventory changes must maintain an auditable movement history.
- Deliveries must be assigned to authorized drivers and vehicles.
- Actual delivered quantities must be recorded.
- Refunds, price changes, and inventory adjustments require appropriate authorization.
- Corporate credit orders must respect approved credit limits.
- Customers must not be able to access other customers' information.

## 5. Development Priorities

### Phase 1 – Foundation

- Authentication and authorization.
- Customer management.
- Address management.
- Product management.
- Supplier management.
- Database entities and relationships.

### Phase 2 – Core Transactions

- Inventory management.
- Supplier procurement.
- Pricing management.
- Order creation and validation.
- Order confirmation.
- Payment integration.

### Phase 3 – Delivery Operations

- Fleet management.
- Driver management.
- Delivery assignment.
- Route planning.
- Delivery status tracking.
- Delivery confirmation and receipts.

### Phase 4 – Administration & Reporting

- Admin dashboard.
- Role-based permissions.
- Notifications.
- Customer support.
- Operational reports.
- Audit logs.

## 6. Development Guidelines

- Build incrementally, module by module.
- Keep business logic separate from controllers.
- Use proper entity relationships and foreign keys.
- Maintain inventory movement and order status histories.
- Use database transactions for critical operations.
- Ensure order and inventory updates remain consistent.
- Avoid unnecessary microservices or premature architectural complexity.
- Keep business rules configurable wherever possible.
- Generate and review EF Core migrations incrementally.
- Include validation and error handling in every module.

## 7. Implementation Instructions for Claude

Before implementing each module:

1. Review the existing project structure and database entities.
2. Identify dependencies on existing modules.
3. Propose the required entities, relationships, and business rules.
4. Identify missing requirements or ambiguous decisions.
5. Implement only after the design is reviewed.
6. Generate migrations where database changes are required.
7. Provide an explanation of the implementation and testing steps.

**Important:** Do not assume unspecified business rules. Keep uncertain values such as minimum order quantity, delivery tariffs, cancellation fees, and credit policies configurable until they are finalized.

Add concrete entity and API boundariesDefine order and inventory state transitions