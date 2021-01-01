﻿--------------------------------------------------------------------------------------------------------------------------------
-- UPGRADE FROM ONTOPIC 4.x TO ONTOPIC 5.x
--------------------------------------------------------------------------------------------------------------------------------
-- There are a few data schema differences that cannot be handled as part of the schema comparison. These should be executed
-- prior to running migrations.
--------------------------------------------------------------------------------------------------------------------------------

--------------------------------------------------------------------------------------------------------------------------------
-- DROP COLUMNS
--------------------------------------------------------------------------------------------------------------------------------
-- Migrations won't drop columns that have data in them. The following drop columns that are no longer needed. This also drops
-- stored procedures that reference those columns—with the knowledge that their replacements will be recreated by the
-- migrations.
--------------------------------------------------------------------------------------------------------------------------------

ALTER
TABLE	Topics
DROP
COLUMN	Stack_Top;

ALTER
TABLE	Attributes
DROP
CONSTRAINT	DF_Attributes_DateModified;

ALTER
TABLE	Attributes
DROP
COLUMN	DateModified;

--------------------------------------------------------------------------------------------------------------------------------
-- MIGRATE CORE ATTRIBUTES
--------------------------------------------------------------------------------------------------------------------------------
-- In OnTopic 5, core attributes that don't utilize versioning have been moved from the Attributes table to the Topics table.
-- This includes Key, ContentType, and ParentID. Previously, these required a lot of workaround since they frequently utilized
-- in a way that's inconsistent with other attributes. By moving them to Topic, we better acknowledge their unique status.
--------------------------------------------------------------------------------------------------------------------------------

ALTER TABLE [dbo].[Topics]
    ADD [TopicKey]    VARCHAR (128) NULL,
        [ContentType] VARCHAR (128) NULL,
        [ParentID]    INT           NULL;

WITH KeyAttributes AS (
  SELECT	TopicID,
                AttributeKey,
                AttributeValue,
                RowNumber = ROW_NUMBER() OVER (
                  PARTITION BY		TopicID,
			AttributeKey
                  ORDER BY		Version DESC
                )
  FROM	[dbo].[Attributes]
  WHERE	AttributeKey
  IN (	'Key',
	'ContentType',
	'ParentID'
  )
)
UPDATE	Topics
SET	Topics.TopicKey		= Pvt.[Key],
	Topics.ContentType	= Pvt.ContentType,
	Topics.ParentID		= Pvt.ParentID
FROM	KeyAttributes
PIVOT (	MIN(AttributeValue)
  FOR	AttributeKey IN (
	  [Key],
	  [ContentType],
	  [ParentID]
	)
)	AS Pvt
WHERE	RowNumber		= 1
AND	Topics.TopicID		= Pvt.TopicID

DELETE
FROM	Attributes
WHERE	AttributeKey
IN (	'Key',
	'ContentType',
	'ParentID'
)

--------------------------------------------------------------------------------------------------------------------------------
-- MIGRATE TOPIC REFERENCES
--------------------------------------------------------------------------------------------------------------------------------
-- In OnTopic 5, references to other topics—such as `DerivedTopic`—have been moved from the Attributes table to a new
-- TopicReferences table, where they act more like relationships. This allows referential integrity to be enforced through
-- foreign key constraints, and formalizes the relationship so we don't need to rely on hacks in e.g. the Topic Data Transer
-- service to infer which attributes represent relationships in order to translate their values from `TopicID` to `UniqueKey`.
--------------------------------------------------------------------------------------------------------------------------------

CREATE
TABLE	[dbo].[TopicReferences] (
	  [Source_TopicID]	INT	NOT NULL,
	  [ReferenceKey]	VARCHAR(128)	NOT NULL,
	  [Target_TopicID]	INT	NOT NULL
);

INSERT
INTO	TopicReferences
SELECT	AttributeIndex.TopicID,
	SUBSTRING(AttributeKey, 0, LEN(AttributeKey)-1),
	AttributeValue
FROM	AttributeIndex
JOIN	Topics
  ON	Topics.TopicID		= CONVERT(INT, AttributeValue)
WHERE	AttributeKey		LIKE '%ID'
  AND	ISNUMERIC(AttributeValue)	= 1
  AND	Topics.TopicID		IS NOT NULL

UPDATE	TopicReferences
SET	ReferenceKey		= 'DerivedTopic'
WHERE	ReferenceKey		= 'Topic'