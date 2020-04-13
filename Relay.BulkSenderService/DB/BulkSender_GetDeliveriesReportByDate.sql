CREATE PROCEDURE [dbo].[BulkSender_GetDeliveriesReportByDate] @UserId INT
	,@StartDate DATETIME
	,@EndDate DATETIME
AS
BEGIN
	SELECT xx.Id
		,xx.DeliveryGuid
		,xx.CreatedAt
		,xx.Status
		,xx.ClickEventsCount
		,xx.OpenEventsCount
		,isnull(xx.SentAt, xx.CreatedAt) AS SentAt
		,xx.MessageId
		,xx.RecipientId
		,m.FromEmail
		,m.FromName
		,m.Subject
		,m.Guid
		,ea.Address
		,isnull(MAX(xx.IsHard + 0), 0) AS IsHard
		,isnull(MAX(xx.MailStatus), 0) AS MailStatus
		,isnull(MAX(xx.BounceDate), xx.CreatedAt) AS BounceDate
		,isnull(MAX(xx.OpenDate), xx.CreatedAt) AS OpenDate
		,isnull(MAX(xx.ClickDate), xx.CreatedAt) AS ClickDate
	FROM (
		SELECT d.Id
			,d.Guid AS DeliveryGuid
			,d.CreatedAt
			,d.Status
			,d.ClickEventsCount
			,d.OpenEventsCount
			,isnull(d.SentAt, d.CreatedAt) AS SentAt
			,d.MessageId
			,d.RecipientId
			,x.IsHard
			,x.MailStatus
			,x.CreatedAt AS BounceDate
			,NULL AS OpenDate
			,NULL AS ClickDate
		FROM (
			SELECT b.IsHard
				,b.MailStatus
				,b.CreatedAt
				,b.DeliveryId
				,ROW_NUMBER() OVER (
					PARTITION BY b.DeliveryId ORDER BY b.createdAt DESC
					) rownumber
			FROM BounceEvent b
			WHERE b.UserId = @UserId
				AND b.CreatedAt BETWEEN @StartDate
					AND @EndDate
			) x
		JOIN dbo.Delivery d ON d.id = x.DeliveryId
		WHERE x.rownumber = 1
		
		UNION
		
		SELECT d.Id
			,d.Guid AS DeliveryGuid
			,d.CreatedAt
			,d.Status
			,d.ClickEventsCount
			,d.OpenEventsCount
			,isnull(d.SentAt, d.CreatedAt) AS SentAt
			,d.MessageId
			,d.RecipientId
			,NULL AS IsHard
			,NULL AS MailStatus
			,NULL AS BounceEventDate
			,NULL AS OpenDate
			,x.CreatedAt AS ClickDate
		FROM (
			SELECT c.CreatedAt
				,c.DeliveryId
				,ROW_NUMBER() OVER (
					PARTITION BY c.DeliveryId ORDER BY c.createdAt DESC
					) rownumber
			FROM ClickEvent c
			WHERE c.UserId = @UserId
				AND c.CreatedAt BETWEEN @StartDate
					AND @EndDate
			) x
		JOIN dbo.Delivery d ON d.id = x.DeliveryId
		WHERE x.rownumber = 1
		
		UNION
		
		SELECT d.Id
			,d.Guid AS DeliveryGuid
			,d.CreatedAt
			,d.Status
			,d.ClickEventsCount
			,d.OpenEventsCount
			,isnull(d.SentAt, d.CreatedAt) AS SentAt
			,d.MessageId
			,d.RecipientId
			,NULL AS IsHard
			,NULL AS MailStatus
			,NULL AS BounceEventDate
			,x.CreatedAt AS OpenDate
			,NULL AS ClickDate
		FROM (
			SELECT o.CreatedAt
				,o.DeliveryId
				,ROW_NUMBER() OVER (
					PARTITION BY o.DeliveryId ORDER BY o.createdAt DESC
					) rownumber
			FROM OpenEvent o
			WHERE o.UserId = @UserId
				AND o.CreatedAt BETWEEN @StartDate
					AND @EndDate
			) x
		JOIN dbo.Delivery d ON d.id = x.DeliveryId
		WHERE x.rownumber = 1
		
		UNION
		
		SELECT d.Id
			,d.Guid AS DeliveryGuid
			,d.CreatedAt
			,d.Status
			,d.ClickEventsCount
			,d.OpenEventsCount
			,isnull(d.SentAt, d.CreatedAt) AS SentAt
			,d.MessageId
			,d.RecipientId
			,NULL AS IsHard
			,NULL AS MailStatus
			,NULL AS BounceEventDate
			,NULL AS OpenDate
			,NULL AS ClickDate
		FROM dbo.Delivery d
		WHERE d.UserId = @UserId
			AND (
				d.CreatedAt BETWEEN @StartDate
					AND @EndDate
				OR (
					d.SentAt IS NOT NULL
					AND d.SentAt BETWEEN @StartDate
						AND @EndDate
					)
				)
		) xx
	JOIN Message m ON xx.MessageId = m.Id
	JOIN EmailAddress ea ON xx.RecipientId = ea.Id
	GROUP BY xx.Id
		,xx.DeliveryGuid
		,xx.CreatedAt
		,xx.Status
		,xx.ClickEventsCount
		,xx.OpenEventsCount
		,isnull(xx.SentAt, xx.CreatedAt)
		,xx.MessageId
		,xx.RecipientId
		,m.FromEmail
		,m.FromName
		,m.Subject
		,m.Guid
		,ea.Address
	ORDER BY xx.id
	OPTION (FORCE ORDER)
END
GO


