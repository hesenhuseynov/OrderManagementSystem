/****** Object:  Table [dbo].[Customers]    Script Date: 5/5/2026 12:35:55 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Customers](
	[CustomerId] [int] IDENTITY(1,1) NOT NULL,
	[FirstName] [nvarchar](100) NOT NULL,
	[LastName] [nvarchar](100) NOT NULL,
	[Email] [nvarchar](250) NOT NULL,
	[Phone] [nvarchar](20) NULL,
	[IsActive] [bit] NOT NULL,
	[CreatedAt] [datetime2](0) NOT NULL,
	[RowVersion] [timestamp] NOT NULL,
 CONSTRAINT [PK_Customers] PRIMARY KEY CLUSTERED 
(
	[CustomerId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[InventoryMovements]    Script Date: 5/5/2026 12:35:55 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[InventoryMovements](
	[InventoryMovementId] [int] IDENTITY(1,1) NOT NULL,
	[ProductId] [int] NOT NULL,
	[OrderId] [int] NULL,
	[MovementType] [varchar](30) NOT NULL,
	[Quantity] [int] NOT NULL,
	[CreatedAt] [datetime2](0) NOT NULL,
	[Reason] [nvarchar](200) NULL,
 CONSTRAINT [PK_InventoryMovements] PRIMARY KEY CLUSTERED 
(
	[InventoryMovementId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[OrderItems]    Script Date: 5/5/2026 12:35:55 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[OrderItems](
	[OrderItemId] [int] IDENTITY(1,1) NOT NULL,
	[OrderId] [int] NOT NULL,
	[ProductId] [int] NOT NULL,
	[Quantity] [int] NOT NULL,
	[UnitPrice] [decimal](18, 2) NOT NULL,
	[LineTotal]  AS (CONVERT([decimal](18,2),[Quantity]*[UnitPrice])) PERSISTED,
 CONSTRAINT [PK_OrderItems] PRIMARY KEY CLUSTERED 
(
	[OrderItemId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_OrderItems_OrderId_ProductId] UNIQUE NONCLUSTERED 
(
	[OrderId] ASC,
	[ProductId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Orders]    Script Date: 5/5/2026 12:35:55 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Orders](
	[OrderId] [int] IDENTITY(1,1) NOT NULL,
	[CustomerId] [int] NOT NULL,
	[OrderDate] [datetime2](0) NOT NULL,
	[Status] [varchar](30) NOT NULL,
	[Currency] [char](3) NOT NULL,
	[TotalAmount] [decimal](18, 2) NOT NULL,
	[UpdatedAt] [datetime2](0) NULL,
	[OrderNumber]  AS ('ORD-'+right(replicate('0',(8))+CONVERT([varchar](8),[OrderId]),(8))) PERSISTED,
	[RowVersion] [timestamp] NOT NULL,
 CONSTRAINT [PK_Orders] PRIMARY KEY CLUSTERED 
(
	[OrderId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[OrderStatusHistory]    Script Date: 5/5/2026 12:35:55 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[OrderStatusHistory](
	[OrderStatusHistoryId] [int] IDENTITY(1,1) NOT NULL,
	[OrderId] [int] NOT NULL,
	[OldStatus] [varchar](30) NULL,
	[NewStatus] [varchar](30) NOT NULL,
	[ChangedAt] [datetime2](0) NOT NULL,
	[ChangedBy] [nvarchar](100) NULL,
	[Reason] [nvarchar](300) NULL,
 CONSTRAINT [PK_OrderStatusHistory] PRIMARY KEY CLUSTERED 
(
	[OrderStatusHistoryId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[OutboxEvents]    Script Date: 5/5/2026 12:35:55 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[OutboxEvents](
	[OutboxEventId] [bigint] IDENTITY(1,1) NOT NULL,
	[EventId] [uniqueidentifier] NOT NULL,
	[EventType] [nvarchar](200) NOT NULL,
	[AggregateType] [nvarchar](100) NOT NULL,
	[AggregateId] [nvarchar](100) NOT NULL,
	[Payload] [nvarchar](max) NOT NULL,
	[OccurredAt] [datetime2](7) NOT NULL,
	[ProcessedAt] [datetime2](7) NULL,
	[RetryCount] [int] NOT NULL,
	[LastError] [nvarchar](max) NULL,
	[LockedAt] [datetime2](7) NULL,
	[LockId] [uniqueidentifier] NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_OutboxEvents] PRIMARY KEY CLUSTERED 
(
	[OutboxEventId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Payments]    Script Date: 5/5/2026 12:35:55 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Payments](
	[PaymentId] [int] IDENTITY(1,1) NOT NULL,
	[OrderId] [int] NOT NULL,
	[PaymentReference] [uniqueidentifier] NOT NULL,
	[Provider] [varchar](50) NOT NULL,
	[Status] [varchar](30) NOT NULL,
	[Amount] [decimal](18, 2) NOT NULL,
	[Currency] [char](3) NOT NULL,
	[IdempotencyKey] [uniqueidentifier] NOT NULL,
	[ExternalTransactionId] [nvarchar](100) NULL,
	[FailureReason] [nvarchar](500) NULL,
	[CreatedAt] [datetime2](0) NOT NULL,
	[ProcessedAt] [datetime2](0) NULL,
	[RowVersion] [timestamp] NOT NULL,
 CONSTRAINT [PK_Payments] PRIMARY KEY CLUSTERED 
(
	[PaymentId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Products]    Script Date: 5/5/2026 12:35:55 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Products](
	[ProductId] [int] IDENTITY(1,1) NOT NULL,
	[Sku] [varchar](50) NOT NULL,
	[ProductName] [nvarchar](200) NOT NULL,
	[Price] [decimal](18, 2) NOT NULL,
	[StockQuantity] [int] NOT NULL,
	[IsActive] [bit] NOT NULL,
	[CreatedAt] [datetime2](0) NOT NULL,
	[RowVersion] [timestamp] NOT NULL,
 CONSTRAINT [PK_Products] PRIMARY KEY CLUSTERED 
(
	[ProductId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UX_Customers_Email]    Script Date: 5/5/2026 12:35:55 AM ******/
