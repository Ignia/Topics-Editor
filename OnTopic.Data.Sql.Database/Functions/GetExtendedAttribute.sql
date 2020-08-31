﻿--------------------------------------------------------------------------------------------------------------------------------
-- GET EXTENDED ATTRIBUTE
--------------------------------------------------------------------------------------------------------------------------------
-- Given a TopicID and an AttributeKey, retrieves the value of the attribute from the index.
--------------------------------------------------------------------------------------------------------------------------------

CREATE
FUNCTION	[dbo].[GetExtendedAttribute]
(
	@TopicID		INT,
	@AttributeKey		NVARCHAR(255)
)
RETURNS	VARCHAR(MAX)
AS

BEGIN

  ------------------------------------------------------------------------------------------------------------------------------
  -- DECLARE AND DEFINE VARIABLES
  ------------------------------------------------------------------------------------------------------------------------------
  DECLARE	@AttributeValue		NVARCHAR(MAX)	= NULL

  ------------------------------------------------------------------------------------------------------------------------------
  -- RETRIEVE VALUE
  ------------------------------------------------------------------------------------------------------------------------------
  SELECT	@AttributeValue		= AttributesXml
	  .query('/attributes/attribute[@key=sql:variable("@AttributeKey")]')
	  .value('.', 'NVARCHAR(MAX)')
  FROM	ExtendedAttributeIndex
  WHERE	TopicID		= @TopicID

  ------------------------------------------------------------------------------------------------------------------------------
  -- RETURN VALUE
  ------------------------------------------------------------------------------------------------------------------------------
  RETURN	@AttributeValue

END
