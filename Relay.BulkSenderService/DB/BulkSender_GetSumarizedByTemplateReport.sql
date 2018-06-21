CREATE PROCEDURE [dbo].[BulkSender_GetSumarizedByTemplateReport] @StartDate DATETIME
	,@EndDate DATETIME
	,@UserId INT
AS
BEGIN
	SELECT t.id
		,t.Name
		,t.Guid
		,t.FromEmail
		,t.FromName
		,tc.Subject
		,isnull(Deliveries.SentCount, 0) AS TotalSentCount
		,isnull(Deliveries.RetryCount, 0) AS TotalRetriesCount
		,isnull(Opens.TotalCount, 0) AS TotalOpensCount
		,isnull(Opens.UniqueCount, 0) AS UniqueOpensCount
		,Opens.LastOpen
		,isnull(Clicks.TotalCount, 0) AS TotalClicksCount
		,isnull(Clicks.UniqueCount, 0) AS UniqueClicksCount
		,Clicks.LastClick
		,isnull(Unsubscriptions.TotalCount, 0) AS TotalUnsubscriptionsCount
		,isnull(Bounces.HardCount, 0) AS HardBouncesCount
		,isnull(Bounces.SoftCount, 0) AS SoftBouncesCount
	FROM Template t
	JOIN TemplateContent tc ON t.TemplateContentId = tc.Id
	OUTER APPLY (
		SELECT count(CASE 
					WHEN d.Status = 1
						THEN 1
					ELSE NULL
					END) AS SentCount
			,sum(d.RetryCount) AS RetryCount
		FROM Delivery d
		JOIN Message m ON d.MessageId = m.Id
		WHERE m.TemplateId = t.Id
			AND m.UserId = @UserId
			AND d.UserId = @UserId
			AND d.CreatedAt >= @StartDate
			AND d.CreatedAt <= @EndDate
		) Deliveries
	OUTER APPLY (
		SELECT count(*) AS TotalCount
			,count(DISTINCT d.id) AS UniqueCount
			,max(oe.CreatedAt) LastOpen
		FROM Delivery d
		JOIN Message m ON d.MessageId = m.Id
		JOIN OpenEvent oe ON d.Id = oe.DeliveryId
			AND oe.MessageId = m.id
			AND oe.UserId = @UserId
		WHERE m.TemplateId = t.Id
			AND m.UserId = @UserId
			AND d.UserId = @UserId
			AND d.CreatedAt >= @StartDate
			AND d.CreatedAt <= @EndDate
		) Opens
	OUTER APPLY (
		SELECT count(*) AS TotalCount
			,count(DISTINCT dl.Id) UniqueCount
			,max(ce.CreatedAt) LastClick
		FROM DeliveryLink dl
		JOIN Delivery d ON dl.DeliveryId = d.Id
		JOIN Message m ON d.MessageId = m.Id
		JOIN ClickEvent ce ON dl.Id = ce.DeliveryLinkId
			AND ce.MessageId = m.Id
			AND ce.UserId = @UserId
		WHERE m.TemplateId = t.Id
			AND m.UserId = @UserId
			AND d.UserId = @UserId
			AND d.CreatedAt >= @StartDate
			AND d.CreatedAt <= @EndDate
		) Clicks
	OUTER APPLY (
		SELECT count(*) AS TotalCount
		FROM Delivery d
		JOIN Message m ON d.MessageId = m.Id
		JOIN UnsubscriptionEvent ue ON d.Id = ue.DeliveryId
		WHERE m.TemplateId = t.Id
			AND m.UserId = @UserId
			AND d.UserId = @UserId
			AND d.CreatedAt >= @StartDate
			AND d.CreatedAt <= @EndDate
		) Unsubscriptions
	OUTER APPLY (
		SELECT count(DISTINCT CASE 
					WHEN be.IsHard = 1
						THEN d.id
					ELSE NULL
					END) AS HardCount
			,count(DISTINCT CASE 
					WHEN be.IsHard = 0
						THEN d.id
					ELSE NULL
					END) AS SoftCount
		FROM Delivery d
		JOIN Message m ON d.MessageId = m.Id
		JOIN BounceEvent be ON d.Id = be.DeliveryId
		WHERE m.TemplateId = t.Id
			AND m.UserId = @UserId
			AND d.UserId = @UserId
			AND d.CreatedAt >= @StartDate
			AND d.CreatedAt <= @EndDate
			AND d.Status IN (
				2
				,4
				)
			AND be.IsHard = 1
		) Bounces
	WHERE t.UserId = @UserId
END