CREATE UNIQUE NONCLUSTERED INDEX [UX_Customers_Email] ON [dbo].[Customers]
(
	[Email] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_InventoryMovements_OrderId]    Script Date: 5/5/2026 12:35:55 AM ******/
CREATE NONCLUSTERED INDEX [IX_InventoryMovements_OrderId] ON [dbo].[InventoryMovements]
(
	[OrderId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_InventoryMovements_ProductId_CreatedAt]    Script Date: 5/5/2026 12:35:55 AM ******/
CREATE NONCLUSTERED INDEX [IX_InventoryMovements_ProductId_CreatedAt] ON [dbo].[InventoryMovements]
(
	[ProductId] ASC,
	[CreatedAt] DESC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_OrderItems_OrderId]    Script Date: 5/5/2026 12:35:55 AM ******/
CREATE NONCLUSTERED INDEX [IX_OrderItems_OrderId] ON [dbo].[OrderItems]
(
	[OrderId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_OrderItems_ProductId]    Script Date: 5/5/2026 12:35:55 AM ******/
CREATE NONCLUSTERED INDEX [IX_OrderItems_ProductId] ON [dbo].[OrderItems]
(
	[ProductId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Orders_CustomerId_OrderDate]    Script Date: 5/5/2026 12:35:55 AM ******/
CREATE NONCLUSTERED INDEX [IX_Orders_CustomerId_OrderDate] ON [dbo].[Orders]
(
	[CustomerId] ASC,
	[OrderDate] DESC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_Orders_Status]    Script Date: 5/5/2026 12:35:55 AM ******/
CREATE NONCLUSTERED INDEX [IX_Orders_Status] ON [dbo].[Orders]
(
	[Status] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ARITHABORT ON
SET CONCAT_NULL_YIELDS_NULL ON
SET QUOTED_IDENTIFIER ON
SET ANSI_NULLS ON
SET ANSI_PADDING ON
SET ANSI_WARNINGS ON
SET NUMERIC_ROUNDABORT OFF
GO
/****** Object:  Index [UX_Orders_OrderNumber]    Script Date: 5/5/2026 12:35:55 AM ******/
CREATE UNIQUE NONCLUSTERED INDEX [UX_Orders_OrderNumber] ON [dbo].[Orders]
(
	[OrderNumber] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_OrderStatusHistory_OrderId]    Script Date: 5/5/2026 12:35:55 AM ******/
CREATE NONCLUSTERED INDEX [IX_OrderStatusHistory_OrderId] ON [dbo].[OrderStatusHistory]
(
	[OrderId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_OrderStatusHistory_OrderId_ChangedAt]    Script Date: 5/5/2026 12:35:55 AM ******/
CREATE NONCLUSTERED INDEX [IX_OrderStatusHistory_OrderId_ChangedAt] ON [dbo].[OrderStatusHistory]
(
	[OrderId] ASC,
	[ChangedAt] DESC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_OutboxEvents_Aggregate]    Script Date: 5/5/2026 12:35:55 AM ******/
CREATE NONCLUSTERED INDEX [IX_OutboxEvents_Aggregate] ON [dbo].[OutboxEvents]
(
	[AggregateType] ASC,
	[AggregateId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_OutboxEvents_Unprocessed]    Script Date: 5/5/2026 12:35:55 AM ******/
CREATE NONCLUSTERED INDEX [IX_OutboxEvents_Unprocessed] ON [dbo].[OutboxEvents]
(
	[ProcessedAt] ASC,
	[OccurredAt] ASC
)
WHERE ([ProcessedAt] IS NULL)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [UX_OutboxEvents_EventId]    Script Date: 5/5/2026 12:35:55 AM ******/
CREATE UNIQUE NONCLUSTERED INDEX [UX_OutboxEvents_EventId] ON [dbo].[OutboxEvents]
(
	[EventId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Payments_OrderId_CreatedAt]    Script Date: 5/5/2026 12:35:55 AM ******/
CREATE NONCLUSTERED INDEX [IX_Payments_OrderId_CreatedAt] ON [dbo].[Payments]
(
	[OrderId] ASC,
	[CreatedAt] DESC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_Payments_Status]    Script Date: 5/5/2026 12:35:55 AM ******/
CREATE NONCLUSTERED INDEX [IX_Payments_Status] ON [dbo].[Payments]
(
	[Status] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UX_Payments_ExternalTransactionId]    Script Date: 5/5/2026 12:35:55 AM ******/
CREATE UNIQUE NONCLUSTERED INDEX [UX_Payments_ExternalTransactionId] ON [dbo].[Payments]
(
	[ExternalTransactionId] ASC
)
WHERE ([ExternalTransactionId] IS NOT NULL)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [UX_Payments_IdempotencyKey]    Script Date: 5/5/2026 12:35:55 AM ******/
CREATE UNIQUE NONCLUSTERED INDEX [UX_Payments_IdempotencyKey] ON [dbo].[Payments]
(
	[IdempotencyKey] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UX_Products_Sku]    Script Date: 5/5/2026 12:35:55 AM ******/
CREATE UNIQUE NONCLUSTERED INDEX [UX_Products_Sku] ON [dbo].[Products]
(
	[Sku] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [dbo].[Customers] ADD  CONSTRAINT [DF_Customers_IsActive]  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [dbo].[Customers] ADD  CONSTRAINT [DF_Customers_CreatedAt]  DEFAULT (sysutcdatetime()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[InventoryMovements] ADD  CONSTRAINT [DF_InventoryMovements_CreatedAt]  DEFAULT (sysutcdatetime()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[Orders] ADD  CONSTRAINT [DF_Orders_OrderDate]  DEFAULT (sysutcdatetime()) FOR [OrderDate]
GO
ALTER TABLE [dbo].[Orders] ADD  CONSTRAINT [DF_Orders_Status]  DEFAULT ('Pending') FOR [Status]
GO
ALTER TABLE [dbo].[Orders] ADD  CONSTRAINT [DF_Orders_Currency]  DEFAULT ('AZN') FOR [Currency]
GO
ALTER TABLE [dbo].[Orders] ADD  CONSTRAINT [DF_Orders_TotalAmount]  DEFAULT ((0)) FOR [TotalAmount]
GO
ALTER TABLE [dbo].[OrderStatusHistory] ADD  CONSTRAINT [DF_OrderStatusHistory_ChangedAt]  DEFAULT (sysutcdatetime()) FOR [ChangedAt]
GO
ALTER TABLE [dbo].[OutboxEvents] ADD  CONSTRAINT [DF_OutboxEvents_OccurredAt]  DEFAULT (sysutcdatetime()) FOR [OccurredAt]
GO
ALTER TABLE [dbo].[OutboxEvents] ADD  CONSTRAINT [DF_OutboxEvents_RetryCount]  DEFAULT ((0)) FOR [RetryCount]
GO
ALTER TABLE [dbo].[OutboxEvents] ADD  CONSTRAINT [DF_OutboxEvents_CreatedAt]  DEFAULT (sysutcdatetime()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[Payments] ADD  CONSTRAINT [DF_Payments_PaymentReference]  DEFAULT (newsequentialid()) FOR [PaymentReference]
GO
ALTER TABLE [dbo].[Payments] ADD  CONSTRAINT [DF_Payments_Status]  DEFAULT ('Pending') FOR [Status]
GO
ALTER TABLE [dbo].[Payments] ADD  CONSTRAINT [DF_Payments_Currency]  DEFAULT ('AZN') FOR [Currency]
GO
ALTER TABLE [dbo].[Payments] ADD  CONSTRAINT [DF_Payments_CreatedAt]  DEFAULT (sysutcdatetime()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[Products] ADD  CONSTRAINT [DF_Products_IsActive]  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [dbo].[Products] ADD  CONSTRAINT [DF_Products_CreatedAt]  DEFAULT (sysutcdatetime()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[InventoryMovements]  WITH CHECK ADD  CONSTRAINT [FK_InventoryMovements_Orders] FOREIGN KEY([OrderId])
REFERENCES [dbo].[Orders] ([OrderId])
GO
ALTER TABLE [dbo].[InventoryMovements] CHECK CONSTRAINT [FK_InventoryMovements_Orders]
GO
ALTER TABLE [dbo].[InventoryMovements]  WITH CHECK ADD  CONSTRAINT [FK_InventoryMovements_Products] FOREIGN KEY([ProductId])
REFERENCES [dbo].[Products] ([ProductId])
GO
ALTER TABLE [dbo].[InventoryMovements] CHECK CONSTRAINT [FK_InventoryMovements_Products]
GO
ALTER TABLE [dbo].[OrderItems]  WITH CHECK ADD  CONSTRAINT [FK_OrderItems_Orders] FOREIGN KEY([OrderId])
REFERENCES [dbo].[Orders] ([OrderId])
GO
ALTER TABLE [dbo].[OrderItems] CHECK CONSTRAINT [FK_OrderItems_Orders]
GO
ALTER TABLE [dbo].[OrderItems]  WITH CHECK ADD  CONSTRAINT [FK_OrderItems_Products] FOREIGN KEY([ProductId])
REFERENCES [dbo].[Products] ([ProductId])
GO
ALTER TABLE [dbo].[OrderItems] CHECK CONSTRAINT [FK_OrderItems_Products]
GO
ALTER TABLE [dbo].[Orders]  WITH CHECK ADD  CONSTRAINT [FK_Orders_Customers] FOREIGN KEY([CustomerId])
REFERENCES [dbo].[Customers] ([CustomerId])
GO
ALTER TABLE [dbo].[Orders] CHECK CONSTRAINT [FK_Orders_Customers]
GO
ALTER TABLE [dbo].[OrderStatusHistory]  WITH CHECK ADD  CONSTRAINT [FK_OrderStatusHistory_Orders] FOREIGN KEY([OrderId])
REFERENCES [dbo].[Orders] ([OrderId])
GO
ALTER TABLE [dbo].[OrderStatusHistory] CHECK CONSTRAINT [FK_OrderStatusHistory_Orders]
GO
ALTER TABLE [dbo].[Payments]  WITH CHECK ADD  CONSTRAINT [FK_Payments_Orders] FOREIGN KEY([OrderId])
REFERENCES [dbo].[Orders] ([OrderId])
GO
ALTER TABLE [dbo].[Payments] CHECK CONSTRAINT [FK_Payments_Orders]
GO
ALTER TABLE [dbo].[InventoryMovements]  WITH CHECK ADD  CONSTRAINT [CK_InventoryMovements_MovementType] CHECK  (([MovementType]='Adjust' OR [MovementType]='Restock' OR [MovementType]='Deduct' OR [MovementType]='Reserve'))
GO
ALTER TABLE [dbo].[InventoryMovements] CHECK CONSTRAINT [CK_InventoryMovements_MovementType]
GO
ALTER TABLE [dbo].[InventoryMovements]  WITH CHECK ADD  CONSTRAINT [CK_InventoryMovements_Quantity_Positive] CHECK  (([Quantity]>(0)))
GO
ALTER TABLE [dbo].[InventoryMovements] CHECK CONSTRAINT [CK_InventoryMovements_Quantity_Positive]
GO
ALTER TABLE [dbo].[OrderItems]  WITH CHECK ADD  CONSTRAINT [CK_OrderItems_Quantity_Positive] CHECK  (([Quantity]>(0)))
GO
ALTER TABLE [dbo].[OrderItems] CHECK CONSTRAINT [CK_OrderItems_Quantity_Positive]
GO
ALTER TABLE [dbo].[OrderItems]  WITH CHECK ADD  CONSTRAINT [CK_OrderItems_UnitPrice_NonNegative] CHECK  (([UnitPrice]>=(0)))
GO
ALTER TABLE [dbo].[OrderItems] CHECK CONSTRAINT [CK_OrderItems_UnitPrice_NonNegative]
GO
ALTER TABLE [dbo].[Orders]  WITH CHECK ADD  CONSTRAINT [CK_Orders_Status] CHECK  (([Status]='Completed' OR [Status]='Cancelled' OR [Status]='Paid' OR [Status]='Pending'))
GO
ALTER TABLE [dbo].[Orders] CHECK CONSTRAINT [CK_Orders_Status]
GO
ALTER TABLE [dbo].[Orders]  WITH CHECK ADD  CONSTRAINT [CK_Orders_TotalAmount_NonNegative] CHECK  (([TotalAmount]>=(0)))
GO
ALTER TABLE [dbo].[Orders] CHECK CONSTRAINT [CK_Orders_TotalAmount_NonNegative]
GO
ALTER TABLE [dbo].[OrderStatusHistory]  WITH CHECK ADD  CONSTRAINT [CK_OrderStatusHistory_NewStatus] CHECK  (([NewStatus]='Completed' OR [NewStatus]='Cancelled' OR [NewStatus]='Paid' OR [NewStatus]='Pending'))
GO
ALTER TABLE [dbo].[OrderStatusHistory] CHECK CONSTRAINT [CK_OrderStatusHistory_NewStatus]
GO
ALTER TABLE [dbo].[OrderStatusHistory]  WITH CHECK ADD  CONSTRAINT [CK_OrderStatusHistory_OldStatus] CHECK  (([OldStatus] IS NULL OR ([OldStatus]='Completed' OR [OldStatus]='Cancelled' OR [OldStatus]='Paid' OR [OldStatus]='Pending')))
GO
ALTER TABLE [dbo].[OrderStatusHistory] CHECK CONSTRAINT [CK_OrderStatusHistory_OldStatus]
GO
ALTER TABLE [dbo].[Payments]  WITH CHECK ADD  CONSTRAINT [CK_Payments_Amount_Positive] CHECK  (([Amount]>(0)))
GO
ALTER TABLE [dbo].[Payments] CHECK CONSTRAINT [CK_Payments_Amount_Positive]
GO
ALTER TABLE [dbo].[Payments]  WITH CHECK ADD  CONSTRAINT [CK_Payments_Status] CHECK  (([Status]='Cancelled' OR [Status]='Refunded' OR [Status]='Failed' OR [Status]='Succeeded' OR [Status]='Authorized' OR [Status]='Pending'))
GO
ALTER TABLE [dbo].[Payments] CHECK CONSTRAINT [CK_Payments_Status]
GO
ALTER TABLE [dbo].[Products]  WITH CHECK ADD  CONSTRAINT [CK_Products_Price_NonNegative] CHECK  (([Price]>=(0)))
GO
ALTER TABLE [dbo].[Products] CHECK CONSTRAINT [CK_Products_Price_NonNegative]
GO
ALTER TABLE [dbo].[Products]  WITH CHECK ADD  CONSTRAINT [CK_Products_StockQuantity_NonNegative] CHECK  (([StockQuantity]>=(0)))
GO
ALTER TABLE [dbo].[Products] CHECK CONSTRAINT [CK_Products_StockQuantity_NonNegative]
GO
