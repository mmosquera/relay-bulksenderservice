CREATE PROCEDURE [dbo].[BulkSender_GetDeliveriesReportByDate] @UserId INT
	,@StartDate DATETIME
	,@EndDate DATETIME
AS
BEGIN
	SELECT d.Id
		,d.CreatedAt
		,d.Status
		,d.ClickEventsCount
		,d.OpenEventsCount
		,isnull(d.SentAt, d.CreatedAt) AS SentAt
		,m.FromEmail
		,m.FromName
		,m.Subject
		,m.Guid		
		,ea.Address
		,isnull(be.IsHard, 0) AS IsHard
		,isnull(be.MailStatus, 0) AS MailStatus
		,isnull(be.CreatedAt, d.CreatedAt) AS BounceDate
		,isnull(oe.CreatedAt, d.CreatedAt) AS OpenDate
		,isnull(ce.CreatedAt, d.CreatedAt) AS ClickDate		
	FROM Delivery d
	JOIN EmailAddress ea ON d.RecipientId = ea.Id
	JOIN Message m ON d.MessageId = m.Id	
	OUTER APPLY (
		SELECT TOP 1 b.IsHard
			,b.MailStatus
			,b.CreatedAt
		FROM BounceEvent b
		WHERE b.DeliveryId = d.Id
		ORDER BY b.CreatedAt DESC
		) be
	OUTER APPLY (
		SELECT TOP 1 o.CreatedAt
		FROM OpenEvent o
		WHERE o.DeliveryId = d.Id
		ORDER BY o.CreatedAt DESC
		) oe
	OUTER APPLY (
		SELECT TOP 1 c.CreatedAt
		FROM ClickEvent c
		WHERE c.DeliveryId = d.Id
		ORDER BY c.CreatedAt DESC
		) ce
	WHERE d.UserId = @UserId
		AND m.UserId = @UserId
		AND d.CreatedAt >= @StartDate
		AND d.CreatedAt < @EndDate
END
GO
