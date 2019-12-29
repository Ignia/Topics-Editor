﻿--------------------------------------------------------------------------------------------------------------------------------
-- GET PARENT ID (FUNCTION)
--------------------------------------------------------------------------------------------------------------------------------
-- Given a @TopicID, returns the TopicID of the node above it in the Topics nested set hierarchy.
--------------------------------------------------------------------------------------------------------------------------------
CREATE
FUNCTION	[dbo].[GetParentID] (
	@TopicID		INT
)
RETURNS	INT
AS
BEGIN
  DECLARE	@CurrentParentID	INT
  SELECT       	@CurrentParentID = (
    SELECT	TOP 1
	TopicID
    FROM	Topics		t2
    WHERE	t2.RangeLeft		< t1.RangeLeft
      AND	ISNULL(t2.RangeRight, 0)	> ISNULL(t1.RangeRight, 0)
    ORDER BY	t2.RangeRight-t1.RangeRight	ASC
  )
  FROM	Topics		t1
  WHERE	TopicID		= @TopicID
  ORDER BY	RangeRight-RangeLeft	DESC
  RETURN	@CurrentParentID
END