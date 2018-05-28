CREATE PROCEDURE [dbo].[BulkSender_GetClicksReport] @Guids GuidList readonly
	,@UserId INT
AS
BEGIN
	SELECT d.Id
		,d.CreatedAt
		,d.Status
		,d.ClickEventsCount
		,d.OpenEventsCount
		,m.FromEmail
		,m.FromName
		,m.Subject
		,m.Guid
		,ea.Address
		,ce.CreatedAt AS ClickDate
		,l.Url
	FROM Delivery d
	JOIN EmailAddress ea ON d.RecipientId = ea.Id
	JOIN Message m ON d.MessageId = m.Id
	JOIN @Guids g ON m.[Guid] = g.[Guid]
	JOIN ClickEvent ce ON d.Id = ce.DeliveryId
	JOIN Link l ON ce.LinkId = l.Id
	WHERE d.UserId = @UserId
		AND m.UserId = @UserId
END
