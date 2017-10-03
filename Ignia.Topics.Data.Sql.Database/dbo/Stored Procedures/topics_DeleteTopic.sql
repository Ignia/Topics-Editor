﻿-----------------------------------------------------------------------------------------------------------------------------------------------
-- Procedure	DELETE TOPIC
--
-- Purpose	Deletes a topic in the tree, including all child topics.
--
-- History	John Mulhausen		04062009  Created initial version.
--		Jeremy Caney		05282010  Reformatted code and refactored identifiers for improved readability.
-----------------------------------------------------------------------------------------------------------------------------------------------

CREATE PROCEDURE [dbo].[topics_DeleteTopic]
		@TopicID		INT
AS

-----------------------------------------------------------------------------------------------------------------------------------------------
-- DECLARE AND SET VARIABLES
-----------------------------------------------------------------------------------------------------------------------------------------------
DECLARE		@RangeLeft		INT
DECLARE		@RangeRight		INT
DECLARE		@RangeWidth		INT

-- SET NOCOUNT ON added to prevent extra result sets from interfering with SELECT statements.
SET NOCOUNT ON;

-----------------------------------------------------------------------------------------------------------------------------------------------
-- DEFINE RANGE TO DELETE
-----------------------------------------------------------------------------------------------------------------------------------------------
SELECT		@RangeLeft		= RangeLeft,
		@RangeRight		= RangeRight,
		@RangeWidth		= RangeRight - RangeLeft + 1
FROM		topics_Topics
WHERE		TopicID			= @TopicID;

-----------------------------------------------------------------------------------------------------------------------------------------------
-- DELETE RELATED ATTRIBUTES
-----------------------------------------------------------------------------------------------------------------------------------------------
DELETE
FROM		topics_TopicAttributes
WHERE		TopicID IN (
  SELECT	TopicID
  FROM		topics_Topics
  WHERE		RangeLeft
    BETWEEN	@RangeLeft
    AND		@RangeRight
)

DELETE
FROM		topics_Blob
WHERE		TopicID In (
  SELECT	TopicID
  FROM		topics_Topics
  WHERE		RangeLeft
    BETWEEN	@RangeLeft
    AND		@RangeRight
)

DELETE
FROM		topics_Relationships
WHERE		Source_TopicID IN (
  SELECT	TopicID
  FROM		topics_Topics
  WHERE		RangeLeft
    BETWEEN	@RangeLeft
    AND		@RangeRight
)
OR		Target_TopicID IN (
  SELECT	TopicID
  FROM		topics_Topics
  WHERE		RangeLeft
    BETWEEN	@RangeLeft
    AND		@RangeRight
)

-----------------------------------------------------------------------------------------------------------------------------------------------
-- DELETE RANGE
-----------------------------------------------------------------------------------------------------------------------------------------------
DELETE
FROM		topics_Topics
WHERE		RangeLeft
  BETWEEN	@RangeLeft
  AND		@RangeRight

-----------------------------------------------------------------------------------------------------------------------------------------------
-- CLOSE LEFT GAP
-----------------------------------------------------------------------------------------------------------------------------------------------
UPDATE		topics_Topics
SET		RangeRight		= RangeRight - @RangeWidth
WHERE		RangeRight		> @RangeRight

Update		topics_Topics
SET		RangeLeft		= RangeLeft - @RangeWidth
WHERE		RangeLeft		> @RangeRight