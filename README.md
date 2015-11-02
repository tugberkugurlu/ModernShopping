# ModernShopping: Modern Software Application Sample

When you look at the sample applications out there today, you will see that they are pretty simple and possibly only focuses on one bit of concept or technology. This is really good to get your head around it at first but after a while, you may struggle to get everything together and produce a lovely solution.

**ModernShopping** targets to provide a fully fledged solution for a shopping company like Amazon.

## Main Objectives

This application will be built several objectives in mind:

 - HTTP interface first approach
 - Microservices-like approach
 - Polyglot persistence
 - Centralized logging
 - Top-notch deployment, release and versioning story
 - Multiple clients (Web, iPhone, Android, etc.)
 - Not-only-one architectural pattern

## Approach to Take

The below is the prefered approach to take to build this project:

 - Authentication/Identity: We need to identify users in our system and persist the identity. Good case for relational data storage system like SQL Server.
 - View: We should be able to view the products on a client. Incredibly sensible for MongoDB.
 - Search: We should be able to search for products.
 - Shopping Cart: User should be able to fill in their shopping cart. Perfect case for event store!
 - Order: Whole point of the business. Without the orders, we are NOTHING! Good case for asyncronous processing (stock limit, price changes, etc.).

### Cross Cutting Concerns

 - Analytics: This is important bit so that
     - 1-) we can approach people with recommendations. Good case for a graph database.
     - 2-) we can identify the application usage. Good case for Google analytics.

### Going Beyond
 - Product Comments: Make users comment on products and identify users with legit comments for the stuff that they have bought.
